using FspPlatformApi.Services.Interfaces.Testing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FspPlatformApi.Controllers;

[Authorize(Roles = "администратор")]
[ApiController]
[Route("api/testing/calibration")]
public class CalibrationController : ControllerBase
{
    private readonly IQuestionCalibrationService _calibration;
    public CalibrationController(IQuestionCalibrationService calibration) => _calibration = calibration;

    [HttpPost("refresh-stats")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        await _calibration.RefreshStatsAsync(ct);
        return NoContent();
    }

    /// <summary>Ротация «плохих» вопросов по статистике v_question_stats.</summary>
    [HttpPost("rotate")]
    public async Task<IActionResult> Rotate(CancellationToken ct)
        => Ok(await _calibration.RotateQuestionsAsync(ct));
}