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

        // Сохраняем запрос в историю, только если это явный поиск (Save != false).
        // Начальная загрузка страницы и служебные вызовы историю не засоряют.
        var save = dto.Save != false;
        EmployerSearchRequest? request = null;
        if (save)
        {
            request = new EmployerSearchRequest
            {
                CompanyId = company.Id, Title = dto.Query ?? "Подборка",
                Filters = JsonSerializer.Serialize(dto), // jsonb-поле скаффолдлено как string
                CreatedBy = ownerUserId
            };
            _db.EmployerSearchRequests.Add(request);
            await _db.SaveChangesAsync();
        }

        // Основа — специализации кандидата, категория подтягивается LEFT JOIN'ом:
        // у неподтверждённых грейдом записи в candidate_categories ещё нет, но по
        // Q&A менторов такие кандидаты остаются в выдаче — со статусом и половинным
        // тестовым весом (штраф в ранжировании ниже по коду).
        // Include: без навигаций Specialization/CurrentGrade маппинг DTO упадёт с NRE,
        // а для синтеза названия категории («специализация · грейд») они обязательны.
        var query = from cs in _db.CandidateSpecializations
                        .Include(x => x.Specialization)
                        .Include(x => x.CurrentGrade)
                    join p in _db.CandidateProfiles on cs.CandidateId equals p.UserId
                    join u in _db.Users on cs.CandidateId equals u.Id
                    from cc in _db.CandidateCategories
                        .Where(x => x.CandidateId == cs.CandidateId)
                        .DefaultIfEmpty()
                    from cat in _db.Categories
                        .Where(x => cc != null && x.Id == cc.CategoryId &&
                                    x.SpecializationId == cs.SpecializationId &&
                                    x.GradeId == cs.CurrentGradeId)
                        .DefaultIfEmpty()
                    where p.IsProfilePublic
                    select new { cc, cs, cat, p, u };

        // Фильтр по категории разворачиваем в пару «специализация + грейд»: тогда
        // в выбранную категорию попадут и неподтверждённые кандидаты этой связки
        // (сравнивать CategoryId напрямую нельзя — строки cc у них нет).
        if (dto.CategoryId is not null)
        {
            // PK Category — short: без приведения FindAsync падает с ArgumentException
            var filterCat = await _db.Categories.FindAsync((short)dto.CategoryId.Value);
            if (filterCat is null)
                return new SearchResponseDto
                {
                    RequestId = request?.Id ?? Guid.Empty, Total = 0,
                    Items = new List<SearchCandidateItemDto>()
                };
            query = query.Where(x => x.cs.SpecializationId == filterCat.SpecializationId
                                  && x.cs.CurrentGradeId == filterCat.GradeId);
        }
        if (dto.SpecializationId is not null) query = query.Where(x => x.cs.SpecializationId == (short)dto.SpecializationId);
        if (dto.GradeId is not null) query = query.Where(x => x.cs.CurrentGradeId == dto.GradeId);
        if (!string.IsNullOrWhiteSpace(dto.City)) query = query.Where(x => x.p.City == dto.City);
        if (dto.HasFsp == true) query = query.Where(x => x.u.FspId != null);
        if (!string.IsNullOrWhiteSpace(dto.Query))
            query = query.Where(x => EF.Functions.ILike(
                x.p.LastName + " " + x.p.FirstName + " " + (x.p.About ?? ""), $"%{dto.Query}%"));

        var candidates = await query.ToListAsync();

        if (dto.SkillIds?.Count > 0)
        {
            var withSkills = await _db.CandidateSkills
                .Where(s => dto.SkillIds.Contains(s.SkillId))
                .Select(s => s.CandidateId).Distinct().ToListAsync();
            candidates = candidates.Where(c => withSkills.Contains(c.cs.CandidateId)).ToList();
        }

        // Исключаем кандидатов, которым эта задача уже АКТИВНО назначена:
        // в picker'е назначения они не нужны (уже в работе), а отменённые —
        // остаются в выдаче, чтобы их можно было назначить повторно.
        if (dto.ExcludeTaskId is not null)
        {
            var activeAssignedIds = await _db.CandidateTasks
                .AsNoTracking()
                .Where(x => x.TaskId == dto.ExcludeTaskId.Value && x.Status != "отменено")
                .Select(x => x.CandidateId)
                .ToListAsync();
            if (activeAssignedIds.Count > 0)
                candidates = candidates.Where(c => !activeAssignedIds.Contains(c.cs.CandidateId)).ToList();
        }

        // ДЕДУПЛИКАЦИЯ: join по специализациям собирает одного кандидата несколько раз
        // (по строке на каждую его специализацию/категорию). В выдаче и в
        // employer_search_results кандидат должен быть один — иначе конфликт
        // уникального ключа (RequestId, CandidateId) при сохранении результатов.
        // Берём строку с основной специализацией (IsPrimary), чтобы категория
        // в выдаче была главной, а не случайной.
        candidates = candidates
            .GroupBy(c => c.cs.CandidateId)
            .Select(g => g.OrderByDescending(x => x.cs.IsPrimary).First())
            .ToList();

        var items = new List<SearchCandidateItemDto>();
        foreach (var c in candidates)
        {
            var bestTest = await _db.TestSessions
                .Where(t => t.CandidateId == c.cs.CandidateId && t.Status == "завершен" && t.Passed == true)
                .MaxAsync(t => (decimal?)t.Percent) ?? 0m;

            // Для показа: лучшая попытка среди ВСЕХ завершённых тестов (включая
            // проваленные). Иначе у завалившего тест в колонке «Тест» неверный 0%.
            var bestAttempt = await _db.TestSessions
                .Where(t => t.CandidateId == c.cs.CandidateId && t.Status == "завершен")
                .MaxAsync(t => (decimal?)t.Percent);

            var fspWeight = c.u.FspId is null ? 0m :
                await _db.FspAchievements.Where(a => a.FspId == c.u.FspId)
                    .SumAsync(a => a.Weight * (a.Verified ? 1.5m : 1m));

            int? skillMatch = dto.SkillIds is { Count: > 0 }
                ? await _db.CandidateSkills.CountAsync(s =>
                    s.CandidateId == c.cs.CandidateId && dto.SkillIds.Contains(s.SkillId))
                : null;

            // Q&A менторов: неподтверждённый грейд не скрываем, но тестовый вес
            // у него вдвое ниже — такой кандидат занимает позиции НИЖЕ подтверждённых
            // со сравнимым результатом, при этом остаётся видимым (не выпадает из оборота).
            var confirmed = c.cs.Status == "грейд_подтвержден";

            // РЕЙТИНГ (максимум ровно 100, как в документации):
            //   0.64 × лучший процент пройденного теста
            // + до 27 баллов за достижения ФСП
            // + до 9 баллов за полноту профиля (ProfileCompleteness 0–100 → × 0.09).
            // У неподтверждённого грейда тестовый компонент равен нулю (bestTest —
            // это только ПРОЙДЕННЫЕ тесты), а половинный множитель — дополнительный
            // штраф на случай появления подтверждённой попытки в истории.
            var rank = Math.Round(
                (confirmed ? bestTest * 0.64m : bestTest * 0.64m * 0.5m) +
                Math.Min(fspWeight, 27m) +
                Math.Min(c.p.ProfileCompleteness * 0.09m, 9m), 3);

            if (dto.MinScore is not null && rank < dto.MinScore) continue;

            var fspAchievements = c.u.FspId is null ? new List<FspAchievementDto>() :
                await _db.FspAchievements.Where(a => a.FspId == c.u.FspId)
                    .Select(a => new FspAchievementDto
                    {
                        Id = a.Id, EventType = a.EventTypeNavigation.Name, EventName = a.EventName,
                        EventDate = a.EventDate, Role = a.Role, ResultPlace = a.ResultPlace,
                        ResultScore = a.ResultScore, Direction = a.Direction,
                        Weight = a.Weight, Verified = a.Verified
                    }).ToListAsync();

            var skillNames = await _db.CandidateSkills.Where(s => s.CandidateId == c.cs.CandidateId)
                .Join(_db.Skills, s => s.SkillId, k => k.Id, (s, k) => k.Name).ToListAsync();

            // ТЗ 2.2 п.2: контакты раскрываются после принятия приглашения ИЛИ
            // самого факта отклика. Отклик в любом статусе, кроме «отозван»,
            // открывает контакты — синхронно с ApplicationService, который отдаёт
            // их в таблице откликов сразу после отклика.
            var contactsVisible = await _db.Invitations.AnyAsync(i =>
                i.CandidateId == c.cs.CandidateId && i.CompanyId == company.Id && i.Status == "принято")
                || await _db.Applications.AnyAsync(a =>
                    a.CandidateId == c.cs.CandidateId && a.Status != "отозван" &&
                    a.Vacancy.CompanyId == company.Id);

            // У неподтверждённого грейда строки категории нет — синтезируем название
            // из текущей специализации и грейда кандидата (та же связка, что и в категории).
            var categoryName = c.cat?.DisplayName
                ?? $"{c.cs.Specialization?.Name} · {c.cs.CurrentGrade?.Name}";

            var explanation = new Dictionary<string, object>
            {
                ["категория"] = categoryName,
                ["результат_теста"] = bestAttempt ?? bestTest,
                ["вес_достижений_фсп"] = fspWeight,
                ["заполненность_профиля"] = c.p.ProfileCompleteness,
                ["совпадение_навыков"] = skillMatch
            };
            if (c.u.FspId is null) explanation["фсп"] = "истории ФСП нет";
            if (!confirmed) explanation["грейд"] = "не подтверждён тестом — опущен в выдаче";

            items.Add(new SearchCandidateItemDto
            {
                Candidate = new CandidatePublicCardDto
                {
                    CandidateId = c.cs.CandidateId,
                    LastName = c.p.LastName, FirstName = c.p.FirstName, MiddleName = c.p.MiddleName,
                    City = c.p.City, About = c.p.About,
                    ExperienceYears = c.p.ExperienceYears,
                    SoftSkills = c.p.SoftSkills?.ToArray(),   // List<string> → string[]
                    FspId = c.u.FspId, HasFspHistory = c.u.FspId != null,
                    // защита ?. — навигация может быть null, не должно ронять поиск
                    SpecializationName = c.cs.Specialization?.Name,
                    GradeName = c.cs.CurrentGrade?.Name, CategoryName = categoryName,
                    Confirmed = confirmed,
                    BestTestPercent = bestTest, BestAttemptPercent = bestAttempt,
                    RankScore = rank, PositionInCategory = 0,
                    ContactsVisible = contactsVisible,
                    Skills = skillNames, FspAchievements = fspAchievements
                },
                RankScore = rank, Explanation = explanation, Position = 0
            });
        }

        var sorted = items.OrderByDescending(i => i.RankScore)
            .Select((item, idx) => item with { Position = idx + 1 }).ToList();

        // Результаты пишем в снапшот только для сохраняемых запросов
        if (save)
            foreach (var item in sorted.Take(100))
                _db.EmployerSearchResults.Add(new EmployerSearchResult
                {
                    RequestId = request!.Id, CandidateId = item.Candidate.CandidateId ?? Guid.Empty,
                    RankScore = item.RankScore, Position = (short)item.Position,
                    Explanation = JsonSerializer.Serialize(item.Explanation) // jsonb-поле как string
                });
        await _db.SaveChangesAsync();

        return new SearchResponseDto
        {
            RequestId = request?.Id ?? Guid.Empty, Total = sorted.Count,
            Items = sorted.Skip((dto.Page - 1) * dto.PageSize).Take(dto.PageSize).ToList(),
            // Фильтры подборки — чтобы фронт мог показать, чем она собрана.
            // Для несохранённых вызовов сериализуем DTO напрямую (request может быть null).
            Filters = JsonSerializer.Deserialize<Dictionary<string, object>>(
                          JsonSerializer.Serialize(dto))
                      ?? new Dictionary<string, object>(),
            CreatedAt = request?.CreatedAt
        };
    }

    public async Task<List<SearchResponseDto>> GetHistoryAsync(Guid ownerUserId, int page, int pageSize)
    {
        var company = await MyCompanyAsync(ownerUserId);
        var requests = await _db.EmployerSearchRequests.Where(r => r.CompanyId == company.Id)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var result = new List<SearchResponseDto>();
        foreach (var r in requests)
            if (await GetSavedResultAsync(ownerUserId, r.Id) is { } saved) result.Add(saved);
        return result;
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
        var rows = await _db.EmployerSearchResults
            .Where(r => r.RequestId == requestId && r.Request.CompanyId == company.Id)
            .OrderBy(r => r.Position).ToListAsync();
        if (rows.Count == 0) return null;

        // Фильтры, которыми собрана подборка (снапшот из employer_search_requests)
        var request = await _db.EmployerSearchRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.CompanyId == company.Id);
        var filters = request is not null && !string.IsNullOrWhiteSpace(request.Filters)
            ? JsonSerializer.Deserialize<Dictionary<string, object>>(request.Filters)
            : null;

        var items = new List<SearchCandidateItemDto>();
        foreach (var r in rows)
        {
            var card = await _db.VCandidatePublicCards
                .FirstOrDefaultAsync(c => c.CandidateId == r.CandidateId);
            if (card is null) continue;

            // Живые данные: основная специализация кандидата (грейд, статус).
            // Профиль мог измениться после сохранения подборки — для показа это ок:
            // фиксируются только ранг/позиция/объяснение.
            var cs = await _db.CandidateSpecializations
                .Include(x => x.Specialization)
                .Include(x => x.CurrentGrade)
                .Where(x => x.CandidateId == r.CandidateId)
                .OrderByDescending(x => x.IsPrimary)
                .FirstOrDefaultAsync();

            // Лучшие попытки: подтверждённая (для истории) и любая (честный показ)
            var bestTest = await _db.TestSessions
                .Where(t => t.CandidateId == r.CandidateId && t.Status == "завершен" && t.Passed == true)
                .MaxAsync(t => (decimal?)t.Percent);
            var bestAttempt = await _db.TestSessions
                .Where(t => t.CandidateId == r.CandidateId && t.Status == "завершен")
                .MaxAsync(t => (decimal?)t.Percent);

            var confirmed = cs?.Status == "грейд_подтвержден";

            var explanation = Jsonb.Element(r.Explanation) is { } el && el.ValueKind == JsonValueKind.Object
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(el.GetRawText()) ?? new Dictionary<string, object>()
                : new Dictionary<string, object>();

            // Категория: сначала из снапшота-объяснения (то, что видели при подборке);
            // если объяснения нет — синтез из текущих специализации и грейда.
            string? categoryName = explanation.TryGetValue("категория", out var catVal)
                ? catVal?.ToString() : null;
            if (string.IsNullOrWhiteSpace(categoryName) && cs is not null)
                categoryName = $"{cs.Specialization?.Name} · {cs.CurrentGrade?.Name}";

            var skillNames = await _db.CandidateSkills.Where(s => s.CandidateId == r.CandidateId)
                .Join(_db.Skills, s => s.SkillId, k => k.Id, (s, k) => k.Name).ToListAsync();

            var fspAchievements = card.FspId is null ? new List<FspAchievementDto>() :
                await _db.FspAchievements.Where(a => a.FspId == card.FspId)
                    .Select(a => new FspAchievementDto
                    {
                        Id = a.Id, EventType = a.EventTypeNavigation.Name, EventName = a.EventName,
                        EventDate = a.EventDate, Role = a.Role, ResultPlace = a.ResultPlace,
                        ResultScore = a.ResultScore, Direction = a.Direction,
                        Weight = a.Weight, Verified = a.Verified
                    }).ToListAsync();

            // ТЗ 2.2 п.2 — те же правила, что в SearchAsync: принятое приглашение
            // или существующий (не отозванный) отклик открывают контакты.
            var contactsVisible = await _db.Invitations.AnyAsync(i =>
                i.CandidateId == r.CandidateId && i.CompanyId == company.Id && i.Status == "принято")
                || await _db.Applications.AnyAsync(a =>
                    a.CandidateId == r.CandidateId && a.Status != "отозван" &&
                    a.Vacancy.CompanyId == company.Id);

            items.Add(new SearchCandidateItemDto
            {
                Candidate = new CandidatePublicCardDto
                {
                    CandidateId = card.CandidateId,
                    LastName = card.LastName ?? "", FirstName = card.FirstName ?? "", MiddleName = card.MiddleName,
                    City = card.City, About = card.About,
                    ExperienceYears = card.ExperienceYears ?? 0m,      // в view decimal?
                    SoftSkills = card.SoftSkills?.ToArray(),           // List<string> → string[]
                    FspId = card.FspId,                                // Guid? — в DTO Guid?, ок
                    HasFspHistory = card.HasFspHistory == true,        // в view bool?
                    SpecializationName = cs?.Specialization?.Name,
                    GradeName = cs?.CurrentGrade?.Name,
                    CategoryName = categoryName,
                    Confirmed = confirmed,
                    BestTestPercent = bestTest, BestAttemptPercent = bestAttempt,
                    RankScore = r.RankScore, PositionInCategory = r.Position,
                    ContactsVisible = contactsVisible,
                    Skills = skillNames, FspAchievements = fspAchievements
                },
                RankScore = r.RankScore,
                Explanation = explanation,
                Position = r.Position
            });
        }
        return new SearchResponseDto
        {
            RequestId = requestId, Total = items.Count, Items = items,
            Filters = filters ?? new Dictionary<string, object>(),
            CreatedAt = request?.CreatedAt
        };
    }
}