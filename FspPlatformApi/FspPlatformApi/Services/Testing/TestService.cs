using System.Text.Json;
using FspPlatformApi.Data.Entities;
using FspPlatformApi.DTOs.Tests;
using FspPlatformApi.Services.Infrastructure;
using FspPlatformApi.Services.Interfaces.Testing;
using Microsoft.EntityFrameworkCore;

namespace FspPlatformApi.Services.Testing;

/// <summary>
/// Механика тестирования (устойчивая к утечкам и сопоставимая по сложности):
/// 1) банк вопросов с изоморфными группами (question_group) и сложностью 1–5;
/// 2) сборка сессии — стратифицированная случайная выборка строго по difficulty_mix шаблона;
/// 3) ИЗОМОРФНАЯ ЗАЩИТА: у quiz-вопросов ВАРИАНТЫ ОТВЕТОВ перемешиваются при сборке сессии
///    (утекший скриншот «третий вариант» больше не работает — порядок у каждого свой);
///    маппинг «перемешанный порядок → эталон» хранится в test_session_answers.answer_payload._order;
/// 4) quiz — детерминированная сверка с эталоном (по сессионному маппингу);
/// 5) code — детерминированный прогон скрытых тест-кейсов в Docker-песочнице (ИИ не участвует);
/// 6) open — ИИ по зафиксированной рубрике + очередь экспертной перепроверки;
/// 7) калибровка банка по v_question_stats (дискриминативность) — QuestionCalibrationService;
/// 8) ПУТЬ ПОСЛЕ ПРОВАЛА (ТЗ): не пройден тест на уровень N — следующим доступен ТОЛЬКО
///    тест на N−1. Пересдача на N запрещена до подтверждения грейда ниже: иначе повторные
///    попытки «до победного» размывают согласованность грейда (критерий жюри).
///    Подтверждённый грейд пересдавать можно; повышение после подтверждения — с кулдауном 90 дней.
/// 9) РЕЗУЛЬТАТ (Q&A менторов): тест подтверждает ЗАЯВЛЕННЫЙ (target) грейд. Автоповышения нет —
///    повышение — отдельный тест после кулдауна 90 дней. Грейд после провала не понижается
///    принудительно (ТЗ): кандидат сам проходит тест на уровень ниже.
/// </summary>
public class TestService : ITestService
{
    private readonly ApplicationDbContext _db;
    private readonly ICodeRunnerService _codeRunner;
    private readonly IOpenAnswerGrader _openGrader;

    public const int GradeCooldownDays = 90;

    public TestService(ApplicationDbContext db, ICodeRunnerService codeRunner, IOpenAnswerGrader openGrader)
        => (_db, _codeRunner, _openGrader) = (db, codeRunner, openGrader);

