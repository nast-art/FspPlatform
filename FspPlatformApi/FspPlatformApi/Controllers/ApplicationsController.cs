using FspPlatformApi.DTOs.Employers;
using FspPlatformApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FspPlatformApi.Controllers;

[Authorize(Roles = "кандидат")]
[ApiController]
[Route("api/applications")]
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationService _applications;
    private readonly ICurrentUserAccessor _current;

    public ApplicationsController(IApplicationService applications, ICurrentUserAccessor current)
        => (_applications, _current) = (applications, current);

    [HttpPost("vacancies/{vacancyId:guid}")]
    public async Task<IActionResult> Apply(Guid vacancyId, ApplyDto dto)
    {
        await _applications.ApplyAsync(_current.UserId, vacancyId, dto);
        return NoContent();
    }

    [HttpGet("me")]
    public async Task<ActionResult<List<ApplicationDto>>> My()
        => Ok(await _applications.GetMyApplicationsAsync(_current.UserId));

    [HttpPost("{applicationId:guid}/withdraw")]
    public async Task<IActionResult> Withdraw(Guid applicationId)
    {
        await _applications.WithdrawAsync(_current.UserId, applicationId);
        return NoContent();
    }
}

/// <summary>Все отклики по всем вакансиям компании работодателя — одним запросом.</summary>
[Authorize(Roles = "работодатель")]
[ApiController]
[Route("api/applications")]
public class CompanyApplicationsController : ControllerBase
{
    private readonly IApplicationService _applications;
    private readonly ICurrentUserAccessor _current;

    public CompanyApplicationsController(IApplicationService applications, ICurrentUserAccessor current)
        => (_applications, _current) = (applications, current);

    [HttpGet("company")]
    public async Task<ActionResult<List<ApplicationDto>>> Get()
        => Ok(await _applications.GetCompanyApplicationsAsync(_current.UserId));
}

[Authorize(Roles = "работодатель")]
[ApiController]
[Route("api/vacancies/{vacancyId:guid}/applications")]
public class VacancyApplicationsController : ControllerBase
{
    private readonly IApplicationService _applications;
    private readonly ICurrentUserAccessor _current;

    public VacancyApplicationsController(IApplicationService applications, ICurrentUserAccessor current)
        => (_applications, _current) = (applications, current);

    [HttpGet]
    public async Task<ActionResult<List<ApplicationDto>>> Get(Guid vacancyId)
        => Ok(await _applications.GetVacancyApplicationsAsync(_current.UserId, vacancyId));

    [HttpPatch("{applicationId:guid}")]
    public async Task<IActionResult> UpdateStatus(Guid vacancyId, Guid applicationId, ApplicationStatusDto dto)
    {
        await _applications.UpdateStatusAsync(_current.UserId, applicationId, dto);
        return NoContent();
    }
}