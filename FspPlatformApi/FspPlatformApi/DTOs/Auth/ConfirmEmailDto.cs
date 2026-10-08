namespace FspPlatformApi.DTOs.Auth;

/// <summary>Подтверждение адреса электронной почты одноразовым токеном.</summary>
public record ConfirmEmailDto
{
    /// <summary>Сырой одноразовый токен из письма (в БД хранится только SHA256-хэш).</summary>
    public required string Token { get; init; }
}