namespace FspPlatformApi.DTOs.Candidates;

/// <summary>
/// Текущий статус кандидата: отрасль, специализация, грейд, история тестирования,
/// доступность смены грейда (кулдаун 90 дней, ТЗ).
/// </summary>
public record MyStatusDto
{
    /// <summary>Название специализации.</summary>
    public required string SpecializationName { get; init; }

    /// <summary>Заявленный (самостоятельно выбранный) грейд.</summary>
    public required string DeclaredGradeName { get; init; }

    /// <summary>Текущий фактический грейд после тестирования — то, что видит работодатель.</summary>
    public required string CurrentGradeName { get; init; }

    /// <summary>Статус специализации (Ожидает тестирования / Грейд подтверждён / ...).</summary>
    public required string Status { get; init; }

    /// <summary>Дата последнего теста.</summary>
    public DateTime? LastTestAt { get; init; }

    /// <summary>Дата последней смены грейда.</summary>
    public DateTime? LastGradeChangeAt { get; init; }

    /// <summary>Можно ли сейчас менять грейд (вне кулдауна).</summary>
    public required bool CanChangeGrade { get; init; }

    /// <summary>Когда будет доступна следующая смена грейда (null — доступна сейчас).</summary>
    public DateTime? NextChangeAvailableAt { get; init; }
}