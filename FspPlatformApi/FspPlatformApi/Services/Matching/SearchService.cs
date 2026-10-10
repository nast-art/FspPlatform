using System.Text.Json;
using FspPlatformApi.Data.Entities;
using FspPlatformApi.DTOs.Candidates;
using FspPlatformApi.DTOs.Fsp;
using FspPlatformApi.DTOs.Search;
using FspPlatformApi.Services.Infrastructure;
using FspPlatformApi.Services.Interfaces.Matching;
using Microsoft.EntityFrameworkCore;

namespace FspPlatformApi.Services.Matching;

/// <summary>
/// Выдача строится по категориям, присвоенным по итогам опроса и тестирования (не по резюме) — ТЗ.
/// Ранжирование (максимум ровно 100): 0.64 × лучший результат теста + до 27 баллов за достижения ФСП
/// + до 9 за полноту профиля. Каждая позиция сопровождается объяснением (почему кандидат в выдаче).
/// Каждый запрос и результат сохраняются — уточнение фильтров не теряет подборку (ТЗ).
/// Служебные вызовы (начальная загрузка страницы, save=false) в историю не пишутся.
///
/// Q&A менторов: кандидат с НЕподтверждённым грейдом НЕ скрывается из выдачи — он показывается
/// со статусом и с половинным тестовым весом (опущен в ранжировании), иначе выпадает из оборота.
///
/// Для назначения задачи (ExcludeTaskId): кандидаты с активным назначением на задачу
/// исключаются из выдачи; отменённые назначения остаются — их можно назначить повторно.
/// </summary>
public class SearchService : ISearchService
{
    private readonly ApplicationDbContext _db;
    public SearchService(ApplicationDbContext db) => _db = db;

    private async Task<Company> MyCompanyAsync(Guid ownerUserId) =>
        await _db.Companies.FirstAsync(c => c.OwnerUserId == ownerUserId && c.Status == "активен");

    public async Task<SearchResponseDto> SearchAsync(Guid ownerUserId, SearchCandidatesDto dto)
    {
        var company = await MyCompanyAsync(ownerUserId);

        // Сохраняем запрос в историю только при явном поиске.
        var save = dto.Save != false;
        EmployerSearchRequest? request = null;
        if (save)
        {
            request = new EmployerSearchRequest
            {
                CompanyId = company.Id,
                Title = dto.Query ?? "Подборка",
                Filters = JsonSerializer.Serialize(dto),
                CreatedBy = ownerUserId
            };
            _db.EmployerSearchRequests.Add(request);
            await _db.SaveChangesAsync();
        }

        // Категория ищется через EXISTS, чтобы не размножать строки кандидата
        // на каждую его категорию. Для неподтверждённого грейда cat остаётся null.
        var query =
            from cs in _db.CandidateSpecializations
                .AsNoTracking()
                .Include(x => x.Specialization)
                .Include(x => x.CurrentGrade)
            join p in _db.CandidateProfiles.AsNoTracking() on cs.CandidateId equals p.UserId
            join u in _db.Users.AsNoTracking() on cs.CandidateId equals u.Id
            from cat in _db.Categories.AsNoTracking()
                .Where(x => x.SpecializationId == cs.SpecializationId
                    && x.GradeId == cs.CurrentGradeId
                    && _db.CandidateCategories.Any(cc =>
                        cc.CandidateId == cs.CandidateId && cc.CategoryId == x.Id))
                .DefaultIfEmpty()
            where p.IsProfilePublic
            select new { cs, cat, p, u };

        if (dto.CategoryId is not null)
        {
            var filterCat = await _db.Categories.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == (short)dto.CategoryId.Value);
            if (filterCat is null)
                return EmptySearchResponse(request, dto);

            query = query.Where(x => x.cs.SpecializationId == filterCat.SpecializationId
                                  && x.cs.CurrentGradeId == filterCat.GradeId);
        }

        if (dto.SpecializationId is not null)
            query = query.Where(x => x.cs.SpecializationId == (short)dto.SpecializationId.Value);
        if (dto.GradeId is not null)
            query = query.Where(x => x.cs.CurrentGradeId == dto.GradeId.Value);
        if (!string.IsNullOrWhiteSpace(dto.City))
            query = query.Where(x => x.p.City == dto.City);
        if (dto.HasFsp == true)
            query = query.Where(x => x.u.FspId != null);
        if (!string.IsNullOrWhiteSpace(dto.Query))
            query = query.Where(x => EF.Functions.ILike(
                x.p.LastName + " " + x.p.FirstName + " " + (x.p.About ?? ""), $"%{dto.Query}%"));

