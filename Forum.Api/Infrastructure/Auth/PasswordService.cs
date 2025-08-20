using System.Security.Cryptography;
using System.Text;

namespace Forum.Api.Infrastructure.Auth;

public class PasswordService : IPasswordService
{
    public string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var salt = GenerateSalt();
        var passwordBytes = Encoding.UTF8.GetBytes(password).Concat(salt).ToArray();
        var hashBytes = sha256.ComputeHash(passwordBytes);
        return Convert.ToBase64String(hashBytes) + ":" + Convert.ToBase64String(salt);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        try
        {
            var parts = hashedPassword.Split(':');
            if (parts.Length != 2) return false;

            var hash = Convert.FromBase64String(parts[0]);
            var salt = Convert.FromBase64String(parts[1]);

            using var sha256 = SHA256.Create();
            var passwordBytes = Encoding.UTF8.GetBytes(password).Concat(salt).ToArray();
            var testHash = sha256.ComputeHash(passwordBytes);

            return hash.SequenceEqual(testHash);
        }
        catch
        {
            return false;
        }
    }

    private static byte[] GenerateSalt()
    {
        var salt = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        return salt;
    }
}