using FspPlatformApi.Data.Entities;
using FspPlatformApi.DTOs.Reports;
using FspPlatformApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FspPlatformApi.Services.Platform;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;
    public ReportService(ApplicationDbContext db, IAuditService audit) => (_db, _audit) = (db, audit);

    public async Task ReportVacancyAsync(Guid userId, ReportVacancyDto dto)
    {
        var vacancy = await _db.Vacancies.FindAsync(dto.VacancyId)
            ?? throw new InvalidOperationException("Вакансия не найдена.");
        _db.VacancyReports.Add(new VacancyReport
        {
            VacancyId = dto.VacancyId, CompanyId = vacancy.CompanyId,
            ReporterId = userId, Reason = dto.Reason, Details = dto.Details, Status = "активен"
        });
        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, "vacancy_reports", dto.VacancyId.ToString(), "insert");
    }

    public async Task<List<VacancyReportDto>> GetReportsAsync() =>
        await _db.VacancyReports.OrderByDescending(r => r.CreatedAt)
            .Select(r => new VacancyReportDto
            {
                Id = r.Id, VacancyId = r.VacancyId ?? Guid.Empty,
                CompanyName = r.Company.Name, Reason = r.Reason,
                Status = r.Status, CreatedAt = r.CreatedAt
            }).ToListAsync();

    /// <summary>'заблокирован' = вакансия снята с публикации (защита от фиктивных вакансий — ТЗ «плюс»).</summary>
    public async Task ModerateAsync(Guid adminUserId, Guid reportId, string status)
    {
        var r = await _db.VacancyReports.Include(x => x.Vacancy)
            .FirstOrDefaultAsync(x => x.Id == reportId)
            ?? throw new InvalidOperationException("Жалоба не найдена.");
        r.Status = status;
        if (status == "заблокирован" && r.Vacancy is not null)
            r.Vacancy.Status = "заблокирован";
        await _db.SaveChangesAsync();
        await _audit.LogAsync(adminUserId, "vacancy_reports", reportId.ToString(), "moderate",
            newValue: new { status });
    }
}