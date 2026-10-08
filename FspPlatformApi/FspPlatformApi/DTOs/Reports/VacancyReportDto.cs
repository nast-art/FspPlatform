namespace FspPlatformApi.DTOs.Reports;

/// <summary>Жалоба на вакансию (кабинет администратора).</summary>
public record VacancyReportDto
{
    /// <summary>ID жалобы.</summary>
    public required Guid Id { get; init; }

    /// <summary>ID вакансии.</summary>
    public required Guid VacancyId { get; init; }

    /// <summary>Название компании.</summary>
    public required string CompanyName { get; init; }

    /// <summary>Причина.</summary>
    public required string Reason { get; init; }

    /// <summary>Статус рассмотрения («активен» = на рассмотрении, «заблокирован» = вакансия снята).</summary>
    public required string Status { get; init; }

    /// <summary>Дата создания.</summary>
    public required DateTime CreatedAt { get; init; }
}