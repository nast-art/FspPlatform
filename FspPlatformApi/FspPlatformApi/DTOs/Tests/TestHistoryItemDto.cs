namespace FspPlatformApi.DTOs.Tests;

/// <summary>Запись истории тестирования кандидата (согласованность грейда при повторных сдачах).</summary>
public record TestHistoryItemDto
{
    /// <summary>ID сессии.</summary>
    public required Guid SessionId { get; init; }

    /// <summary>Специализация.</summary>
    public required string SpecializationName { get; init; }

    /// <summary>Целевой грейд теста.</summary>
    public required string TargetGradeName { get; init; }

    /// <summary>Процент результата.</summary>
    public decimal? Percent { get; init; }

    /// <summary>Сдан / не сдан.</summary>
    public bool? Passed { get; init; }

    /// <summary>Статус сессии.</summary>
    public required string Status { get; init; }

    /// <summary>Дата завершения.</summary>
    public DateTime? FinishedAt { get; init; }
}