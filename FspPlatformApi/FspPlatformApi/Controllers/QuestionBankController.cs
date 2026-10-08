using System.Text.Json;
using FspPlatformApi.Data.Entities;
using FspPlatformApi.Services.Infrastructure;
using FspPlatformApi.Services.Interfaces.Testing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FspPlatformApi.Controllers;

/// <summary>
/// Генерация банка вопросов через ИИ. Пайплайн: generate → дедупликация → validate
/// (цикл до 3 попыток) → ЧЕРНОВИК (is_active=false) → эксперт проверяет
/// эквивалентность сложности внутри групп → activate.
/// ТЗ/testing_design.md: «ИИ — для черновиков, сложность выравниваем вручную»,
/// «ИИ никогда не является единственным источником оценки».
/// Дедупликация: повторяющиеся формулировки не попадают в БД (защита от
/// расползания заданий по банку и рынку).
/// Служебная роль "администратор" (statuses: entity='роль', code='администратор') —
/// техническая роль команды методологов, кабинета не имеет (ТЗ 2.1: два кабинета —
/// кандидат и работодатель, перечень минимальный).
///
/// generate-all: пакетная генерация по ВСЕМ активным парам «специализация × грейд»,
/// у которых настроен шаблон теста (test_assignments). difficulty_mix берётся
/// из шаблона пары. Между вызовами GigaChat — пауза (Generation:DelaySeconds,
/// по умолчанию 15 с) во избежание rate limit. Возвращает сводку по каждой паре.
/// </summary>
[ApiController]
[Route("api/testing/question-bank")]
[Authorize(Roles = "администратор")]
public class QuestionBankController : ControllerBase
{
    // Легальные коды типов вопросов — строго по DDL БД (ApplicationDbContext):
    // дефолт test_questions.question_type = 'тест_с_вариантами',
    // FK test_questions_question_type_fkey → statuses.code.
    private const string TypeQuiz = "тест_с_вариантами";
    private const string TypeCode = "задача_с_кодом";
    private const string TypeOpen = "открытый_вопрос";

    private readonly IQuestionGeneratorService _generator;
    private readonly IQuestionValidatorService _validator;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<QuestionBankController> _log;
    private readonly IConfiguration _cfg;

    public QuestionBankController(IQuestionGeneratorService generator, IQuestionValidatorService validator,
        ApplicationDbContext db, ILogger<QuestionBankController> log, IConfiguration cfg)
        => (_generator, _validator, _db, _log, _cfg) = (generator, validator, db, log, cfg);

