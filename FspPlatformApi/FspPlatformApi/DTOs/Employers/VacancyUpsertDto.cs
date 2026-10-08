namespace FspPlatformApi.DTOs.Employers;

/// <summary>
/// Создание/редактирование вакансии. Уровень зарплаты обязателен (ТЗ 2.2 п.1).
/// </summary>
public record VacancyUpsertDto
{
    /// <summary>Название вакансии.</summary>
    public required string Title { get; init; }

    /// <summary>Описание («чем занимается команда»).</summary>
    public required string Description { get; init; }

    /// <summary>ID специализации.</summary>
    public required int SpecializationId { get; init; }

    /// <summary>Грейд «от».</summary>
    public required short GradeFromId { get; init; }

    /// <summary>Грейд «до».</summary>
    public required short GradeToId { get; init; }

    /// <summary>Зарплата от, руб.</summary>
    public required int SalaryFrom { get; init; }

    /// <summary>Зарплата до, руб.</summary>
    public required int SalaryTo { get; init; }

    /// <summary>Формат занятости (fulltime/parttime/remote).</summary>
    public string? EmploymentType { get; init; }

    /// <summary>Локация.</summary>
    public string? Location { get; init; }

    /// <summary>Требуемые навыки (ID из справочника).</summary>
    public List<short>? SkillIds { get; init; }
}