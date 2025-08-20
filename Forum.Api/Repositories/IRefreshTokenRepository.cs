using Forum.Api.Models.Entities;

namespace Forum.Api.Repositories;

public interface IRefreshTokenRepository
{
    Task<long> CreateAsync(RefreshToken refreshToken);
    Task<RefreshToken?> GetByTokenHashAsync(byte[] tokenHash);
    Task RevokeAsync(long id);
    Task RevokeAllForUserAsync(long userId);
}