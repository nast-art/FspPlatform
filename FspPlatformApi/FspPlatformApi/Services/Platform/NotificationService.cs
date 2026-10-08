using System.Text.Json;
using FspPlatformApi.Data.Entities;
using FspPlatformApi.DTOs.Notifications;
using FspPlatformApi.Services.Infrastructure;
using FspPlatformApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FspPlatformApi.Services.Platform;

public class NotificationService : INotificationService
{
    private static readonly JsonElement EmptyObject = JsonDocument.Parse("{}").RootElement;

    private readonly ApplicationDbContext _db;
    public NotificationService(ApplicationDbContext db) => _db = db;

    public async Task<List<NotificationDto>> GetMyAsync(Guid userId) =>
        await _db.Notifications.Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt).Take(50)
            .Select(n => new NotificationDto
            {
                Id = n.Id, Type = n.Type,
                Payload = Jsonb.Element(n.Payload) ?? EmptyObject,
                CreatedAt = n.CreatedAt, ReadAt = n.ReadAt
            }).ToListAsync();

    public async Task MarkReadAsync(Guid userId, Guid notificationId)
    {
        var n = await _db.Notifications
            .FirstOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId)
            ?? throw new UnauthorizedAccessException();
        n.ReadAt ??= DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task CreateAsync(Guid userId, string type, object payload)
    {
        _db.Notifications.Add(new Notification
        {
            UserId = userId, Type = type,
            Payload = JsonSerializer.Serialize(payload) // jsonb-поле скаффолдлено как string
        });
        await _db.SaveChangesAsync();
    }
}