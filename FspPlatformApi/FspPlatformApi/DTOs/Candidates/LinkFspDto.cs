namespace FspPlatformApi.DTOs.Candidates;

/// <summary>Привязка профиля к ID участника ФСП (в будущем — Keycloak).</summary>
public record LinkFspDto
{
    /// <summary>ID участника ФСП (UUID).</summary>
    public required Guid FspId { get; init; }
}