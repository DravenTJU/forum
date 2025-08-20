using Forum.Api.Models.Entities;

namespace Forum.Api.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(long id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUsernameAsync(string username);
    Task<long> CreateAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(long id);
}