        // Фильтры по навыкам и активным назначениям выполняются в SQL, а не после
        // загрузки всех профилей в память.
        if (dto.SkillIds is { Count: > 0 })
            query = query.Where(x => _db.CandidateSkills.Any(s =>
                s.CandidateId == x.cs.CandidateId && dto.SkillIds.Contains(s.SkillId)));

        if (dto.ExcludeTaskId is not null)
            query = query.Where(x => !_db.CandidateTasks.Any(t =>
                t.TaskId == dto.ExcludeTaskId.Value && t.CandidateId == x.cs.CandidateId
                && t.Status != "отменено"));

        var candidates = (await query.ToListAsync())
            .GroupBy(c => c.cs.CandidateId)
            .Select(g => g.OrderByDescending(x => x.cs.IsPrimary).First())
            .ToList();

        var candidateIds = candidates.Select(c => c.cs.CandidateId).Distinct().ToList();
        var bestPassedByCandidate = new Dictionary<Guid, decimal>();
        var bestAttemptByCandidate = new Dictionary<Guid, decimal?>();
        var fspWeightById = new Dictionary<Guid, decimal>();
        var skillMatchByCandidate = new Dictionary<Guid, int>();

        if (candidateIds.Count > 0)
        {
            // Две групповые выборки вместо двух SQL-запросов на каждого кандидата.
            var passedTestRows = await _db.TestSessions.AsNoTracking()
                .Where(t => candidateIds.Contains(t.CandidateId)
                    && t.Status == "завершен" && t.Passed == true)
                .GroupBy(t => t.CandidateId)
                .Select(g => new { CandidateId = g.Key, Percent = g.Max(t => (decimal?)t.Percent) })
                .ToListAsync();
            bestPassedByCandidate = passedTestRows.ToDictionary(x => x.CandidateId, x => x.Percent ?? 0m);

            var attemptRows = await _db.TestSessions.AsNoTracking()
                .Where(t => candidateIds.Contains(t.CandidateId) && t.Status == "завершен")
                .GroupBy(t => t.CandidateId)
                .Select(g => new { CandidateId = g.Key, Percent = g.Max(t => (decimal?)t.Percent) })
                .ToListAsync();
            bestAttemptByCandidate = attemptRows.ToDictionary(x => x.CandidateId, x => x.Percent);

            var fspIds = candidates.Where(c => c.u.FspId.HasValue)
                .Select(c => c.u.FspId!.Value).Distinct().ToList();
            if (fspIds.Count > 0)
            {
                var weightRows = await _db.FspAchievements.AsNoTracking()
                    .Where(a => fspIds.Contains(a.FspId))
                    .GroupBy(a => a.FspId)
                    .Select(g => new
                    {
                        FspId = g.Key,
                        Weight = g.Sum(a => a.Weight * (a.Verified ? 1.5m : 1m))
                    })
                    .ToListAsync();
                fspWeightById = weightRows.ToDictionary(x => x.FspId, x => x.Weight);
            }

            if (dto.SkillIds is { Count: > 0 })
            {
                var skillMatchRows = await _db.CandidateSkills.AsNoTracking()
                    .Where(s => candidateIds.Contains(s.CandidateId) && dto.SkillIds.Contains(s.SkillId))
                    .GroupBy(s => s.CandidateId)
                    .Select(g => new { CandidateId = g.Key, Count = g.Count() })
                    .ToListAsync();
                skillMatchByCandidate = skillMatchRows.ToDictionary(x => x.CandidateId, x => x.Count);
            }
        }

