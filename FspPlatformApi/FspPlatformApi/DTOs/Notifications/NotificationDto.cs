namespace FspPlatformApi.DTOs.Notifications;

/// <summary>Уведомление пользователя (приглашения, смены статусов, задания).</summary>
public record NotificationDto
{
    /// <summary>ID уведомления.</summary>
    public required Guid Id { get; init; }

    /// <summary>Тип («получено_приглашение», «смена_статуса_отклика», ...).</summary>
    public required string Type { get; init; }

    /// <summary>Данные события (JSON).</summary>
    public required System.Text.Json.JsonElement Payload { get; init; }

    /// <summary>Дата создания.</summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>Дата прочтения (null — не прочитано).</summary>
    public DateTime? ReadAt { get; init; }
}