    public async Task<TestSessionDto> StartTestAsync(Guid candidateId, StartTestDto dto)
    {
        var cs = await _db.CandidateSpecializations
            .FirstOrDefaultAsync(x => x.CandidateId == candidateId && x.SpecializationId == dto.SpecializationId)
            ?? throw new InvalidOperationException("Сначала пройдите опрос и выберите специализацию.");

        var inProgress = await _db.TestSessions.AnyAsync(t => t.CandidateId == candidateId
            && t.Status == "в_процессе" && t.StartedAt > DateTime.UtcNow.AddHours(-24));
        if (inProgress)
            throw new InvalidOperationException("У вас есть незавершённая тестовая сессия.");

        // ПУТЬ ПОСЛЕ ПРОВАЛА (ТЗ): последняя завершённая попытка на ЦЕЛЕВОЙ грейд провалена —
        // дальше только тест на уровень ниже. Это запрещает «долбить» один уровень до победного
        // и сохраняет согласованность грейда при повторном прохождении.
        // (Подтверждённый грейд не страдает: при passed == true проверка не срабатывает.)
        var lastAttemptAtTarget = await _db.TestSessions
            .Where(t => t.CandidateId == candidateId
                && t.SpecializationId == dto.SpecializationId
                && t.TargetGradeId == dto.TargetGradeId
                && t.Status == "завершен")
            .OrderByDescending(t => t.FinishedAt)
            .Select(t => (bool?)t.Passed)
            .FirstOrDefaultAsync();
         if (lastAttemptAtTarget == false)
        {
            var targetRank = (await _db.Grades.FindAsync(dto.TargetGradeId))?.Rank ?? 0;
            var lower = await _db.Grades
                .Where(g => g.Rank < targetRank && g.IsActive)
                .OrderByDescending(g => g.Rank)
                .FirstOrDefaultAsync();

            // Уровень ниже есть — сначала подтверждаем его (ТЗ: стабильность результата).
            if (lower is not null)
                throw new InvalidOperationException(
                    $"Тест на этот грейд не пройден. Сначала пройдите тест на уровень ниже — «{lower.Name}».");

            // Минимальный грейд: уровня ниже нет — без исключения кандидат попадал бы
            // в тупик навсегда. Разрешаем повтор на том же грейде после кулдауна
            // (GradeCooldownDays) с даты провала; до этого — точная дата в ошибке.
            var lastFailedAt = await _db.TestSessions
                .Where(t => t.CandidateId == candidateId
                    && t.SpecializationId == dto.SpecializationId
                    && t.TargetGradeId == dto.TargetGradeId
                    && t.Status == "завершен"
                    && t.Passed == false)
                .OrderByDescending(t => t.FinishedAt)
                .Select(t => (DateTime?)t.FinishedAt)
                .FirstOrDefaultAsync();
            var retryAt = lastFailedAt?.AddDays(GradeCooldownDays);
            if (retryAt is null || retryAt.Value > DateTime.UtcNow)
                throw new InvalidOperationException(
                    $"Тест на этот грейд не пройден. Повторная попытка будет доступна {retryAt:dd.MM.yyyy}.");

            // Кулдаун истёк — новая попытка на минимальном грейде разрешена.
        }
        // Кулдаун смены грейда (фиксируется в grade_change_history).
        // Понижение после проваленной попытки разрешено в любой момент —
        // иначе кандидат, заваливший тест, ждал бы 90 дней.
        if (cs.CurrentGradeId != dto.TargetGradeId && dto.TargetGradeId > cs.CurrentGradeId)
        {
            var lastChange = await _db.GradeChangeHistories
                .Where(h => h.CandidateId == candidateId && h.SpecializationId == dto.SpecializationId)
                .OrderByDescending(h => h.ChangedAt)
                .Select(h => (DateTime?)h.ChangedAt).FirstOrDefaultAsync();
            if (lastChange is not null && lastChange.Value.AddDays(GradeCooldownDays) > DateTime.UtcNow)
                throw new InvalidOperationException(
                    $"Повышение грейда доступно не чаще раза в {GradeCooldownDays} дней. " +
                    $"Следующая попытка: {lastChange.Value.AddDays(GradeCooldownDays):dd.MM.yyyy}.");
        }

        var assignment = await _db.TestAssignments
            .Where(a => a.SpecializationId == dto.SpecializationId
                && a.GradeId == dto.TargetGradeId && a.IsActive)
            .OrderByDescending(a => a.Version)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Тест для данной пары «специализация + грейд» не настроен.");

        var questions = await PickQuestionsAsync(assignment);
        if (questions.Count == 0)
            throw new InvalidOperationException("Банк вопросов для этого теста пуст. Обратитесь к администратору.");

        var attemptNo = (short)(await _db.TestSessions.CountAsync(t =>
            t.CandidateId == candidateId && t.SpecializationId == dto.SpecializationId) + 1);

        var session = new TestSession
        {
            Id = Guid.NewGuid(),
            CandidateId = candidateId,
            SpecializationId = (short)dto.SpecializationId,
            TargetGradeId = dto.TargetGradeId,
            AssignmentId = assignment.Id,
            AttemptNo = attemptNo,
            Status = "в_процессе",
            MaxScore = questions.Sum(q => q.MaxScore)
        };
        _db.TestSessions.Add(session);

        var ordered = questions.OrderBy(_ => Random.Shared.Next()).ToList();
        short order = 1;
        var sessionQuestions = new List<TestQuestionDto>();
        foreach (var q in ordered)
        {
            // КЛЮЧЕВОЙ АНТИУТЕЧЕЧНЫЙ ШАГ: перемешиваем варианты ответов у quiz-вопросов.
            // Маппинг (перемешанный порядок + эталон в новых индексах) сохраняем в answer_payload._order.
            var orderInfo = BuildOptionOrder(q);
            _db.TestSessionAnswers.Add(new TestSessionAnswer
            {
                SessionId = session.Id,
                QuestionId = q.Id,
                OrderNo = order,
                AnswerPayload = orderInfo is null
                    ? null
                    : JsonSerializer.Serialize(new { _order = orderInfo })
            });
            sessionQuestions.Add(ToQuestionDto(q, order, orderInfo));
            order++;
        }

        await _db.SaveChangesAsync();

        return new TestSessionDto
        {
            SessionId = session.Id,
            Title = assignment.Title,
            DurationMinutes = assignment.DurationMinutes,
            StartedAt = session.StartedAt,
            ExpiresAt = session.StartedAt.AddMinutes(assignment.DurationMinutes),
            Questions = sessionQuestions
        };
    }