        // Сначала считаем только поля, нужные для ранжирования. Подробные карточки
        // подгружаются пакетно только для текущей страницы, а не для всего банка.
        var rankedCandidates = candidates.Select(c =>
        {
            var candidateId = c.cs.CandidateId;
            var bestTest = bestPassedByCandidate.TryGetValue(candidateId, out var passedPercent)
                ? passedPercent : 0m;
            decimal? bestAttempt = bestAttemptByCandidate.TryGetValue(candidateId, out var attemptPercent)
                ? attemptPercent : null;
            var fspWeight = c.u.FspId is Guid fspId && fspWeightById.TryGetValue(fspId, out var weight)
                ? weight : 0m;
            var confirmed = c.cs.Status == "грейд_подтвержден";
            var rank = Math.Round(
                (confirmed ? bestTest * 0.64m : bestTest * 0.64m * 0.5m) +
                Math.Min(fspWeight, 27m) + Math.Min(c.p.ProfileCompleteness * 0.09m, 9m), 3);
            var categoryName = c.cat?.DisplayName
                ?? $"{c.cs.Specialization?.Name} · {c.cs.CurrentGrade?.Name}";
            int? skillMatch = dto.SkillIds is { Count: > 0 }
                ? (skillMatchByCandidate.TryGetValue(candidateId, out var matched) ? matched : 0)
                : null;

            return new
            {
                Candidate = c,
                BestTest = bestTest,
                BestAttempt = bestAttempt,
                FspWeight = fspWeight,
                Confirmed = confirmed,
                Rank = rank,
                CategoryName = categoryName,
                SkillMatch = skillMatch
            };
        })
        .Where(x => dto.MinScore is null || x.Rank >= dto.MinScore.Value)
        .OrderByDescending(x => x.Rank)
        .ToList();

        var total = rankedCandidates.Count;
        var pageNumber = Math.Max(1, dto.Page);
        var pageSize = Math.Clamp(dto.PageSize, 1, 100);
        var pageCandidates = rankedCandidates.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        var pageCandidateIds = pageCandidates.Select(x => x.Candidate.cs.CandidateId).Distinct().ToList();
        var skillNamesByCandidate = new Dictionary<Guid, List<string>>();
        var achievementsByFspId = new Dictionary<Guid, List<FspAchievementDto>>();
        var contactsVisibleIds = new HashSet<Guid>();

        if (pageCandidateIds.Count > 0)
        {
            // Навыки страницы — один запрос на все карточки.
            var skillRows = await _db.CandidateSkills.AsNoTracking()
                .Where(s => pageCandidateIds.Contains(s.CandidateId))
                .Join(_db.Skills.AsNoTracking(), s => s.SkillId, k => k.Id,
                    (s, k) => new { s.CandidateId, Name = k.Name })
                .ToListAsync();
            skillNamesByCandidate = skillRows.GroupBy(x => x.CandidateId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Name).Distinct().ToList());

