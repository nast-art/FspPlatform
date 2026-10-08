using FspPlatformApi.Data.Entities;
using FspPlatformApi.DTOs.Tests;
using FspPlatformApi.Services.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FspPlatformApi.Controllers;

/// <summary>
/// Экспертная выборочная перепроверка open-ответов, первично оценённых ИИ по рубрике.
/// Экспертный скор перезаписывает ИИ-скор и пересчитывает итог сессии.
/// </summary>
[Authorize(Roles = "администратор")]
[ApiController]
[Route("api/expert-review")]
public class ExpertReviewController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public ExpertReviewController(ApplicationDbContext db) => _db = db;

    [HttpGet("pending")]
    public async Task<ActionResult<List<OpenAnswerReviewItemDto>>> Pending()
    {
        var rows = await _db.TestSessionAnswers
            .Include(a => a.Session).ThenInclude(s => s.Candidate)
            .Include(a => a.Question)
            .Where(a => a.Question.QuestionType == "открытый_вопрос"
                && a.AnswerPayload != null && !a.AutoGraded)
            .OrderBy(a => a.GradedAt)
            .Take(50).ToListAsync();

        return Ok(rows.Select(a => new OpenAnswerReviewItemDto
        {
            AnswerId = a.Id,
            SessionId = a.SessionId,
            CandidateId = a.Session.CandidateId,
            CandidateName = a.Session.Candidate.LastName + " " + a.Session.Candidate.FirstName,
            QuestionPrompt = a.Question.Prompt,
            Rubric = Jsonb.Element(a.Question.CorrectPayload)?.TryGetProperty("rubric", out var r) == true
                ? r.GetString() ?? "" : "",
            AnswerText = Jsonb.Element(a.AnswerPayload)?.TryGetProperty("text", out var t) == true
                ? t.GetString() : null,
            AiScore = a.Score,
            AiFeedback = null,
            ExpertScore = null
        }).ToList());
    }

    [HttpPost("{answerId:long}/score")]
    public async Task<IActionResult> Score(long answerId, ExpertScoreDto dto)
    {
        var a = await _db.TestSessionAnswers.Include(x => x.Session).Include(x => x.Question)
            .FirstOrDefaultAsync(x => x.Id == answerId)
            ?? throw new InvalidOperationException("Ответ не найден.");

        a.Score = Math.Clamp(dto.Score, 0m, a.Question.MaxScore);
        a.GradedAt = DateTime.UtcNow; // экспертная проверка завершена

        // пересчёт итога сессии экспертным скором
        var s = a.Session;
        var all = await _db.TestSessionAnswers.Where(x => x.SessionId == s.Id).ToListAsync();
        s.TotalScore = all.Where(x => x.Score.HasValue).Sum(x => x.Score!.Value);
        if (s.MaxScore > 0 && s.Status == "завершен")
        {
            var threshold = (await _db.TestAssignments.FindAsync(s.AssignmentId))?.PassThreshold ?? 60m;
            var percent = Math.Round(s.TotalScore.Value / s.MaxScore.Value * 100m, 2);
            s.Passed = percent >= threshold;
        }
        await _db.SaveChangesAsync();
        return NoContent();
    }
}