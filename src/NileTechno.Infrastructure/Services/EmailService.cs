using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NileTechno.Application.Common.Interfaces;

namespace NileTechno.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task<EmailSendResult> SendWelcomeEmailAsync(string toEmail, string fullName, CancellationToken ct = default)
        => SendAsync(toEmail, "مرحباً بك", $"مرحباً {fullName}، تم إنشاء حسابك بنجاح.", ct);

    public Task<EmailSendResult> SendVerificationEmailAsync(string toEmail, string fullName, string verificationLink, CancellationToken ct = default)
        => SendAsync(toEmail, "تفعيل الحساب", $"مرحباً {fullName}، رابط التفعيل:<br/><a href=\"{verificationLink}\">{verificationLink}</a>", ct);

    public Task<EmailSendResult> SendPasswordResetEmailAsync(string toEmail, string fullName, string resetLink, CancellationToken ct = default)
        => SendAsync(toEmail, "استعادة كلمة المرور", $"مرحباً {fullName}، رابط الاستعادة:<br/><a href=\"{resetLink}\">{resetLink}</a>", ct);

    public Task<EmailSendResult> SendLoginNotificationEmailAsync(string toEmail, string fullName, string ipAddress, CancellationToken ct = default)
        => SendAsync(toEmail, "تنبيه تسجيل دخول", $"مرحباً {fullName}، تم تسجيل الدخول من {ipAddress}.", ct);

    public Task<EmailSendResult> SendOrderStatusEmailAsync(string toEmail, string customerName, string orderNumber, string status, CancellationToken ct = default)
        => SendAsync(toEmail, $"تحديث الطلب {orderNumber}", $"مرحباً {customerName}، حالة الطلب {orderNumber} أصبحت: {status}.", ct);

    public Task<EmailSendResult> SendGenericEmailAsync(string toEmail, string subject, string title, string messageHtml, CancellationToken ct = default)
        => SendAsync(toEmail, subject, $"<h3>{title}</h3>{messageHtml}", ct);

    public async Task<EmailSendResult> TestSmtpAsync(CancellationToken ct = default)
    {
        try
        {
            using var client = CreateClient();
            if (client is null)
                return new EmailSendResult(false, "SMTP is not configured");

            await client.SendMailAsync(BuildMessage(
                _configuration["Smtp:FromEmail"] ?? _configuration["Smtp:User"] ?? "noreply@local",
                "SMTP test",
                "SMTP connection ok"), ct);
            return new EmailSendResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP test failed");
            return new EmailSendResult(false, ex.Message);
        }
    }

    private async Task<EmailSendResult> SendAsync(string to, string subject, string html, CancellationToken ct)
    {
        try
        {
            using var client = CreateClient();
            if (client is null)
            {
                _logger.LogInformation("Email to {To}: {Subject}", to, subject);
                return new EmailSendResult(true);
            }

            await client.SendMailAsync(BuildMessage(to, subject, html), ct);
            return new EmailSendResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email send failed to {To}", to);
            return new EmailSendResult(false, ex.Message);
        }
    }

    private SmtpClient? CreateClient()
    {
        var host = _configuration["Smtp:Host"];
        var user = _configuration["Smtp:User"];
        var password = _configuration["Smtp:Password"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
            return null;

        var port = int.TryParse(_configuration["Smtp:Port"], out var p) ? p : 587;
        return new SmtpClient(host, port)
        {
            EnableSsl = port != 25,
            Credentials = new NetworkCredential(user, password)
        };
    }

    private MailMessage BuildMessage(string to, string subject, string html)
    {
        var fromEmail = _configuration["Smtp:FromEmail"] ?? _configuration["Smtp:User"] ?? "noreply@local";
        var fromName = _configuration["Smtp:FromName"] ?? "NileTechno";
        var message = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = subject,
            Body = html,
            IsBodyHtml = true
        };
        message.To.Add(to);
        return message;
    }
}
