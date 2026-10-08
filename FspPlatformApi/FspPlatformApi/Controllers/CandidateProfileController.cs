using FspPlatformApi.DTOs.Candidates;
using FspPlatformApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FspPlatformApi.Controllers;

[Authorize(Roles = "кандидат")]
[ApiController]
[Route("api/candidates/me")]
public class CandidateProfileController : ControllerBase
{
    private readonly ICandidateService _candidates;
    private readonly ICurrentUserAccessor _current;
    public CandidateProfileController(ICandidateService candidates, ICurrentUserAccessor current)
        => (_candidates, _current) = (candidates, current);

    [HttpGet("profile")]
    public async Task<ActionResult<CandidateProfileUpsertDto>> GetProfile()
        => Ok(await _candidates.GetMyProfileAsync(_current.UserId));

    [HttpPut("profile")]
    public async Task<IActionResult> UpsertProfile(CandidateProfileUpsertDto dto)
    {
        await _candidates.UpsertMyProfileAsync(_current.UserId, dto);
        return NoContent();
    }

    /// <summary>
    /// Опрос по отрасли и специализации + самостоятельный выбор предполагаемого грейда.
    /// Можно вызывать несколько раз — каждая новая специализация добавляется к кандидату
    /// (первая становится основной, IsPrimary).
    /// </summary>
    [HttpPost("survey")]
    public async Task<ActionResult<Guid>> StartSurvey(StartSurveyDto dto)
        => Ok(await _candidates.StartSurveyAsync(_current.UserId, dto));

    [HttpPost("survey/answers")]
    public async Task<IActionResult> SubmitSurvey(SurveySubmitDto dto)
    {
        await _candidates.SubmitSurveyAsync(_current.UserId, dto);
        return NoContent();
    }

    /// <summary>Статус основной специализации (обратная совместимость).</summary>
    [HttpGet("status")]
    public async Task<ActionResult<MyStatusDto>> Status()
        => Ok(await _candidates.GetMyStatusAsync(_current.UserId));

    /// <summary>Все специализации кандидата со статусами грейдов (несколько специализаций).</summary>
    [HttpGet("specializations")]
    public async Task<ActionResult<List<MySpecializationDto>>> Specializations()
        => Ok(await _candidates.GetMySpecializationsAsync(_current.UserId));

    /// <summary>Связь профиля с ID участника ФСП (Keycloak-ready: users.fsp_id).</summary>
    [HttpPost("fsp-link")]
    public async Task<IActionResult> LinkFsp(LinkFspDto dto)
    {
        await _candidates.LinkFspAsync(_current.UserId, dto);
        return NoContent();
    }

    /// <summary>
    /// Загрузка PDF-резюме с автораспознаванием (multipart/form-data, поле file).
    /// Возвращает распознанные: контакты, город, ФИО, стаж, «о себе», софт-скиллы, стек (ТЗ 2.1).
    /// </summary>
    [HttpPost("resume")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadResume(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Файл не передан" });

        var result = await _candidates.UploadResumeAsync(
            _current.UserId, file.OpenReadStream(), file.FileName);

        return Ok(new
        {
            stored = result.StoredPath,
            recognizedEmail = result.RecognizedEmail,
            recognizedPhone = result.RecognizedPhone,
            recognizedTelegram = result.RecognizedTelegram,
            recognizedCity = result.RecognizedCity,
            recognizedLastName = result.RecognizedLastName,
            recognizedFirstName = result.RecognizedFirstName,
            recognizedMiddleName = result.RecognizedMiddleName,
            recognizedExperienceYears = result.RecognizedExperienceYears,
            recognizedAbout = result.RecognizedAbout,
            recognizedSoftSkills = result.RecognizedSoftSkills,
            recognizedSkills = result.RecognizedSkills,
            message = "PDF сохранён. Распознанные данные подставлены в форму (пустые поля)."
        });
    }

    /// <summary>
    /// Скачивание автосгенерированного стандартизированного PDF-профиля (ТЗ 2.1).
    /// Единый шаблон для всех кандидатов — сопоставимый документ для работодателя.
    /// </summary>
    [HttpGet("profile.pdf")]
    public async Task<IActionResult> DownloadProfilePdf()
    {
        var bytes = await _candidates.GetProfilePdfAsync(_current.UserId);
        return File(bytes, "application/pdf", $"fsp_profile_{_current.UserId}.pdf");
    }
}