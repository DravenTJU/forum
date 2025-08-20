using Dapper;
using Forum.Api.Infrastructure.Database;
using Forum.Api.Models.Entities;

namespace Forum.Api.Repositories;

public class TagRepository : ITagRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public TagRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<Tag>> GetAllAsync()
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            SELECT id, name, slug, description, color, usage_count as UsageCount,
                   created_at as CreatedAt, updated_at as UpdatedAt
            FROM tags 
            ORDER BY usage_count DESC, name";

        return await connection.QueryAsync<Tag>(sql);
    }

    public async Task<Tag?> GetByIdAsync(long id)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            SELECT id, name, slug, description, color, usage_count as UsageCount,
                   created_at as CreatedAt, updated_at as UpdatedAt
            FROM tags 
            WHERE id = @Id";

        return await connection.QuerySingleOrDefaultAsync<Tag>(sql, new { Id = id });
    }

    public async Task<Tag?> GetBySlugAsync(string slug)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            SELECT id, name, slug, description, color, usage_count as UsageCount,
                   created_at as CreatedAt, updated_at as UpdatedAt
            FROM tags 
            WHERE slug = @Slug";

        return await connection.QuerySingleOrDefaultAsync<Tag>(sql, new { Slug = slug });
    }

    public async Task<long> CreateAsync(Tag tag)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            INSERT INTO tags (name, slug, description, color, usage_count)
            VALUES (@Name, @Slug, @Description, @Color, @UsageCount);
            SELECT LAST_INSERT_ID();";

        return await connection.QuerySingleAsync<long>(sql, tag);
    }

    public async Task UpdateAsync(Tag tag)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            UPDATE tags 
            SET name = @Name, slug = @Slug, description = @Description, 
                color = @Color, usage_count = @UsageCount
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, tag);
    }

    public async Task DeleteAsync(long id)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = "DELETE FROM tags WHERE id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id });
    }
}