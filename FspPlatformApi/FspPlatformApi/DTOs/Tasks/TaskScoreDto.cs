namespace FspPlatformApi.DTOs.Tasks;

/// <summary>Оценка ответа работодателем.</summary>
public record TaskScoreDto
{
    /// <summary>Балл.</summary>
    public required decimal Score { get; init; }
}