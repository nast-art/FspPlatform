using FspPlatformApi.DTOs.Tests;
using FspPlatformApi.Services.Interfaces;
using FspPlatformApi.Services.Interfaces.Testing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FspPlatformApi.Controllers;

[Authorize(Roles = "кандидат")]
[ApiController]
[Route("api/tests")]
public class TestsController : ControllerBase
{
    private readonly ITestService _tests;
    private readonly ICurrentUserAccessor _current;
    public TestsController(ITestService tests, ICurrentUserAccessor current)
        => (_tests, _current) = (tests, current);

    /// <summary>Сборка уникальной сессии по difficulty_mix шаблона (стратифицированная выборка из банка).</summary>
    [HttpPost("start")]
    public async Task<ActionResult<TestSessionDto>> Start(StartTestDto dto)
        => Ok(await _tests.StartTestAsync(_current.UserId, dto));

    [HttpGet("{sessionId:guid}")]
    public async Task<ActionResult<TestSessionDto>> Get(Guid sessionId)
        => Ok(await _tests.GetSessionAsync(_current.UserId, sessionId));

    /// <summary>Ответы: quiz сверяется с эталоном, code прогоняется по скрытым тест-кейсам
    /// в Docker, open оценивается ИИ по рубрике.</summary>
    [HttpPost("{sessionId:guid}/answers")]
    public async Task<IActionResult> Submit(Guid sessionId, SubmitAnswersDto dto)
    {
        await _tests.SubmitAnswersAsync(_current.UserId, sessionId, dto);
        return NoContent();
    }

    [HttpPost("{sessionId:guid}/finish")]
    public async Task<ActionResult<TestResultDto>> Finish(Guid sessionId)
        => Ok(await _tests.FinishTestAsync(_current.UserId, sessionId));

    [HttpGet("history")]
    public async Task<ActionResult<List<TestHistoryItemDto>>> History()
        => Ok(await _tests.GetHistoryAsync(_current.UserId));
}