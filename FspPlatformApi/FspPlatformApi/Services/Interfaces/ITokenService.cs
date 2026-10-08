using FspPlatformApi.Data.Entities;

namespace FspPlatformApi.Services.Interfaces;

public interface ITokenService
{
    string CreateToken(User user);
}