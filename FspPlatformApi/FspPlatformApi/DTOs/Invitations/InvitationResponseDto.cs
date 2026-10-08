namespace FspPlatformApi.DTOs.Invitations;

/// <summary>Ответ кандидата на приглашение.</summary>
public record InvitationResponseDto
{
    /// <summary>Действие: «accept» (принять) | «decline» (отклонить).</summary>
    public required string Action { get; init; }
}