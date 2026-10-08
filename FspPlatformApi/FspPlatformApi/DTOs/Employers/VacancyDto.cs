namespace FspPlatformApi.DTOs.Employers;

/// <summary>Вакансия. ЗП видна всем, включая кандидатов до начала общения (ТЗ 2.2 п.1).</summary>
public record VacancyDto
{
    /// <summary>ID вакансии.</summary>
    public required Guid Id { get; init; }

    /// <summary>Название.</summary>
    public required string Title { get; init; }

    /// <summary>Описание.</summary>
    public required string Description { get; init; }

    /// <summary>Название компании.</summary>
    public required string CompanyName { get; init; }

    /// <summary>Специализация.</summary>
    public required string SpecializationName { get; init; }

    /// <summary>Диапазон грейдов («Джуниор – Миддл»).</summary>
    public required string GradeRange { get; init; }

    /// <summary>Зарплата от, руб.</summary>
    public required int SalaryFrom { get; init; }

    /// <summary>Зарплата до, руб.</summary>
    public required int SalaryTo { get; init; }

    /// <summary>Валюта.</summary>
    public required string Currency { get; init; }

    /// <summary>Формат занятости.</summary>
    public string? EmploymentType { get; init; }

    /// <summary>Локация.</summary>
    public string? Location { get; init; }

    /// <summary>Статус («черновик» / «активен» / «в_архиве»).</summary>
    public required string Status { get; init; }

    /// <summary>Дата публикации.</summary>
    public DateTime? PublishedAt { get; init; }

    /// <summary>Требуемые навыки (названия).</summary>
    public required List<string> Skills { get; init; }
}