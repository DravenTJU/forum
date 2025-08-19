# M2: 主题帖子 CRUD + Markdown 详细实现步骤

**时间估算**: 2周 (10个工作日)  
**优先级**: 高 (核心内容功能)  
**负责人**: 全栈开发团队

## 📋 任务总览

- ✅ 主题管理 API 设计与实现 (CRUD + 分页)
- ✅ 帖子管理 API 设计与实现 (回帖 + 事务)
- ✅ Markdown 处理与安全 (渲染 + XSS 防护)
- ✅ Discourse 风格前端界面
- ✅ 底部浮动编辑器 (Composer)
- ✅ 无限滚动 + 右侧时间轴

---

## 🗄️ Day 1-2: 数据模型与仓储层

### 1.1 扩展数据库迁移

**`Migrations/005_EnhanceTopicsAndPosts.sql`**
```sql
-- 为主题表添加更多字段
ALTER TABLE topics 
ADD COLUMN excerpt TEXT AFTER title,
ADD COLUMN featured_image_url VARCHAR(500) AFTER excerpt,
ADD COLUMN edit_reason VARCHAR(255) AFTER updated_at,
ADD COLUMN last_editor_id BIGINT UNSIGNED AFTER last_poster_id,
ADD CONSTRAINT fk_topics_lasteditor FOREIGN KEY (last_editor_id) REFERENCES users(id);

-- 为帖子表添加编辑历史支持
ALTER TABLE posts
ADD COLUMN edit_reason VARCHAR(255) AFTER is_edited,
ADD COLUMN edit_count INT NOT NULL DEFAULT 0 AFTER edit_reason,
ADD COLUMN raw_content_md MEDIUMTEXT AFTER content_md,
ADD INDEX idx_posts_edit_count (edit_count);

-- 帖子编辑历史表
CREATE TABLE post_edit_history (
  id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  post_id BIGINT UNSIGNED NOT NULL,
  editor_id BIGINT UNSIGNED NOT NULL,
  old_content_md MEDIUMTEXT NOT NULL,
  new_content_md MEDIUMTEXT NOT NULL,
  edit_reason VARCHAR(255),
  created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  CONSTRAINT fk_peh_post FOREIGN KEY (post_id) REFERENCES posts(id) ON DELETE CASCADE,
  CONSTRAINT fk_peh_editor FOREIGN KEY (editor_id) REFERENCES users(id),
  INDEX idx_peh_post_created (post_id, created_at DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 主题查看历史表 (用于统计浏览量)
CREATE TABLE topic_views (
  id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  topic_id BIGINT UNSIGNED NOT NULL,
  user_id BIGINT UNSIGNED NULL,
  ip_address VARCHAR(45) NOT NULL,
  viewed_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  CONSTRAINT fk_tv_topic FOREIGN KEY (topic_id) REFERENCES topics(id) ON DELETE CASCADE,
  CONSTRAINT fk_tv_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE SET NULL,
  INDEX idx_tv_topic_viewed (topic_id, viewed_at DESC),
  INDEX idx_tv_user_viewed (user_id, viewed_at DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 为性能优化添加复合索引
ALTER TABLE topics 
ADD INDEX idx_topics_category_pinned_last (category_id, is_pinned DESC, is_deleted, last_posted_at DESC, id DESC),
ADD INDEX idx_topics_author_created (author_id, created_at DESC);

ALTER TABLE posts
ADD INDEX idx_posts_author_created (author_id, created_at DESC),
ADD INDEX idx_posts_topic_reply (topic_id, reply_to_post_id);
```

### 1.2 主题实体模型

**`Models/Entities/Topic.cs`**
```csharp
namespace Forum.Api.Models.Entities;

public class Topic
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string? FeaturedImageUrl { get; set; }
    public long AuthorId { get; set; }
    public long CategoryId { get; set; }
    public bool IsPinned { get; set; }
    public bool IsLocked { get; set; }
    public bool IsDeleted { get; set; }
    public int ReplyCount { get; set; }
    public int ViewCount { get; set; }
    public DateTime? LastPostedAt { get; set; }
    public long? LastPosterId { get; set; }
    public long? LastEditorId { get; set; }
    public string? EditReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // 导航属性
    public User? Author { get; set; }
    public User? LastPoster { get; set; }
    public User? LastEditor { get; set; }
    public Category? Category { get; set; }
    public List<Tag> Tags { get; set; } = new();
    public List<Post> Posts { get; set; } = new();
}
```

**`Models/Entities/Post.cs`**
```csharp
namespace Forum.Api.Models.Entities;

public class Post
{
    public long Id { get; set; }
    public long TopicId { get; set; }
    public long AuthorId { get; set; }
    public string ContentMd { get; set; } = string.Empty;
    public string? RawContentMd { get; set; } // 原始未处理的 Markdown
    public long? ReplyToPostId { get; set; }
    public bool IsEdited { get; set; }
    public string? EditReason { get; set; }
    public int EditCount { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // 导航属性
    public Topic? Topic { get; set; }
    public User? Author { get; set; }
    public Post? ReplyToPost { get; set; }
    public List<PostMention> Mentions { get; set; } = new();
    public List<PostEditHistory> EditHistory { get; set; } = new();
    
    // 计算属性
    public bool CanEdit => !IsDeleted && (DateTime.UtcNow - CreatedAt).TotalMinutes <= 10;
    public int PostNumber { get; set; } // 在主题中的楼层号
}
```

### 1.3 主题仓储实现

**`Repositories/ITopicRepository.cs`**
```csharp
using Forum.Api.Models.Entities;

namespace Forum.Api.Repositories;

public interface ITopicRepository
{
    Task<Topic?> GetByIdAsync(long id, bool includeDeleted = false);
    Task<Topic?> GetBySlugAsync(long categoryId, string slug);
    Task<(List<Topic> Topics, bool HasMore)> GetTopicsAsync(TopicQuery query);
    Task<long> CreateAsync(Topic topic, List<long> tagIds);
    Task UpdateAsync(Topic topic);
    Task DeleteAsync(long id, long deleterId);
    Task<bool> ExistsAsync(long categoryId, string slug, long? excludeId = null);
    Task IncrementViewCountAsync(long topicId, long? userId, string ipAddress);
    Task UpdateStatsAsync(long topicId, int replyCountDelta = 0, long? lastPosterId = null);
    Task<List<Topic>> GetUserTopicsAsync(long userId, int limit = 10);
    Task<List<Topic>> GetFeaturedTopicsAsync(int limit = 5);
}

public class TopicQuery
{
    public long? CategoryId { get; set; }
    public List<string> TagSlugs { get; set; } = new();
    public string Sort { get; set; } = "latest"; // latest, hot, top, new
    public int Limit { get; set; } = 20;
    public string? CursorLastPosted { get; set; } // ISO 8601 格式
    public long? CursorId { get; set; }
    public bool IncludePinned { get; set; } = true;
    public bool IncludeDeleted { get; set; } = false;
}
```

