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

    public async Task<IEnumerable<Tag>> GetByTopicIdAsync(long topicId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            SELECT t.id, t.name, t.slug, t.description, t.color, t.usage_count as UsageCount,
                   t.created_at as CreatedAt, t.updated_at as UpdatedAt
            FROM tags t
            INNER JOIN topic_tags tt ON t.id = tt.tag_id
            WHERE tt.topic_id = @TopicId
            ORDER BY t.name";

        return await connection.QueryAsync<Tag>(sql, new { TopicId = topicId });
    }

    public async Task<Dictionary<long, IEnumerable<Tag>>> GetByTopicIdsAsync(IEnumerable<long> topicIds)
    {
        if (!topicIds.Any())
        {
            return new Dictionary<long, IEnumerable<Tag>>();
        }

        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            SELECT tt.topic_id as TopicId, t.id, t.name, t.slug, t.description, t.color, 
                   t.usage_count as UsageCount, t.created_at as CreatedAt, t.updated_at as UpdatedAt
            FROM tags t
            INNER JOIN topic_tags tt ON t.id = tt.tag_id
            WHERE tt.topic_id IN @TopicIds
            ORDER BY tt.topic_id, t.name";

        var result = await connection.QueryAsync<dynamic>(sql, new { TopicIds = topicIds.ToArray() });
        
        return result
            .GroupBy(r => (long)r.TopicId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(r => new Tag
                {
                    Id = r.id,
                    Name = r.name,
                    Slug = r.slug,
                    Description = r.description,
                    Color = r.color,
                    UsageCount = r.UsageCount,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                }).AsEnumerable()
            );
    }
}