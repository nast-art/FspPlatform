namespace FspPlatformApi.DTOs.Candidates;

/// <summary>
/// Одна специализация кандидата со статусом грейда (ТЗ: кандидат может иметь
/// несколько специализаций, каждая — со своим грейдом, тестом и кулдауном).
/// </summary>
public record MySpecializationDto
{
    /// <summary>ID специализации (нужен фронту для старта теста).</summary>
    public required short SpecializationId { get; init; }

    /// <summary>Название специализации.</summary>
    public required string SpecializationName { get; init; }

    /// <summary>ID заявленного грейда.</summary>
    public required short DeclaredGradeId { get; init; }

    /// <summary>Заявленный (самостоятельно выбранный) грейд.</summary>
    public required string DeclaredGradeName { get; init; }

    /// <summary>ID текущего фактического грейда — то, что видит работодатель.</summary>
    public required short CurrentGradeId { get; init; }

    /// <summary>Текущий фактический грейд после тестирования.</summary>
    public required string CurrentGradeName { get; init; }

    /// <summary>Статус (Ожидает тестирования / Грейд подтверждён / ...).</summary>
    public required string Status { get; init; }

    /// <summary>Основная специализация (первая выбранная при регистрации).</summary>
    public required bool IsPrimary { get; init; }

    /// <summary>Дата последнего теста по этой специализации.</summary>
    public DateTime? LastTestAt { get; init; }

    /// <summary>Дата последней смены грейда по этой специализации.</summary>
    public DateTime? LastGradeChangeAt { get; init; }

    /// <summary>Можно ли сейчас менять грейд по этой специализации (вне кулдауна).</summary>
    public required bool CanChangeGrade { get; init; }

    /// <summary>Когда будет доступна следующая смена грейда (null — доступна сейчас).</summary>
    public DateTime? NextChangeAvailableAt { get; init; }
}