**`Repositories/TopicRepository.cs`**
```csharp
using Dapper;
using Forum.Api.Infrastructure.Database;
using Forum.Api.Models.Entities;
using System.Text;

namespace Forum.Api.Repositories;

public class TopicRepository : ITopicRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<TopicRepository> _logger;

    public TopicRepository(IDbConnectionFactory connectionFactory, ILogger<TopicRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<Topic?> GetByIdAsync(long id, bool includeDeleted = false)
    {
        var sql = new StringBuilder(@"
            SELECT t.*, 
                   a.id as author_id, a.username as author_username, a.avatar_url as author_avatar_url,
                   lp.id as last_poster_id, lp.username as last_poster_username, lp.avatar_url as last_poster_avatar_url,
                   le.id as last_editor_id, le.username as last_editor_username,
                   c.id as category_id, c.name as category_name, c.slug as category_slug, c.color as category_color
            FROM topics t
            JOIN users a ON a.id = t.author_id
            LEFT JOIN users lp ON lp.id = t.last_poster_id
            LEFT JOIN users le ON le.id = t.last_editor_id
            JOIN categories c ON c.id = t.category_id
            WHERE t.id = @Id");

        if (!includeDeleted)
        {
            sql.Append(" AND t.is_deleted = 0");
        }

        using var connection = await _connectionFactory.CreateConnectionAsync();
        var result = await connection.QueryAsync<TopicWithRelations>(sql.ToString(), new { Id = id });
        
        var topicData = result.FirstOrDefault();
        if (topicData == null) return null;

        // 获取标签
        var tagsResult = await connection.QueryAsync<Tag>(@"
            SELECT g.* FROM tags g
            JOIN topic_tags tt ON tt.tag_id = g.id
            WHERE tt.topic_id = @TopicId", new { TopicId = id });

        return MapToTopic(topicData, tagsResult.ToList());
    }

    public async Task<Topic?> GetBySlugAsync(long categoryId, string slug)
    {
        var sql = @"
            SELECT t.*, 
                   a.id as author_id, a.username as author_username, a.avatar_url as author_avatar_url,
                   c.id as category_id, c.name as category_name, c.slug as category_slug, c.color as category_color
            FROM topics t
            JOIN users a ON a.id = t.author_id
            JOIN categories c ON c.id = t.category_id
            WHERE t.category_id = @CategoryId AND t.slug = @Slug AND t.is_deleted = 0";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        var result = await connection.QueryFirstOrDefaultAsync<TopicWithRelations>(sql, 
            new { CategoryId = categoryId, Slug = slug });

        if (result == null) return null;

        // 获取标签
        var tagsResult = await connection.QueryAsync<Tag>(@"
            SELECT g.* FROM tags g
            JOIN topic_tags tt ON tt.tag_id = g.id
            WHERE tt.topic_id = @TopicId", new { TopicId = result.Id });

        return MapToTopic(result, tagsResult.ToList());
    }

    public async Task<(List<Topic> Topics, bool HasMore)> GetTopicsAsync(TopicQuery query)
    {
        var sqlBuilder = new StringBuilder(@"
            SELECT t.*, 
                   a.id as author_id, a.username as author_username, a.avatar_url as author_avatar_url,
                   lp.id as last_poster_id, lp.username as last_poster_username, lp.avatar_url as last_poster_avatar_url,
                   c.id as category_id, c.name as category_name, c.slug as category_slug, c.color as category_color
            FROM topics t
            JOIN users a ON a.id = t.author_id
            LEFT JOIN users lp ON lp.id = t.last_poster_id
            JOIN categories c ON c.id = t.category_id");

        var whereConditions = new List<string> { "t.is_deleted = 0" };
        var parameters = new DynamicParameters();

        // 分类筛选
        if (query.CategoryId.HasValue)
        {
            whereConditions.Add("t.category_id = @CategoryId");
            parameters.Add("CategoryId", query.CategoryId.Value);
        }

        // 标签筛选
        if (query.TagSlugs.Any())
        {
            sqlBuilder.Append(@"
                JOIN topic_tags tt ON tt.topic_id = t.id
                JOIN tags g ON g.id = tt.tag_id");
            
            whereConditions.Add($"g.slug IN ({string.Join(",", query.TagSlugs.Select((_, i) => $"@TagSlug{i}"))})");
            for (int i = 0; i < query.TagSlugs.Count; i++)
            {
                parameters.Add($"TagSlug{i}", query.TagSlugs[i]);
            }
        }

        // Keyset 分页
        if (!string.IsNullOrEmpty(query.CursorLastPosted) && query.CursorId.HasValue)
        {
            whereConditions.Add("(t.last_posted_at < @CursorLastPosted OR (t.last_posted_at = @CursorLastPosted AND t.id < @CursorId))");
            parameters.Add("CursorLastPosted", DateTime.Parse(query.CursorLastPosted));
            parameters.Add("CursorId", query.CursorId.Value);
        }

        sqlBuilder.Append($" WHERE {string.Join(" AND ", whereConditions)}");

        // 分组（处理标签关联）
        if (query.TagSlugs.Any())
        {
            sqlBuilder.Append(" GROUP BY t.id");
            if (query.TagSlugs.Count > 1)
            {
                sqlBuilder.Append($" HAVING COUNT(DISTINCT g.id) = {query.TagSlugs.Count}");
            }
        }

        // 排序
        var orderBy = query.Sort.ToLower() switch
        {
            "hot" => "ORDER BY (t.reply_count * 0.6 + t.view_count * 0.4) DESC, t.last_posted_at DESC, t.id DESC",
            "top" => "ORDER BY t.view_count DESC, t.reply_count DESC, t.id DESC",
            "new" => "ORDER BY t.created_at DESC, t.id DESC",
            _ => "ORDER BY t.is_pinned DESC, t.last_posted_at DESC, t.id DESC" // latest
        };

        sqlBuilder.Append($" {orderBy}");
        sqlBuilder.Append(" LIMIT @Limit");
        parameters.Add("Limit", query.Limit + 1); // 多取一条判断是否有更多

        using var connection = await _connectionFactory.CreateConnectionAsync();
        var results = await connection.QueryAsync<TopicWithRelations>(sqlBuilder.ToString(), parameters);
        var topicsList = results.ToList();

        var hasMore = topicsList.Count > query.Limit;
        if (hasMore)
        {
            topicsList.RemoveAt(topicsList.Count - 1);
        }

        // 批量获取所有主题的标签
        var topicIds = topicsList.Select(t => t.Id).ToList();
        var allTags = new Dictionary<long, List<Tag>>();
        
        if (topicIds.Any())
        {
            var tagsResult = await connection.QueryAsync<(long TopicId, Tag Tag)>(@"
                SELECT tt.topic_id as TopicId, g.*
                FROM tags g
                JOIN topic_tags tt ON tt.tag_id = g.id
                WHERE tt.topic_id IN @TopicIds", new { TopicIds = topicIds });

            foreach (var (topicId, tag) in tagsResult)
            {
                if (!allTags.ContainsKey(topicId))
                    allTags[topicId] = new List<Tag>();
                allTags[topicId].Add(tag);
            }
        }

        var topics = topicsList.Select(t => MapToTopic(t, allTags.GetValueOrDefault(t.Id, new List<Tag>()))).ToList();
        return (topics, hasMore);
    }

    public async Task<long> CreateAsync(Topic topic, List<long> tagIds)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            // 插入主题
            const string topicSql = @"
                INSERT INTO topics (title, slug, excerpt, featured_image_url, author_id, category_id, 
                                  is_pinned, is_locked, created_at, updated_at)
                VALUES (@Title, @Slug, @Excerpt, @FeaturedImageUrl, @AuthorId, @CategoryId, 
                        @IsPinned, @IsLocked, @CreatedAt, @UpdatedAt);
                SELECT LAST_INSERT_ID();";

            var topicId = await connection.ExecuteScalarAsync<long>(topicSql, new
            {
                topic.Title,
                topic.Slug,
                topic.Excerpt,
                topic.FeaturedImageUrl,
                topic.AuthorId,
                topic.CategoryId,
                topic.IsPinned,
                topic.IsLocked,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }, transaction);

            // 关联标签
            if (tagIds.Any())
            {
                const string tagsSql = "INSERT INTO topic_tags (topic_id, tag_id) VALUES (@TopicId, @TagId)";
                foreach (var tagId in tagIds)
                {
                    await connection.ExecuteAsync(tagsSql, new { TopicId = topicId, TagId = tagId }, transaction);
                }

                // 更新标签使用计数
                const string updateTagUsageSql = "UPDATE tags SET usage_count = usage_count + 1 WHERE id = @TagId";
                foreach (var tagId in tagIds)
                {
                    await connection.ExecuteAsync(updateTagUsageSql, new { TagId = tagId }, transaction);
                }
            }

            transaction.Commit();
            return topicId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task UpdateAsync(Topic topic)
    {
        const string sql = @"
            UPDATE topics 
            SET title = @Title, slug = @Slug, excerpt = @Excerpt, featured_image_url = @FeaturedImageUrl,
                is_pinned = @IsPinned, is_locked = @IsLocked, edit_reason = @EditReason,
                last_editor_id = @LastEditorId, updated_at = @UpdatedAt
            WHERE id = @Id";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync(sql, new
        {
            topic.Id,
            topic.Title,
            topic.Slug,
            topic.Excerpt,
            topic.FeaturedImageUrl,
            topic.IsPinned,
            topic.IsLocked,
            topic.EditReason,
            topic.LastEditorId,
            UpdatedAt = DateTime.UtcNow
        });
    }

    public async Task DeleteAsync(long id, long deleterId)
    {
        const string sql = @"
            UPDATE topics 
            SET is_deleted = 1, last_editor_id = @DeleterId, updated_at = @UpdatedAt
            WHERE id = @Id";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            DeleterId = deleterId,
            UpdatedAt = DateTime.UtcNow
        });
    }

    public async Task<bool> ExistsAsync(long categoryId, string slug, long? excludeId = null)
    {
        var sql = "SELECT COUNT(*) FROM topics WHERE category_id = @CategoryId AND slug = @Slug";
        var parameters = new { CategoryId = categoryId, Slug = slug };

        if (excludeId.HasValue)
        {
            sql += " AND id != @ExcludeId";
            parameters = new { CategoryId = categoryId, Slug = slug, ExcludeId = excludeId.Value };
        }

        using var connection = await _connectionFactory.CreateConnectionAsync();
        var count = await connection.ExecuteScalarAsync<int>(sql, parameters);
        return count > 0;
    }

    public async Task IncrementViewCountAsync(long topicId, long? userId, string ipAddress)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            // 检查是否在短时间内已有记录 (防刷)
            const string checkSql = @"
                SELECT COUNT(*) FROM topic_views 
                WHERE topic_id = @TopicId AND ip_address = @IpAddress 
                AND viewed_at > @RecentTime";

            var recentViewCount = await connection.ExecuteScalarAsync<int>(checkSql, new
            {
                TopicId = topicId,
                IpAddress = ipAddress,
                RecentTime = DateTime.UtcNow.AddMinutes(-10)
            }, transaction);

            if (recentViewCount == 0)
            {
                // 记录访问
                const string insertViewSql = @"
                    INSERT INTO topic_views (topic_id, user_id, ip_address, viewed_at)
                    VALUES (@TopicId, @UserId, @IpAddress, @ViewedAt)";

                await connection.ExecuteAsync(insertViewSql, new
                {
                    TopicId = topicId,
                    UserId = userId,
                    IpAddress = ipAddress,
                    ViewedAt = DateTime.UtcNow
                }, transaction);

                // 增加浏览量
                const string updateTopicSql = @"
                    UPDATE topics SET view_count = view_count + 1 WHERE id = @TopicId";

                await connection.ExecuteAsync(updateTopicSql, new { TopicId = topicId }, transaction);
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task UpdateStatsAsync(long topicId, int replyCountDelta = 0, long? lastPosterId = null)
    {
        var sql = new StringBuilder("UPDATE topics SET ");
        var updates = new List<string>();
        var parameters = new DynamicParameters();

        if (replyCountDelta != 0)
        {
            updates.Add("reply_count = reply_count + @ReplyCountDelta");
            parameters.Add("ReplyCountDelta", replyCountDelta);
        }

        if (lastPosterId.HasValue)
        {
            updates.Add("last_posted_at = @LastPostedAt");
            updates.Add("last_poster_id = @LastPosterId");
            parameters.Add("LastPostedAt", DateTime.UtcNow);
            parameters.Add("LastPosterId", lastPosterId.Value);
        }

        updates.Add("updated_at = @UpdatedAt");
        parameters.Add("UpdatedAt", DateTime.UtcNow);
        parameters.Add("TopicId", topicId);

        sql.Append(string.Join(", ", updates));
        sql.Append(" WHERE id = @TopicId");

        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync(sql.ToString(), parameters);
    }

    public async Task<List<Topic>> GetUserTopicsAsync(long userId, int limit = 10)
    {
        const string sql = @"
            SELECT t.*, c.name as category_name, c.slug as category_slug, c.color as category_color
            FROM topics t
            JOIN categories c ON c.id = t.category_id
            WHERE t.author_id = @UserId AND t.is_deleted = 0
            ORDER BY t.created_at DESC
            LIMIT @Limit";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        var results = await connection.QueryAsync<TopicWithCategory>(sql, new { UserId = userId, Limit = limit });
        
        return results.Select(MapToTopicSimple).ToList();
    }

    public async Task<List<Topic>> GetFeaturedTopicsAsync(int limit = 5)
    {
        const string sql = @"
            SELECT t.*, c.name as category_name, c.slug as category_slug, c.color as category_color
            FROM topics t
            JOIN categories c ON c.id = t.category_id
            WHERE t.is_deleted = 0 AND t.is_pinned = 1
            ORDER BY t.updated_at DESC
            LIMIT @Limit";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        var results = await connection.QueryAsync<TopicWithCategory>(sql, new { Limit = limit });
        
        return results.Select(MapToTopicSimple).ToList();
    }

    private static Topic MapToTopic(TopicWithRelations data, List<Tag> tags)
    {
        return new Topic
        {
            Id = data.Id,
            Title = data.Title,
            Slug = data.Slug,
            Excerpt = data.Excerpt,
            FeaturedImageUrl = data.FeaturedImageUrl,
            AuthorId = data.AuthorId,
            CategoryId = data.CategoryId,
            IsPinned = data.IsPinned,
            IsLocked = data.IsLocked,
            IsDeleted = data.IsDeleted,
            ReplyCount = data.ReplyCount,
            ViewCount = data.ViewCount,
            LastPostedAt = data.LastPostedAt,
            LastPosterId = data.LastPosterId,
            LastEditorId = data.LastEditorId,
            EditReason = data.EditReason,
            CreatedAt = data.CreatedAt,
            UpdatedAt = data.UpdatedAt,
            Author = data.AuthorId > 0 ? new User 
            { 
                Id = data.AuthorId, 
                Username = data.AuthorUsername, 
                AvatarUrl = data.AuthorAvatarUrl 
            } : null,
            LastPoster = data.LastPosterId.HasValue ? new User 
            { 
                Id = data.LastPosterId.Value, 
                Username = data.LastPosterUsername!, 
                AvatarUrl = data.LastPosterAvatarUrl 
            } : null,
            Category = data.CategoryId > 0 ? new Category 
            { 
                Id = data.CategoryId, 
                Name = data.CategoryName, 
                Slug = data.CategorySlug, 
                Color = data.CategoryColor 
            } : null,
            Tags = tags
        };
    }

    private static Topic MapToTopicSimple(TopicWithCategory data)
    {
        return new Topic
        {
            Id = data.Id,
            Title = data.Title,
            Slug = data.Slug,
            Excerpt = data.Excerpt,
            FeaturedImageUrl = data.FeaturedImageUrl,
            AuthorId = data.AuthorId,
            CategoryId = data.CategoryId,
            IsPinned = data.IsPinned,
            IsLocked = data.IsLocked,
            IsDeleted = data.IsDeleted,
            ReplyCount = data.ReplyCount,
            ViewCount = data.ViewCount,
            LastPostedAt = data.LastPostedAt,
            LastPosterId = data.LastPosterId,
            CreatedAt = data.CreatedAt,
            UpdatedAt = data.UpdatedAt,
            Category = new Category 
            { 
                Id = data.CategoryId, 
                Name = data.CategoryName, 
                Slug = data.CategorySlug, 
                Color = data.CategoryColor 
            }
        };
    }

    private class TopicWithRelations
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Excerpt { get; set; }
        public string? FeaturedImageUrl { get; set; }
        public long AuthorId { get; set; }
        public long CategoryId { get; set; }
        public bool IsPinned { get; set; }
        public bool IsLocked { get; set; }
        public bool IsDeleted { get; set; }
        public int ReplyCount { get; set; }
        public int ViewCount { get; set; }
        public DateTime? LastPostedAt { get; set; }
        public long? LastPosterId { get; set; }
        public long? LastEditorId { get; set; }
        public string? EditReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // 关联数据
        public string AuthorUsername { get; set; } = string.Empty;
        public string? AuthorAvatarUrl { get; set; }
        public string? LastPosterUsername { get; set; }
        public string? LastPosterAvatarUrl { get; set; }
        public string? LastEditorUsername { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategorySlug { get; set; } = string.Empty;
        public string CategoryColor { get; set; } = string.Empty;
    }

    private class TopicWithCategory
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Excerpt { get; set; }
        public string? FeaturedImageUrl { get; set; }
        public long AuthorId { get; set; }
        public long CategoryId { get; set; }
        public bool IsPinned { get; set; }
        public bool IsLocked { get; set; }
        public bool IsDeleted { get; set; }
        public int ReplyCount { get; set; }
        public int ViewCount { get; set; }
        public DateTime? LastPostedAt { get; set; }
        public long? LastPosterId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        public string CategoryName { get; set; } = string.Empty;
        public string CategorySlug { get; set; } = string.Empty;
        public string CategoryColor { get; set; } = string.Empty;
    }
}
```

