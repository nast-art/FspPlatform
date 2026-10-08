namespace FspPlatformApi.DTOs.Tests;

/// <summary>Старт тестовой сессии на заявленный грейд по выбранной специализации.</summary>
public record StartTestDto
{
    /// <summary>ID специализации.</summary>
    public required int SpecializationId { get; init; }

    /// <summary>Целевой грейд теста.</summary>
    public required short TargetGradeId { get; init; }
}