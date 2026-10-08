using System.Text.Json;

namespace FspPlatformApi.Services.Infrastructure;

/// <summary>
/// Хелперы для jsonb-полей. EF Core скаффолдил jsonb-колонки как string,
/// поэтому все чтения/записи идут через сериализацию/парсинг.
/// </summary>
public static class Jsonb
{
    /// <summary>Безопасно разобрать jsonb-строку в JsonElement (null — если пусто или невалидный JSON).</summary>
    public static JsonElement? Element(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonDocument.Parse(json).RootElement.Clone(); }
        catch (JsonException) { return null; }
    }

    /// <summary>Сериализовать объект в строку для записи в jsonb-поле.</summary>
    public static string? From(object? value) => value is null ? null : JsonSerializer.Serialize(value);
}