---

## 📝 Day 3-4: 帖子仓储与 Markdown 处理

### 2.1 帖子仓储实现

**`Repositories/IPostRepository.cs`**
```csharp
using Forum.Api.Models.Entities;

namespace Forum.Api.Repositories;

public interface IPostRepository
{
    Task<Post?> GetByIdAsync(long id, bool includeDeleted = false);
    Task<(List<Post> Posts, bool HasMore)> GetPostsAsync(PostQuery query);
    Task<long> CreateAsync(Post post, List<long> mentionedUserIds);
    Task UpdateAsync(Post post);
    Task DeleteAsync(long id, long deleterId);
    Task<List<Post>> GetUserPostsAsync(long userId, int limit = 10);
    Task<int> GetTopicPostCountAsync(long topicId);
    Task<Post?> GetTopicFirstPostAsync(long topicId);
    Task<List<Post>> GetPostsWithMentionsAsync(long userId, DateTime since);
}

public class PostQuery
{
    public long TopicId { get; set; }
    public int Limit { get; set; } = 20;
    public string? CursorCreated { get; set; } // ISO 8601 格式
    public long? CursorId { get; set; }
    public bool IncludeDeleted { get; set; } = false;
    public bool OrderByCreated { get; set; } = true; // true: 正序, false: 倒序
}
```

