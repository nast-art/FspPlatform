using FspPlatformApi.DTOs.Invitations;

namespace FspPlatformApi.Services.Interfaces.Matching;

public interface IInvitationService
{
    Task<InvitationDto> SendAsync(Guid ownerUserId, SendInvitationDto dto);
    Task<List<InvitationDto>> GetCompanyInvitationsAsync(Guid ownerUserId);
    Task<List<InvitationDto>> GetMyInvitationsAsync(Guid candidateId);
    Task MarkViewedAsync(Guid candidateId, Guid invitationId);
    Task RespondAsync(Guid candidateId, Guid invitationId, InvitationResponseDto dto);
    Task RevokeAsync(Guid ownerUserId, Guid invitationId);
}