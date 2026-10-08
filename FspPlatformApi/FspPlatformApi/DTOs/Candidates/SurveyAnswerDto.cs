namespace FspPlatformApi.DTOs.Candidates;

/// <summary>Один ответ опроса (свободная схема: ключ вопроса + произвольное JSON-значение).</summary>
public record SurveyAnswerDto
{
    /// <summary>Ключ вопроса («отрасль», «специализация», «формат_работы», ...).</summary>
    public required string QuestionKey { get; init; }

    /// <summary>Значение ответа (объект/массив/примитив).</summary>
    public required System.Text.Json.JsonElement AnswerValue { get; init; }
}