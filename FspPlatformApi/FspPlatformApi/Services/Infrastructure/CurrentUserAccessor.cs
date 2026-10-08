using System.Security.Claims;
using FspPlatformApi.Services.Interfaces;

namespace FspPlatformApi.Services.Infrastructure;

public class CurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _http;
    public CurrentUserAccessor(IHttpContextAccessor http) => _http = http;

    public bool IsAuthenticated => _http.HttpContext?.User.Identity?.IsAuthenticated == true;

    public Guid UserId =>
        Guid.TryParse(_http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id : Guid.Empty;

    public string Role => _http.HttpContext?.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
}