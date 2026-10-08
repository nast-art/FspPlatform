using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FspPlatformApi.Services.Interfaces.Testing;

namespace FspPlatformApi.Services.Testing;

/// <summary>
/// ИИ-проверяльщик open-вопросов: первичный скор строго по зафиксированной рубрике.
/// Усиление стабильности: в системный промпт встроены few-shot якоря (слабый/средний/сильный ответ → балл),
/// чтобы модель калибровала шкалу, а не гадала. Если ключ не задан — эвристический грейдер.
/// В любом случае auto_graded=false и ответ попадает в выборочную экспертную перепроверку.
/// </summary>
public class OpenAnswerGrader : IOpenAnswerGrader
{
    private readonly IConfiguration _cfg;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<OpenAnswerGrader> _logger;

    public OpenAnswerGrader(IConfiguration cfg, IHttpClientFactory httpFactory, ILogger<OpenAnswerGrader> logger)
        => (_cfg, _httpFactory, _logger) = (cfg, httpFactory, logger);

    // Few-shot якоря шкалы: примеры калибровки «ответ → балл» для max_score = 2.
    // Подбираются командой на эталонной разметке; здесь — разумные значения по умолчанию.
    private const string ScaleAnchors = """
        Примеры калибровки шкалы (для MAX=2):
        СЛАБЫЙ (0-0.5): общие слова без механизма ("нужно оптимизировать запрос").
        СРЕДНИЙ (1-1.5): верная идея, но без деталей ("добавить индекс по часто фильтруемому полю").
        СИЛЬНЫЙ (1.5-2): конкретный механизм + проверка ("EXPLAIN ANALYZE, составной индекс по (status, created_at), проверка seq scan").
        Оценивай строго по этой шкале и рубрике, не щедрее СИЛЬНОГО примера и не строже СЛАБОГО без причины.
        """;

    public async Task<OpenGradeResult> GradeAsync(string prompt, string rubric, string answer,
        decimal maxScore, CancellationToken ct = default)
    {
        var apiKey = _cfg["OpenAI:ApiKey"];
        if (string.IsNullOrEmpty(apiKey)) return HeuristicGrade(rubric, answer, maxScore);

        try
        {
            return await AiGradeAsync(apiKey, prompt, rubric, answer, maxScore, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ИИ-грейдер недоступен, fallback на эвристику");
            return HeuristicGrade(rubric, answer, maxScore);
        }
    }

    private async Task<OpenGradeResult> AiGradeAsync(string apiKey, string prompt, string rubric,
        string answer, decimal maxScore, CancellationToken ct)
    {
        // ИЗМЕНЕНО: именованный клиент с DoH-обходом DNS (регистрация в Program.cs),
        // а не голый CreateClient() — иначе запросы к Gemini идут через блокируемый DNS.
        var client = _httpFactory.CreateClient("ai");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var requestBody = new
        {
            model = _cfg["OpenAI:Model"] ?? "gpt-4o-mini",
            temperature = 0.2,              // низкая температура — стабильность скоров
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content =
                        "Ты — строгий экспертный проверяющий. Оцени ответ кандидата ТОЛЬКО по рубрике. " +
                        "Верни JSON: {\"score\": число от 0 до MAX, \"feedback\": краткое обоснование}. " +
                        $"MAX = {maxScore}. Не придумывай критерии вне рубрики.\n" +
                        ScaleAnchors
                },
                new
                {
                    role = "user",
                    content = $"ВОПРОС:\n{prompt}\n\nРУБРИКА:\n{rubric}\n\nОТВЕТ КАНДИДАТА:\n{answer}"
                }
            }
        };

        var baseUrl = (_cfg["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1").TrimEnd('/');
        using var response = await client.PostAsync($"{baseUrl}/chat/completions",
            new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"), ct);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var content = doc.RootElement.GetProperty("choices")[0]
            .GetProperty("message").GetProperty("content").GetString() ?? "{}";
        using var grade = JsonDocument.Parse(content);

        var score = grade.RootElement.TryGetProperty("score", out var s) && s.TryGetDecimal(out var dec)
            ? Math.Clamp(dec, 0m, maxScore) : 0m;
        var feedback = grade.RootElement.TryGetProperty("feedback", out var f) ? f.GetString() ?? "" : "";
        return new OpenGradeResult(score, feedback, "ai");
    }

    /// <summary>Эвристика без ИИ: рубрика — список критериев (каждый со словами-индикаторами через ;).
    /// Балл = доля выполненных критериев × maxScore. Честный fallback, не генерация.</summary>
    private static OpenGradeResult HeuristicGrade(string rubric, string answer, decimal maxScore)
    {
        if (string.IsNullOrWhiteSpace(answer)) return new OpenGradeResult(0m, "Пустой ответ.", "heuristic");
        var answerLower = answer.ToLowerInvariant();
        var criteria = rubric.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (criteria.Length == 0)
            return new OpenGradeResult(0m, "Рубрика не задана — требуется экспертная проверка.", "heuristic");

        var met = criteria.Count(c =>
        {
            var indicators = c.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return indicators.Any(ind => answerLower.Contains(ind.ToLowerInvariant()));
        });
        var score = Math.Round(maxScore * met / criteria.Length, 2);
        return new OpenGradeResult(score, $"Выполнено критериев рубрики: {met}/{criteria.Length}.", "heuristic");
    }
}