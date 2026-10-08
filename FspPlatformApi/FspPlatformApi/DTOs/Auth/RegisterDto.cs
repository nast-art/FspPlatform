namespace FspPlatformApi.DTOs.Auth;

/// <summary>
/// Запрос регистрации пользователя (ТЗ 2.2 п.4: регистрация по электронной почте с подтверждением адреса).
/// </summary>
public record RegisterDto
{
    /// <summary>Электронная почта (уникальная, регистр не важен).</summary>
    public required string Email { get; init; }

    /// <summary>Пароль (минимум 8 символов; хранится только BCrypt-хэш).</summary>
    public required string Password { get; init; }

    /// <summary>Роль: «кандидат» или «работодатель».</summary>
    public required string Role { get; init; }

    /// <summary>
    /// Опциональный ID участника ФСП (UUID). Может отсутствовать —
    /// корректная обработка кандидата без истории ФСП обязательна (ТЗ).
    /// </summary>
    public string? FspId { get; init; }
}