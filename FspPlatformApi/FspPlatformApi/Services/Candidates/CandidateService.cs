using System.Text.Json;
using System.Text.RegularExpressions;
using FspPlatformApi.Data.Entities;
using FspPlatformApi.DTOs.Candidates;
using FspPlatformApi.DTOs.Fsp;
using FspPlatformApi.Services.Infrastructure;   // ProfilePdfGenerator
using FspPlatformApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using UglyToad.PdfPig;

namespace FspPlatformApi.Services.Candidates;

public class CandidateService : ICandidateService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;
    public const int GradeCooldownDays = 90; // ТЗ: смена грейда не чаще раза в 3 месяца

    public CandidateService(ApplicationDbContext db, IAuditService audit) => (_db, _audit) = (db, audit);

    public async Task<CandidateProfileUpsertDto?> GetMyProfileAsync(Guid userId)
    {
        var p = await _db.CandidateProfiles
            .Include(p => p.CandidateSkills)
            .FirstOrDefaultAsync(p => p.UserId == userId);
        if (p is null) return null;
        var roleIds = await _db.Set<Dictionary<string, object>>("CandidateRole")
            .Where(r => (Guid)r["CandidateId"] == userId)
            .Select(r => (short)r["RoleId"]).ToListAsync();
        return new CandidateProfileUpsertDto
        {
            LastName = p.LastName, FirstName = p.FirstName, MiddleName = p.MiddleName,
            Phone = p.Phone, Telegram = p.Telegram, City = p.City, About = p.About,
            ExperienceYears = p.ExperienceYears,
            SoftSkills = p.SoftSkills?.ToArray(),   // в сущности List<string>, в DTO string[]
            Skills = p.CandidateSkills
                .Select(s => new CandidateSkillDto { SkillId = s.SkillId, Proficiency = s.Proficiency, YearsExp = s.YearsExp })
                .ToList(),
            RoleIds = roleIds, IsProfilePublic = p.IsProfilePublic
        };
    }

    public async Task UpsertMyProfileAsync(Guid userId, CandidateProfileUpsertDto dto)
    {
        var p = await _db.CandidateProfiles.FirstOrDefaultAsync(x => x.UserId == userId)
            ?? throw new InvalidOperationException("Профиль не найден.");

        p.LastName = dto.LastName; p.FirstName = dto.FirstName; p.MiddleName = dto.MiddleName;
        p.Phone = dto.Phone; p.Telegram = dto.Telegram; p.City = dto.City; p.About = dto.About;
        p.ExperienceYears = dto.ExperienceYears;
        p.SoftSkills = dto.SoftSkills?.ToList();   // string[] → List<string>
        p.IsProfilePublic = dto.IsProfilePublic;

        _db.CandidateSkills.RemoveRange(_db.CandidateSkills.Where(s => s.CandidateId == userId));
        if (dto.Skills is not null)
            foreach (var s in dto.Skills)
                _db.CandidateSkills.Add(new CandidateSkill
                {
                    CandidateId = userId, SkillId = s.SkillId,
                    Proficiency = s.Proficiency, YearsExp = s.YearsExp
                });

        p.ProfileCompleteness = ComputeCompleteness(dto);
        await _db.SaveChangesAsync();
    }

    private static short ComputeCompleteness(CandidateProfileUpsertDto d)
    {
        short pct = 0;
        if (!string.IsNullOrWhiteSpace(d.LastName)) pct += 15;
        if (!string.IsNullOrWhiteSpace(d.FirstName)) pct += 15;
        if (!string.IsNullOrWhiteSpace(d.City)) pct += 5;
        if (!string.IsNullOrWhiteSpace(d.About)) pct += 10;
        if (!string.IsNullOrWhiteSpace(d.Phone) || !string.IsNullOrWhiteSpace(d.Telegram)) pct += 10;
        if (d.ExperienceYears > 0) pct += 10;
        if (d.SoftSkills?.Length > 0) pct += 5;
        pct += (short)Math.Min(30, (d.Skills?.Count ?? 0) * 3);
        return pct;
    }

    public async Task<Guid> StartSurveyAsync(Guid userId, StartSurveyDto dto)
    {
        var spec = await _db.Specializations.FindAsync((short)dto.SpecializationId)
            ?? throw new InvalidOperationException("Специализация не найдена.");
        var grade = await _db.Grades.FindAsync(dto.DeclaredGradeId)
            ?? throw new InvalidOperationException("Грейд не найден.");

        // Профиль кандидата обязан существовать (FK candidate_specializations → candidate_profiles).
        // Обычно он создаётся при регистрации роли «кандидат», но для старых/ручных аккаунтов
        // создаём здесь — опрос недоступен без профиля.
        if (!await _db.CandidateProfiles.AnyAsync(x => x.UserId == userId))
        {
            _db.CandidateProfiles.Add(new CandidateProfile { UserId = userId });
        }

        var session = new SurveySession { CandidateId = userId, Source = "регистрация" };
        _db.SurveySessions.Add(session);

        var existing = await _db.CandidateSpecializations
            .FirstOrDefaultAsync(x => x.CandidateId == userId && x.SpecializationId == spec.Id);
        if (existing is null)
        {
            // Первая созданная специализация становится основной (IsPrimary).
            // Так кандидат может иметь несколько специализаций — каждая со своим грейдом и тестом.
            _db.CandidateSpecializations.Add(new CandidateSpecialization
            {
                CandidateId = userId,
                SpecializationId = spec.Id,
                DeclaredGradeId = grade.Id,
                CurrentGradeId = grade.Id,
                Status = "ожидает_тестирования",
                IsPrimary = !await _db.CandidateSpecializations.AnyAsync(x => x.CandidateId == userId)
            });
        }
        else
        {
            existing.DeclaredGradeId = grade.Id;
            existing.Status = "ожидает_тестирования";
        }
        await _db.SaveChangesAsync();
        return session.Id;
    }

    public async Task SubmitSurveyAsync(Guid userId, SurveySubmitDto dto)
    {
        var session = await _db.SurveySessions
            .FirstOrDefaultAsync(s => s.Id == dto.SessionId && s.CandidateId == userId)
            ?? throw new InvalidOperationException("Сессия опроса не найдена.");

        foreach (var a in dto.Answers)
            _db.SurveyAnswers.Add(new SurveyAnswer
            {
                SessionId = session.Id,
                QuestionKey = a.QuestionKey,
                AnswerValue = JsonSerializer.Serialize(a.AnswerValue) // jsonb-поле скаффолдлено как string
            });
        session.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    /// <summary>Статус основной специализации (обратная совместимость).</summary>
    public async Task<MyStatusDto> GetMyStatusAsync(Guid userId)
    {
        var cs = await _db.CandidateSpecializations
            .Include(x => x.Specialization).Include(x => x.DeclaredGrade).Include(x => x.CurrentGrade)
            .Where(x => x.CandidateId == userId).OrderByDescending(x => x.IsPrimary)
            .FirstOrDefaultAsync() ?? throw new InvalidOperationException("Специализация не выбрана.");

        var lastChange = await _db.GradeChangeHistories
            .Where(h => h.CandidateId == userId && h.SpecializationId == cs.SpecializationId)
            .OrderByDescending(h => h.ChangedAt)
            .Select(h => (DateTime?)h.ChangedAt).FirstOrDefaultAsync();

        var nextAvailable = lastChange?.AddDays(GradeCooldownDays);
        return new MyStatusDto
        {
            SpecializationName = cs.Specialization.Name,
            DeclaredGradeName = cs.DeclaredGrade.Name,
            CurrentGradeName = cs.CurrentGrade.Name,
            Status = cs.StatusNavigation?.Name ?? cs.Status,
            LastTestAt = cs.LastTestAt,
            LastGradeChangeAt = cs.LastGradeChangeAt,
            CanChangeGrade = nextAvailable is null || nextAvailable <= DateTime.UtcNow,
            NextChangeAvailableAt = nextAvailable
        };
    }

    /// <summary>Все специализации кандидата со статусами грейдов и кулдаунами по каждой.</summary>
    public async Task<List<MySpecializationDto>> GetMySpecializationsAsync(Guid userId)
    {
        var items = await _db.CandidateSpecializations
            .Include(x => x.Specialization)
            .Include(x => x.DeclaredGrade)
            .Include(x => x.CurrentGrade)
            .Include(x => x.StatusNavigation)          // ← КЛЮЧЕВОЕ: без него Name = null
            .Where(x => x.CandidateId == userId)       //    и фронт получает код 'грейд_подтвержден'
            .OrderByDescending(x => x.IsPrimary)       //    вместо названия 'Грейд подтверждён'
            .ThenBy(x => x.Specialization.Name)
            .ToListAsync();

        if (items.Count == 0)
            throw new InvalidOperationException("Специализация не выбрана.");

        var specIds = items.Select(i => i.SpecializationId).ToList();
        var lastChanges = await _db.GradeChangeHistories
            .Where(h => h.CandidateId == userId && specIds.Contains(h.SpecializationId))
            .GroupBy(h => h.SpecializationId)
            .Select(g => new { SpecId = g.Key, Last = g.Max(h => h.ChangedAt) })
            .ToDictionaryAsync(x => x.SpecId, x => (DateTime?)x.Last);

        return items.Select(cs =>
        {
            var lastChange = lastChanges.GetValueOrDefault(cs.SpecializationId);
            var nextAvailable = lastChange?.AddDays(GradeCooldownDays);
            return new MySpecializationDto
            {
                SpecializationId = cs.SpecializationId,
                SpecializationName = cs.Specialization.Name,
                DeclaredGradeId = cs.DeclaredGradeId,
                DeclaredGradeName = cs.DeclaredGrade.Name,
                CurrentGradeId = cs.CurrentGradeId,
                CurrentGradeName = cs.CurrentGrade.Name,
                Status = cs.StatusNavigation?.Name ?? cs.Status,
                IsPrimary = cs.IsPrimary,
                LastTestAt = cs.LastTestAt,
                LastGradeChangeAt = cs.LastGradeChangeAt,
                CanChangeGrade = nextAvailable is null || nextAvailable <= DateTime.UtcNow,
                NextChangeAvailableAt = nextAvailable
            };
        }).ToList();
    }

    public async Task LinkFspAsync(Guid userId, LinkFspDto dto)
    {
        var user = await _db.Users.FindAsync(userId) ?? throw new UnauthorizedAccessException();
        if (await _db.Users.AnyAsync(u => u.FspId == dto.FspId && u.Id != userId))
            throw new InvalidOperationException("Этот ID ФСП уже привязан к другому аккаунту.");
        user.FspId = dto.FspId;
        user.FspLinkedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<CandidatePublicCardDto> GetPublicCardAsync(Guid candidateId, Guid? employerUserId)
    {
        var card = await _db.VCandidatePublicCards.FirstOrDefaultAsync(c => c.CandidateId == candidateId)
            ?? throw new InvalidOperationException("Кандидат не найден.");
        // в представлении v_candidate_public_card поле nullable (bool?) — сравниваем явно
        if (card.IsProfilePublic != true)
            throw new InvalidOperationException("Профиль кандидата скрыт настройками приватности.");

        var cs = await _db.CandidateSpecializations
            .Include(x => x.Specialization).Include(x => x.CurrentGrade)
            .FirstOrDefaultAsync(x => x.CandidateId == candidateId && x.IsPrimary);

        string? categoryName = null;
        if (cs is not null)
            categoryName = (await _db.Categories.FirstOrDefaultAsync(
                c => c.SpecializationId == cs.SpecializationId && c.GradeId == cs.CurrentGradeId))?.DisplayName;

        var bestTest = cs is null ? null : await _db.TestSessions
            .Where(t => t.CandidateId == candidateId
                && t.SpecializationId == cs.SpecializationId
                && t.Status == "завершен" && t.Passed == true)
            .MaxAsync(t => (decimal?)t.Percent);

        // В карточке показываем последнюю попытку по основной специализации, чтобы после
        // автоматического понижения грейда рядом с ним отображался актуальный процент.
        var bestAttempt = cs is null ? null : await _db.TestSessions
            .Where(t => t.CandidateId == candidateId
                && t.SpecializationId == cs.SpecializationId
                && t.Status == "завершен")
            .OrderByDescending(t => t.FinishedAt)
            .Select(t => (decimal?)t.Percent)
            .FirstOrDefaultAsync();

        var rank = await _db.VCategoryRankings.FirstOrDefaultAsync(r => r.CandidateId == candidateId);

        var skillNames = await _db.CandidateSkills.Where(s => s.CandidateId == candidateId)
            .Join(_db.Skills, s => s.SkillId, k => k.Id, (s, k) => k.Name).ToListAsync();

        var fsp = card.FspId is null ? new List<FspAchievementDto>() :
            await _db.FspAchievements.Where(a => a.FspId == card.FspId)
                .OrderByDescending(a => a.EventDate)
                .Select(a => new FspAchievementDto
                {
                    Id = a.Id,
                    EventType = a.EventTypeNavigation.Name,
                    EventName = a.EventName,
                    EventDate = a.EventDate,
                    Role = a.Role, ResultPlace = a.ResultPlace, ResultScore = a.ResultScore,
                    Direction = a.Direction, Weight = a.Weight, Verified = a.Verified
                }).ToListAsync();

        var contactsVisible = employerUserId is not null &&
            await CanSeeContactsAsync(candidateId, employerUserId.Value);

        return new CandidatePublicCardDto
        {
            CandidateId = candidateId,
            LastName = card.LastName ?? "", FirstName = card.FirstName ?? "", MiddleName = card.MiddleName,
            City = card.City, About = card.About,
            ExperienceYears = card.ExperienceYears ?? 0m,      // в view decimal?
            SoftSkills = card.SoftSkills?.ToArray(),           // List<string> → string[]
            FspId = card.FspId,
            HasFspHistory = card.HasFspHistory == true,        // в view bool?
            SpecializationName = cs?.Specialization.Name,
            GradeName = cs?.CurrentGrade.Name,
            // Q&A менторов: неподтверждённый грейд не скрываем — показываем статусом.
            // False → фронт рисует бейдж «грейд не подтверждён тестом».
            Confirmed = cs?.Status == "грейд_подтвержден",
            CategoryName = categoryName,
            BestTestPercent = bestTest, BestAttemptPercent = bestAttempt,
            RankScore = rank?.RankScore ?? 0m,
            PositionInCategory = (int)(rank?.PositionInCategory ?? 0L), // в view bigint (long)
            ContactsVisible = contactsVisible,
            Skills = skillNames,
            FspAchievements = fsp
        };
    }

    /// <summary>Контакты раскрываются после принятия приглашения ИЛИ самого факта отклика кандидата (ТЗ 2.2 п.2).</summary>
    public async Task<CandidateContactsDto> GetContactsAsync(Guid candidateId, Guid employerUserId)
    {
        if (!await CanSeeContactsAsync(candidateId, employerUserId))
            throw new UnauthorizedAccessException(
                "Контакты доступны только после принятия приглашения или отклика кандидата.");

        var company = await _db.Companies
            .FirstAsync(c => c.OwnerUserId == employerUserId && c.Status == "активен");
        await _audit.LogAsync(employerUserId, "candidate_profiles", candidateId.ToString(),
            "view_contacts", newValue: new { companyId = company.Id });

        var u = await _db.Users.FindAsync(candidateId);
        var p = await _db.CandidateProfiles.FindAsync(candidateId);
        return new CandidateContactsDto { Email = u?.Email, Phone = p?.Phone, Telegram = p?.Telegram };
    }

    // ------------------------------------------------------------------
    // PDF-резюме и автогенерация стандартизированного PDF-профиля (ТЗ 2.1)
    // ------------------------------------------------------------------

    // Максимальный размер PDF (5 МБ)
    private const long MaxResumeBytes = 5 * 1024 * 1024;

    /// <summary>
    /// Загрузка PDF-резюме с автораспознаванием (ТЗ 2.1).
    /// Текст извлекаем PdfPig, затем нормализуем: восстанавливаем пробелы на границах
    /// (пунктуация/регистр/алфавит), иначе слова слипаются.
    /// Город — только слова вида «Москва»/«Санкт-Петербург» (строчный «хвост»),
    /// заголовки-секций отсекаются даже в склеенном виде («ОБРАЗОВАНИЕМФТИ»).
    /// </summary>
    public async Task<ResumeUploadResult> UploadResumeAsync(Guid userId, Stream fileStream, string fileName)
    {
        var p = await _db.CandidateProfiles.FirstOrDefaultAsync(x => x.UserId == userId)
            ?? throw new InvalidOperationException("Профиль не найден.");

        if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Допустим только PDF-файл.");

        using var ms = new MemoryStream();
        await fileStream.CopyToAsync(ms);
        if (ms.Length == 0) throw new InvalidOperationException("Файл пуст.");
        if (ms.Length > MaxResumeBytes) throw new InvalidOperationException("Файл больше 5 МБ.");

        // --- Извлекаем текст со всех страниц ---
        string text;
        try
        {
            using var pdf = PdfDocument.Open(ms.ToArray());
            text = string.Join("\n", pdf.GetPages().Select(pg => pg.Text));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Не удалось прочитать текстовый слой PDF (возможно, это скан). " +
                "Заполните форму вручную. Техническая причина: " + ex.Message);
        }

        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException(
                "В PDF нет текстового слоя (скан). Заполните форму вручную.");

        // --- Нормализация: PdfPig иногда теряет пробелы между токенами ---
        text = text.Replace("\r", string.Empty);
        text = Regex.Replace(text, @"(?<=[,.;:!?])(?=\S)", " ");        // пробел после пунктуации
        text = Regex.Replace(text, @"(?<=[а-яё])(?=[А-ЯЁ])", " ");      // стык строчной→заглавной кириллицы
        text = Regex.Replace(text, @"(?<=\d)(?=[А-ЯЁA-Z])", " ");       // цифра → буква
        text = Regex.Replace(text, @"(?<=[а-яёa-z])(?=[A-Z])", " ");    // стык регистров латиницы

        // --- Email: домен только латиница ---
        var emailMatch = Regex.Match(text, @"[\w.+-]+@[A-Za-z0-9-]+(\.[A-Za-z]{2,})+");
        var email = emailMatch.Success ? emailMatch.Value : null;

        // --- Телефон: кандидат с ≥10 цифр ---
        string? phone = Regex.Matches(text, @"\+?\d[\d\s\-()]{8,}\d")
            .Select(m => m.Value.Trim())
            .FirstOrDefault(v => v.Count(char.IsDigit) >= 10);

        // --- Telegram: @handle, НЕ внутри email ---
        var tgMatch = Regex.Match(text, @"(?<![\w.+-])@([A-Za-z0-9_]{3,32})");
        var telegram = tgMatch.Success ? "@" + tgMatch.Groups[1].Value : null;

        // --- Город: слова с заглавной буквы и СТРОЧНЫМ хвостом ---
        // «СЕБЕData» / «ОБРАЗОВАНИЕМФТИ» не пройдут — у прописных заголовков хвоста нет.
        // Допускает «Москва», «Санкт-Петербург», «Нижний Новгород».
        string? city = null;
        var cityMatch = Regex.Match(
            text,
            @"Город\s*[:\-]\s*([А-ЯЁ][а-яё]+(?:\s*[-–]\s*[А-ЯЁ][а-яё]+)?(?:\s+[А-ЯЁ][а-яё]+)?)",
            RegexOptions.IgnoreCase);
        if (cityMatch.Success)
            city = cityMatch.Groups[1].Value.Trim();

        // --- ФИО: первые 2–3 кириллических слова с заглавной буквы ---
        string? lastName = null, firstName = null, middleName = null;
        var fioMatch = Regex.Match(text, @"\b([А-ЯЁ][а-яё]+)\s+([А-ЯЁ][а-яё]+)(?:\s+([А-ЯЁ][а-яё]+))?");
        if (fioMatch.Success)
        {
            lastName = fioMatch.Groups[1].Value;
            firstName = fioMatch.Groups[2].Value;
            middleName = fioMatch.Groups[3].Success ? fioMatch.Groups[3].Value : null;
        }

        // --- Стаж: диапазоны «2020 — 2022», «2022 — настоящее время» ---
        decimal? expYears = null;
        int? startYear = null, endYear = null;
        foreach (Match m in Regex.Matches(text, @"(19|20)\d{2}\s*[—–-]\s*((19|20)\d{2}|настоящ)"))
        {
            var sy = int.Parse(m.Value[..4]);
            startYear = startYear is null ? sy : Math.Min(startYear.Value, sy);

            var tail = m.Value[4..].TrimStart('—', '–', '-', ' ');
            var ey = int.TryParse(new string(tail.Take(4).ToArray()), out var eyOut) ? eyOut : DateTime.UtcNow.Year;
            endYear = endYear is null ? ey : Math.Max(endYear.Value, ey);
        }
        if (startYear is not null && endYear is not null && endYear > startYear)
            expYears = endYear.Value - startYear.Value;

        // Граница секции. Длинные заголовки («образование», «достижен», «сертификат»,
        // «софт-скилл») сопоставляем БЕЗ \b — ловим склейки вида «ОБРАЗОВАНИЕМФТИ».
        // Короткие («стек», «опыт») — только с границей слова, чтобы не резать текст.
        const string nextHeading =
            @"\s*(опыт\s+работы\b|стек\b|софт[\s-]*скилл|образование|навыки\b|достижен" +
            @"|языки\b|контакты\b|о\s+себе\b|сертификат|курсы\b|проекты\b)|\z";

        // Разрыв склейки при смене алфавита («СТЕКPython» → «СТЕК Python»): без
        // пробела граница слова после «стек» не наступает — секция «о себе»
        // поглощала стек, а заголовок не распознавался. Обратная склейка
        // («PythonСТЕК») тоже ловится второй альтернативой.
        text = Regex.Replace(
            text,
            @"(?<=[А-ЯЁ])(?=[A-Za-z])|(?<=[A-Za-z])(?=[А-ЯЁ])",
            " ");

        // --- «О себе»: секция до следующего заголовка ---
        string? about = null;
        var aboutMatch = Regex.Match(
            text,
            @"о\s+себе\s*[:\-]?\s*(.+?)(?=" + nextHeading + ")",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (aboutMatch.Success)
        {
            about = string.Join(" ",
                aboutMatch.Groups[1].Value
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim()));
            if (string.IsNullOrWhiteSpace(about)) about = null;
        }

        // --- Софт-скиллы: секция «СОФТ-СКИЛЛЫ» до следующего заголовка ---
        string[]? softSkills = null;
        var ssMatch = Regex.Match(
            text,
            @"софт[\s-]*скилл[ыи]?\s*[:\-]?\s*(.+?)(?=" + nextHeading + ")",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (ssMatch.Success)
        {
            softSkills = ssMatch.Groups[1].Value
                .Split(new[] { ',', ';', '•', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim().TrimEnd('.'))
                .Where(x => x.Length > 1 && x.Length < 60 && !x.Any(char.IsDigit)) // без дат/мусора
                .ToArray();
            if (softSkills.Length == 0) softSkills = null;
        }

        // --- Стек: совпадения названий со справочником навыков ---
        var dictSkills = await _db.Skills.Where(x => x.IsActive)
            .Select(x => x.Name).ToListAsync();
        var recognizedSkills = dictSkills
            .Where(n => text.Contains(n, StringComparison.OrdinalIgnoreCase))
            .ToList();

        // --- Сохраняем файл ---
        var dir = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Uploads");
        Directory.CreateDirectory(dir);
        var storedPath = Path.Combine(dir, $"resume_{userId}.pdf");
        await File.WriteAllBytesAsync(storedPath, ms.ToArray());

        p.ResumePdfPath = storedPath;
        await _db.SaveChangesAsync();

        // Именованные аргументы: привязка по имени параметра record
        return new ResumeUploadResult(
            StoredPath: storedPath,
            RecognizedEmail: email,
            RecognizedPhone: phone,
            RecognizedTelegram: telegram,
            RecognizedCity: city,
            RecognizedLastName: lastName,
            RecognizedFirstName: firstName,
            RecognizedMiddleName: middleName,
            RecognizedExperienceYears: expYears,
            RecognizedAbout: about,
            RecognizedSoftSkills: softSkills,
            RecognizedSkills: recognizedSkills);
    }

    /// <summary>Формирует стандартизированный PDF-профиль из данных формы (ТЗ 2.1).</summary>
    public async Task<byte[]> GetProfilePdfAsync(Guid userId)
    {
        var p = await _db.CandidateProfiles
            .Include(x => x.CandidateSkills)
            .FirstOrDefaultAsync(x => x.UserId == userId)
            ?? throw new InvalidOperationException("Профиль не найден.");

        var u = await _db.Users.FindAsync(userId);
        var cs = await _db.CandidateSpecializations
            .FirstOrDefaultAsync(x => x.CandidateId == userId && x.IsPrimary);
        string? specName = null, gradeName = null;
        if (cs is not null)
        {
            specName = (await _db.Specializations.FindAsync(cs.SpecializationId))?.Name;
            gradeName = (await _db.Grades.FindAsync(cs.CurrentGradeId))?.Name;
        }

        var skillIds = p.CandidateSkills.Select(x => x.SkillId).ToList();
        var skills = await _db.Skills.Where(x => skillIds.Contains(x.Id))
            .Select(x => x.Name).ToListAsync();

        // ТЗ: неподтверждённый грейд не скрываем, а честно помечаем — в PDF тоже.
        var gradeConfirmed = cs?.Status == "грейд_подтвержден";
        return ProfilePdfGenerator.Build(p, u?.Email ?? "", specName, gradeName, skills, gradeConfirmed);
    }

    private async Task<bool> CanSeeContactsAsync(Guid candidateId, Guid employerUserId)
    {
        var hasAcceptedInvitation = await _db.Invitations.AnyAsync(i =>
            i.CandidateId == candidateId && i.Company.OwnerUserId == employerUserId &&
            i.Status == "принято");
        if (hasAcceptedInvitation) return true;

        // ТЗ 2.2 п.2: раскрытие — после принятия приглашения ИЛИ самого факта отклика.
        // Достаточно существующего отклика; «отозван» исключаем — кандидат сам
        // отозвал заявку, значит контакт не предлагал. Раньше здесь требовался
        // статус «принят» (клик работодателя) — это расходилось с ТЗ и ломало
        // сценарий: контакты уже отдавались в таблице откликов сразу после отклика,
        // а карточка их прятала.
        return await _db.Applications.AnyAsync(a =>
            a.CandidateId == candidateId &&
            a.Status != "отозван" &&
            a.Vacancy.Company.OwnerUserId == employerUserId);
    }
}