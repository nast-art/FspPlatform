using FspPlatformApi.Data.Entities;
using FspPlatformApi.DTOs.Fsp;
using FspPlatformApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FspPlatformApi.Services.Platform;

public class FspAchievementService : IFspAchievementService
{
    private readonly ApplicationDbContext _db;
    public FspAchievementService(ApplicationDbContext db) => _db = db;

    private async Task<Guid> MyFspIdAsync(Guid userId) =>
        (await _db.Users.FindAsync(userId))?.FspId
        ?? throw new InvalidOperationException("Профиль не связан с ID участника ФСП.");

    public async Task<List<FspAchievementDto>> GetMineAsync(Guid userId)
    {
        var fspId = await MyFspIdAsync(userId);
        return await Query(fspId).ToListAsync();
    }

    public async Task<List<FspAchievementDto>> GetByCandidateAsync(Guid candidateId)
    {
        var fspId = (await _db.Users.FindAsync(candidateId))?.FspId;
        if (fspId is null) return new List<FspAchievementDto>(); // кандидат без истории ФСП — корректно, без падения (ТЗ)
        return await Query(fspId.Value).ToListAsync();
    }

    private IQueryable<FspAchievementDto> Query(Guid fspId) =>
        _db.FspAchievements.Where(a => a.FspId == fspId).OrderByDescending(a => a.EventDate)
            .Select(a => new FspAchievementDto
            {
                Id = a.Id, EventType = a.EventTypeNavigation.Name, EventName = a.EventName,
                EventDate = a.EventDate, Role = a.Role, ResultPlace = a.ResultPlace,
                ResultScore = a.ResultScore, Direction = a.Direction,
                Weight = a.Weight, Verified = a.Verified
            });

    /// <summary>MVP: кандидат вносит достижения сам (демо). При интеграции с реестром ФСП
    /// заменяется синхронизацией; verified=true ставит только реестр.</summary>
    public async Task<FspAchievementDto> UpsertAsync(Guid userId, FspAchievementUpsertDto dto)
    {
        var fspId = await MyFspIdAsync(userId);
        var exists = await _db.FspAchievements.FirstOrDefaultAsync(a =>
            a.FspId == fspId && a.EventName == dto.EventName && a.EventDate == dto.EventDate);
        if (exists is not null)
        {
            exists.EventType = dto.EventType; exists.Role = dto.Role;
            exists.ResultPlace = dto.ResultPlace; exists.ResultScore = dto.ResultScore;
            exists.Direction = dto.Direction; exists.Weight = dto.Weight;
        }
        else
        {
            _db.FspAchievements.Add(new FspAchievement
            {
                FspId = fspId, EventType = dto.EventType, EventName = dto.EventName,
                EventDate = dto.EventDate, Role = dto.Role, ResultPlace = dto.ResultPlace,
                ResultScore = dto.ResultScore, Direction = dto.Direction, Weight = dto.Weight
            });
        }
        await _db.SaveChangesAsync();
        return await Query(fspId).FirstAsync();
    }

    public async Task DeleteAsync(Guid userId, Guid achievementId)
    {
        var fspId = await MyFspIdAsync(userId);
        var a = await _db.FspAchievements
            .FirstOrDefaultAsync(x => x.Id == achievementId && x.FspId == fspId)
            ?? throw new UnauthorizedAccessException();
        _db.FspAchievements.Remove(a);
        await _db.SaveChangesAsync();
    }
}