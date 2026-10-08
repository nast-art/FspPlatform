namespace FspPlatformApi.DTOs.Ats;

/// <summary>Интеграция с ATS работодателя.</summary>
public record AtsIntegrationDto
{
    /// <summary>ID интеграции.</summary>
    public required Guid Id { get; init; }

    /// <summary>Провайдер.</summary>
    public required string Provider { get; init; }

    /// <summary>Конфигурация (JSON).</summary>
    public required System.Text.Json.JsonElement Config { get; init; }

    /// <summary>Активна ли интеграция.</summary>
    public required bool IsActive { get; init; }

    /// <summary>Дата последней синхронизации.</summary>
    public DateTime? LastSyncAt { get; init; }
}