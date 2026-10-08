using FspPlatformApi.DTOs.Reports;
using FspPlatformApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FspPlatformApi.Controllers;

[Authorize]
[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reports;
    private readonly ICurrentUserAccessor _current;
    public ReportsController(IReportService reports, ICurrentUserAccessor current)
        => (_reports, _current) = (reports, current);

    /// <summary>Жалоба на вакансию — основа механизма защиты от фиктивных вакансий (ТЗ «плюс»).</summary>
    [HttpPost("vacancies")]
    public async Task<IActionResult> ReportVacancy(ReportVacancyDto dto)
    {
        await _reports.ReportVacancyAsync(_current.UserId, dto);
        return NoContent();
    }

    [Authorize(Roles = "администратор")]
    [HttpGet]
    public async Task<ActionResult<List<VacancyReportDto>>> Get()
        => Ok(await _reports.GetReportsAsync());

    [Authorize(Roles = "администратор")]
    [HttpPatch("{reportId:guid}")]
    public async Task<IActionResult> Moderate(Guid reportId, [FromBody] string status)
    {
        await _reports.ModerateAsync(_current.UserId, reportId, status);
        return NoContent();
    }
}