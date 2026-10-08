using System.Net;
using System.Text.Json;
using FspPlatformApi.Data.Entities;
using FspPlatformApi.Services.Interfaces;

namespace FspPlatformApi.Services.Infrastructure;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;
    public AuditService(ApplicationDbContext db) => _db = db;

    public async Task LogAsync(Guid? userId, string entity, string entityId, string action,
        object? oldValue = null, object? newValue = null, string? ip = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Entity = entity,
            EntityId = entityId,
            Action = action,
            OldValue = oldValue is null ? null : JsonSerializer.Serialize(oldValue), // jsonb как string
            NewValue = newValue is null ? null : JsonSerializer.Serialize(newValue),
            // колонка inet → в сущности System.Net.IPAddress: строку парсим, невалидный IP — null
            IpAddress = IPAddress.TryParse(ip, out var ipAddr) ? ipAddr : null
        });
        await _db.SaveChangesAsync();
    }
}