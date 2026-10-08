namespace FspPlatformApi.DTOs.Auth;

/// <summary>Смена пароля авторизованного пользователя.</summary>
public record ChangePasswordDto
{
    /// <summary>Текущий пароль.</summary>
    public required string OldPassword { get; init; }

    /// <summary>Новый пароль (минимум 8 символов).</summary>
    public required string NewPassword { get; init; }
}