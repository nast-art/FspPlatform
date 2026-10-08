using FspPlatformApi.Data.Entities;
using FspPlatformApi.Services.Interfaces.Testing;
using Microsoft.EntityFrameworkCore;

namespace FspPlatformApi.Services.Testing;

/// <summary>
/// Калибровка банка по v_question_stats: вопросы, которые все отгадывают (avg &gt; 0.95),
/// никто не решает (avg &lt; 0.1) или с низкой дискриминативностью (корреляция &lt; порога),
/// выключаются (is_active=false) и подлежат ротации. Закрывает критерий жюри «дискриминативность заданий».
/// </summary>
public class QuestionCalibrationService : IQuestionCalibrationService
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _cfg;

    public QuestionCalibrationService(ApplicationDbContext db, IConfiguration cfg) => (_db, _cfg) = (db, cfg);

    public async Task RefreshStatsAsync(CancellationToken ct = default)
        => await _db.Database.ExecuteSqlAsync($"REFRESH MATERIALIZED VIEW v_question_stats", ct);

    public async Task<CalibrationReportDto> RotateQuestionsAsync(CancellationToken ct = default)
    {
        await RefreshStatsAsync(ct);

        var maxAvg = _cfg.GetValue<decimal>("Calibration:MaxAvgScore", 0.95m);
        var minAvg = _cfg.GetValue<decimal>("Calibration:MinAvgScore", 0.10m);
        var minDiscrimination = _cfg.GetValue<double>("Calibration:MinDiscrimination", 0.20);

        var stats = await _db.VQuestionStats.Where(s => s.Attempts >= 5).ToListAsync(ct);
        var toDeactivate = stats.Where(s =>
            s.AvgRelativeScore > maxAvg ||
            s.AvgRelativeScore < minAvg ||
            (s.DiscriminationIndex.HasValue && s.DiscriminationIndex.Value < minDiscrimination))
            .Select(s => s.QuestionId)
            .Where(id => id.HasValue)          // в view question_id nullable — отсекаем null
            .Select(id => id!.Value)
            .ToList();

        var questions = await _db.TestQuestions.Where(q => toDeactivate.Contains(q.Id)).ToListAsync(ct);
        foreach (var q in questions) q.IsActive = false;
        await _db.SaveChangesAsync(ct);

        return new CalibrationReportDto(questions.Count, stats.Count, toDeactivate);
    }
}