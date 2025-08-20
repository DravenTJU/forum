using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Forum.Api.Infrastructure.Email;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendEmailVerificationAsync(string toEmail, string username, string verificationToken)
    {
        var subject = "验证您的邮箱地址";
        var body = $"""
            您好 {username}，

            请点击以下链接验证您的邮箱地址：
            http://localhost:5173/verify-email?token={verificationToken}

            如果您没有注册我们的论坛，请忽略此邮件。

            谢谢！
            论坛团队
            """;

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendPasswordResetAsync(string toEmail, string username, string resetToken)
    {
        var subject = "重置您的密码";
        var body = $"""
            您好 {username}，

            请点击以下链接重置您的密码：
            http://localhost:5173/reset-password?token={resetToken}

            如果您没有请求重置密码，请忽略此邮件。

            谢谢！
            论坛团队
            """;

        await SendEmailAsync(toEmail, subject, body);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            message.Body = new TextPart("plain")
            {
                Text = body
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_settings.SmtpUsername, _settings.SmtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            throw;
        }
    }
}