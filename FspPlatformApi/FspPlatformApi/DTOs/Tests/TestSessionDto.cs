namespace FspPlatformApi.DTOs.Tests;

/// <summary>Активная тестовая сессия кандидата.</summary>
public record TestSessionDto
{
    /// <summary>ID сессии.</summary>
    public required Guid SessionId { get; init; }

    /// <summary>Название теста (из шаблона test_assignments).</summary>
    public required string Title { get; init; }

    /// <summary>Лимит времени, минут.</summary>
    public required int DurationMinutes { get; init; }

    /// <summary>Дата старта.</summary>
    public required DateTime StartedAt { get; init; }

    /// <summary>Дедлайн (started_at + duration).</summary>
    public required DateTime ExpiresAt { get; init; }

    /// <summary>Вопросы сессии (порядок перемешан).</summary>
    public required List<TestQuestionDto> Questions { get; init; }
}