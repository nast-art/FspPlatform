using FspPlatformApi.DTOs.Reports;

namespace FspPlatformApi.Services.Interfaces;

public interface IReportService
{
    Task ReportVacancyAsync(Guid userId, ReportVacancyDto dto);
    Task<List<VacancyReportDto>> GetReportsAsync();
    Task ModerateAsync(Guid adminUserId, Guid reportId, string status);
}