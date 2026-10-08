using FspPlatformApi.DTOs.Notifications;

namespace FspPlatformApi.Services.Interfaces;

public interface INotificationService
{
    Task<List<NotificationDto>> GetMyAsync(Guid userId);
    Task MarkReadAsync(Guid userId, Guid notificationId);
    Task CreateAsync(Guid userId, string type, object payload);
}