namespace FspPlatformApi.DTOs.Tests;

/// <summary>Пакетная отправка ответов сессии.</summary>
public record SubmitAnswersDto
{
    /// <summary>Ответы по вопросам.</summary>
    public required List<SubmitAnswerDto> Answers { get; init; }
}