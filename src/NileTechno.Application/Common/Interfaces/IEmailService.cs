namespace NileTechno.Application.Common.Interfaces;

public record EmailSendResult(bool Success, string? Error = null);

public interface IEmailService
{
    Task<EmailSendResult> SendWelcomeEmailAsync(string toEmail, string fullName, CancellationToken ct = default);
    Task<EmailSendResult> SendVerificationEmailAsync(string toEmail, string fullName, string verificationLink, CancellationToken ct = default);
    Task<EmailSendResult> SendPasswordResetEmailAsync(string toEmail, string fullName, string resetLink, CancellationToken ct = default);
    Task<EmailSendResult> SendLoginNotificationEmailAsync(string toEmail, string fullName, string ipAddress, CancellationToken ct = default);
    Task<EmailSendResult> SendOrderStatusEmailAsync(string toEmail, string customerName, string orderNumber, string status, CancellationToken ct = default);
    Task<EmailSendResult> SendGenericEmailAsync(string toEmail, string subject, string title, string messageHtml, CancellationToken ct = default);
    Task<EmailSendResult> TestSmtpAsync(CancellationToken ct = default);
}