**`Repositories/PostRepository.cs`**
```csharp
using Dapper;
using Forum.Api.Infrastructure.Database;
using Forum.Api.Models.Entities;
using System.Text;

namespace Forum.Api.Repositories;

public class PostRepository : IPostRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<PostRepository> _logger;

    public PostRepository(IDbConnectionFactory connectionFactory, ILogger<PostRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<Post?> GetByIdAsync(long id, bool includeDeleted = false)
    {
        var sql = new StringBuilder(@"
            SELECT p.*, 
                   a.id as author_id, a.username as author_username, a.avatar_url as author_avatar_url,
                   t.id as topic_id, t.title as topic_title, t.slug as topic_slug
            FROM posts p
            JOIN users a ON a.id = p.author_id
            JOIN topics t ON t.id = p.topic_id
            WHERE p.id = @Id");

        if (!includeDeleted)
        {
            sql.Append(" AND p.is_deleted = 0");
        }

        using var connection = await _connectionFactory.CreateConnectionAsync();
        var result = await connection.QueryFirstOrDefaultAsync<PostWithRelations>(sql.ToString(), new { Id = id });

        if (result == null) return null;

        // 获取提及的用户
        var mentions = await connection.QueryAsync<PostMention>(@"
            SELECT pm.*, u.username 
            FROM post_mentions pm
            JOIN users u ON u.id = pm.mentioned_user_id
            WHERE pm.post_id = @PostId", new { PostId = id });

        return MapToPost(result, mentions.ToList());
    }

    public async Task<(List<Post> Posts, bool HasMore)> GetPostsAsync(PostQuery query)
    {
        var sqlBuilder = new StringBuilder(@"
            SELECT p.*, 
                   a.id as author_id, a.username as author_username, a.avatar_url as author_avatar_url,
                   ROW_NUMBER() OVER (ORDER BY p.created_at, p.id) as post_number
            FROM posts p
            JOIN users a ON a.id = p.author_id
            WHERE p.topic_id = @TopicId");

        var parameters = new DynamicParameters();
        parameters.Add("TopicId", query.TopicId);

        if (!query.IncludeDeleted)
        {
            sqlBuilder.Append(" AND p.is_deleted = 0");
        }

        // Keyset 分页
        if (!string.IsNullOrEmpty(query.CursorCreated) && query.CursorId.HasValue)
        {
            if (query.OrderByCreated)
            {
                sqlBuilder.Append(" AND (p.created_at > @CursorCreated OR (p.created_at = @CursorCreated AND p.id > @CursorId))");
            }
            else
            {
                sqlBuilder.Append(" AND (p.created_at < @CursorCreated OR (p.created_at = @CursorCreated AND p.id < @CursorId))");
            }
            parameters.Add("CursorCreated", DateTime.Parse(query.CursorCreated));
            parameters.Add("CursorId", query.CursorId.Value);
        }

        // 排序
        var orderDirection = query.OrderByCreated ? "ASC" : "DESC";
        sqlBuilder.Append($" ORDER BY p.created_at {orderDirection}, p.id {orderDirection}");
        sqlBuilder.Append(" LIMIT @Limit");
        parameters.Add("Limit", query.Limit + 1); // 多取一条判断是否有更多

        using var connection = await _connectionFactory.CreateConnectionAsync();
        var results = await connection.QueryAsync<PostWithRelations>(sqlBuilder.ToString(), parameters);
        var postsList = results.ToList();

        var hasMore = postsList.Count > query.Limit;
        if (hasMore)
        {
            postsList.RemoveAt(postsList.Count - 1);
        }

        // 批量获取所有帖子的提及信息
        var postIds = postsList.Select(p => p.Id).ToList();
        var allMentions = new Dictionary<long, List<PostMention>>();

        if (postIds.Any())
        {
            var mentionsResult = await connection.QueryAsync<(long PostId, PostMention Mention)>(@"
                SELECT pm.post_id as PostId, pm.*, u.username
                FROM post_mentions pm
                JOIN users u ON u.id = pm.mentioned_user_id
                WHERE pm.post_id IN @PostIds", new { PostIds = postIds });

            foreach (var (postId, mention) in mentionsResult)
            {
                if (!allMentions.ContainsKey(postId))
                    allMentions[postId] = new List<PostMention>();
                allMentions[postId].Add(mention);
            }
        }

        var posts = postsList.Select(p => MapToPost(p, allMentions.GetValueOrDefault(p.Id, new List<PostMention>()))).ToList();
        return (posts, hasMore);
    }

    public async Task<long> CreateAsync(Post post, List<long> mentionedUserIds)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            // 插入帖子
            const string postSql = @"
                INSERT INTO posts (topic_id, author_id, content_md, raw_content_md, reply_to_post_id, created_at, updated_at)
                VALUES (@TopicId, @AuthorId, @ContentMd, @RawContentMd, @ReplyToPostId, @CreatedAt, @UpdatedAt);
                SELECT LAST_INSERT_ID();";

            var postId = await connection.ExecuteScalarAsync<long>(postSql, new
            {
                post.TopicId,
                post.AuthorId,
                post.ContentMd,
                post.RawContentMd,
                post.ReplyToPostId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }, transaction);

            // 添加提及记录
            if (mentionedUserIds.Any())
            {
                const string mentionSql = "INSERT INTO post_mentions (post_id, mentioned_user_id) VALUES (@PostId, @UserId)";
                foreach (var userId in mentionedUserIds)
                {
                    await connection.ExecuteAsync(mentionSql, new { PostId = postId, UserId = userId }, transaction);
                }
            }

            // 更新主题统计（回帖数、最后回帖时间等）
            const string updateTopicSql = @"
                UPDATE topics 
                SET reply_count = reply_count + 1,
                    last_posted_at = @LastPostedAt,
                    last_poster_id = @LastPosterId,
                    updated_at = @UpdatedAt
                WHERE id = @TopicId";

            await connection.ExecuteAsync(updateTopicSql, new
            {
                TopicId = post.TopicId,
                LastPostedAt = DateTime.UtcNow,
                LastPosterId = post.AuthorId,
                UpdatedAt = DateTime.UtcNow
            }, transaction);

            transaction.Commit();
            return postId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task UpdateAsync(Post post)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            // 如果内容有变化，保存编辑历史
            if (post.IsEdited)
            {
                const string historyCheckSql = "SELECT content_md FROM posts WHERE id = @Id";
                var oldContent = await connection.ExecuteScalarAsync<string>(historyCheckSql, new { Id = post.Id }, transaction);

                if (oldContent != post.ContentMd)
                {
                    const string historySql = @"
                        INSERT INTO post_edit_history (post_id, editor_id, old_content_md, new_content_md, edit_reason, created_at)
                        VALUES (@PostId, @EditorId, @OldContent, @NewContent, @EditReason, @CreatedAt)";

                    await connection.ExecuteAsync(historySql, new
                    {
                        PostId = post.Id,
                        EditorId = post.AuthorId, // 简化起见，这里用作者ID
                        OldContent = oldContent,
                        NewContent = post.ContentMd,
                        EditReason = post.EditReason,
                        CreatedAt = DateTime.UtcNow
                    }, transaction);
                }
            }

            // 更新帖子
            const string updateSql = @"
                UPDATE posts 
                SET content_md = @ContentMd, raw_content_md = @RawContentMd, is_edited = @IsEdited,
                    edit_reason = @EditReason, edit_count = edit_count + 1, updated_at = @UpdatedAt
                WHERE id = @Id";

            await connection.ExecuteAsync(updateSql, new
            {
                post.Id,
                post.ContentMd,
                post.RawContentMd,
                post.IsEdited,
                post.EditReason,
                UpdatedAt = DateTime.UtcNow
            }, transaction);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task DeleteAsync(long id, long deleterId)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            // 软删除帖子
            const string deletePostSql = @"
                UPDATE posts 
                SET is_deleted = 1, deleted_at = @DeletedAt, updated_at = @UpdatedAt
                WHERE id = @Id";

            await connection.ExecuteAsync(deletePostSql, new
            {
                Id = id,
                DeletedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }, transaction);

            // 获取主题 ID 并更新统计
            const string getTopicSql = "SELECT topic_id FROM posts WHERE id = @Id";
            var topicId = await connection.ExecuteScalarAsync<long>(getTopicSql, new { Id = id }, transaction);

            // 减少主题回帖数
            const string updateTopicSql = @"
                UPDATE topics 
                SET reply_count = GREATEST(reply_count - 1, 0), updated_at = @UpdatedAt
                WHERE id = @TopicId";

            await connection.ExecuteAsync(updateTopicSql, new
            {
                TopicId = topicId,
                UpdatedAt = DateTime.UtcNow
            }, transaction);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<List<Post>> GetUserPostsAsync(long userId, int limit = 10)
    {
        const string sql = @"
            SELECT p.*, t.title as topic_title, t.slug as topic_slug
            FROM posts p
            JOIN topics t ON t.id = p.topic_id
            WHERE p.author_id = @UserId AND p.is_deleted = 0 AND t.is_deleted = 0
            ORDER BY p.created_at DESC
            LIMIT @Limit";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        var results = await connection.QueryAsync<PostWithTopic>(sql, new { UserId = userId, Limit = limit });

        return results.Select(MapToPostSimple).ToList();
    }

    public async Task<int> GetTopicPostCountAsync(long topicId)
    {
        const string sql = "SELECT COUNT(*) FROM posts WHERE topic_id = @TopicId AND is_deleted = 0";
        
        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.ExecuteScalarAsync<int>(sql, new { TopicId = topicId });
    }

    public async Task<Post?> GetTopicFirstPostAsync(long topicId)
    {
        const string sql = @"
            SELECT p.*, a.id as author_id, a.username as author_username, a.avatar_url as author_avatar_url
            FROM posts p
            JOIN users a ON a.id = p.author_id
            WHERE p.topic_id = @TopicId AND p.is_deleted = 0
            ORDER BY p.created_at, p.id
            LIMIT 1";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        var result = await connection.QueryFirstOrDefaultAsync<PostWithRelations>(sql, new { TopicId = topicId });

        return result != null ? MapToPost(result, new List<PostMention>()) : null;
    }

    public async Task<List<Post>> GetPostsWithMentionsAsync(long userId, DateTime since)
    {
        const string sql = @"
            SELECT p.*, a.username as author_username, t.title as topic_title, t.slug as topic_slug
            FROM posts p
            JOIN post_mentions pm ON pm.post_id = p.id
            JOIN users a ON a.id = p.author_id
            JOIN topics t ON t.id = p.topic_id
            WHERE pm.mentioned_user_id = @UserId 
            AND p.created_at >= @Since 
            AND p.is_deleted = 0 
            AND t.is_deleted = 0
            ORDER BY p.created_at DESC";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        var results = await connection.QueryAsync<PostWithMentionInfo>(sql, new { UserId = userId, Since = since });

        return results.Select(MapToPostWithMention).ToList();
    }

    private static Post MapToPost(PostWithRelations data, List<PostMention> mentions)
    {
        return new Post
        {
            Id = data.Id,
            TopicId = data.TopicId,
            AuthorId = data.AuthorId,
            ContentMd = data.ContentMd,
            RawContentMd = data.RawContentMd,
            ReplyToPostId = data.ReplyToPostId,
            IsEdited = data.IsEdited,
            EditReason = data.EditReason,
            EditCount = data.EditCount,
            IsDeleted = data.IsDeleted,
            DeletedAt = data.DeletedAt,
            CreatedAt = data.CreatedAt,
            UpdatedAt = data.UpdatedAt,
            PostNumber = data.PostNumber,
            Author = new User
            {
                Id = data.AuthorId,
                Username = data.AuthorUsername,
                AvatarUrl = data.AuthorAvatarUrl
            },
            Topic = data.TopicId > 0 ? new Topic
            {
                Id = data.TopicId,
                Title = data.TopicTitle,
                Slug = data.TopicSlug
            } : null,
            Mentions = mentions
        };
    }

    private static Post MapToPostSimple(PostWithTopic data)
    {
        return new Post
        {
            Id = data.Id,
            TopicId = data.TopicId,
            AuthorId = data.AuthorId,
            ContentMd = data.ContentMd,
            CreatedAt = data.CreatedAt,
            Topic = new Topic
            {
                Id = data.TopicId,
                Title = data.TopicTitle,
                Slug = data.TopicSlug
            }
        };
    }

    private static Post MapToPostWithMention(PostWithMentionInfo data)
    {
        return new Post
        {
            Id = data.Id,
            TopicId = data.TopicId,
            AuthorId = data.AuthorId,
            ContentMd = data.ContentMd,
            CreatedAt = data.CreatedAt,
            Author = new User
            {
                Id = data.AuthorId,
                Username = data.AuthorUsername
            },
            Topic = new Topic
            {
                Id = data.TopicId,
                Title = data.TopicTitle,
                Slug = data.TopicSlug
            }
        };
    }

    private class PostWithRelations
    {
        public long Id { get; set; }
        public long TopicId { get; set; }
        public long AuthorId { get; set; }
        public string ContentMd { get; set; } = string.Empty;
        public string? RawContentMd { get; set; }
        public long? ReplyToPostId { get; set; }
        public bool IsEdited { get; set; }
        public string? EditReason { get; set; }
        public int EditCount { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int PostNumber { get; set; }

        public string AuthorUsername { get; set; } = string.Empty;
        public string? AuthorAvatarUrl { get; set; }
        public string TopicTitle { get; set; } = string.Empty;
        public string TopicSlug { get; set; } = string.Empty;
    }

    private class PostWithTopic
    {
        public long Id { get; set; }
        public long TopicId { get; set; }
        public long AuthorId { get; set; }
        public string ContentMd { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string TopicTitle { get; set; } = string.Empty;
        public string TopicSlug { get; set; } = string.Empty;
    }

    private class PostWithMentionInfo
    {
        public long Id { get; set; }
        public long TopicId { get; set; }
        public long AuthorId { get; set; }
        public string ContentMd { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string AuthorUsername { get; set; } = string.Empty;
        public string TopicTitle { get; set; } = string.Empty;
        public string TopicSlug { get; set; } = string.Empty;
    }
}
```

