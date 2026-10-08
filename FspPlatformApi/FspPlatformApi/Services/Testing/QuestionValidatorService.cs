using System.Text.Json;
using FspPlatformApi.Services.Interfaces.Testing;

namespace FspPlatformApi.Services.Testing;

/// <summary>
/// Валидатор банка вопросов ПЕРЕД вставкой в БД. Порт tools/validate-questions.mjs.
/// Проверяет: эталон quiz (1 правильный, индексы в диапазоне, нет дублей вариантов),
/// рубрики open, max_score &gt; 0, и прогоняет эталонные решения code-задач по тест-кейсам
/// через ICodeRunnerService (изолированный Docker-раннер — как продовый прогон кода кандидатов).
/// </summary>
public class QuestionValidatorService : IQuestionValidatorService
{
    private readonly ICodeRunnerService _codeRunner;
    private readonly ILogger<QuestionValidatorService> _log;

    public QuestionValidatorService(ICodeRunnerService codeRunner, ILogger<QuestionValidatorService> log)
        => (_codeRunner, _log) = (codeRunner, log);

    public async Task<ValidationReport> ValidateAsync(GeneratedQuestionBatch batch, CancellationToken ct = default)
    {
        var report = new ValidationReport();
        var codeBlockNum = 0;

        foreach (var q in batch.Questions)
        {
            report.Checked++;
            ValidateRow(report, report.Checked, q);

            if (q.QuestionType == "задача_с_кодом")
            {
                codeBlockNum++;
                await ValidateCodeBlockAsync(report, codeBlockNum, q, ct);
            }
        }

        return report;
    }

    // ---------------------------------------------------------------------------
    // Проверки одной строки (quiz + open + max_score)
    // ---------------------------------------------------------------------------
    private static void ValidateRow(ValidationReport report, int n, GeneratedQuestion q)
    {
        if (q.Payload.TryGetValue("options", out var optsEl) && optsEl.ValueKind == JsonValueKind.Array &&
            q.CorrectPayload.TryGetValue("correct", out var corrEl) && corrEl.ValueKind == JsonValueKind.Array)
        {
            var options = optsEl.EnumerateArray().Select(o => o.GetString() ?? "").ToList();
            var correct = corrEl.EnumerateArray()
                .Where(c => c.ValueKind == JsonValueKind.Number)
                .Select(c => c.GetInt32()).ToList();

            if (correct.Count != 1)
                report.Errors.Add($"вопрос {n}: у quiz должен быть ровно 1 правильный вариант, найдено {correct.Count}");
            foreach (var i in correct)
                if (i < 0 || i >= options.Count)
                    report.Errors.Add($"вопрос {n}: индекс правильного ответа {i} вне диапазона 0..{options.Count - 1}");

            var lower = options.Select(o => o.ToLowerInvariant().Trim()).ToList();
            if (lower.Distinct().Count() != lower.Count)
                report.Errors.Add($"вопрос {n}: есть дублирующиеся варианты ответа");
        }

        if (q.CorrectPayload.TryGetValue("rubric", out var rubEl) && rubEl.ValueKind == JsonValueKind.String)
        {
            var rubric = rubEl.GetString() ?? "";
            var criteria = rubric.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
            if (criteria.Count < 2)
                report.Warnings.Add($"вопрос {n}: рубрика содержит <2 критериев — проверьте вручную");
            foreach (var c in criteria)
                if (!c.Contains(';'))
                    report.Warnings.Add($"вопрос {n}: критерий без индикаторов через \";\": \"{Truncate(c, 50)}\"");
        }

        if (!(q.MaxScore > 0))
            report.Errors.Add($"вопрос {n}: max_score должен быть > 0");
    }

    // ---------------------------------------------------------------------------
    // code-задачи: эталон прогоняем по тест-кейсам в Docker (как у кандидатов)
    // ---------------------------------------------------------------------------
    private async Task ValidateCodeBlockAsync(ValidationReport report, int blockNum, GeneratedQuestion q, CancellationToken ct)
    {
        var lang = (GetString(q.Payload, "language") ?? "python").ToLowerInvariant();
        var reference = GetString(q.Payload, "reference");

        if (string.IsNullOrEmpty(reference))
        {
            report.Warnings.Add($"code-блок {blockNum}: нет эталонного решения (payload.reference) — прогон пропущен");
            return;
        }

        var testCases = GetTestCases(q.Payload);
        if (testCases.Count == 0)
        {
            report.Warnings.Add($"code-блок {blockNum}: нет тест-кейсов (payload.testCases) — прогон пропущен");
            return;
        }

        try
        {
            var result = await _codeRunner.RunAsync(lang, reference, testCases, ct);

            if (result.Error is not null)
                report.Errors.Add($"code-блок {blockNum} ({lang}): эталон упал: {Truncate(result.Error, 200)}");
            else if (result.Passed != result.Total)
                report.Errors.Add(
                    $"code-блок {blockNum} ({lang}): эталон прошёл {result.Passed}/{result.Total} тест-кейсов");
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Прогон эталона code-блока {BlockNum} провалился", blockNum);
            report.Errors.Add($"code-блок {blockNum} ({lang}): раннер недоступен: {Truncate(ex.Message, 200)}");
        }
    }

    // ---------------------------------------------------------------------------
    // Утилиты
    // ---------------------------------------------------------------------------
    private static string? GetString(Dictionary<string, JsonElement> dict, string key)
        => dict.TryGetValue(key, out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;

    private static List<CodeTestCase> GetTestCases(Dictionary<string, JsonElement> payload)
    {
        var list = new List<CodeTestCase>();
        if (!payload.TryGetValue("testCases", out var el) || el.ValueKind != JsonValueKind.Array)
            return list;
        foreach (var tc in el.EnumerateArray())
        {
            var input = tc.TryGetProperty("input", out var i) && i.ValueKind == JsonValueKind.String ? i.GetString() ?? "" : "";
            var expected = tc.TryGetProperty("expected", out var e) && e.ValueKind == JsonValueKind.String ? e.GetString() ?? "" : "";
            list.Add(new CodeTestCase(input, expected));
        }
        return list;
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
}