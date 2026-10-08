using FspPlatformApi.DTOs.Invitations;
using FspPlatformApi.Services.Interfaces;
using FspPlatformApi.Services.Interfaces.Matching;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FspPlatformApi.Controllers;

[Authorize(Roles = "работодатель")]
[ApiController]
[Route("api/invitations")]
public class InvitationsController : ControllerBase
{
    private readonly IInvitationService _invitations;
    private readonly ICurrentUserAccessor _current;
    public InvitationsController(IInvitationService invitations, ICurrentUserAccessor current)
        => (_invitations, _current) = (invitations, current);

    /// <summary>Выход на контакт: работодатель направляет приглашение конкретному кандидату
    /// с описанием и ЗП, без обязательной привязки к вакансии (смысловой центр ТЗ).</summary>
    [HttpPost]
    public async Task<ActionResult<InvitationDto>> Send(SendInvitationDto dto)
        => Ok(await _invitations.SendAsync(_current.UserId, dto));

    [HttpGet]
    public async Task<ActionResult<List<InvitationDto>>> GetCompanyInvitations()
        => Ok(await _invitations.GetCompanyInvitationsAsync(_current.UserId));

    [HttpPost("{id:guid}/revoke")]
    public async Task<IActionResult> Revoke(Guid id)
    {
        await _invitations.RevokeAsync(_current.UserId, id);
        return NoContent();
    }
}

[Authorize(Roles = "кандидат")]
[ApiController]
[Route("api/my-invitations")]
public class MyInvitationsController : ControllerBase
{
    private readonly IInvitationService _invitations;
    private readonly ICurrentUserAccessor _current;
    public MyInvitationsController(IInvitationService invitations, ICurrentUserAccessor current)
        => (_invitations, _current) = (invitations, current);

    /// <summary>Входящие предложения: кандидат видит вакансию/описание и ЗП до начала общения (ТЗ 2.2 п.1).</summary>
    [HttpGet]
    public async Task<ActionResult<List<InvitationDto>>> Get()
        => Ok(await _invitations.GetMyInvitationsAsync(_current.UserId));

    [HttpPost("{id:guid}/view")]
    public async Task<IActionResult> View(Guid id)
    {
        await _invitations.MarkViewedAsync(_current.UserId, id);
        return NoContent();
    }

    [HttpPost("{id:guid}/respond")]
    public async Task<IActionResult> Respond(Guid id, InvitationResponseDto dto)
    {
        await _invitations.RespondAsync(_current.UserId, id, dto);
        return NoContent();
    }
}