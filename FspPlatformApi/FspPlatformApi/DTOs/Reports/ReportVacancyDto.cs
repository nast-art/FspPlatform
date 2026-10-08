namespace FspPlatformApi.DTOs.Reports;

/// <summary>Жалоба на вакансию — основа механизма защиты от фиктивных вакансий (ТЗ «плюсом», за рамками MVP).</summary>
public record ReportVacancyDto
{
    /// <summary>ID вакансии.</summary>
    public required Guid VacancyId { get; init; }

    /// <summary>Причина жалобы.</summary>
    public required string Reason { get; init; }

    /// <summary>Дополнительные детали.</summary>
    public string? Details { get; init; }
}