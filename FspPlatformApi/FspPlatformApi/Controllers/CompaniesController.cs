using FspPlatformApi.DTOs.Employers;
using FspPlatformApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FspPlatformApi.Controllers;

[Authorize(Roles = "работодатель")]
[ApiController]
[Route("api/companies")]
public class CompaniesController : ControllerBase
{
    private readonly ICompanyService _companies;
    private readonly ICurrentUserAccessor _current;
    public CompaniesController(ICompanyService companies, ICurrentUserAccessor current)
        => (_companies, _current) = (companies, current);

    [HttpGet("me")]
    public async Task<ActionResult<CompanyDto>> Get()
        => Ok(await _companies.GetMyCompanyAsync(_current.UserId));

    [HttpPost]
    public async Task<ActionResult<CompanyDto>> Create(CompanyUpsertDto dto)
        => Ok(await _companies.CreateAsync(_current.UserId, dto));

    [HttpPut("{companyId:guid}")]
    public async Task<ActionResult<CompanyDto>> Update(Guid companyId, CompanyUpsertDto dto)
        => Ok(await _companies.UpdateAsync(_current.UserId, companyId, dto));
}