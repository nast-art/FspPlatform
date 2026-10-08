using FspPlatformApi.DTOs.Employers;
using FspPlatformApi.DTOs.Search;
using FspPlatformApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FspPlatformApi.Controllers;

[ApiController]
[Route("api/vacancies")]
public class VacanciesController : ControllerBase
{
    private readonly IVacancyService _vacancies;
    private readonly ICurrentUserAccessor _current;
    public VacanciesController(IVacancyService vacancies, ICurrentUserAccessor current)
        => (_vacancies, _current) = (vacancies, current);

    /// <summary>Опубликованные вакансии — видят все, в т.ч. кандидат для самостоятельного отклика.</summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<List<VacancyDto>>> GetPublished(
        [FromQuery] int? specializationId, [FromQuery] string? query,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await _vacancies.GetPublishedAsync(
            new SearchVacancyQuery { SpecializationId = specializationId, Query = query, Page = page, PageSize = pageSize }));

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VacancyDto>> Get(Guid id) => Ok(await _vacancies.GetAsync(id));

    [Authorize(Roles = "работодатель")]
    [HttpPost]
    public async Task<ActionResult<VacancyDto>> Create(VacancyUpsertDto dto)
        => Ok(await _vacancies.CreateAsync(_current.UserId, dto));

    [Authorize(Roles = "работодатель")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<VacancyDto>> Update(Guid id, VacancyUpsertDto dto)
        => Ok(await _vacancies.UpdateAsync(_current.UserId, id, dto));

    [Authorize(Roles = "работодатель")]
    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id)
    {
        await _vacancies.PublishAsync(_current.UserId, id);
        return NoContent();
    }

    [Authorize(Roles = "работодатель")]
    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id)
    {
        await _vacancies.CloseAsync(_current.UserId, id);
        return NoContent();
    }
}