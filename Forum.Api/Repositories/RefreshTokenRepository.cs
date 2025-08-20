using Dapper;
using Forum.Api.Infrastructure.Database;
using Forum.Api.Models.Entities;

namespace Forum.Api.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RefreshTokenRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<long> CreateAsync(RefreshToken refreshToken)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            INSERT INTO refresh_tokens (user_id, token_hash, expires_at, ua, ip, created_at)
            VALUES (@UserId, @TokenHash, @ExpiresAt, @UserAgent, @IpAddress, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        return await connection.QuerySingleAsync<long>(sql, refreshToken);
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(byte[] tokenHash)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            SELECT id, user_id as UserId, token_hash as TokenHash, expires_at as ExpiresAt, 
                   revoked_at as RevokedAt, ua as UserAgent, ip as IpAddress, created_at as CreatedAt
            FROM refresh_tokens 
            WHERE token_hash = @TokenHash AND revoked_at IS NULL AND expires_at > @Now";

        return await connection.QuerySingleOrDefaultAsync<RefreshToken>(sql, new { 
            TokenHash = tokenHash, 
            Now = DateTime.UtcNow 
        });
    }

    public async Task RevokeAsync(long id)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            UPDATE refresh_tokens 
            SET revoked_at = @RevokedAt 
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, new { Id = id, RevokedAt = DateTime.UtcNow });
    }

    public async Task RevokeAllForUserAsync(long userId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            UPDATE refresh_tokens 
            SET revoked_at = @RevokedAt 
            WHERE user_id = @UserId AND revoked_at IS NULL";

        await connection.ExecuteAsync(sql, new { UserId = userId, RevokedAt = DateTime.UtcNow });
    }
}