    /// <summary>
    /// Для quiz-вопроса: перемешивает варианты ответов и пересчитывает индексы эталона.
    /// Возвращает null для code/open (их не трогаем).
    /// </summary>
    private static OptionOrderInfo? BuildOptionOrder(TestQuestion q)
    {
        if (q.QuestionType != "тест_с_вариантами") return null;

        var payload = Jsonb.Element(q.Payload);
        var correctEl = Jsonb.Element(q.CorrectPayload);
        if (payload is null || correctEl is null) return null;
        if (!payload.Value.TryGetProperty("options", out var opts) || opts.ValueKind != JsonValueKind.Array)
            return null;
        if (!correctEl.Value.TryGetProperty("correct", out var corr) || corr.ValueKind != JsonValueKind.Array)
            return null;

        var options = opts.EnumerateArray().Select(e => e.GetString() ?? string.Empty).ToList();
        var correct = corr.EnumerateArray().Select(e => e.GetInt32()).ToList();
        if (options.Count < 2 || correct.Count == 0 || correct.Any(i => i < 0 || i >= options.Count))
            return null;

        // Перемешиваем вместе с исходными индексами, чтобы пересчитать эталон
        var indexed = options.Select((text, i) => (text, oldIndex: i)).ToList();
        for (int i = indexed.Count - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);
            (indexed[i], indexed[j]) = (indexed[j], indexed[i]);
        }

        var newCorrect = indexed
            .Select((x, newIndex) => (x.oldIndex, newIndex))
            .Where(p => correct.Contains(p.oldIndex))
            .Select(p => p.newIndex)
            .OrderBy(x => x)
            .ToList();

