namespace FspPlatformApi.DTOs.Tasks;

/// <summary>Ответ кандидата на задание: решение или предложенный подход (ТЗ п.2).</summary>
public record TaskAnswerDto
{
    /// <summary>Текст решения/подхода.</summary>
    public string? AnswerText { get; init; }

    /// <summary>Доп. данные (JSON: ссылка на репозиторий, код и т.п.).</summary>
    public System.Text.Json.JsonElement? AnswerPayload { get; init; }
}