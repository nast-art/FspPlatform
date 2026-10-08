namespace FspPlatformApi.DTOs.Fsp;

/// <summary>
/// Добавление/обновление достижения ФСП (MVP: самостоятельный ввод демо-данных;
/// при интеграции с реестром заменяется синхронизацией, verified ставит только реестр).
/// </summary>
public record FspAchievementUpsertDto
{
    /// <summary>Тип события (code из statuses: «соревнование» и т.д.).</summary>
    public required string EventType { get; init; }

    /// <summary>Название события.</summary>
    public required string EventName { get; init; }

    /// <summary>Дата события.</summary>
    public required DateOnly EventDate { get; init; }

    /// <summary>Роль.</summary>
    public string? Role { get; init; }

    /// <summary>Место.</summary>
    public short? ResultPlace { get; init; }

    /// <summary>Балл рейтинга.</summary>
    public decimal? ResultScore { get; init; }

    /// <summary>Направление.</summary>
    public string? Direction { get; init; }

    /// <summary>Вес для ранжирования (по умолчанию 1.00).</summary>
    public decimal Weight { get; init; } = 1.00m;
}