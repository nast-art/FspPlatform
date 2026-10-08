namespace FspPlatformApi.DTOs.Fsp;

/// <summary>Достижение участника ФСП (влияет на ранжирование: вес × 1.5, если верифицировано реестром).</summary>
public record FspAchievementDto
{
    /// <summary>ID достижения.</summary>
    public required Guid Id { get; init; }

    /// <summary>Тип события («Соревнование» / «Мероприятие» / ...).</summary>
    public required string EventType { get; init; }

    /// <summary>Название соревнования/мероприятия.</summary>
    public required string EventName { get; init; }

    /// <summary>Дата события.</summary>
    public required DateOnly EventDate { get; init; }

    /// <summary>Роль (participant / winner / finalist / jury).</summary>
    public string? Role { get; init; }

    /// <summary>Место (если есть).</summary>
    public short? ResultPlace { get; init; }

    /// <summary>Балл рейтинга ФСП (если доступен).</summary>
    public decimal? ResultScore { get; init; }

    /// <summary>Направление (алгоритмы, веб, ...).</summary>
    public string? Direction { get; init; }

    /// <summary>Вес достижения для ранжирования.</summary>
    public required decimal Weight { get; init; }

    /// <summary>Подтверждено ли реестром ФСП (MVP: вносится самим кандидатом — false).</summary>
    public required bool Verified { get; init; }
}