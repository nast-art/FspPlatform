namespace FspPlatformApi.Services.Interfaces.Testing;

public interface IQuestionCalibrationService
{
    /// <summary>Обновить материализованное представление v_question_stats.</summary>
    Task RefreshStatsAsync(CancellationToken ct = default);

    /// <summary>
    /// Ротация банка: вопросы, которые все отгадывают (avg &gt; 0.95), никто не решает (avg &lt; 0.1)
    /// или с низкой дискриминативностью (корреляция &lt; порога), выключаются (is_active=false).
    /// </summary>
    Task<CalibrationReportDto> RotateQuestionsAsync(CancellationToken ct = default);
}

public record CalibrationReportDto(int Deactivated, int TotalChecked, List<Guid> DeactivatedQuestionIds);