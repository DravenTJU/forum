using Dapper;
using Forum.Api.Infrastructure.Database;
using Forum.Api.Models.Entities;

namespace Forum.Api.Repositories;

public class PostRepository : IPostRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PostRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<Post>> GetByTopicIdAsync(long topicId, int limit = 20, long? cursorId = null, DateTime? cursorCreated = null)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        var sql = @"
            SELECT id, topic_id as TopicId, author_id as AuthorId, content_md as ContentMd,
                   reply_to_post_id as ReplyToPostId, is_edited as IsEdited, is_deleted as IsDeleted,
                   deleted_at as DeletedAt, created_at as CreatedAt, updated_at as UpdatedAt
            FROM posts 
            WHERE topic_id = @TopicId AND is_deleted = 0";

        var parameters = new DynamicParameters();
        parameters.Add("TopicId", topicId);

        if (cursorId.HasValue && cursorCreated.HasValue)
        {
            sql += " AND (created_at > @CursorCreated OR (created_at = @CursorCreated AND id > @CursorId))";
            parameters.Add("CursorCreated", cursorCreated);
            parameters.Add("CursorId", cursorId);
        }

        sql += " ORDER BY created_at ASC, id ASC LIMIT @Limit";
        parameters.Add("Limit", limit);

        return await connection.QueryAsync<Post>(sql, parameters);
    }

    public async Task<Post?> GetByIdAsync(long id)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            SELECT id, topic_id as TopicId, author_id as AuthorId, content_md as ContentMd,
                   reply_to_post_id as ReplyToPostId, is_edited as IsEdited, is_deleted as IsDeleted,
                   deleted_at as DeletedAt, created_at as CreatedAt, updated_at as UpdatedAt
            FROM posts 
            WHERE id = @Id";

        return await connection.QuerySingleOrDefaultAsync<Post>(sql, new { Id = id });
    }

    public async Task<long> CreateAsync(Post post)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            INSERT INTO posts (topic_id, author_id, content_md, reply_to_post_id, 
                              is_edited, is_deleted, deleted_at)
            VALUES (@TopicId, @AuthorId, @ContentMd, @ReplyToPostId, 
                    @IsEdited, @IsDeleted, @DeletedAt);
            SELECT LAST_INSERT_ID();";

        return await connection.QuerySingleAsync<long>(sql, post);
    }

    public async Task UpdateAsync(Post post)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            UPDATE posts 
            SET topic_id = @TopicId, author_id = @AuthorId, content_md = @ContentMd,
                reply_to_post_id = @ReplyToPostId, is_edited = @IsEdited, 
                is_deleted = @IsDeleted, deleted_at = @DeletedAt
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, post);
    }

    public async Task DeleteAsync(long id)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = "UPDATE posts SET is_deleted = 1, deleted_at = NOW(3) WHERE id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<int> GetReplyCountByTopicIdAsync(long topicId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            SELECT COUNT(*) 
            FROM posts 
            WHERE topic_id = @TopicId AND is_deleted = 0";

        return await connection.QuerySingleAsync<int>(sql, new { TopicId = topicId });
    }

    public async Task<Post?> GetLastPostByTopicIdAsync(long topicId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            SELECT id, topic_id as TopicId, author_id as AuthorId, content_md as ContentMd,
                   reply_to_post_id as ReplyToPostId, is_edited as IsEdited, is_deleted as IsDeleted,
                   deleted_at as DeletedAt, created_at as CreatedAt, updated_at as UpdatedAt
            FROM posts 
            WHERE topic_id = @TopicId AND is_deleted = 0
            ORDER BY created_at DESC, id DESC 
            LIMIT 1";

        return await connection.QuerySingleOrDefaultAsync<Post>(sql, new { TopicId = topicId });
    }

    public async Task<Dictionary<long, int>> GetReplyCountsByTopicIdsAsync(IEnumerable<long> topicIds)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        if (!topicIds.Any()) return new Dictionary<long, int>();
        
        const string sql = @"
            SELECT topic_id as TopicId, COUNT(*) as ReplyCount 
            FROM posts 
            WHERE topic_id IN @TopicIds AND is_deleted = 0
            GROUP BY topic_id";

        var results = await connection.QueryAsync<(long TopicId, int ReplyCount)>(sql, new { TopicIds = topicIds });
        
        var dictionary = results.ToDictionary(r => r.TopicId, r => r.ReplyCount);
        
        // 确保所有传入的主题ID都有对应的记录，没有帖子的主题设置为0
        foreach (var topicId in topicIds)
        {
            if (!dictionary.ContainsKey(topicId))
            {
                dictionary[topicId] = 0;
            }
        }
        
        return dictionary;
    }

    public async Task<Dictionary<long, Post?>> GetLastPostsByTopicIdsAsync(IEnumerable<long> topicIds)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        if (!topicIds.Any()) return new Dictionary<long, Post?>();
        
        const string sql = @"
            SELECT p1.id, p1.topic_id as TopicId, p1.author_id as AuthorId, p1.content_md as ContentMd,
                   p1.reply_to_post_id as ReplyToPostId, p1.is_edited as IsEdited, p1.is_deleted as IsDeleted,
                   p1.deleted_at as DeletedAt, p1.created_at as CreatedAt, p1.updated_at as UpdatedAt
            FROM posts p1
            INNER JOIN (
                SELECT topic_id, MAX(created_at) as max_created_at, MAX(id) as max_id
                FROM posts 
                WHERE topic_id IN @TopicIds AND is_deleted = 0
                GROUP BY topic_id
            ) p2 ON p1.topic_id = p2.topic_id 
               AND p1.created_at = p2.max_created_at 
               AND p1.id = p2.max_id";

        var posts = await connection.QueryAsync<Post>(sql, new { TopicIds = topicIds });
        var dictionary = posts.ToDictionary(p => p.TopicId, p => (Post?)p);
        
        // 确保所有传入的主题ID都有对应的记录，没有帖子的主题设置为null
        foreach (var topicId in topicIds)
        {
            if (!dictionary.ContainsKey(topicId))
            {
                dictionary[topicId] = null;
            }
        }
        
        return dictionary;
    }
}