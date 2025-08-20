using Forum.Api.Models.Entities;

namespace Forum.Api.Services;

public interface IUserService
{
    Task<User?> GetByIdAsync(long id);
    Task<User?> GetByUsernameAsync(string username);
    Task UpdateProfileAsync(long userId, string? bio, string? avatarUrl);
}