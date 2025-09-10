using Forum.Api.Infrastructure.Email;
using Moq;

namespace Forum.Api.Tests.Mocks;

/// <summary>
/// 邮件服务Mock配置
/// 提供可验证的邮件发送模拟
/// </summary>
public class MockEmailService
{
    public Mock<IEmailService> Mock { get; }
    public IEmailService Object => Mock.Object;

    // 捕获的邮件发送记录
    public List<EmailRecord> SentEmails { get; } = new();

    public MockEmailService()
    {
        Mock = new Mock<IEmailService>();
        SetupDefaultBehavior();
    }

    /// <summary>
    /// 设置默认Mock行为
    /// </summary>
    private void SetupDefaultBehavior()
    {
        // 邮箱验证邮件
        Mock.Setup(x => x.SendEmailVerificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask)
            .Callback<string, string, string>((email, username, token) =>
            {
                SentEmails.Add(new EmailRecord
                {
                    Type = EmailType.Verification,
                    ToEmail = email,
                    Username = username,
                    Token = token,
                    SentAt = DateTime.UtcNow
                });
            });

        // 密码重置邮件
        Mock.Setup(x => x.SendPasswordResetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask)
            .Callback<string, string, string>((email, username, token) =>
            {
                SentEmails.Add(new EmailRecord
                {
                    Type = EmailType.PasswordReset,
                    ToEmail = email,
                    Username = username,
                    Token = token,
                    SentAt = DateTime.UtcNow
                });
            });

        // 通知邮件（@提及等）
        Mock.Setup(x => x.SendNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask)
            .Callback<string, string, string, string>((email, username, subject, content) =>
            {
                SentEmails.Add(new EmailRecord
                {
                    Type = EmailType.Notification,
                    ToEmail = email,
                    Username = username,
                    Subject = subject,
                    Content = content,
                    SentAt = DateTime.UtcNow
                });
            });
    }

    /// <summary>
    /// 设置邮件发送失败
    /// </summary>
    public void SetupSendFailure(EmailType emailType, Exception exception)
    {
        switch (emailType)
        {
            case EmailType.Verification:
                Mock.Setup(x => x.SendEmailVerificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                    .ThrowsAsync(exception);
                break;
            case EmailType.PasswordReset:
                Mock.Setup(x => x.SendPasswordResetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                    .ThrowsAsync(exception);
                break;
            case EmailType.Notification:
                Mock.Setup(x => x.SendNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                    .ThrowsAsync(exception);
                break;
        }
    }

    /// <summary>
    /// 设置邮件发送延迟
    /// </summary>
    public void SetupSendDelay(TimeSpan delay)
    {
        Mock.Setup(x => x.SendEmailVerificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(async () =>
            {
                await Task.Delay(delay);
            });

        Mock.Setup(x => x.SendPasswordResetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(async () =>
            {
                await Task.Delay(delay);
            });
    }

    /// <summary>
    /// 验证邮件是否已发送
    /// </summary>
    public bool WasEmailSent(EmailType type, string toEmail)
    {
        return SentEmails.Any(e => e.Type == type && e.ToEmail == toEmail);
    }

    /// <summary>
    /// 获取发送给指定邮箱的邮件
    /// </summary>
    public List<EmailRecord> GetEmailsTo(string email)
    {
        return SentEmails.Where(e => e.ToEmail == email).ToList();
    }

    /// <summary>
    /// 获取特定类型的邮件
    /// </summary>
    public List<EmailRecord> GetEmailsByType(EmailType type)
    {
        return SentEmails.Where(e => e.Type == type).ToList();
    }

    /// <summary>
    /// 清空邮件记录
    /// </summary>
    public void ClearSentEmails()
    {
        SentEmails.Clear();
    }

    /// <summary>
    /// 重置Mock
    /// </summary>
    public void Reset()
    {
        Mock.Reset();
        ClearSentEmails();
        SetupDefaultBehavior();
    }

    /// <summary>
    /// 验证邮箱验证邮件发送次数
    /// </summary>
    public void VerifyVerificationEmailSent(string email, Times times)
    {
        Mock.Verify(x => x.SendEmailVerificationAsync(email, It.IsAny<string>(), It.IsAny<string>()), times);
    }

    /// <summary>
    /// 验证密码重置邮件发送次数
    /// </summary>
    public void VerifyPasswordResetEmailSent(string email, Times times)
    {
        Mock.Verify(x => x.SendPasswordResetAsync(email, It.IsAny<string>(), It.IsAny<string>()), times);
    }
}

/// <summary>
/// 邮件发送记录
/// </summary>
public class EmailRecord
{
    public EmailType Type { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Token { get; set; }
    public string? Subject { get; set; }
    public string? Content { get; set; }
    public DateTime SentAt { get; set; }
}

/// <summary>
/// 邮件类型枚举
/// </summary>
public enum EmailType
{
    Verification,
    PasswordReset,
    Notification
}