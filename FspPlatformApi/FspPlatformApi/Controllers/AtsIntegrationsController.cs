using FspPlatformApi.DTOs.Ats;
using FspPlatformApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FspPlatformApi.Controllers;

[Authorize(Roles = "работодатель")]
[ApiController]
[Route("api/ats-integrations")]
public class AtsIntegrationsController : ControllerBase
{
    private readonly IAtsIntegrationService _ats;
    private readonly ICurrentUserAccessor _current;
    public AtsIntegrationsController(IAtsIntegrationService ats, ICurrentUserAccessor current)
        => (_ats, _current) = (ats, current);

    /// <summary>Концепция интеграции с ATS работодателя (за рамками MVP — ТЗ «плюсом»).</summary>
    [HttpGet]
    public async Task<ActionResult<List<AtsIntegrationDto>>> Get()
        => Ok(await _ats.GetMyAsync(_current.UserId));

    [HttpPost]
    public async Task<ActionResult<AtsIntegrationDto>> Upsert(AtsIntegrationUpsertDto dto)
        => Ok(await _ats.UpsertAsync(_current.UserId, dto));

    [HttpPost("{id:guid}/sync")]
    public async Task<IActionResult> Sync(Guid id)
    {
        await _ats.TriggerSyncAsync(_current.UserId, id);
        return NoContent();
    }
}