    /// <summary>
    /// Сгенерировать и провалидировать банк для ОДНОЙ пары «специализация + грейд».
    /// Цикл generate → validate: при невалидном отчёте генерация повторяется
    /// (максимум 3 захода), чтобы отсеять случайный брак модели.
    /// В БД попадает только валидный ЧЕРНОВИК (is_active=false) — эксперт утверждает через /activate.
    /// </summary>
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateBankRequest req, CancellationToken ct)
    {
        try
        {
            var result = await GenerateOnePairAsync(req, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Генерация банка вопросов провалилась");
            return StatusCode(500, new { error = ex.GetType().Name, message = ex.Message });
        }
    }

    /// <summary>
    /// ПАКЕТНАЯ генерация: все активные пары «специализация × грейд», у которых есть
    /// активный шаблон теста (test_assignments). Пары без шаблона пропускаются
    /// (см. сид test_assignments) — иначе кандидат не сможет пройти тест на пару.
    /// Опционально — только одна специализация (?specializationId=).
    /// Пауза между парами: Generation:DelaySeconds (по умолчанию 15).
    /// Можно отменить, закрыв запрос (CancellationToken).
    /// </summary>
    [HttpPost("generate-all")]
    public async Task<IActionResult> GenerateAll([FromQuery] int? specializationId, CancellationToken ct)
    {
        var delaySec = _cfg.GetValue<int>("Generation:DelaySeconds", 15);

        // Все активные пары, у которых настроен активный шаблон теста;
        // difficulty_mix берём из шаблона — генератор целится в ту же структуру,
        // что и сборщик сессий (PickQuestionsAsync).
        var pairs = await _db.TestAssignments
            .AsNoTracking()
            .Include(a => a.Specialization)
            .Include(a => a.Grade)
            .Where(a => a.IsActive
                && a.Specialization.IsActive
                && a.Grade.IsActive
                && (specializationId == null || a.SpecializationId == specializationId))
            .GroupBy(a => new { a.SpecializationId, a.GradeId })
            .Select(g => g.OrderByDescending(a => a.Version).First())
            .ToListAsync(ct);

        if (pairs.Count == 0)
            return Ok(new { processed = 0, message = "Нет активных пар с настроенным шаблоном теста (test_assignments)." });

        _log.LogInformation("Пакетная генерация: {Count} пар, пауза {Delay}s", pairs.Count, delaySec);

        var results = new List<object>();
        var totalInserted = 0;

        foreach (var a in pairs)
        {
            ct.ThrowIfCancellationRequested();

            Dictionary<int, int>? mix = null;
            try
            {
                mix = JsonSerializer.Deserialize<Dictionary<int, int>>(a.DifficultyMix);
            }
            catch (JsonException)
            {
                _log.LogWarning("difficulty_mix шаблона пары {Spec}/{Grade} невалиден, используем дефолт",
                    a.SpecializationId, a.GradeId);
            }

            var req = new GenerateBankRequest
            {
                SpecializationId = a.SpecializationId,
                GradeId = a.GradeId,
                SpecializationName = a.Specialization.Name,
                GradeName = a.Grade.Name,
                DifficultyMix = mix
            };

            object pairResult;
            try
            {
                pairResult = await GenerateOnePairAsync(req, ct);
                var inserted = (int)((dynamic)pairResult).inserted;
                totalInserted += inserted;
                _log.LogInformation("Пара {Spec}/{Grade}: вставлено {Inserted} черновиков",
                    a.Specialization.Name, a.Grade.Name, inserted);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Пара {Spec}/{Grade}: генерация провалилась",
                    a.Specialization.Name, a.Grade.Name);
                pairResult = new
                {
                    checkedCount = 0,
                    errors = new[] { $"{ex.GetType().Name}: {ex.Message}" },
                    warnings = Array.Empty<string>(),
                    isValid = false,
                    inserted = 0,
                    attempt = 0,
                    duplicatesSkipped = 0,
                    message = "Пара пропущена из-за ошибки"
                };
            }

            results.Add(new
            {
                specializationId = a.SpecializationId,
                specializationName = a.Specialization.Name,
                gradeId = a.GradeId,
                gradeName = a.Grade.Name,
                result = pairResult
            });

            // пауза между вызовами GigaChat — бережём rate limit
            if (!ct.IsCancellationRequested)
                await Task.Delay(TimeSpan.FromSeconds(delaySec), ct);
        }

        return Ok(new
        {
            processed = pairs.Count,
            totalInserted,
            pairs = results
        });
    }

    /// <summary>
    /// ЭКСПЕРТНОЕ УТВЕРЖДЕНИЕ (testing_design.md: «человек проверяет эквивалентность
    /// сложности, утверждает вопрос»). Активирует черновики по списку id
    /// или целиком группы по question_group. После активации вопросы доступны сборке сессий.
    /// </summary>
    [HttpPost("activate")]
    public async Task<IActionResult> Activate([FromBody] ActivateRequest req, CancellationToken ct)
    {
        var query = _db.TestQuestions.Where(q => !q.IsActive);

        if (req.QuestionIds.Count > 0)
            query = query.Where(q => req.QuestionIds.Contains(q.Id));
        if (req.QuestionGroupIds.Count > 0)
            query = query.Where(q => req.QuestionGroupIds.Contains(q.QuestionGroup));

        var toActivate = await query.ToListAsync(ct);
        if (toActivate.Count == 0)
            return Ok(new { activated = 0, message = "Не найдено неактивных вопросов по заданным фильтрам" });

        foreach (var q in toActivate) q.IsActive = true;
        await _db.SaveChangesAsync(ct);

        _log.LogInformation("Эксперт активировал {Count} вопросов", toActivate.Count);
        return Ok(new { activated = toActivate.Count, questionIds = toActivate.Select(q => q.Id) });
    }

    /// <summary>Черновики, ожидающие экспертной проверки: неактивные вопросы пары специализация/грейд.</summary>
    [HttpGet("drafts")]
    public async Task<IActionResult> Drafts([FromQuery] int specializationId, [FromQuery] int gradeId, CancellationToken ct)
    {
        var drafts = await _db.TestQuestions
            .Where(q => !q.IsActive && q.SpecializationId == specializationId && q.GradeId == gradeId)
            .OrderBy(q => q.QuestionGroup).ThenBy(q => q.Difficulty)
            .Select(q => new
            {
                q.Id,
                q.QuestionGroup,
                q.QuestionType,
                q.Difficulty,
                q.Prompt,
                Payload = q.Payload,
                CorrectPayload = q.CorrectPayload,
                q.MaxScore
            })
            .ToListAsync(ct);

        return Ok(drafts);
    }

    /// <summary>Удалить отклонённые черновики (после ревью эксперт отклоняет брак).</summary>
    [HttpPost("reject")]
    public async Task<IActionResult> Reject([FromBody] ActivateRequest req, CancellationToken ct)
    {
        var query = _db.TestQuestions.Where(q => !q.IsActive);
        if (req.QuestionIds.Count > 0)
            query = query.Where(q => req.QuestionIds.Contains(q.Id));
        if (req.QuestionGroupIds.Count > 0)
            query = query.Where(q => req.QuestionGroupIds.Contains(q.QuestionGroup));

        var toDelete = await query.ToListAsync(ct);
        _db.TestQuestions.RemoveRange(toDelete);
        await _db.SaveChangesAsync(ct);

        _log.LogInformation("Эксперт отклонил {Count} черновых вопросов", toDelete.Count);
        return Ok(new { deleted = toDelete.Count });
    }

    // ------------------------------------------------------------------
    // ОБЩАЯ ЛОГИКА ГЕНЕРАЦИИ ОДНОЙ ПАРЫ (используется /generate и /generate-all).
    // Возвращает анонимный объект результата; вызывающий оборачивает в сводку.
    // ------------------------------------------------------------------
    private async Task<object> GenerateOnePairAsync(GenerateBankRequest req, CancellationToken ct)
    {
        const int maxAttempts = 3;
        ValidationReport? lastReport = null;
        var totalDuplicatesSkipped = 0;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var batch = await _generator.GenerateAsync(req.SpecializationName, req.GradeName, req.DifficultyMix, ct);

            // --------------------------------------------------------------
            // ДЕДУПЛИКАЦИЯ (анти-утечка: одинаковые формулировки не должны
            // плодиться в банке между генерациями и внутри одного батча)
            // --------------------------------------------------------------
            // 1. Формулировки, уже существующие в БД для этой пары
            //    (черновики тоже считаем — иначе получим два одинаковых черновика)
            var existingPrompts = await _db.TestQuestions
                .Where(t => t.SpecializationId == req.SpecializationId && t.GradeId == req.GradeId)
                .Select(t => t.Prompt)
                .ToListAsync(ct);

            var existingSet = existingPrompts
                .Select(NormalizePrompt)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 2. Дубли внутри самой генерации (модель может выдать одну формулировку дважды)
            var seenInBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var duplicates = batch.Questions
                .Where(q =>
                {
                    var key = NormalizePrompt(q.Prompt);
                    // в existingSet и seenInBatch элемент добавляется, если его ещё не было —
                    // Add вернёт false для дубликата
                    return !existingSet.Add(key) || !seenInBatch.Add(key);
                })
                .Select(q => q.Prompt)
                .ToList();

            if (duplicates.Count > 0)
            {
                totalDuplicatesSkipped += duplicates.Count;
                _log.LogWarning("Попытка {Attempt}: отброшено дубликатов вопросов: {Count} ({Samples})",
                    attempt, duplicates.Count, string.Join(" | ", duplicates.Take(3)));

                batch.Questions = batch.Questions
                    .Where(q => !duplicates.Contains(q.Prompt))
                    .ToList();
            }

            if (batch.Questions.Count == 0)
            {
                _log.LogWarning("Попытка {Attempt}/{Max}: модель вернула только дубликаты существующих вопросов",
                    attempt, maxAttempts);
                lastReport = new ValidationReport(); // пустой отчёт → попытка неудачная, идём дальше
                continue;
            }
            // --------------------------------------------------------------

            var report = await _validator.ValidateAsync(batch, ct);

            lastReport = report;

            if (report.IsValid)
            {
                var entities = batch.Questions.Select(q => new TestQuestion
                {
                    // short: в БД specialization_id/grade_id/difficulty — smallint
                    SpecializationId = (short)req.SpecializationId,
                    GradeId = (short)req.GradeId,
                    // Нормализация типа строго под коды справочника statuses,
                    // иначе FK test_questions_question_type_fkey отклонит INSERT (23503)
                    QuestionType = NormalizeQuestionType(q.QuestionType, _log),
                    QuestionGroup = q.QuestionGroup,
                    Difficulty = (short)q.Difficulty,
                    Prompt = q.Prompt,
                    Payload = Jsonb.From(q.Payload),
                    CorrectPayload = Jsonb.From(q.CorrectPayload),
                    MaxScore = q.MaxScore,
                    // Черновик — эксперт утверждает через /activate
                    IsActive = false
                }).ToList();

                _db.TestQuestions.AddRange(entities);
                await _db.SaveChangesAsync(ct);

                _log.LogInformation("Создан черновик: {Count} вопросов, spec={Spec} grade={Grade} (попытка {Attempt})",
                    entities.Count, req.SpecializationId, req.GradeId, attempt);

                return new
                {
                    report.Checked,
                    errors = report.Errors,
                    warnings = report.Warnings,
                    isValid = true,
                    inserted = entities.Count,
                    attempt,
                    duplicatesSkipped = totalDuplicatesSkipped,
                    // активируются только по /activate после экспертной проверки
                    draftQuestionIds = entities.Select(e => e.Id),
                    questionGroupIds = entities.Select(e => e.QuestionGroup).Distinct()
                };
            }

            _log.LogWarning("Попытка {Attempt}/{Max}: валидация не пройдена: {Errors}",
                attempt, maxAttempts, string.Join(" | ", report.Errors));
        }

        // все попытки неудачны — возвращаем последний отчёт, в БД ничего не пишем
        return new
        {
            lastReport!.Checked,
            errors = lastReport.Errors,
            warnings = lastReport.Warnings,
            isValid = false,
            inserted = 0,
            attempts = maxAttempts,
            duplicatesSkipped = totalDuplicatesSkipped,
            message = "После 3 попыток модель не выдала валидный набор. Попробуйте позже или скорректируйте difficultyMix/промпт."
        };
    }

    // ------------------------------------------------------------------
    // НОРМАЛИЗАЦИЯ ТИПА ВОПРОСА под справочник statuses (коды по DDL БД).
    // Модель обычно возвращает тест_с_вариантами напрямую (так просит промпт),
    // но может фантазировать: quiz, викторина, code, открытый...
    // FK примет только три кода: тест_с_вариантами | задача_с_кодом | открытый_вопрос.
    // ------------------------------------------------------------------
    private static string NormalizeQuestionType(string? rawType, ILogger log)
    {
        var t = (rawType ?? "").Trim().ToLowerInvariant();

        if (t.Contains("код") || t is "code" or "coding")
            return TypeCode;
        if (t.Contains("открыт") || t is "open" or "open_question" or "openanswer")
            return TypeOpen;
        if (t is TypeQuiz or "quiz" or "тест" or "викторина"
            or "выбор" or "тестовый_вопрос" or "multiple_choice" or "тест_вариантами")
            return TypeQuiz;

        // неизвестное по умолчанию считаем quiz (самый частый тип), но пишем warning
        log.LogWarning("Неизвестный тип вопроса от модели '{Raw}' — сохранён как '{Fallback}'",
            rawType, TypeQuiz);
        return TypeQuiz;
    }

    // ------------------------------------------------------------------
    // Нормализация формулировки для дедупликации: регистр + пробелы по краям.
    // Пунктуацию не трогаем — «что такое orm?» и «Что такое ORM» совпадут,
    // а «ORM — это...» уже другая формулировка (допустимо).
    // ------------------------------------------------------------------
    private static string NormalizePrompt(string? prompt)
        => (prompt ?? "").Trim();
}

public class GenerateBankRequest
{
    public int SpecializationId { get; set; }
    public int GradeId { get; set; }
    public string SpecializationName { get; set; } = string.Empty;
    public string GradeName { get; set; } = string.Empty;

    /// <summary>
    /// Структура сложности из test_assignments.difficulty_mix, например {"1":3,"2":4,"3":2,"4":1}.
    /// System.Text.Json автоматически конвертирует строковые ключи JSON в int.
    /// Если null — генератор использует дефолтное распределение из промпта.
    /// </summary>
    public Dictionary<int, int>? DifficultyMix { get; set; }
}

public class ActivateRequest
{
    public List<Guid> QuestionIds { get; set; } = new();
    public List<Guid> QuestionGroupIds { get; set; } = new();
}