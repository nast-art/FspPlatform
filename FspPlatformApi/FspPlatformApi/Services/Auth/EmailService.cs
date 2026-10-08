using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using FspPlatformApi.Services.Interfaces;

namespace FspPlatformApi.Services.Auth;

/// <summary>
/// Отправка писем через SMTP (MailKit).
/// Если Smtp:Host не задан — письмо логируется в консоль (режим разработки,
/// чтобы сквозной сценарий работал без почтового сервера). При заданном Host —
/// реальная отправка: подключение с TLS/SSL, аутентификация, отправка, корректное закрытие.
///
/// Настройки (appsettings.json / переменные окружения Smtp__*):
///   Smtp:Host      — сервер исходящей почты (например, smtp.yandex.ru);
///   Smtp:Port      — 465 при Ssl=true, 587 при Ssl=false (по умолчанию 465);
///   Smtp:Ssl       — true: TLS сразу (SslOnConnect); false: STARTTLS;
///   Smtp:NoTls     — true: без шифрования вообще (None) — для локальных
///                    SMTP-ловушек (Maildev/MailHog); по умолчанию false.
///   Smtp:User      — логин (обычно полный адрес);
///   Smtp:Password  — пароль ПРИЛОЖЕНИЯ (не пароль аккаунта!);
///   Smtp:From      — адрес отправителя;
///   Smtp:FromName  — имя отправителя;
///   Smtp:TimeoutMs — таймаут операций (по умолчанию 10000).
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _cfg;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration cfg, ILogger<EmailService> logger)
        => (_cfg, _logger) = (cfg, logger);

    public async Task SendEmailConfirmationAsync(string email, string confirmUrl)
    {
        const string subject = "Подтвержение регистрации — Платформа ФСП";
        var textBody =
            "Здравствуйте!\n\n" +
            "Вы зарегистрировались на цифровой платформе подбора ИТ-специалистов " +
            "Федерации спортивного программирования.\n\n" +
            "Подтвердите адрес электронной почты, перейдя по ссылке:\n" +
            confirmUrl + "\n\n" +
            "Ссылка действует 24 часа и может быть использована один раз.\n\n" +
            "Если вы не регистрировались — просто проигнорируйте это письмо.\n\n" +
            "— Команда платформы ФСП";

        // HTML-версия с кнопкой-гиперссылкой (MailKit отправит обе части:
        // text/plain — запасной вариант, text/html — с кликабельной ссылкой).
        var htmlBody =
            "<div style=\"font-family:Arial,sans-serif;font-size:14px;line-height:1.6;color:#111\">" +
            "<p>Здравствуйте!</p>" +
            "<p>Вы зарегистрировались на цифровой платформе подбора ИТ-специалистов " +
            "Федерации спортивного программирования.</p>" +
            "<p>Подтвердите адрес электронной почты:</p>" +
            "<p><a href=\"" + confirmUrl + "\" " +
            "style=\"display:inline-block;background:#4f46e5;color:#ffffff;padding:12px 28px;" +
            "border-radius:8px;text-decoration:none;font-weight:bold\">Подтвердить email</a></p>" +
            "<p style=\"font-size:12px;color:#555\">Если кнопка не работает, откройте ссылку в браузере:<br>" +
            "<a href=\"" + confirmUrl + "\">" + confirmUrl + "</a></p>" +
            "<p>Ссылка действует 24 часа и может быть использована один раз.</p>" +
            "<p>Если вы не регистрировались — просто проигнорируйте это письмо.</p>" +
            "<p>— Команда платформы ФСП</p></div>";

        await SendAsync(email, subject, textBody, htmlBody);
    }

    public async Task SendNotificationAsync(string email, string subject, string body)
        => await SendAsync(email, subject, body);

    private async Task SendAsync(string to, string subject, string body, string? htmlBody = null)
    {
        var smtp = _cfg.GetSection("Smtp");
        var host = smtp["Host"];

        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogInformation("[DEV-EMAIL] To={To} | Subject={Subject} | Body={Body}", to, subject, body);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            smtp["FromName"] ?? "Платформа ФСП",
            smtp["From"] ?? "no-reply@fsp.ru"));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        // Обе части: text/plain — fallback, text/html — кнопка-гиперссылка.
        message.Body = htmlBody is null
            ? new TextPart("plain") { Text = body }
            : new BodyBuilder { TextBody = body, HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();

        var port = smtp.GetValue<int>("Port", 465);
        var timeoutMs = smtp.GetValue<int>("TimeoutMs", 10000);
        client.Timeout = timeoutMs;

        // Ssl=true   → порт 465, TLS сразу (SecureSocketOptions.SslOnConnect)
        // Ssl=false  → порт 587, STARTTLS (SecureSocketOptions.StartTls)
        // NoTls=true → без шифрования вообще (SecureSocketOptions.None) —
        //                единственный режим, совместимый с Maildev/MailHog.
        var useSsl = smtp.GetValue<bool>("Ssl", true);
        var socketOptions = smtp.GetValue<bool>("NoTls", false)
            ? SecureSocketOptions.None
            : useSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;

        await client.ConnectAsync(host, port, socketOptions);

        var user = smtp["User"];
        if (!string.IsNullOrWhiteSpace(user))
            await client.AuthenticateAsync(user, smtp["Password"] ?? string.Empty);

        await client.SendAsync(message);
        await client.DisconnectAsync(quit: true);

        _logger.LogInformation("Письмо отправлено на {To}: {Subject}", to, subject);
    }
}