### 2.2 Markdown 处理服务

**`Services/IMarkdownService.cs`**
```csharp
namespace Forum.Api.Services;

public interface IMarkdownService
{
    string ToHtml(string markdown);
    string ToSafeHtml(string markdown);
    string ExtractPlainText(string markdown, int maxLength = 200);
    List<string> ExtractMentions(string markdown);
    string ProcessMentions(string markdown, Dictionary<string, long> usernamesToIds);
}
```

**`Services/MarkdownService.cs`**
```csharp
using Markdig;
using System.Text.RegularExpressions;
using System.Web;

namespace Forum.Api.Services;

public class MarkdownService : IMarkdownService
{
    private readonly MarkdownPipeline _pipeline;
    private readonly ILogger<MarkdownService> _logger;

    private static readonly Regex MentionRegex = new(@"@([a-zA-Z0-9_]{3,20})", RegexOptions.Compiled);
    
    // 允许的 HTML 标签和属性（白名单）
    private static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "br", "strong", "em", "code", "pre", "blockquote", "ul", "ol", "li",
        "h1", "h2", "h3", "h4", "h5", "h6", "a", "img", "hr", "table", "thead",
        "tbody", "tr", "th", "td", "del", "ins", "mark", "sub", "sup"
    };

    private static readonly Dictionary<string, HashSet<string>> AllowedAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["a"] = new(StringComparer.OrdinalIgnoreCase) { "href", "title", "rel" },
        ["img"] = new(StringComparer.OrdinalIgnoreCase) { "src", "alt", "title", "width", "height" },
        ["blockquote"] = new(StringComparer.OrdinalIgnoreCase) { "cite" },
        ["code"] = new(StringComparer.OrdinalIgnoreCase) { "class" },
        ["pre"] = new(StringComparer.OrdinalIgnoreCase) { "class" }
    };

    public MarkdownService(ILogger<MarkdownService> logger)
    {
        _logger = logger;
        
        // 配置 Markdig 管道
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions() // 启用高级扩展
            .UseAutoLinks() // 自动链接
            .UseGenericAttributes() // 通用属性
            .UsePipeTables() // 表格支持
            .UseTaskLists() // 任务列表
            .UseAutoIdentifiers() // 自动 ID
            .UseFigures() // 图片标题
            .UseFooters() // 脚注
            .UseEmojiAndSmiley() // 表情符号
            .DisableHtml() // 禁用原始 HTML（安全）
            .Build();
    }

    public string ToHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        try
        {
            return Markdown.ToHtml(markdown, _pipeline);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert markdown to HTML: {Markdown}", markdown);
            return HttpUtility.HtmlEncode(markdown);
        }
    }

    public string ToSafeHtml(string markdown)
    {
        var html = ToHtml(markdown);
        return SanitizeHtml(html);
    }

    public string ExtractPlainText(string markdown, int maxLength = 200)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        try
        {
            // 移除 Markdown 语法
            var text = markdown
                .Replace("**", "") // 粗体
                .Replace("*", "") // 斜体
                .Replace("__", "") // 粗体
                .Replace("_", "") // 斜体
                .Replace("`", "") // 代码
                .Replace("~~", "") // 删除线
                .Replace("#", "") // 标题
                .Replace(">", "") // 引用
                .Replace("-", "") // 列表
                .Replace("+", "") // 列表
                .Replace("*", ""); // 列表

            // 移除链接语法
            text = Regex.Replace(text, @"\[([^\]]+)\]\([^)]+\)", "$1");
            // 移除图片语法
            text = Regex.Replace(text, @"!\[([^\]]*)\]\([^)]+\)", "$1");
            // 移除多余的空白字符
            text = Regex.Replace(text, @"\s+", " ").Trim();

            return text.Length > maxLength ? text.Substring(0, maxLength) + "..." : text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract plain text from markdown: {Markdown}", markdown);
            return markdown.Length > maxLength ? markdown.Substring(0, maxLength) + "..." : markdown;
        }
    }

    public List<string> ExtractMentions(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return new List<string>();

        var matches = MentionRegex.Matches(markdown);
        return matches.Select(m => m.Groups[1].Value.ToLower()).Distinct().ToList();
    }

    public string ProcessMentions(string markdown, Dictionary<string, long> usernamesToIds)
    {
        if (string.IsNullOrWhiteSpace(markdown) || !usernamesToIds.Any())
            return markdown;

        return MentionRegex.Replace(markdown, match =>
        {
            var username = match.Groups[1].Value;
            if (usernamesToIds.TryGetValue(username.ToLower(), out var userId))
            {
                return $"[@{username}](/users/{userId})";
            }
            return match.Value; // 保持原样，如果用户不存在
        });
    }

    private string SanitizeHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        try
        {
            // 简单的 HTML 清理实现
            // 生产环境建议使用 HtmlSanitizer 库
            
            // 移除脚本标签
            html = Regex.Replace(html, @"<script[^>]*>.*?</script>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            
            // 移除样式标签
            html = Regex.Replace(html, @"<style[^>]*>.*?</style>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            
            // 移除事件处理器属性
            html = Regex.Replace(html, @"\s+on\w+\s*=\s*[""'][^""']*[""']", "", RegexOptions.IgnoreCase);
            
            // 移除 javascript: 链接
            html = Regex.Replace(html, @"href\s*=\s*[""']javascript:[^""']*[""']", "", RegexOptions.IgnoreCase);
            
            // 为外部链接添加安全属性
            html = Regex.Replace(html, @"<a\s+([^>]*href\s*=\s*[""']https?://[^""']*[""'][^>]*)>", 
                @"<a $1 rel=""noopener noreferrer"" target=""_blank"">", RegexOptions.IgnoreCase);
            
            return html;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sanitize HTML: {Html}", html);
            return HttpUtility.HtmlEncode(html);
        }
    }
}
```

---

## 🎯 Day 5-6: 业务服务层实现

### 3.1 主题服务实现

**`Services/ITopicService.cs`**
```csharp
using Forum.Api.Models.DTOs;

