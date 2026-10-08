using System.Text.Json;

namespace FspPlatformApi.Services.Interfaces.Testing;

/// <summary>Генерация банка вопросов через OpenAI-совместимый ИИ API (Gemini и др.).</summary>
public interface IQuestionGeneratorService
{
    /// <summary>
    /// Генерирует набор вопросов для пары специализация/грейд.
    /// difficultyMix — структура сложности из test_assignments.difficulty_mix ({"1":3,"2":4,...}):
    /// если задана, модели диктуется точное количество вопросов каждой сложности,
    /// чтобы генерация совпадала со сборкой сессии (ТЗ: сопоставимая сложность наборов).
    /// </summary>
    Task<GeneratedQuestionBatch> GenerateAsync(string specialization, string grade,
        IReadOnlyDictionary<int, int>? difficultyMix = null, CancellationToken ct = default);
}

public class GeneratedQuestionBatch
{
    public List<GeneratedQuestion> Questions { get; set; } = new();
}

public class GeneratedQuestion
{
    /// <summary>group_index из ответа модели: одинаковый у изоморфных вариантов одной компетенции.</summary>
    public int GroupIndex { get; set; }

    /// <summary>UUID группы: одинаковый у всех вопросов с одинаковым GroupIndex (как в .mjs-скрипте).</summary>
    public Guid QuestionGroup { get; set; }

    /// <summary>тест_с_вариантами | задача_с_кодом | открытый_вопрос.</summary>
    public string QuestionType { get; set; } = string.Empty;

    public int Difficulty { get; set; }
    public string Prompt { get; set; } = string.Empty;

    /// <summary>options (quiz) / language+reference+testCases (code) / {} (open).</summary>
    public Dictionary<string, JsonElement> Payload { get; set; } = new();

    /// <summary>correct: [индекс] (quiz) / rubric: "..." (open) / {} (code).</summary>
    public Dictionary<string, JsonElement> CorrectPayload { get; set; } = new();

    public int MaxScore { get; set; }
}