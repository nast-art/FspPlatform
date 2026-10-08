using FspPlatformApi.DTOs.Candidates;
using FspPlatformApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FspPlatformApi.Controllers;

[Authorize(Roles = "работодатель")]
[ApiController]
[Route("api/candidates")]
public class CandidateCardController : ControllerBase
{
    private readonly ICandidateService _candidates;
    private readonly ICurrentUserAccessor _current;
    public CandidateCardController(ICandidateService candidates, ICurrentUserAccessor current)
        => (_candidates, _current) = (candidates, current);

    /// <summary>Публичная карточка: без контактов до принятия приглашения/отклика (ТЗ 2.2 п.2).</summary>
    [HttpGet("{candidateId:guid}")]
    public async Task<ActionResult<CandidatePublicCardDto>> GetCard(Guid candidateId)
        => Ok(await _candidates.GetPublicCardAsync(candidateId, _current.UserId));

    /// <summary>Контакты — только после принятого приглашения/отклика, с аудитом view_contacts.</summary>
    [HttpGet("{candidateId:guid}/contacts")]
    public async Task<ActionResult<CandidateContactsDto>> GetContacts(Guid candidateId)
        => Ok(await _candidates.GetContactsAsync(candidateId, _current.UserId));
}