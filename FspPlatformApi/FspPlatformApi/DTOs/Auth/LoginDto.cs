namespace FspPlatformApi.DTOs.Auth;

/// <summary>Запрос входа в систему.</summary>
public record LoginDto
{
    /// <summary>Электронная почта.</summary>
    public required string Email { get; init; }

    /// <summary>Пароль.</summary>
    public required string Password { get; init; }
}