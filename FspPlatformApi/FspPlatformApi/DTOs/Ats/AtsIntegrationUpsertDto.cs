namespace FspPlatformApi.DTOs.Ats;

/// <summary>
/// Конфигурация интеграции с ATS работодателя (концепция за рамками MVP, ТЗ «плюсом»).
/// Config: endpoint, credentials_ref, field_mapping.
/// </summary>
public record AtsIntegrationUpsertDto
{
    /// <summary>Провайдер («greenhouse», «lever», «custom», ...).</summary>
    public required string Provider { get; init; }

    /// <summary>Конфигурация подключения и маппинга полей (JSON).</summary>
    public required System.Text.Json.JsonElement Config { get; init; }
}