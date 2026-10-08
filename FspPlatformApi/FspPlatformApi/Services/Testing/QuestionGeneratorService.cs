using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FspPlatformApi.Services.Interfaces.Testing;

namespace FspPlatformApi.Services.Testing;

/// <summary>
/// Генератор банка вопросов через OpenAI-совместимый API GigaChat (SberCloud).
/// HttpClient — именованный "ai" (проверка сертификата отключена callback'ом
/// в Program.cs — корни Минцифры не установлены, см. документацию).
/// Авторизация: OAuth access_token от GigaChatTokenProvider (кэшируется, Bearer).
/// Порт tools/generate-questions.mjs + точный difficulty_mix из test_assignments.
/// Типы вопросов приведены к справочнику statuses:
/// тест_с_вариантами | задача_с_кодом | открытый_вопрос.
/// Изоморфные группы: правило 9 требует 2-3 варианта на одну компетенцию
/// (одинаковый group_index) — testing_design.md: утечка одного варианта
/// не раскрывает остальные.
/// Устойчивость: max_tokens поднят (иначе GigaChat обрезает JSON),
/// markdown-обёртки ```json срезаются, при битом JSON — понятная ошибка с превью.
///
/// Правило 5 (мультиязычность): code-задачи ротируют языки раннера
/// python | javascript | csharp | java | go | cpp; для каждого языка задан
/// паттерн чтения stdin — эталон без чтения stdin не пройдёт скрытые тесты.
/// </summary>
public class QuestionGeneratorService : IQuestionGeneratorService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _cfg;
    private readonly ILogger<QuestionGeneratorService> _log;
    private readonly GigaChatTokenProvider _tokenProvider;

    public QuestionGeneratorService(IHttpClientFactory httpFactory, IConfiguration cfg,
        ILogger<QuestionGeneratorService> log, GigaChatTokenProvider tokenProvider)
        => (_httpFactory, _cfg, _log, _tokenProvider) = (httpFactory, cfg, log, tokenProvider);

    public async Task<GeneratedQuestionBatch> GenerateAsync(string specialization, string grade,
        IReadOnlyDictionary<int, int>? difficultyMix = null, CancellationToken ct = default)
    {
        var prompt = BuildPrompt(specialization, grade, difficultyMix);
        var raw = await CallApiWithRetryAsync(prompt, ct);

        // срезаем markdown-обёртки и берём чистую JSON-подстроку
        var json = ExtractJson(raw);

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            // модель обрезала ответ или вернула не JSON — показываем превью для диагностики
            var preview = json.Length > 500 ? json[..500] + "..." : json;
            throw new InvalidOperationException(
                $"Модель вернула невалидный JSON ({ex.Message}). Начало ответа: {preview}", ex);
        }

        using (doc)
        {
            var root = doc.RootElement;
            var questionsEl = root.TryGetProperty("questions", out var q) ? q : root;

            var batch = new GeneratedQuestionBatch();
            var groupIds = new Dictionary<int, Guid>();

            foreach (var item in questionsEl.EnumerateArray())
            {
                var groupIndex = item.TryGetProperty("group_index", out var gi) && gi.ValueKind == JsonValueKind.Number
                    ? gi.GetInt32()
                    : batch.Questions.Count + 1;
                if (!groupIds.TryGetValue(groupIndex, out var groupId))
                    groupIds[groupIndex] = groupId = Guid.NewGuid();

                var type = item.TryGetProperty("question_type", out var qt) ? qt.GetString() ?? "" : "";

                batch.Questions.Add(new GeneratedQuestion
                {
                    GroupIndex = groupIndex,
                    QuestionGroup = groupId,
                    QuestionType = type,
                    Difficulty = item.TryGetProperty("difficulty", out var d) && d.ValueKind == JsonValueKind.Number
                        ? d.GetInt32() : 3,
                    Prompt = item.TryGetProperty("prompt", out var p) ? p.GetString() ?? "" : "",
                    Payload = DeserializeDict(item, "payload"),
                    CorrectPayload = DeserializeDict(item, "correct_payload"),
                    MaxScore = item.TryGetProperty("max_score", out var ms) && ms.ValueKind == JsonValueKind.Number
                        ? ms.GetInt32()
                        : type switch
                        {
                            "задача_с_кодом" => 3,
                            "открытый_вопрос" => 2,
                            _ => 1
                        }
                });
            }

            _log.LogInformation("Сгенерировано {Count} вопросов для {Spec}/{Grade} (mix: {Mix})",
                batch.Questions.Count, specialization, grade,
                difficultyMix is null ? "по умолчанию 6:2:2" : string.Join(",", difficultyMix.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}:{kv.Value}")));
            return batch;
        }
    }

    // ------------------------------------------------------------------
    // Вызов API с повторными попытками (как в generate-questions.mjs: 3 попытки, backoff)
    // ------------------------------------------------------------------
    private async Task<string> CallApiWithRetryAsync(string prompt, CancellationToken ct)
    {
        var client = _httpFactory.CreateClient("ai");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await _tokenProvider.GetAccessTokenAsync(ct));

        var requestBody = new
        {
            model = _cfg["OpenAI:Model"] ?? "GigaChat",
            temperature = 0.7,
            // без этого GigaChat обрезает ответ ~500 токенами и JSON приходит незакрытым
            max_tokens = 8000,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = "Отвечай только валидным JSON." },
                new { role = "user", content = prompt }
            }
        };

        Exception? lastError = null;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                var baseUrl = (_cfg["OpenAI:BaseUrl"] ?? "https://gigachat.devices.sberbank.ru/api/v1").TrimEnd('/');
                using var resp = await client.PostAsync($"{baseUrl}/chat/completions",
                    new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"), ct);
                if (!resp.IsSuccessStatusCode)
                {
                    var errBody = await resp.Content.ReadAsStringAsync(ct);
                    var cut = Math.Min(200, errBody.Length);
                    throw new InvalidOperationException($"HTTP {(int)resp.StatusCode}: {errBody[..cut]}");
                }

                using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
                return doc.RootElement.GetProperty("choices")[0]
                    .GetProperty("message").GetProperty("content").GetString() ?? "{}";
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastError = ex;
                _log.LogWarning("Попытка {Attempt}/3 неудачна: {Message}", attempt, ex.Message);
                if (attempt < 3) await Task.Delay(2000 * attempt, ct);
            }
        }

        throw new InvalidOperationException($"ИИ API недоступен после 3 попыток: {lastError?.Message}");
    }

    // ------------------------------------------------------------------
    // Промпт — логика из buildPrompt() generate-questions.mjs + точный difficulty_mix.
    // ОБЫЧНЫЙ raw string БЕЗ '$': в содержимом есть "{{" подряд,
    // интерполяция запрещена — подстановка через .Replace() с токенами __TOKEN__.
    // Правила сформулированы максимально буквально (GigaChat читает их дословно):
    // отдельным пунктом запрещено >1 правильного ответа и отсутствие эталона.
    // Правило 9: каждая компетенция — семейство из 2-3 изоморфных вариантов
    // (одинаковый group_index) — защита от утечек заданий.
    // Коды типов: тест_с_вариантами | задача_с_кодом | открытый_вопрос.
    // Правило 5: ротация языков раннера (python, javascript, csharp, java, go, cpp)
    // и паттерны чтения stdin — эталон должен реально проходить скрытые тесты.
    // ------------------------------------------------------------------
    private static string BuildPrompt(string specialization, string grade,
        IReadOnlyDictionary<int, int>? difficultyMix)
    {
        var focus = grade switch
        {
            "Стажёр" => "абсолютные основы: синтаксис, базовые понятия, простейшие операции",
            "Джуниор" => "практические задачи ежедневной работы без глубокой архитектуры",
            "Миддл" => "самостоятельная работа: оптимизация, паттерны, отладка, проектирование модуля",
            "Сеньор" => "проектирование, трейдоффы, масштабирование, глубокое понимание платформы",
            _ => "профессиональные задачи"
        };

        // Строка с требуемым количеством вопросов каждой сложности — жёсткое ограничение сбора сессии.
        string difficultyRule;
        if (difficultyMix is not null && difficultyMix.Count > 0)
        {
            var total = difficultyMix.Values.Sum();
            difficultyRule =
                $"3. РАСПРЕДЕЛЕНИЕ СЛОЖНОСТИ СТРОГОЕ. Всего ровно {total} вопросов:\n" +
                string.Join("\n", difficultyMix.OrderBy(kv => kv.Key)
                    .Select(kv => $"- ровно {kv.Value} вопрос(а) с difficulty = {kv.Key}"));
        }
        else
        {
            difficultyRule =
                "3. difficulty: суммарно по набору 3×сложность 1-2, 4×сложность 2-3, 2×сложность 3-4, 1×сложность 4-5.";
        }

        const string template = """
            Ты — методолог по оценке ИТ-компетенций. Сгенерируй вопросы для теста "__SPECIALIZATION__" на грейд "__GRADE__".
            Фокус уровня: __FOCUS__.

            Верни СТРОГО JSON (без текста вокруг, без markdown-обёрток, без комментариев):
            {
              "questions": [
                {
                  "group_index": 1,
                  "question_type": "тест_с_вариантами" | "задача_с_кодом" | "открытый_вопрос",
                  "difficulty": 1-5,
                  "prompt": "формулировка",
                  "payload": { "options": ["a","b","c","d"] },
                  "payload": { "language": "python|javascript|csharp|java|go|cpp",
                               "reference": "ЭТАЛОННОЕ РЕШЕНИЕ — читает stdin, пишет stdout",
                               "testCases": [ { "input": "...", "expected": "..." } ] },
                  "payload": {},
                  "correct_payload": { "correct": [индекс] },
                  "correct_payload": { "rubric": "критерий1: слово1;слово2\nкритерий2: слово3;слово4" },
                  "correct_payload": {},
                  "max_score": 1 | 3 | 2
                }
              ]
            }

            ЖЁСТКИЕ ПРАВИЛА (нарушение любого = ответ бракуется):
            1. Все вопросы ЯЗЫКОНЕЗАВИСИМЫЕ: без упоминания конкретного языка программирования, псевдокод/общие понятия. Исключение — задача_с_кодом: в ней явно указывается language.
            2. Варианты одной группы (group_index) — ИЗОМОРФНЫЕ: та же компетенция, другие числа/формулировки, равная сложность. Утечка одного варианта не должна раскрывать остальные.
            __DIFFICULTY_RULE__
            4. У КАЖДОГО тест_с_вариантами поле correct_payload.correct — МАССИВ РОВНО ИЗ ОДНОГО ЦЕЛОГО ЧИСЛА (индекс единственного правильного ответа). НИКОГДА не два и больше индексов. Все варианты в options различны, только один верен, без двусмысленности.
            5. У КАЖДОГО задача_с_кодом обязательно заполнены ВСЕ поля payload: language, reference (полный эталонный код, читает stdin, пишет stdout) и testCases — РОВНО 4 тест-кейса вида {"input": "...", "expected": "..."}. Эталон ОБЯЗАН проходить все 4 кейса.
               Поле language — ОДНО из: "python", "javascript", "csharp", "java", "go", "cpp". ЧЕРЕДУЙ языки между code-задачами в наборе (не ставь один язык на все).
               Эталон ОБЯЗАН читать ввод со stdin по паттерну своего языка:
               - python:      import sys + sys.stdin.read()
               - javascript:  require('fs').readFileSync(0, 'utf8') или readline
               - csharp:      класс Solution, метод Main, Console.In.ReadToEnd() (using System;)
               - java:        класс Solution, метод main, new Scanner(System.in) или BufferedReader
               - go:          package main, bufio.NewReader(os.Stdin), обязательно func main()
               - cpp:         #include <bits/stdc++.h> или нужные заголовки, std::cin / istreambuf_iterator
            6. У КАЖДОГО открытый_вопрос поле correct_payload.rubric содержит НЕ МЕНЕЕ 2 критериев, каждый критерий — строка вида "название: индикатор1;индикатор2".
            7. Соотношение типов: 6 тест_с_вариантами : 2 задача_с_кодом : 2 открытый_вопрос — если это не противоречит правилу 3.
            8. Не повторяй шаблонные вопросы про HTTP-коды и git — бери предметные темы: __SPECIALIZATION__.
            9. group_index: заводи КАЖДУЮ компетенцию семейством из 2-3 вопросов с ОДИНАКОВЫМ group_index. Пример: questions[0].group_index = 1 и questions[1].group_index = 1 — это два изоморфных варианта одной компетенции; questions[2].group_index = 2 и questions[3].group_index = 2 — следующее семейство; и так далее. Общее число вопросов строго по правилу 3, но уникальных group_index будет в 2-2.5 раза меньше числа вопросов.
            """;

        return template
            .Replace("__SPECIALIZATION__", specialization)
            .Replace("__GRADE__", grade)
            .Replace("__FOCUS__", focus)
            .Replace("__DIFFICULTY_RULE__", difficultyRule);
    }

    // ------------------------------------------------------------------
    // УТИЛИТЫ
    // ------------------------------------------------------------------

    /// <summary>
    /// Вытаскивает чистый JSON из ответа модели:
    /// срезает markdown-обёртки ```json ... ``` (модели их любят добавлять)
    /// и берёт подстроку от первой '{' до последней '}' включительно.
    /// </summary>
    private static string ExtractJson(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "{}";

        var s = raw.Trim();

        // срез markdown-обёртки
        if (s.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewline = s.IndexOf('\n');
            if (firstNewline > 0) s = s[(firstNewline + 1)..];
            var lastFence = s.LastIndexOf("```", StringComparison.Ordinal);
            if (lastFence > 0) s = s[..lastFence];
            s = s.Trim();
        }

        var start = s.IndexOf('{');
        var end = s.LastIndexOf('}');
        if (start >= 0 && end > start)
            return s[start..(end + 1)];

        return s;
    }

    private static Dictionary<string, JsonElement> DeserializeDict(JsonElement item, string propName)
    {
        var result = new Dictionary<string, JsonElement>();
        if (!item.TryGetProperty(propName, out var el) || el.ValueKind != JsonValueKind.Object)
            return result;
        foreach (var prop in el.EnumerateObject())
            result[prop.Name] = prop.Value.Clone();
        return result;
    }
}