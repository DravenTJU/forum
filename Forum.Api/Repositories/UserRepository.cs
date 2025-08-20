using Dapper;
using Forum.Api.Infrastructure.Database;
using Forum.Api.Models.Entities;

namespace Forum.Api.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetByIdAsync(long id)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            SELECT id, username, email, password_hash as PasswordHash, status, 
                   email_verified as EmailVerified, avatar_url as AvatarUrl, bio,
                   last_seen_at as LastSeenAt, created_at as CreatedAt, updated_at as UpdatedAt
            FROM users 
            WHERE id = @Id";

        return await connection.QuerySingleOrDefaultAsync<User>(sql, new { Id = id });
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            SELECT id, username, email, password_hash as PasswordHash, status, 
                   email_verified as EmailVerified, avatar_url as AvatarUrl, bio,
                   last_seen_at as LastSeenAt, created_at as CreatedAt, updated_at as UpdatedAt
            FROM users 
            WHERE email = @Email";

        return await connection.QuerySingleOrDefaultAsync<User>(sql, new { Email = email });
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            SELECT id, username, email, password_hash as PasswordHash, status, 
                   email_verified as EmailVerified, avatar_url as AvatarUrl, bio,
                   last_seen_at as LastSeenAt, created_at as CreatedAt, updated_at as UpdatedAt
            FROM users 
            WHERE username = @Username";

        return await connection.QuerySingleOrDefaultAsync<User>(sql, new { Username = username });
    }

    public async Task<long> CreateAsync(User user)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            INSERT INTO users (username, email, password_hash, status, email_verified, avatar_url, bio)
            VALUES (@Username, @Email, @PasswordHash, 'active', @EmailVerified, @AvatarUrl, @Bio);
            SELECT LAST_INSERT_ID();";

        return await connection.QuerySingleAsync<long>(sql, user);
    }

    public async Task UpdateAsync(User user)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            UPDATE users 
            SET username = @Username, email = @Email, password_hash = @PasswordHash, 
                status = @Status, email_verified = @EmailVerified, avatar_url = @AvatarUrl, 
                bio = @Bio, last_seen_at = @LastSeenAt
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, user);
    }

    public async Task DeleteAsync(long id)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = "DELETE FROM users WHERE id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id });
    }
}