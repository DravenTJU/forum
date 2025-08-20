namespace Forum.Api.Infrastructure.Email;

public interface IEmailService
{
    Task SendEmailVerificationAsync(string toEmail, string username, string verificationToken);
    Task SendPasswordResetAsync(string toEmail, string username, string resetToken);
}