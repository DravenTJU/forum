namespace Forum.Api.Infrastructure.Auth;

public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hashedPassword);
}