using FspPlatformApi.DTOs.Ats;

namespace FspPlatformApi.Services.Interfaces;

public interface IAtsIntegrationService
{
    Task<AtsIntegrationDto> UpsertAsync(Guid ownerUserId, AtsIntegrationUpsertDto dto);
    Task<List<AtsIntegrationDto>> GetMyAsync(Guid ownerUserId);
    Task TriggerSyncAsync(Guid ownerUserId, Guid integrationId);
}