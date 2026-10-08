namespace FspPlatformApi.DTOs.Tests;

/// <summary>Экспертная оценка open-ответа (перезаписывает ИИ-скор и пересчитывает итог сессии).</summary>
public record ExpertScoreDto
{
    /// <summary>Балл (будет ограничен max_score вопроса).</summary>
    public required decimal Score { get; init; }
}