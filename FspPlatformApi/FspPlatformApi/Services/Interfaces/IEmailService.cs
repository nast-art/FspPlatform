namespace FspPlatformApi.Services.Interfaces;

public interface IEmailService
{
    Task SendEmailConfirmationAsync(string email, string confirmUrl);
    Task SendNotificationAsync(string email, string subject, string body);
}