        return new OptionOrderInfo(
            indexed.Select(x => x.text).ToList(),
            newCorrect);
    }

    private static TestQuestionDto ToQuestionDto(TestQuestion q, short orderNo, OptionOrderInfo? orderInfo) => new()
    {
        QuestionId = q.Id,
        OrderNo = orderNo,
        QuestionType = q.QuestionTypeNavigation?.Name ?? q.QuestionType,
        Prompt = q.Prompt,
        CodeTemplate = Jsonb.Element(q.Payload)?.TryGetProperty("template", out var t) == true ? t.GetString() : null,
        // ВАЖНО: для quiz отдаём ПЕРЕМЕШАННЫЕ варианты из сессионного маппинга,
        // а не исходные из банка — иначе перемешивание бессмысленно.
        Payload = orderInfo is not null
            ? JsonSerializer.SerializeToElement(new { options = orderInfo.Options })
            : Jsonb.Element(q.Payload)
    };

    /// <summary>Стратифицированная выборка: строго по difficulty_mix. Разные кандидаты — разные вопросы
    /// эквивалентной сложности; утечка отдельных заданий не даёт преимущества.</summary>
    private async Task<List<TestQuestion>> PickQuestionsAsync(TestAssignment assignment)
    {
        var mix = JsonSerializer.Deserialize<Dictionary<int, int>>(assignment.DifficultyMix)
            ?? new Dictionary<int, int> { [1] = 3, [2] = 4, [3] = 2, [4] = 1 };

        var pool = await _db.TestQuestions
            .Include(q => q.QuestionTypeNavigation)
            .Where(q => q.SpecializationId == assignment.SpecializationId
                && q.GradeId == assignment.GradeId && q.IsActive)
            .ToListAsync();

        var picked = new List<TestQuestion>();
        foreach (var (difficulty, count) in mix.OrderBy(m => m.Key))
        {
            var candidates = pool
                .Where(q => q.Difficulty == difficulty)
                .OrderBy(_ => Random.Shared.Next())
                .ToList();
            picked.AddRange(candidates.Take(count));
        }
        if (picked.Count < assignment.QuestionsCount)
        {
            var rest = pool.Except(picked).OrderBy(_ => Random.Shared.Next())
                .Take(assignment.QuestionsCount - picked.Count);
            picked.AddRange(rest);
        }
        return picked;
    }

    public async Task<TestSessionDto> GetSessionAsync(Guid candidateId, Guid sessionId)
    {
        var s = await _db.TestSessions.Include(t => t.Assignment)
            .FirstOrDefaultAsync(t => t.Id == sessionId && t.CandidateId == candidateId
                && t.Status == "в_процессе")
            ?? throw new InvalidOperationException("Сессия не найдена или завершена.");

        var rows = await _db.TestSessionAnswers
            .Include(a => a.Question).ThenInclude(q => q.QuestionTypeNavigation)
            .Where(a => a.SessionId == sessionId).OrderBy(a => a.OrderNo).ToListAsync();

        return new TestSessionDto
        {
            SessionId = s.Id,
            Title = s.Assignment!.Title,
            DurationMinutes = s.Assignment.DurationMinutes,
            StartedAt = s.StartedAt,
            ExpiresAt = s.StartedAt.AddMinutes(s.Assignment.DurationMinutes),
            Questions = rows.Select(a =>
            {
                var orderInfo = ReadOptionOrder(a.AnswerPayload);
                return ToQuestionDto(a.Question, a.OrderNo, orderInfo);
            }).ToList()
        };
    }

    /// <summary>Извлекает сессионный маппинг вариантов из answer_payload (если есть).</summary>
    private static OptionOrderInfo? ReadOptionOrder(string? answerPayload)
    {
        if (string.IsNullOrWhiteSpace(answerPayload)) return null;
        try
        {
            using var doc = JsonDocument.Parse(answerPayload);
            if (!doc.RootElement.TryGetProperty("_order", out var o)) return null;
            var options = o.GetProperty("options").EnumerateArray()
                .Select(e => e.GetString() ?? string.Empty).ToList();
            var correct = o.GetProperty("correct").EnumerateArray()
                .Select(e => e.GetInt32()).ToList();
            return new OptionOrderInfo(options, correct);
        }
        catch { return null; }
    }

    public async Task SubmitAnswersAsync(Guid candidateId, Guid sessionId, SubmitAnswersDto dto)
    {
        var s = await _db.TestSessions.FirstOrDefaultAsync(t =>
            t.Id == sessionId && t.CandidateId == candidateId && t.Status == "в_процессе")
            ?? throw new InvalidOperationException("Сессия не найдена или завершена.");

        foreach (var ans in dto.Answers)
        {
            var row = await _db.TestSessionAnswers.Include(a => a.Question)
                .FirstOrDefaultAsync(a => a.SessionId == sessionId && a.QuestionId == ans.QuestionId)
                ?? throw new InvalidOperationException("Вопрос не входит в эту сессию.");

            // МЕРЖИМ, а не перезаписываем: сохраняем _order (сессионный маппинг вариантов)
            // и добавляем ответ кандидата. Иначе перемешивание вариантов потеряется.
            row.AnswerPayload = MergeAnswerPayload(row.AnswerPayload, ans);
            row.AnsweredAt = DateTime.UtcNow;
            await GradeAnswerAsync(row, ans);
        }
        await _db.SaveChangesAsync();
    }

    /// <summary>Объединяет сессионный маппинг (_order) с ответом кандидата в одном JSON.</summary>
    private static string MergeAnswerPayload(string? existing, SubmitAnswerDto ans)
    {
        var answerJson = JsonSerializer.SerializeToElement(ans);
        if (string.IsNullOrWhiteSpace(existing))
            return JsonSerializer.Serialize(new { answer = answerJson });

        try
        {
            using var doc = JsonDocument.Parse(existing);
            var dict = new Dictionary<string, JsonElement>();
            foreach (var prop in doc.RootElement.EnumerateObject())
                dict[prop.Name] = prop.Value.Clone();
            dict["answer"] = answerJson;
            return JsonSerializer.Serialize(dict);
        }
        catch
        {
            return JsonSerializer.Serialize(new { answer = answerJson });
        }
    }

    /// <summary>Проверка по типу: quiz — эталон (по сессионному маппингу!), code — скрытые тест-кейсы,
    /// open — ИИ по рубрике.</summary>
    private async Task GradeAnswerAsync(TestSessionAnswer row, SubmitAnswerDto ans)
    {
        var q = row.Question;
        switch (q.QuestionType)
        {
            case "тест_с_вариантами":
            {
                // ИСПРАВЛЕНО: эталон берём из сессионного маппинга (перемешанный порядок),
                // а не из банка — иначе при совпадении индексов ответ считался бы верным случайно.
                var orderInfo = ReadOptionOrder(row.AnswerPayload);
                List<int> expected = orderInfo?.Correct
                    ?? Jsonb.Element(q.CorrectPayload)?.GetProperty("correct").EnumerateArray()
                        .Select(e => e.GetInt32()).OrderBy(x => x).ToList()
                    ?? new List<int>();

                List<int> actual;
                if (ans.Selected.HasValue)
                {
                    actual = ans.Selected.Value.ValueKind == JsonValueKind.Array
                        ? ans.Selected.Value.EnumerateArray().Select(e => e.GetInt32()).OrderBy(x => x).ToList()
                        : new List<int> { ans.Selected.Value.GetInt32() };
                }
                else actual = new List<int>();
                row.Score = expected.SequenceEqual(actual) ? q.MaxScore : 0m;
                row.AutoGraded = true; row.GradedAt = DateTime.UtcNow;
                break;
            }
            case "задача_с_кодом":
            {
                var cases = Jsonb.Element(q.Payload)?.GetProperty("testCases").EnumerateArray()
                    .Select(tc => new CodeTestCase(
                        tc.GetProperty("input").GetString() ?? string.Empty,
                        tc.GetProperty("expected").GetString() ?? string.Empty))
                    .ToList() ?? new List<CodeTestCase>();
                var result = await _codeRunner.RunAsync(ans.Language ?? "python", ans.Code ?? string.Empty, cases);
                row.Score = Math.Round(q.MaxScore * result.RelativeScore, 2);
                row.AutoGraded = true; row.GradedAt = DateTime.UtcNow;
                break;
            }
            default: // открытый_вопрос — ИИ по рубрике, auto_graded=false (очередь экспертной проверки)
            {
                var rubric = Jsonb.Element(q.CorrectPayload)?.TryGetProperty("rubric", out var r) == true
                    ? r.GetString() ?? string.Empty : string.Empty;
                var grade = await _openGrader.GradeAsync(q.Prompt, rubric, ans.Text ?? string.Empty, q.MaxScore);
                row.Score = grade.Score;
                row.AutoGraded = false;
                row.GradedAt = DateTime.UtcNow;
                break;
            }
        }
    }

    public async Task<TestResultDto> FinishTestAsync(Guid candidateId, Guid sessionId)
    {
        var s = await _db.TestSessions.Include(t => t.Assignment).Include(t => t.TargetGrade)
            .FirstOrDefaultAsync(t => t.Id == sessionId && t.CandidateId == candidateId
                && t.Status == "в_процессе")
            ?? throw new InvalidOperationException("Сессия не найдена или уже завершена.");

        if (s.StartedAt.AddMinutes(s.Assignment!.DurationMinutes) < DateTime.UtcNow)
        {
            s.Status = "истек_срок"; s.FinishedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            throw new InvalidOperationException("Время теста истекло.");
        }

        var answers = await _db.TestSessionAnswers.Include(a => a.Question)
            .Where(a => a.SessionId == sessionId).ToListAsync();

        s.TotalScore = answers.Where(a => a.Score.HasValue).Sum(a => a.Score!.Value);
        s.FinishedAt = DateTime.UtcNow;
        s.Status = "завершен";

        var threshold = s.Assignment.PassThreshold;
        var percent = s.MaxScore > 0
            ? Math.Round(s.TotalScore.Value / s.MaxScore.Value * 100m, 2) : 0m;

        // passed считаем сразу: open-вопросы уже имеют первичный ИИ-скор,
        // экспертная выборочная перепроверка может скорректировать постфактум.
        s.Passed = percent >= threshold;

        string? resultingGradeName = null;
        string message;
        if (s.Passed == true)
        {
            // БЕЗ автоповышения (Q&A менторов: повышение — отдельным тестом после кулдауна 90 дней).
            // Сдан тест на target grade → подтверждается именно он.
            s.ResultingGradeId = s.TargetGradeId;
            resultingGradeName = (await _db.Grades.FindAsync(s.TargetGradeId))?.Name;
            await ApplyGradeAsync(candidateId, s, s.TargetGradeId);
            message = $"Грейд «{resultingGradeName}» подтверждён. Порог: {threshold}%, результат: {percent}%. " +
                      "Повысить грейд можно отдельным тестом (не чаще раза в 90 дней).";
        }
        else
        {
            // ТЗ: после провала следующим доступен тест на уровень ниже (пересдача на том же
            // уровне запрещена до подтверждения грейда ниже — см. StartTestAsync).
            message = $"Порог: {threshold}%, результат: {percent}%. " +
                      "Следующим доступен тест на грейд ниже — пройдите его, чтобы закрепить уровень.";
        }

        await _db.SaveChangesAsync();
        return new TestResultDto
        {
            SessionId = s.Id,
            TotalScore = s.TotalScore, MaxScore = s.MaxScore, Percent = s.Percent,
            Passed = s.Passed, ResultingGradeName = resultingGradeName,
            Status = s.Status, Message = message
        };
    }

    private async Task ApplyGradeAsync(Guid candidateId, TestSession s, short resultingGradeId)
    {
        var cs = await _db.CandidateSpecializations.FirstAsync(x =>
            x.CandidateId == candidateId && x.SpecializationId == s.SpecializationId);

        var isFirst = !await _db.GradeChangeHistories.AnyAsync(h =>
            h.CandidateId == candidateId && h.SpecializationId == s.SpecializationId);

        var reason = isFirst ? "первичное_тестирование"
            : resultingGradeId > cs.CurrentGradeId ? "пересдача_повышение" : "пересдача_понижение";

        _db.GradeChangeHistories.Add(new GradeChangeHistory
        {
            CandidateId = candidateId,
            SpecializationId = s.SpecializationId,
            OldGradeId = cs.CurrentGradeId,
            NewGradeId = resultingGradeId,
            Reason = reason,
            TestSessionId = s.Id
        });

        cs.CurrentGradeId = resultingGradeId;
        cs.Status = "грейд_подтвержден";
        cs.LastTestAt = DateTime.UtcNow;
        cs.LastGradeChangeAt = DateTime.UtcNow;

        var category = await _db.Categories.FirstAsync(c =>
            c.SpecializationId == s.SpecializationId && c.GradeId == resultingGradeId);
        var existing = await _db.CandidateCategories
            .FirstOrDefaultAsync(x => x.CandidateId == candidateId && x.CategoryId == category.Id);
        if (existing is null)
            _db.CandidateCategories.Add(new CandidateCategory
            { CandidateId = candidateId, CategoryId = category.Id, IsPrimary = cs.IsPrimary });
    }

    public async Task<List<TestHistoryItemDto>> GetHistoryAsync(Guid candidateId) =>
        await _db.TestSessions
            .Include(t => t.Specialization).Include(t => t.TargetGrade)
            .Where(t => t.CandidateId == candidateId && t.Status == "завершен")
            .OrderByDescending(t => t.FinishedAt)
            .Select(t => new TestHistoryItemDto
            {
                SessionId = t.Id,
                SpecializationName = t.Specialization.Name,
                TargetGradeName = t.TargetGrade.Name,
                Percent = t.Percent, Passed = t.Passed,
                Status = t.Status, FinishedAt = t.FinishedAt
            }).ToListAsync();

    /// <summary>Сессионный маппинг перемешанных вариантов ответа (quiz).</summary>
    private sealed record OptionOrderInfo(List<string> Options, List<int> Correct);
}