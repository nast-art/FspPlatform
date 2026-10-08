namespace FspPlatformApi.DTOs.Candidates;

/// <summary>
/// Старт опроса по отрасли и специализации с самостоятельным выбором предполагаемого грейда (ТЗ).
/// Заявленный грейд — только заявка, фактический определяется тестом.
/// </summary>
public record StartSurveyDto
{
    /// <summary>ID специализации из справочника.</summary>
    public required int SpecializationId { get; init; }

    /// <summary>ID предполагаемого грейда (заявка кандидата).</summary>
    public required short DeclaredGradeId { get; init; }
}