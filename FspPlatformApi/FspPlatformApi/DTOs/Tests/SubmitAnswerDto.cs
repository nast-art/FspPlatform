namespace FspPlatformApi.DTOs.Tests;

/// <summary>
/// Один ответ кандидата. Заполняется по типу вопроса:
/// quiz → Selected; code → Code + Language; open → Text.
/// </summary>
public record SubmitAnswerDto
{
    /// <summary>ID вопроса (должен входить в сессию).</summary>
    public required Guid QuestionId { get; init; }

    /// <summary>Код решения (для задача_с_кодом).</summary>
    public string? Code { get; init; }

    /// <summary>Язык кода: python | javascript (для задача_с_кодом).</summary>
    public string? Language { get; init; }

    /// <summary>Развёрнутый текстовый ответ (для открытый_вопрос).</summary>
    public string? Text { get; init; }

    /// <summary>
    /// Выбранный вариант(ы) для тест_с_вариантами: число или массив чисел
    /// (индексы вариантов из payload.options).
    /// </summary>
    public System.Text.Json.JsonElement? Selected { get; init; }
}