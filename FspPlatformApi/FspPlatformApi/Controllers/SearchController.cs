using FspPlatformApi.DTOs.Search;
using FspPlatformApi.Services.Interfaces;
using FspPlatformApi.Services.Interfaces.Matching;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FspPlatformApi.Controllers;

[Authorize(Roles = "работодатель")]
[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _search;
    private readonly ICurrentUserAccessor _current;
    public SearchController(ISearchService search, ICurrentUserAccessor current)
        => (_search, _current) = (search, current);

    /// <summary>Поиск по банку кандидатов: фильтры по категории/грейду/стеку/ФСП, ранжирование,
    /// объяснимость каждой позиции. Подборка сохраняется и доступна в истории.</summary>
    [HttpPost("candidates")]
    public async Task<ActionResult<SearchResponseDto>> Search(SearchCandidatesDto dto)
        => Ok(await _search.SearchAsync(_current.UserId, dto));

    [HttpGet("history")]
    public async Task<ActionResult<List<SearchResponseDto>>> History(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        => Ok(await _search.GetHistoryAsync(_current.UserId, page, pageSize));

    [HttpGet("results/{requestId:guid}")]
    public async Task<ActionResult<SearchResponseDto>> Result(Guid requestId)
        => Ok(await _search.GetSavedResultAsync(_current.UserId, requestId));
}