            var pageFspIds = pageCandidates.Where(x => x.Candidate.u.FspId.HasValue)
                .Select(x => x.Candidate.u.FspId!.Value).Distinct().ToList();
            if (pageFspIds.Count > 0)
            {
                var achievementRows = await _db.FspAchievements.AsNoTracking()
                    .Where(a => pageFspIds.Contains(a.FspId))
                    .Select(a => new
                    {
                        a.FspId,
                        Achievement = new FspAchievementDto
                        {
                            Id = a.Id, EventType = a.EventTypeNavigation.Name, EventName = a.EventName,
                            EventDate = a.EventDate, Role = a.Role, ResultPlace = a.ResultPlace,
                            ResultScore = a.ResultScore, Direction = a.Direction,
                            Weight = a.Weight, Verified = a.Verified
                        }
                    })
                    .ToListAsync();
                achievementsByFspId = achievementRows.GroupBy(x => x.FspId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.Achievement).ToList());
            }

            var acceptedInvitationIds = await _db.Invitations.AsNoTracking()
                .Where(i => pageCandidateIds.Contains(i.CandidateId)
                    && i.CompanyId == company.Id && i.Status == "принято")
                .Select(i => i.CandidateId).Distinct().ToListAsync();
            var applicationIds = await _db.Applications.AsNoTracking()
                .Where(a => pageCandidateIds.Contains(a.CandidateId)
                    && a.Status != "отозван" && a.Vacancy.CompanyId == company.Id)
                .Select(a => a.CandidateId).Distinct().ToListAsync();
            contactsVisibleIds.UnionWith(acceptedInvitationIds);
            contactsVisibleIds.UnionWith(applicationIds);
        }

        var items = pageCandidates.Select((entry, pageIndex) =>
        {
            var c = entry.Candidate;
            var candidateId = c.cs.CandidateId;
            var achievements = c.u.FspId is Guid fspId && achievementsByFspId.TryGetValue(fspId, out var foundAchievements)
                ? foundAchievements : new List<FspAchievementDto>();
            var skillNames = skillNamesByCandidate.TryGetValue(candidateId, out var foundSkills)
                ? foundSkills : new List<string>();
            var position = (pageNumber - 1) * pageSize + pageIndex + 1;
            var explanation = new Dictionary<string, object>
            {
                ["категория"] = entry.CategoryName,
                ["результат_теста"] = entry.BestAttempt ?? entry.BestTest,
                ["вес_достижений_фсп"] = entry.FspWeight,
                ["заполненность_профиля"] = c.p.ProfileCompleteness,
                ["совпадение_навыков"] = entry.SkillMatch
            };
            if (c.u.FspId is null) explanation["фсп"] = "истории ФСП нет";
            if (!entry.Confirmed) explanation["грейд"] = "не подтверждён тестом — опущен в выдаче";

            return new SearchCandidateItemDto
            {
                Candidate = new CandidatePublicCardDto
                {
                    CandidateId = candidateId,
                    LastName = c.p.LastName, FirstName = c.p.FirstName, MiddleName = c.p.MiddleName,
                    City = c.p.City, About = c.p.About, ExperienceYears = c.p.ExperienceYears,
                    SoftSkills = c.p.SoftSkills?.ToArray(),
                    FspId = c.u.FspId, HasFspHistory = c.u.FspId != null,
                    SpecializationName = c.cs.Specialization?.Name,
                    GradeName = c.cs.CurrentGrade?.Name, CategoryName = entry.CategoryName,
                    Confirmed = entry.Confirmed,
                    BestTestPercent = entry.BestTest, BestAttemptPercent = entry.BestAttempt,
                    RankScore = entry.Rank, PositionInCategory = 0,
                    ContactsVisible = contactsVisibleIds.Contains(candidateId),
                    Skills = skillNames, FspAchievements = achievements
                },
                RankScore = entry.Rank, Explanation = explanation, Position = position
            };
        }).ToList();

        // Сохраняем только верхние 100 результатов, как и раньше. Для снапшота
        // достаточно записать рейтинг/позицию/объяснение — полные DTO не нужны.
        if (save)
        {
            foreach (var entry in rankedCandidates.Take(100).Select((value, index) => new { value, index }))
            {
                var c = entry.value.Candidate;
                var explanation = new Dictionary<string, object>
                {
                    ["категория"] = entry.value.CategoryName,
                    ["результат_теста"] = entry.value.BestAttempt ?? entry.value.BestTest,
                    ["вес_достижений_фсп"] = entry.value.FspWeight,
                    ["заполненность_профиля"] = c.p.ProfileCompleteness,
                    ["совпадение_навыков"] = entry.value.SkillMatch
                };
                if (c.u.FspId is null) explanation["фсп"] = "истории ФСП нет";
                if (!entry.value.Confirmed) explanation["грейд"] = "не подтверждён тестом — опущен в выдаче";

                _db.EmployerSearchResults.Add(new EmployerSearchResult
                {
                    RequestId = request!.Id,
                    CandidateId = c.cs.CandidateId,
                    RankScore = entry.value.Rank,
                    Position = (short)(entry.index + 1),
                    Explanation = JsonSerializer.Serialize(explanation)
                });
            }
            await _db.SaveChangesAsync();
        }

        return new SearchResponseDto
        {
            RequestId = request?.Id ?? Guid.Empty,
            Total = total,
            Items = items,
            Filters = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(dto))
                ?? new Dictionary<string, object>(),
            CreatedAt = request?.CreatedAt
        };
    }

    private static SearchResponseDto EmptySearchResponse(EmployerSearchRequest? request, SearchCandidatesDto dto) => new()
    {
        RequestId = request?.Id ?? Guid.Empty,
        Total = 0,
        Items = new List<SearchCandidateItemDto>(),
        Filters = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(dto))
            ?? new Dictionary<string, object>(),
        CreatedAt = request?.CreatedAt
    };

    public async Task<List<SearchResponseDto>> GetHistoryAsync(Guid ownerUserId, int page, int pageSize)
    {
        var company = await MyCompanyAsync(ownerUserId);
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 50);
        var requests = await _db.EmployerSearchRequests.AsNoTracking()
            .Where(r => r.CompanyId == company.Id)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync();

        if (requests.Count == 0)
            return new List<SearchResponseDto>();

        // История показывает только дату и число сохранённых результатов. Раньше
        // для каждой записи вызывался GetSavedResultAsync(), который собирал все
        // карточки и выполнял много SQL-запросов на каждого кандидата. Теперь это
        // один GROUP BY; подробная подборка загружается только по клику пользователя.
        var requestIds = requests.Select(r => r.Id).ToList();
        var counts = await _db.EmployerSearchResults.AsNoTracking()
            .Where(r => requestIds.Contains(r.RequestId))
            .GroupBy(r => r.RequestId)
            .Select(g => new { RequestId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RequestId, x => x.Count);

        return requests.Select(r => new SearchResponseDto
        {
            RequestId = r.Id,
            Total = counts.TryGetValue(r.Id, out var count) ? count : 0,
            Items = new List<SearchCandidateItemDto>(),
            Filters = string.IsNullOrWhiteSpace(r.Filters)
                ? new Dictionary<string, object>()
                : JsonSerializer.Deserialize<Dictionary<string, object>>(r.Filters)
                    ?? new Dictionary<string, object>(),
            CreatedAt = r.CreatedAt
        }).ToList();
    }

    /// <summary>
    /// Возвращает сохранённую подборку (снапшот из employer_search_results).
    /// Раньше карточки собирались из v_candidate_public_cards с явными null —
    /// «Мои запросы» показывали сплошные «—» (категория, тест). Теперь карточка
    /// дособирается из живых данных; из снапшота берём только то, что должно быть
    /// зафиксировано: ранг, позицию и объяснение. Плюс отдаём фильтры запроса —
    /// фронт восстанавливает ими панель фильтров при открытии подборки из истории.
    /// </summary>
    public async Task<SearchResponseDto?> GetSavedResultAsync(Guid ownerUserId, Guid requestId)
    {
        var company = await MyCompanyAsync(ownerUserId);
        var request = await _db.EmployerSearchRequests.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == requestId && r.CompanyId == company.Id);
        if (request is null)
            return null;

        var rows = await _db.EmployerSearchResults.AsNoTracking()
            .Where(r => r.RequestId == requestId)
            .OrderBy(r => r.Position)
            .ToListAsync();

        var filters = !string.IsNullOrWhiteSpace(request.Filters)
            ? JsonSerializer.Deserialize<Dictionary<string, object>>(request.Filters)
            : null;

        // Пустые подборки тоже можно повторно открыть из истории.
        if (rows.Count == 0)
            return new SearchResponseDto
            {
                RequestId = requestId,
                Total = 0,
                Items = new List<SearchCandidateItemDto>(),
                Filters = filters ?? new Dictionary<string, object>(),
                CreatedAt = request.CreatedAt
            };

        var candidateIds = rows.Select(r => r.CandidateId).Distinct().ToList();
        var cards = await _db.VCandidatePublicCards.AsNoTracking()
            .Where(c => c.CandidateId != null && candidateIds.Contains(c.CandidateId.Value))
            .ToListAsync();
        var cardsByCandidate = cards.Where(c => c.CandidateId.HasValue)
            .GroupBy(c => c.CandidateId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        // Текущие специализации загружаются одной выборкой для всех кандидатов.
        var specializationRows = await _db.CandidateSpecializations.AsNoTracking()
            .Include(x => x.Specialization)
            .Include(x => x.CurrentGrade)
            .Where(x => candidateIds.Contains(x.CandidateId))
            .OrderByDescending(x => x.IsPrimary)
            .ToListAsync();
        var specializationsByCandidate = specializationRows
            .GroupBy(x => x.CandidateId)
            .ToDictionary(g => g.Key, g => g.First());

        var passedRows = await _db.TestSessions.AsNoTracking()
            .Where(t => candidateIds.Contains(t.CandidateId)
                && t.Status == "завершен" && t.Passed == true)
            .GroupBy(t => t.CandidateId)
            .Select(g => new { CandidateId = g.Key, Percent = g.Max(t => (decimal?)t.Percent) })
            .ToListAsync();
        var bestPassedByCandidate = passedRows.ToDictionary(x => x.CandidateId, x => x.Percent);

        var attemptRows = await _db.TestSessions.AsNoTracking()
            .Where(t => candidateIds.Contains(t.CandidateId) && t.Status == "завершен")
            .GroupBy(t => t.CandidateId)
            .Select(g => new { CandidateId = g.Key, Percent = g.Max(t => (decimal?)t.Percent) })
            .ToListAsync();
        var bestAttemptByCandidate = attemptRows.ToDictionary(x => x.CandidateId, x => x.Percent);

        var skillRows = await _db.CandidateSkills.AsNoTracking()
            .Where(s => candidateIds.Contains(s.CandidateId))
            .Join(_db.Skills.AsNoTracking(), s => s.SkillId, k => k.Id,
                (s, k) => new { s.CandidateId, Name = k.Name })
            .ToListAsync();
        var skillsByCandidate = skillRows.GroupBy(x => x.CandidateId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).Distinct().ToList());

        var fspIds = cards.Where(c => c.CandidateId.HasValue && c.FspId.HasValue)
            .Select(c => c.FspId!.Value).Distinct().ToList();
        var achievementsByFspId = new Dictionary<Guid, List<FspAchievementDto>>();
        if (fspIds.Count > 0)
        {
            var achievementRows = await _db.FspAchievements.AsNoTracking()
                .Where(a => fspIds.Contains(a.FspId))
                .Select(a => new
                {
                    a.FspId,
                    Achievement = new FspAchievementDto
                    {
                        Id = a.Id, EventType = a.EventTypeNavigation.Name, EventName = a.EventName,
                        EventDate = a.EventDate, Role = a.Role, ResultPlace = a.ResultPlace,
                        ResultScore = a.ResultScore, Direction = a.Direction,
                        Weight = a.Weight, Verified = a.Verified
                    }
                })
                .ToListAsync();
            achievementsByFspId = achievementRows.GroupBy(x => x.FspId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Achievement).ToList());
        }

        var acceptedInvitationIds = await _db.Invitations.AsNoTracking()
            .Where(i => candidateIds.Contains(i.CandidateId)
                && i.CompanyId == company.Id && i.Status == "принято")
            .Select(i => i.CandidateId).Distinct().ToListAsync();
        var applicationIds = await _db.Applications.AsNoTracking()
            .Where(a => candidateIds.Contains(a.CandidateId)
                && a.Status != "отозван" && a.Vacancy.CompanyId == company.Id)
            .Select(a => a.CandidateId).Distinct().ToListAsync();
        var contactsVisibleIds = acceptedInvitationIds.ToHashSet();
        contactsVisibleIds.UnionWith(applicationIds);

        var items = new List<SearchCandidateItemDto>(rows.Count);
        foreach (var row in rows)
        {
            if (!cardsByCandidate.TryGetValue(row.CandidateId, out var card))
                continue;

            specializationsByCandidate.TryGetValue(row.CandidateId, out var cs);
            bestPassedByCandidate.TryGetValue(row.CandidateId, out var bestTest);
            bestAttemptByCandidate.TryGetValue(row.CandidateId, out var bestAttempt);
            skillsByCandidate.TryGetValue(row.CandidateId, out var skills);
            skills ??= new List<string>();

            var confirmed = cs?.Status == "грейд_подтвержден";
            var explanation = Jsonb.Element(row.Explanation) is { } el && el.ValueKind == JsonValueKind.Object
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(el.GetRawText()) ?? new Dictionary<string, object>()
                : new Dictionary<string, object>();

            string? categoryName = explanation.TryGetValue("категория", out var catVal)
                ? catVal?.ToString() : null;
            if (string.IsNullOrWhiteSpace(categoryName) && cs is not null)
                categoryName = $"{cs.Specialization?.Name} · {cs.CurrentGrade?.Name}";

            var achievements = card.FspId is Guid fspId && achievementsByFspId.TryGetValue(fspId, out var foundAchievements)
                ? foundAchievements : new List<FspAchievementDto>();

            items.Add(new SearchCandidateItemDto
            {
                Candidate = new CandidatePublicCardDto
                {
                    CandidateId = card.CandidateId,
                    LastName = card.LastName ?? "", FirstName = card.FirstName ?? "", MiddleName = card.MiddleName,
                    City = card.City, About = card.About,
                    ExperienceYears = card.ExperienceYears ?? 0m,
                    SoftSkills = card.SoftSkills?.ToArray(),
                    FspId = card.FspId,
                    HasFspHistory = card.HasFspHistory == true,
                    SpecializationName = cs?.Specialization?.Name,
                    GradeName = cs?.CurrentGrade?.Name,
                    CategoryName = categoryName,
                    Confirmed = confirmed,
                    BestTestPercent = bestTest,
                    BestAttemptPercent = bestAttempt,
                    RankScore = row.RankScore,
                    PositionInCategory = row.Position,
                    ContactsVisible = contactsVisibleIds.Contains(row.CandidateId),
                    Skills = skills,
                    FspAchievements = achievements
                },
                RankScore = row.RankScore,
                Explanation = explanation,
                Position = row.Position
            });
        }

        return new SearchResponseDto
        {
            RequestId = requestId,
            Total = items.Count,
            Items = items,
            Filters = filters ?? new Dictionary<string, object>(),
            CreatedAt = request.CreatedAt
        };
    }

}