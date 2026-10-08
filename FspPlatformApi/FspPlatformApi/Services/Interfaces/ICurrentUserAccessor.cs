namespace FspPlatformApi.Services.Interfaces;

/// <summary>Текущий пользователь из JWT (claims sub совпадает с users.id и будущим Keycloak subject).</summary>
public interface ICurrentUserAccessor
{
    Guid UserId { get; }
    string Role { get; }
    bool IsAuthenticated { get; }
}