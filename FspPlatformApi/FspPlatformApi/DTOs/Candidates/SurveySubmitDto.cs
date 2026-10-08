namespace FspPlatformApi.DTOs.Candidates;

/// <summary>Отправка ответов опроса (история опроса хранится в survey_sessions/survey_answers).</summary>
public record SurveySubmitDto
{
    /// <summary>ID сессии опроса.</summary>
    public required Guid SessionId { get; init; }

    /// <summary>Ответы по вопросам.</summary>
    public required List<SurveyAnswerDto> Answers { get; init; }
}