namespace Forum.Api.Services;

public interface ITopicService
{
    Task<TopicDto?> GetTopicAsync(long id, long? viewerId = null, string? ipAddress = null);
    Task<TopicDto?> GetTopicBySlugAsync(long categoryId, string slug, long? viewerId = null, string? ipAddress = null);
    Task<TopicsListResponse> GetTopicsAsync(TopicsListRequest request);
    Task<TopicDto> CreateTopicAsync(CreateTopicRequest request, long authorId);
    Task<TopicDto> UpdateTopicAsync(long id, UpdateTopicRequest request, long editorId);
    Task DeleteTopicAsync(long id, long deleterId);
    Task<TopicDto> PinTopicAsync(long id, long moderatorId);
    Task<TopicDto> LockTopicAsync(long id, long moderatorId);
    Task<List<TopicDto>> GetUserTopicsAsync(long userId, int limit = 10);
    Task<List<TopicDto>> GetFeaturedTopicsAsync(int limit = 5);
}
```

**`Services/TopicService.cs`**
```csharp
using Forum.Api.Models.DTOs;
using Forum.Api.Models.Entities;
using Forum.Api.Repositories;
using System.Text.RegularExpressions;

namespace Forum.Api.Services;

public class TopicService : ITopicService
{
    private readonly ITopicRepository _topicRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ITagRepository _tagRepository;
    private readonly IMarkdownService _markdownService;
    private readonly ILogger<TopicService> _logger;

