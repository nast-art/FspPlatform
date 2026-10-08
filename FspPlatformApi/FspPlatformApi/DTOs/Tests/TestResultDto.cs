namespace FspPlatformApi.DTOs.Tests;

/// <summary>Итог завершённой тестовой сессии.</summary>
public record TestResultDto
{
    /// <summary>ID сессии.</summary>
    public required Guid SessionId { get; init; }

    /// <summary>Суммарный балл.</summary>
    public decimal? TotalScore { get; init; }

    /// <summary>Максимальный балл набора.</summary>
    public decimal? MaxScore { get; init; }

    /// <summary>Процент (вычисляемое поле БД).</summary>
    public decimal? Percent { get; init; }

    /// <summary>
    /// Итог: true — сдан, false — не сдан (грейд НЕ понижается принудительно),
    /// null — ожидает проверки.
    /// </summary>
    public bool? Passed { get; init; }

    /// <summary>Имя присвоенного грейда (null, если не сдан).</summary>
    public string? ResultingGradeName { get; init; }

    /// <summary>Статус сессии («завершен», «истек_срок»).</summary>
    public required string Status { get; init; }

    /// <summary>Пояснение для кандидата (порог, результат, что дальше).</summary>
    public required string Message { get; init; }
}