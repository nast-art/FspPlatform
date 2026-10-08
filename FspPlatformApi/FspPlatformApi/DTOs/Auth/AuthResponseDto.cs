namespace FspPlatformApi.DTOs.Auth;

/// <summary>Ответ при регистрации/входе: профиль + JWT.</summary>
public record AuthResponseDto
{
    /// <summary>ID пользователя (UUID — совместим с будущим ФСП ID / Keycloak subject).</summary>
    public required Guid UserId { get; init; }

    /// <summary>Электронная почта.</summary>
    public required string Email { get; init; }

    /// <summary>Роль пользователя («кандидат» / «работодатель» / «администратор»).</summary>
    public required string Role { get; init; }

    /// <summary>JWT-токен для авторизации (Bearer).</summary>
    public required string Token { get; init; }

    /// <summary>Подтверждён ли адрес электронной почты.</summary>
    public required bool EmailConfirmed { get; init; }

    /// <summary>ID участника ФСП, если профиль связан; иначе null.</summary>
    public Guid? FspId { get; init; }
}