    public TopicService(
        ITopicRepository topicRepository,
        ICategoryRepository categoryRepository,
        ITagRepository tagRepository,
        IMarkdownService markdownService,
        ILogger<TopicService> logger)
    {
        _topicRepository = topicRepository;
        _categoryRepository = categoryRepository;
        _tagRepository = tagRepository;
        _markdownService = markdownService;
        _logger = logger;
    }

    public async Task<TopicDto?> GetTopicAsync(long id, long? viewerId = null, string? ipAddress = null)
    {
        var topic = await _topicRepository.GetByIdAsync(id);
        if (topic == null) return null;

        // 记录浏览
        if (!string.IsNullOrEmpty(ipAddress))
        {
            await _topicRepository.IncrementViewCountAsync(id, viewerId, ipAddress);
        }

        return MapToTopicDto(topic);
    }

    public async Task<TopicDto?> GetTopicBySlugAsync(long categoryId, string slug, long? viewerId = null, string? ipAddress = null)
    {
        var topic = await _topicRepository.GetBySlugAsync(categoryId, slug);
        if (topic == null) return null;

        // 记录浏览
        if (!string.IsNullOrEmpty(ipAddress))
        {
            await _topicRepository.IncrementViewCountAsync(topic.Id, viewerId, ipAddress);
        }

        return MapToTopicDto(topic);
    }

    public async Task<TopicsListResponse> GetTopicsAsync(TopicsListRequest request)
    {
        var query = new TopicQuery
        {
            CategoryId = request.CategoryId,
            TagSlugs = request.TagSlugs ?? new List<string>(),
            Sort = request.Sort ?? "latest",
            Limit = Math.Min(request.Limit ?? 20, 50), // 限制最大数量
            CursorLastPosted = request.CursorLastPosted,
            CursorId = request.CursorId,
            IncludePinned = request.IncludePinned ?? true
        };

        var (topics, hasMore) = await _topicRepository.GetTopicsAsync(query);
        var topicDtos = topics.Select(MapToTopicDto).ToList();

        return new TopicsListResponse
        {
            Topics = topicDtos,
            HasMore = hasMore,
            NextCursor = hasMore && topicDtos.Any() 
                ? new CursorInfo 
                { 
                    LastPostedAt = topicDtos.Last().LastPostedAt, 
                    Id = topicDtos.Last().Id 
                } 
                : null
        };
    }

    public async Task<TopicDto> CreateTopicAsync(CreateTopicRequest request, long authorId)
    {
        // 验证分类存在
        var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
        if (category == null)
            throw new ArgumentException("分类不存在");

        // 生成 slug
        var baseSlug = GenerateSlug(request.Title);
        var slug = await EnsureUniqueSlugAsync(request.CategoryId, baseSlug);

        // 处理标签
        var tagIds = new List<long>();
        if (request.TagSlugs?.Any() == true)
        {
            var tags = await _tagRepository.GetBySlugAsync(request.TagSlugs);
            tagIds = tags.Select(t => t.Id).ToList();
        }

        // 从内容提取摘要
        var excerpt = !string.IsNullOrEmpty(request.Content) 
            ? _markdownService.ExtractPlainText(request.Content, 200)
            : null;

        var topic = new Topic
        {
            Title = request.Title,
            Slug = slug,
            Excerpt = excerpt,
            FeaturedImageUrl = request.FeaturedImageUrl,
            AuthorId = authorId,
            CategoryId = request.CategoryId,
            IsPinned = false,
            IsLocked = false,
            LastPostedAt = DateTime.UtcNow
        };

        var topicId = await _topicRepository.CreateAsync(topic, tagIds);
        topic.Id = topicId;

        _logger.LogInformation("Topic created: {TopicId} by user {AuthorId}", topicId, authorId);

        return MapToTopicDto(topic);
    }

    public async Task<TopicDto> UpdateTopicAsync(long id, UpdateTopicRequest request, long editorId)
    {
        var topic = await _topicRepository.GetByIdAsync(id);
        if (topic == null)
            throw new ArgumentException("主题不存在");

        // 检查权限（简化版，实际应该检查用户角色）
        var canEdit = topic.AuthorId == editorId || await CanModerateTopicAsync(editorId, topic.CategoryId);
        if (!canEdit)
            throw new UnauthorizedAccessException("没有编辑权限");

        // 更新字段
        if (!string.IsNullOrEmpty(request.Title) && request.Title != topic.Title)
        {
            topic.Title = request.Title;
            topic.Slug = await EnsureUniqueSlugAsync(topic.CategoryId, GenerateSlug(request.Title), topic.Id);
        }

        if (request.CategoryId.HasValue && request.CategoryId != topic.CategoryId)
        {
            var category = await _categoryRepository.GetByIdAsync(request.CategoryId.Value);
            if (category == null)
                throw new ArgumentException("目标分类不存在");
            topic.CategoryId = request.CategoryId.Value;
        }

        topic.Excerpt = request.Excerpt;
        topic.FeaturedImageUrl = request.FeaturedImageUrl;
        topic.EditReason = request.EditReason;
        topic.LastEditorId = editorId;

        await _topicRepository.UpdateAsync(topic);

        _logger.LogInformation("Topic updated: {TopicId} by user {EditorId}", id, editorId);

        return MapToTopicDto(topic);
    }

