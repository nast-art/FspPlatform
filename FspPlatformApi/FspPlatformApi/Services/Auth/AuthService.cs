using System.Net;
using System.Security.Cryptography;
using System.Text;
using FspPlatformApi.Data.Entities;
using FspPlatformApi.DTOs.Auth;
using FspPlatformApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FspPlatformApi.Services.Auth;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _db;
    private readonly ITokenService _tokens;
    private readonly IEmailService _email;
    private readonly IConfiguration _cfg;

    public AuthService(ApplicationDbContext db, ITokenService tokens, IEmailService email, IConfiguration cfg)
        => (_db, _tokens, _email, _cfg) = (db, tokens, email, cfg);

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto, string? ipAddress)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            throw new InvalidOperationException("Пользователь с таким email уже существует.");

        Guid? fspId = dto.FspId is null ? null : Guid.Parse(dto.FspId);
        if (fspId is not null && await _db.Users.AnyAsync(u => u.FspId == fspId))
            throw new InvalidOperationException("Этот ID участника ФСП уже привязан к другому аккаунту.");

        var user = new User
        {
            Id = Guid.NewGuid(),  
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = dto.Role,
            IsActive = true, 
            FspId = fspId,
            FspLinkedAt = fspId is not null ? DateTime.UtcNow : null
        };
        _db.Users.Add(user);

        if (dto.Role == "кандидат")
        {
            _db.CandidateProfiles.Add(new CandidateProfile { UserId = user.Id });
            _db.ConsentLogs.Add(new ConsentLog
            {
                UserId = user.Id,
                ConsentType = "условия_сервиса",
                Granted = true,
                TermsVersion = _cfg["Legal:TermsVersion"] ?? "1.0",
                // колонка inet → в сущности System.Net.IPAddress: строку парсим, невалидный IP — null
                IpAddress = IPAddress.TryParse(ipAddress, out var ip) ? ip : null
            });
        }

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _db.AuthTokens.Add(new AuthToken
        {
            UserId = user.Id,
            TokenType = "подтверждение_почты",
            TokenHash = ComputeHash(rawToken),
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        });

        await _db.SaveChangesAsync();
        await _email.SendEmailConfirmationAsync(email,
            $"{_cfg["App:PublicUrl"]}/api/auth/confirm-email?token={rawToken}");

        return new AuthResponseDto
        {
            UserId = user.Id, Email = email, Role = user.Role,
            Token = _tokens.CreateToken(user),
            EmailConfirmed = user.EmailConfirmed, FspId = user.FspId
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive)
            ?? throw new UnauthorizedAccessException("Неверный email или пароль.");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Неверный email или пароль.");

        // ТЗ 2.2 п.4: подтверждение адреса. Для демо ослабляется конфигом RequireConfirmedEmail=false.
        if (_cfg.GetValue<bool>("Auth:RequireConfirmedEmail") && !user.EmailConfirmed)
            throw new UnauthorizedAccessException("Email не подтверждён. Проверьте почту.");

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return new AuthResponseDto
        {
            UserId = user.Id, Email = email, Role = user.Role,
            Token = _tokens.CreateToken(user),
            EmailConfirmed = user.EmailConfirmed, FspId = user.FspId
        };
    }

    public async Task ConfirmEmailAsync(string token)
    {
        var hash = ComputeHash(token);
        var authToken = await _db.AuthTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash
                && t.TokenType == "подтверждение_почты");

        // Токен не найден или срок действия истёк — действительно битая ссылка.
        if (authToken is null || authToken.ExpiresAt <= DateTime.UtcNow)
            throw new InvalidOperationException("Токен недействителен или истёк.");

        // Токен уже использован, но почта подтверждена — повторный клик/дубль
        // запроса не считаем ошибкой (идемпотентность, в т.ч. StrictMode на фронте).
        if (authToken.UsedAt is not null)
        {
            if (authToken.User.EmailConfirmed)
                return;
            throw new InvalidOperationException("Токен недействителен или истёк.");
        }

        authToken.UsedAt = DateTime.UtcNow;
        authToken.User.EmailConfirmed = true;
        await _db.SaveChangesAsync();
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        var user = await _db.Users.FindAsync(userId) ?? throw new UnauthorizedAccessException();
        if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.PasswordHash))
            throw new InvalidOperationException("Старый пароль неверен.");
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _db.SaveChangesAsync();
    }

    private static string ComputeHash(string raw) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
}