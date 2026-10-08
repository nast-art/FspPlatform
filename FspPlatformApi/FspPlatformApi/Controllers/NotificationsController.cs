using FspPlatformApi.DTOs.Notifications;
using FspPlatformApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FspPlatformApi.Controllers;

[Authorize]
[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;
    private readonly ICurrentUserAccessor _current;
    public NotificationsController(INotificationService notifications, ICurrentUserAccessor current)
        => (_notifications, _current) = (notifications, current);

    [HttpGet]
    public async Task<ActionResult<List<NotificationDto>>> Get()
        => Ok(await _notifications.GetMyAsync(_current.UserId));

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> Read(Guid id)
    {
        await _notifications.MarkReadAsync(_current.UserId, id);
        return NoContent();
    }
}