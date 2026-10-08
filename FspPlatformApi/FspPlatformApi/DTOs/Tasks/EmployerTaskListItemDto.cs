namespace FspPlatformApi.DTOs.Tasks;

/// <summary>Задача работодателя со статистикой назначений (для списка «Мои задачи»).</summary>
public record EmployerTaskListItemDto
{
    /// <summary>ID задачи (employer_tasks.id).</summary>
    public required Guid Id { get; init; }

    /// <summary>Название.</summary>
    public required string Title { get; init; }

    /// <summary>Описание.</summary>
    public required string Description { get; init; }

    /// <summary>Специализация (null — не задана).</summary>
    public string? SpecializationName { get; init; }

    /// <summary>Грейд (null — любой).</summary>
    public string? GradeName { get; init; }

    /// <summary>Максимальный балл.</summary>
    public required decimal MaxScore { get; init; }

    /// <summary>Активна ли задача (false — в архиве).</summary>
    public required bool IsActive { get; init; }

    /// <summary>Дата создания.</summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>Скольким кандидатам назначена.</summary>
    public required int AssignedCount { get; init; }

    /// <summary>Сколько кандидатов выполнило.</summary>
    public required int AnsweredCount { get; init; }

    /// <summary>Средний балл по выполненным ответам (null — ответов ещё нет).</summary>
    public decimal? AvgScore { get; init; }
}