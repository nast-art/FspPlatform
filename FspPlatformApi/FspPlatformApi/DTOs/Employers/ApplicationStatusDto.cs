namespace FspPlatformApi.DTOs.Employers;

/// <summary>Смена статуса отклика работодателем.</summary>
public record ApplicationStatusDto
{
    /// <summary>Новый статус: «просмотрен» | «принят» | «отклонен».</summary>
    public required string Status { get; init; }
}