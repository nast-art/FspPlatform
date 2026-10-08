using FspPlatformApi.Data.Entities;
using FspPlatformApi.DTOs.Ats;
using FspPlatformApi.Services.Infrastructure;
using FspPlatformApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FspPlatformApi.Services.Platform;

/// <summary>
/// ATS-интеграция — концепция за рамками MVP (ТЗ «будет плюсом»): конфигурация и маппинг полей
/// в config JSONB, заглушка синхронизации. Реальный адаптер (Greenhouse/Lever/custom) подключается
/// реализацией IAtsProvider под конкретный provider — точка расширения зафиксирована.
/// </summary>
public class AtsIntegrationService : IAtsIntegrationService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;
    public AtsIntegrationService(ApplicationDbContext db, IAuditService audit) => (_db, _audit) = (db, audit);

    private async Task<Company> MyCompanyAsync(Guid ownerUserId) =>
        await _db.Companies.FirstAsync(c => c.OwnerUserId == ownerUserId && c.Status == "активен")    ?? throw new InvalidOperationException("Сначала создайте профиль компании.");

    public async Task<AtsIntegrationDto> UpsertAsync(Guid ownerUserId, AtsIntegrationUpsertDto dto)
    {
        var company = await MyCompanyAsync(ownerUserId);
        var existing = await _db.AtsIntegrations
            .FirstOrDefaultAsync(i => i.CompanyId == company.Id && i.Provider == dto.Provider);
        if (existing is null)
        {
            existing = new AtsIntegration { CompanyId = company.Id, Provider = dto.Provider, IsActive = false };
            _db.AtsIntegrations.Add(existing);
        }
        existing.Config = dto.Config.GetRawText(); // jsonb-поле скаффолдлено как string
        await _db.SaveChangesAsync();
        return Map(existing);
    }

    public async Task<List<AtsIntegrationDto>> GetMyAsync(Guid ownerUserId)
    {
        var company = await MyCompanyAsync(ownerUserId);
        return await _db.AtsIntegrations.Where(i => i.CompanyId == company.Id)
            .Select(i => new AtsIntegrationDto
            {
                Id = i.Id, Provider = i.Provider,
                Config = Jsonb.Element(i.Config) ?? default,
                IsActive = i.IsActive, LastSyncAt = i.LastSyncAt
            }).ToListAsync();
    }

    public async Task TriggerSyncAsync(Guid ownerUserId, Guid integrationId)
    {
        var company = await MyCompanyAsync(ownerUserId);
        var i = await _db.AtsIntegrations
            .FirstAsync(x => x.Id == integrationId && x.CompanyId == company.Id);
        // Здесь будет вызов адаптера ATS по endpoint из config; фиксируем факт синхронизации.
        i.LastSyncAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(ownerUserId, "ats_integrations", integrationId.ToString(), "sync_triggered");
    }

    private static AtsIntegrationDto Map(AtsIntegration i) => new()
    {
        Id = i.Id, Provider = i.Provider,
        Config = Jsonb.Element(i.Config) ?? default,
        IsActive = i.IsActive, LastSyncAt = i.LastSyncAt
    };
}