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
}