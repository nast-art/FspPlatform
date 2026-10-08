using FspPlatformApi.DTOs.Fsp;

namespace FspPlatformApi.Services.Interfaces;

public interface IFspAchievementService
{
    Task<List<FspAchievementDto>> GetMineAsync(Guid userId);
    Task<List<FspAchievementDto>> GetByCandidateAsync(Guid candidateId);
    Task<FspAchievementDto> UpsertAsync(Guid userId, FspAchievementUpsertDto dto);
    Task DeleteAsync(Guid userId, Guid achievementId);
}