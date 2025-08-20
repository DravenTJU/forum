using Forum.Api.Models.Entities;
using Forum.Api.Repositories;

namespace Forum.Api.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User?> GetByIdAsync(long id)
    {
        return await _userRepository.GetByIdAsync(id);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _userRepository.GetByUsernameAsync(username);
    }

    public async Task UpdateProfileAsync(long userId, string? bio, string? avatarUrl)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        user.Bio = bio;
        user.AvatarUrl = avatarUrl;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
    }
}