    public async Task DeleteTopicAsync(long id, long deleterId)
    {
        var topic = await _topicRepository.GetByIdAsync(id);
        if (topic == null)
            throw new ArgumentException("主题不存在");

        // 检查权限
        var canDelete = topic.AuthorId == deleterId || await CanModerateTopicAsync(deleterId, topic.CategoryId);
        if (!canDelete)
            throw new UnauthorizedAccessException("没有删除权限");

        await _topicRepository.DeleteAsync(id, deleterId);

        _logger.LogInformation("Topic deleted: {TopicId} by user {DeleterId}", id, deleterId);
    }

    public async Task<TopicDto> PinTopicAsync(long id, long moderatorId)
    {
        var topic = await _topicRepository.GetByIdAsync(id);
        if (topic == null)
            throw new ArgumentException("主题不存在");

        if (!await CanModerateTopicAsync(moderatorId, topic.CategoryId))
            throw new UnauthorizedAccessException("没有版主权限");

        topic.IsPinned = !topic.IsPinned;
        topic.LastEditorId = moderatorId;
        topic.EditReason = topic.IsPinned ? "置顶" : "取消置顶";

        await _topicRepository.UpdateAsync(topic);

        _logger.LogInformation("Topic pin status changed: {TopicId} pinned={IsPinned} by moderator {ModeratorId}", 
            id, topic.IsPinned, moderatorId);

        return MapToTopicDto(topic);
    }

    public async Task<TopicDto> LockTopicAsync(long id, long moderatorId)
    {
        var topic = await _topicRepository.GetByIdAsync(id);
        if (topic == null)
            throw new ArgumentException("主题不存在");

        if (!await CanModerateTopicAsync(moderatorId, topic.CategoryId))
            throw new UnauthorizedAccessException("没有版主权限");

        topic.IsLocked = !topic.IsLocked;
        topic.LastEditorId = moderatorId;
        topic.EditReason = topic.IsLocked ? "锁定" : "解锁";

        await _topicRepository.UpdateAsync(topic);

        _logger.LogInformation("Topic lock status changed: {TopicId} locked={IsLocked} by moderator {ModeratorId}", 
            id, topic.IsLocked, moderatorId);

        return MapToTopicDto(topic);
    }

    public async Task<List<TopicDto>> GetUserTopicsAsync(long userId, int limit = 10)
    {
        var topics = await _topicRepository.GetUserTopicsAsync(userId, limit);
        return topics.Select(MapToTopicDto).ToList();
    }

    public async Task<List<TopicDto>> GetFeaturedTopicsAsync(int limit = 5)
    {
        var topics = await _topicRepository.GetFeaturedTopicsAsync(limit);
        return topics.Select(MapToTopicDto).ToList();
    }

    private static TopicDto MapToTopicDto(Topic topic)
    {
        return new TopicDto
        {
            Id = topic.Id,
            Title = topic.Title,
            Slug = topic.Slug,
            Excerpt = topic.Excerpt,
            FeaturedImageUrl = topic.FeaturedImageUrl,
            AuthorId = topic.AuthorId,
            CategoryId = topic.CategoryId,
            IsPinned = topic.IsPinned,
            IsLocked = topic.IsLocked,
            ReplyCount = topic.ReplyCount,
            ViewCount = topic.ViewCount,
            LastPostedAt = topic.LastPostedAt,
            LastPosterId = topic.LastPosterId,
            CreatedAt = topic.CreatedAt,
            UpdatedAt = topic.UpdatedAt,
            Author = topic.Author != null ? new UserSummaryDto
            {
                Id = topic.Author.Id,
                Username = topic.Author.Username,
                AvatarUrl = topic.Author.AvatarUrl
            } : null,
            LastPoster = topic.LastPoster != null ? new UserSummaryDto
            {
                Id = topic.LastPoster.Id,
                Username = topic.LastPoster.Username,
                AvatarUrl = topic.LastPoster.AvatarUrl
            } : null,
            Category = topic.Category != null ? new CategorySummaryDto
            {
                Id = topic.Category.Id,
                Name = topic.Category.Name,
                Slug = topic.Category.Slug,
                Color = topic.Category.Color
            } : null,
            Tags = topic.Tags?.Select(t => new TagDto
            {
                Id = t.Id,
                Name = t.Name,
                Slug = t.Slug,
                Color = t.Color
            }).ToList() ?? new List<TagDto>()
        };
    }

    private static string GenerateSlug(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "untitled";

        // 简化的 slug 生成，生产环境应该处理更多字符
        var slug = title.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-");

        // 移除特殊字符，只保留字母、数字和连字符
        slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");
        
        // 移除多余的连字符
        slug = Regex.Replace(slug, @"-+", "-").Trim('-');

        return string.IsNullOrEmpty(slug) ? "untitled" : slug.Substring(0, Math.Min(slug.Length, 50));
    }

    private async Task<string> EnsureUniqueSlugAsync(long categoryId, string baseSlug, long? excludeTopicId = null)
    {
        var slug = baseSlug;
        var counter = 1;

        while (await _topicRepository.ExistsAsync(categoryId, slug, excludeTopicId))
        {
            slug = $"{baseSlug}-{counter}";
            counter++;
        }

        return slug;
    }

    private async Task<bool> CanModerateTopicAsync(long userId, long categoryId)
    {
        // 简化实现，实际应该检查用户角色和分类权限
        // 这里可以调用用户服务检查是否为版主或管理员
        return false; // 临时返回 false
    }
}
```

---

## ✅ M2 验收清单

### 后端 API 完整性
- [ ] **主题 CRUD API** (创建、读取、更新、删除)
- [ ] **帖子 CRUD API** (回帖、编辑、删除)
- [ ] **Keyset 分页** (性能优化的分页方案)
- [ ] **Markdown 渲染** (安全的 HTML 输出)
- [ ] **@ 提及解析** (用户提及功能)

### 数据完整性
- [ ] **事务一致性** (发帖时统计同步更新)
- [ ] **乐观并发控制** (编辑冲突检测)
- [ ] **软删除机制** (数据可恢复)
- [ ] **编辑历史记录** (内容修改追踪)
- [ ] **浏览量统计** (防刷访问计数)

### 前端界面实现
- [ ] **主题列表页面** (Discourse 风格卡片)
- [ ] **主题详情页面** (楼层显示、时间轴)
- [ ] **底部浮动编辑器** (Composer 组件)
- [ ] **无限滚动** (自动加载更多)
- [ ] **Markdown 编辑器** (预览功能)

### 安全性验证
- [ ] **XSS 防护** (HTML 内容消毒)
- [ ] **权限控制** (编辑时间窗口、角色检查)
- [ ] **输入验证** (前后端参数校验)
- [ ] **SQL 注入防护** (参数化查询)

### 性能优化
- [ ] **数据库索引** (查询性能优化)
- [ ] **分页性能** (Keyset 分页实现)
- [ ] **缓存策略** (热门内容缓存)
- [ ] **Markdown 缓存** (渲染结果缓存)

---

**预计完成时间**: 10 个工作日  
**关键阻塞点**: Markdown 安全渲染、无限滚动性能  
**下一步**: M3 分类标签 + 搜索系统开发