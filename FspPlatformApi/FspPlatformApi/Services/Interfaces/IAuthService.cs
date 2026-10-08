using FspPlatformApi.DTOs.Auth;

namespace FspPlatformApi.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto, string? ipAddress);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task ConfirmEmailAsync(string token);
    Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
}