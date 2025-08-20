using Dapper;
using Forum.Api.Infrastructure.Database;
using Forum.Api.Models.Entities;

namespace Forum.Api.Repositories;

public class TopicRepository : ITopicRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public TopicRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<Topic>> GetAllAsync(long? categoryId = null, int limit = 20, long? cursorId = null, DateTime? cursorLastPosted = null)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        var sql = @"
            SELECT id, title, slug, author_id as AuthorId, category_id as CategoryId,
                   is_pinned as IsPinned, is_locked as IsLocked, is_deleted as IsDeleted,
                   reply_count as ReplyCount, view_count as ViewCount, 
                   last_posted_at as LastPostedAt, last_poster_id as LastPosterId,
                   created_at as CreatedAt, updated_at as UpdatedAt
            FROM topics 
            WHERE is_deleted = 0";

        var parameters = new DynamicParameters();

        if (categoryId.HasValue)
        {
            sql += " AND category_id = @CategoryId";
            parameters.Add("CategoryId", categoryId);
        }

        if (cursorId.HasValue && cursorLastPosted.HasValue)
        {
            sql += " AND (last_posted_at < @CursorLastPosted OR (last_posted_at = @CursorLastPosted AND id < @CursorId))";
            parameters.Add("CursorLastPosted", cursorLastPosted);
            parameters.Add("CursorId", cursorId);
        }

        sql += " ORDER BY is_pinned DESC, last_posted_at DESC, id DESC LIMIT @Limit";
        parameters.Add("Limit", limit);

        return await connection.QueryAsync<Topic>(sql, parameters);
    }

    public async Task<Topic?> GetByIdAsync(long id)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            SELECT id, title, slug, author_id as AuthorId, category_id as CategoryId,
                   is_pinned as IsPinned, is_locked as IsLocked, is_deleted as IsDeleted,
                   reply_count as ReplyCount, view_count as ViewCount, 
                   last_posted_at as LastPostedAt, last_poster_id as LastPosterId,
                   created_at as CreatedAt, updated_at as UpdatedAt
            FROM topics 
            WHERE id = @Id";

        return await connection.QuerySingleOrDefaultAsync<Topic>(sql, new { Id = id });
    }

    public async Task<long> CreateAsync(Topic topic)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            INSERT INTO topics (title, slug, author_id, category_id, is_pinned, is_locked, 
                               is_deleted, reply_count, view_count, last_posted_at, last_poster_id)
            VALUES (@Title, @Slug, @AuthorId, @CategoryId, @IsPinned, @IsLocked, 
                    @IsDeleted, @ReplyCount, @ViewCount, @LastPostedAt, @LastPosterId);
            SELECT LAST_INSERT_ID();";

        return await connection.QuerySingleAsync<long>(sql, topic);
    }

    public async Task UpdateAsync(Topic topic)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = @"
            UPDATE topics 
            SET title = @Title, slug = @Slug, author_id = @AuthorId, category_id = @CategoryId,
                is_pinned = @IsPinned, is_locked = @IsLocked, is_deleted = @IsDeleted,
                reply_count = @ReplyCount, view_count = @ViewCount, 
                last_posted_at = @LastPostedAt, last_poster_id = @LastPosterId
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, topic);
    }

    public async Task DeleteAsync(long id)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        
        const string sql = "UPDATE topics SET is_deleted = 1 WHERE id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id });
    }
}