using FspPlatformApi.DTOs.Fsp;
using FspPlatformApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FspPlatformApi.Controllers;

[Authorize(Roles = "кандидат")]
[ApiController]
[Route("api/fsp-achievements")]
public class FspAchievementsController : ControllerBase
{
    private readonly IFspAchievementService _fsp;
    private readonly ICurrentUserAccessor _current;
    public FspAchievementsController(IFspAchievementService fsp, ICurrentUserAccessor current)
        => (_fsp, _current) = (fsp, current);

    [HttpGet]
    public async Task<ActionResult<List<FspAchievementDto>>> Mine()
        => Ok(await _fsp.GetMineAsync(_current.UserId));

    [HttpPost]
    public async Task<ActionResult<FspAchievementDto>> Upsert(FspAchievementUpsertDto dto)
        => Ok(await _fsp.UpsertAsync(_current.UserId, dto));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _fsp.DeleteAsync(_current.UserId, id);
        return NoContent();
    }
}