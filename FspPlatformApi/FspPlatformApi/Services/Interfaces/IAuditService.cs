namespace FspPlatformApi.Services.Interfaces;

public interface IAuditService
{
    Task LogAsync(Guid? userId, string entity, string entityId, string action,
        object? oldValue = null, object? newValue = null, string? ip = null);
}