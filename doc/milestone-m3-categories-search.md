# M3: 分类标签 + 搜索系统详细实现步骤

**时间估算**: 1周 (5个工作日)  
**优先级**: 中高 (内容组织与发现)  
**负责人**: 全栈开发团队

## 📋 任务总览

- ✅ 分类管理系统 (CRUD + 权限控制)
- ✅ 标签系统实现 (动态标签 + 使用统计)
- ✅ MySQL 全文搜索 (FULLTEXT 索引)
- ✅ 搜索结果排序优化
- ✅ 前端筛选与搜索界面
- ✅ 性能优化与缓存策略

---

## 🗂️ Day 1: 分类管理系统

### 1.1 分类仓储实现

**`Repositories/ICategoryRepository.cs`**
```csharp
using Forum.Api.Models.Entities;

namespace Forum.Api.Repositories;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(long id);
    Task<Category?> GetBySlugAsync(string slug);
    Task<List<Category>> GetAllAsync(bool includeArchived = false);
    Task<List<Category>> GetUserModeratedCategoriesAsync(long userId);
    Task<long> CreateAsync(Category category);
    Task UpdateAsync(Category category);
    Task ArchiveAsync(long id);
    Task<bool> ExistsAsync(string slug, long? excludeId = null);
    Task<bool> CanUserModerateAsync(long userId, long categoryId);
    Task AddModeratorAsync(long categoryId, long userId);
    Task RemoveModeratorAsync(long categoryId, long userId);
    Task<List<CategoryStats>> GetCategoryStatsAsync();
}

public class CategoryStats
{
    public long CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int TopicCount { get; set; }
    public int PostCount { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public Topic? LatestTopic { get; set; }
}
```

**`Repositories/CategoryRepository.cs`**
```csharp
using Dapper;
using Forum.Api.Infrastructure.Database;
using Forum.Api.Models.Entities;

namespace Forum.Api.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<CategoryRepository> _logger;

    public CategoryRepository(IDbConnectionFactory connectionFactory, ILogger<CategoryRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<Category?> GetByIdAsync(long id)
    {
        const string sql = "SELECT * FROM categories WHERE id = @Id";
        
        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QueryFirstOrDefaultAsync<Category>(sql, new { Id = id });
    }

    public async Task<Category?> GetBySlugAsync(string slug)
    {
        const string sql = "SELECT * FROM categories WHERE slug = @Slug";
        
        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QueryFirstOrDefaultAsync<Category>(sql, new { Slug = slug });
    }

    public async Task<List<Category>> GetAllAsync(bool includeArchived = false)
    {
        var sql = "SELECT * FROM categories";
        if (!includeArchived)
        {
            sql += " WHERE is_archived = 0";
        }
        sql += " ORDER BY `order`, name";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        var results = await connection.QueryAsync<Category>(sql);
        return results.ToList();
    }

    public async Task<List<Category>> GetUserModeratedCategoriesAsync(long userId)
    {
        const string sql = @"
            SELECT c.* FROM categories c
            JOIN category_moderators cm ON cm.category_id = c.id
            WHERE cm.user_id = @UserId AND c.is_archived = 0
            ORDER BY c.`order`, c.name";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        var results = await connection.QueryAsync<Category>(sql, new { UserId = userId });
        return results.ToList();
    }

    public async Task<long> CreateAsync(Category category)
    {
        const string sql = @"
            INSERT INTO categories (name, slug, description, color, `order`, is_archived, created_at, updated_at)
            VALUES (@Name, @Slug, @Description, @Color, @Order, @IsArchived, @CreatedAt, @UpdatedAt);
            SELECT LAST_INSERT_ID();";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.ExecuteScalarAsync<long>(sql, new
        {
            category.Name,
            category.Slug,
            category.Description,
            category.Color,
            category.Order,
            category.IsArchived,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
    }

    public async Task UpdateAsync(Category category)
    {
        const string sql = @"
            UPDATE categories 
            SET name = @Name, slug = @Slug, description = @Description, 
                color = @Color, `order` = @Order, is_archived = @IsArchived,
                updated_at = @UpdatedAt
            WHERE id = @Id";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync(sql, new
        {
            category.Id,
            category.Name,
            category.Slug,
            category.Description,
            category.Color,
            category.Order,
            category.IsArchived,
            UpdatedAt = DateTime.UtcNow
        });
    }

    public async Task ArchiveAsync(long id)
    {
        const string sql = "UPDATE categories SET is_archived = 1, updated_at = @UpdatedAt WHERE id = @Id";
        
        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync(sql, new { Id = id, UpdatedAt = DateTime.UtcNow });
    }

    public async Task<bool> ExistsAsync(string slug, long? excludeId = null)
    {
        var sql = "SELECT COUNT(*) FROM categories WHERE slug = @Slug";
        var parameters = new { Slug = slug };

        if (excludeId.HasValue)
        {
            sql += " AND id != @ExcludeId";
            parameters = new { Slug = slug, ExcludeId = excludeId.Value };
        }

        using var connection = await _connectionFactory.CreateConnectionAsync();
        var count = await connection.ExecuteScalarAsync<int>(sql, parameters);
        return count > 0;
    }

    public async Task<bool> CanUserModerateAsync(long userId, long categoryId)
    {
        const string sql = @"
            SELECT COUNT(*) FROM category_moderators 
            WHERE user_id = @UserId AND category_id = @CategoryId";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        var count = await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId, CategoryId = categoryId });
        return count > 0;
    }

    public async Task AddModeratorAsync(long categoryId, long userId)
    {
        const string sql = @"
            INSERT IGNORE INTO category_moderators (category_id, user_id) 
            VALUES (@CategoryId, @UserId)";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync(sql, new { CategoryId = categoryId, UserId = userId });
    }

    public async Task RemoveModeratorAsync(long categoryId, long userId)
    {
        const string sql = @"
            DELETE FROM category_moderators 
            WHERE category_id = @CategoryId AND user_id = @UserId";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync(sql, new { CategoryId = categoryId, UserId = userId });
    }

    public async Task<List<CategoryStats>> GetCategoryStatsAsync()
    {
        const string sql = @"
            SELECT 
                c.id as CategoryId,
                c.name as CategoryName,
                c.slug as CategorySlug,
                c.color as Color,
                COUNT(DISTINCT t.id) as TopicCount,
                COUNT(DISTINCT p.id) as PostCount,
                MAX(COALESCE(t.last_posted_at, t.created_at)) as LastActivityAt
            FROM categories c
            LEFT JOIN topics t ON t.category_id = c.id AND t.is_deleted = 0
            LEFT JOIN posts p ON p.topic_id = t.id AND p.is_deleted = 0
            WHERE c.is_archived = 0
            GROUP BY c.id, c.name, c.slug, c.color
            ORDER BY c.`order`, c.name";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        var results = await connection.QueryAsync<CategoryStats>(sql);
        return results.ToList();
    }
}
```

### 1.2 分类服务实现

**`Services/ICategoryService.cs`**
```csharp
using Forum.Api.Models.DTOs;

namespace Forum.Api.Services;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetCategoriesAsync(bool includeStats = false);
    Task<CategoryDto?> GetCategoryAsync(long id);
    Task<CategoryDto?> GetCategoryBySlugAsync(string slug);
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request, long creatorId);
    Task<CategoryDto> UpdateCategoryAsync(long id, UpdateCategoryRequest request, long editorId);
    Task DeleteCategoryAsync(long id, long deleterId);
    Task<List<CategoryDto>> GetUserModeratedCategoriesAsync(long userId);
    Task AddModeratorAsync(long categoryId, long userId, long adderId);
    Task RemoveModeratorAsync(long categoryId, long userId, long removerId);
}
```

**`Services/CategoryService.cs`**
```csharp
using Forum.Api.Models.DTOs;
using Forum.Api.Models.Entities;
using Forum.Api.Repositories;
using System.Text.RegularExpressions;

namespace Forum.Api.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(ICategoryRepository categoryRepository, ILogger<CategoryService> logger)
    {
        _categoryRepository = categoryRepository;
        _logger = logger;
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync(bool includeStats = false)
    {
        if (includeStats)
        {
            var stats = await _categoryRepository.GetCategoryStatsAsync();
            return stats.Select(s => new CategoryDto
            {
                Id = s.CategoryId,
                Name = s.CategoryName,
                Slug = s.CategorySlug,
                Color = s.Color,
                TopicCount = s.TopicCount,
                PostCount = s.PostCount,
                LastActivityAt = s.LastActivityAt,
                LatestTopic = s.LatestTopic != null ? new TopicSummaryDto
                {
                    Id = s.LatestTopic.Id,
                    Title = s.LatestTopic.Title,
                    Slug = s.LatestTopic.Slug,
                    CreatedAt = s.LatestTopic.CreatedAt
                } : null
            }).ToList();
        }

        var categories = await _categoryRepository.GetAllAsync();
        return categories.Select(MapToCategoryDto).ToList();
    }

    public async Task<CategoryDto?> GetCategoryAsync(long id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        return category != null ? MapToCategoryDto(category) : null;
    }

    public async Task<CategoryDto?> GetCategoryBySlugAsync(string slug)
    {
        var category = await _categoryRepository.GetBySlugAsync(slug);
        return category != null ? MapToCategoryDto(category) : null;
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request, long creatorId)
    {
        var slug = GenerateSlug(request.Name);
        slug = await EnsureUniqueSlugAsync(slug);

        var category = new Category
        {
            Name = request.Name,
            Slug = slug,
            Description = request.Description,
            Color = request.Color ?? "#007acc",
            Order = request.Order ?? 0,
            IsArchived = false
        };

        var categoryId = await _categoryRepository.CreateAsync(category);
        category.Id = categoryId;

        _logger.LogInformation("Category created: {CategoryId} by user {CreatorId}", categoryId, creatorId);

        return MapToCategoryDto(category);
    }

    public async Task<CategoryDto> UpdateCategoryAsync(long id, UpdateCategoryRequest request, long editorId)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null)
            throw new ArgumentException("分类不存在");

        if (!string.IsNullOrEmpty(request.Name) && request.Name != category.Name)
        {
            category.Name = request.Name;
            var newSlug = GenerateSlug(request.Name);
            category.Slug = await EnsureUniqueSlugAsync(newSlug, category.Id);
        }

        if (!string.IsNullOrEmpty(request.Description))
            category.Description = request.Description;

        if (!string.IsNullOrEmpty(request.Color))
            category.Color = request.Color;

        if (request.Order.HasValue)
            category.Order = request.Order.Value;

        if (request.IsArchived.HasValue)
            category.IsArchived = request.IsArchived.Value;

        await _categoryRepository.UpdateAsync(category);

        _logger.LogInformation("Category updated: {CategoryId} by user {EditorId}", id, editorId);

        return MapToCategoryDto(category);
    }

    public async Task DeleteCategoryAsync(long id, long deleterId)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null)
            throw new ArgumentException("分类不存在");

        await _categoryRepository.ArchiveAsync(id);

        _logger.LogInformation("Category archived: {CategoryId} by user {DeleterId}", id, deleterId);
    }

    public async Task<List<CategoryDto>> GetUserModeratedCategoriesAsync(long userId)
    {
        var categories = await _categoryRepository.GetUserModeratedCategoriesAsync(userId);
        return categories.Select(MapToCategoryDto).ToList();
    }

    public async Task AddModeratorAsync(long categoryId, long userId, long adderId)
    {
        var category = await _categoryRepository.GetByIdAsync(categoryId);
        if (category == null)
            throw new ArgumentException("分类不存在");

        await _categoryRepository.AddModeratorAsync(categoryId, userId);

        _logger.LogInformation("Moderator added: User {UserId} to category {CategoryId} by {AdderId}", 
            userId, categoryId, adderId);
    }

    public async Task RemoveModeratorAsync(long categoryId, long userId, long removerId)
    {
        await _categoryRepository.RemoveModeratorAsync(categoryId, userId);

        _logger.LogInformation("Moderator removed: User {UserId} from category {CategoryId} by {RemoverId}", 
            userId, categoryId, removerId);
    }

    private static CategoryDto MapToCategoryDto(Category category)
    {
        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            Color = category.Color,
            Order = category.Order,
            IsArchived = category.IsArchived,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }

    private static string GenerateSlug(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "untitled";

        var slug = name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-");

        slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");
        slug = Regex.Replace(slug, @"-+", "-").Trim('-');

        return string.IsNullOrEmpty(slug) ? "untitled" : slug.Substring(0, Math.Min(slug.Length, 50));
    }

    private async Task<string> EnsureUniqueSlugAsync(string baseSlug, long? excludeId = null)
    {
        var slug = baseSlug;
        var counter = 1;

        while (await _categoryRepository.ExistsAsync(slug, excludeId))
        {
            slug = $"{baseSlug}-{counter}";
            counter++;
        }

        return slug;
    }
}
```

---

## 🏷️ Day 2: 标签系统实现

### 2.1 标签仓储实现

**`Repositories/ITagRepository.cs`**
```csharp
using Forum.Api.Models.Entities;

namespace Forum.Api.Repositories;

public interface ITagRepository
{
    Task<Tag?> GetByIdAsync(long id);
    Task<Tag?> GetBySlugAsync(string slug);
    Task<List<Tag>> GetBySlugAsync(List<string> slugs);
    Task<List<Tag>> GetAllAsync(int limit = 100);
    Task<List<Tag>> GetPopularAsync(int limit = 20);
    Task<List<Tag>> GetTopicTagsAsync(long topicId);
    Task<long> CreateAsync(Tag tag);
    Task UpdateAsync(Tag tag);
    Task DeleteAsync(long id);
    Task<bool> ExistsAsync(string slug, long? excludeId = null);
    Task IncrementUsageAsync(long tagId);
    Task DecrementUsageAsync(long tagId);
    Task<List<Tag>> SearchTagsAsync(string query, int limit = 10);
    Task SyncTagUsageCountsAsync();
}
```

**`Repositories/TagRepository.cs`**
```csharp
using Dapper;
using Forum.Api.Infrastructure.Database;
using Forum.Api.Models.Entities;

namespace Forum.Api.Repositories;

public class TagRepository : ITagRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<TagRepository> _logger;

    public TagRepository(IDbConnectionFactory connectionFactory, ILogger<TagRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<Tag?> GetByIdAsync(long id)
    {
        const string sql = "SELECT * FROM tags WHERE id = @Id";
        
        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QueryFirstOrDefaultAsync<Tag>(sql, new { Id = id });
    }

    public async Task<Tag?> GetBySlugAsync(string slug)
    {
        const string sql = "SELECT * FROM tags WHERE slug = @Slug";
        
        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.QueryFirstOrDefaultAsync<Tag>(sql, new { Slug = slug });
    }

    public async Task<List<Tag>> GetBySlugAsync(List<string> slugs)
    {
        if (!slugs.Any()) return new List<Tag>();

        const string sql = "SELECT * FROM tags WHERE slug IN @Slugs";
        
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var results = await connection.QueryAsync<Tag>(sql, new { Slugs = slugs });
        return results.ToList();
    }

    public async Task<List<Tag>> GetAllAsync(int limit = 100)
    {
        const string sql = "SELECT * FROM tags ORDER BY usage_count DESC, name LIMIT @Limit";
        
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var results = await connection.QueryAsync<Tag>(sql, new { Limit = limit });
        return results.ToList();
    }

    public async Task<List<Tag>> GetPopularAsync(int limit = 20)
    {
        const string sql = @"
            SELECT * FROM tags 
            WHERE usage_count > 0 
            ORDER BY usage_count DESC, name 
            LIMIT @Limit";
        
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var results = await connection.QueryAsync<Tag>(sql, new { Limit = limit });
        return results.ToList();
    }

    public async Task<List<Tag>> GetTopicTagsAsync(long topicId)
    {
        const string sql = @"
            SELECT t.* FROM tags t
            JOIN topic_tags tt ON tt.tag_id = t.id
            WHERE tt.topic_id = @TopicId
            ORDER BY t.usage_count DESC, t.name";
        
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var results = await connection.QueryAsync<Tag>(sql, new { TopicId = topicId });
        return results.ToList();
    }

    public async Task<long> CreateAsync(Tag tag)
    {
        const string sql = @"
            INSERT INTO tags (name, slug, description, color, usage_count, created_at, updated_at)
            VALUES (@Name, @Slug, @Description, @Color, @UsageCount, @CreatedAt, @UpdatedAt);
            SELECT LAST_INSERT_ID();";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        return await connection.ExecuteScalarAsync<long>(sql, new
        {
            tag.Name,
            tag.Slug,
            tag.Description,
            tag.Color,
            tag.UsageCount,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
    }

    public async Task UpdateAsync(Tag tag)
    {
        const string sql = @"
            UPDATE tags 
            SET name = @Name, slug = @Slug, description = @Description, 
                color = @Color, usage_count = @UsageCount, updated_at = @UpdatedAt
            WHERE id = @Id";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync(sql, new
        {
            tag.Id,
            tag.Name,
            tag.Slug,
            tag.Description,
            tag.Color,
            tag.UsageCount,
            UpdatedAt = DateTime.UtcNow
        });
    }

    public async Task DeleteAsync(long id)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            // 删除主题关联
            await connection.ExecuteAsync("DELETE FROM topic_tags WHERE tag_id = @Id", new { Id = id }, transaction);
            
            // 删除标签
            await connection.ExecuteAsync("DELETE FROM tags WHERE id = @Id", new { Id = id }, transaction);
            
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string slug, long? excludeId = null)
    {
        var sql = "SELECT COUNT(*) FROM tags WHERE slug = @Slug";
        var parameters = new { Slug = slug };

        if (excludeId.HasValue)
        {
            sql += " AND id != @ExcludeId";
            parameters = new { Slug = slug, ExcludeId = excludeId.Value };
        }

        using var connection = await _connectionFactory.CreateConnectionAsync();
        var count = await connection.ExecuteScalarAsync<int>(sql, parameters);
        return count > 0;
    }

    public async Task IncrementUsageAsync(long tagId)
    {
        const string sql = "UPDATE tags SET usage_count = usage_count + 1, updated_at = @UpdatedAt WHERE id = @Id";
        
        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync(sql, new { Id = tagId, UpdatedAt = DateTime.UtcNow });
    }

    public async Task DecrementUsageAsync(long tagId)
    {
        const string sql = @"
            UPDATE tags 
            SET usage_count = GREATEST(usage_count - 1, 0), updated_at = @UpdatedAt 
            WHERE id = @Id";
        
        using var connection = await _connectionFactory.CreateConnectionAsync();
        await connection.ExecuteAsync(sql, new { Id = tagId, UpdatedAt = DateTime.UtcNow });
    }

    public async Task<List<Tag>> SearchTagsAsync(string query, int limit = 10)
    {
        const string sql = @"
            SELECT * FROM tags 
            WHERE name LIKE @Query OR slug LIKE @Query
            ORDER BY usage_count DESC, name
            LIMIT @Limit";

        var searchPattern = $"%{query}%";
        
        using var connection = await _connectionFactory.CreateConnectionAsync();
        var results = await connection.QueryAsync<Tag>(sql, new { Query = searchPattern, Limit = limit });
        return results.ToList();
    }

    public async Task SyncTagUsageCountsAsync()
    {
        const string sql = @"
            UPDATE tags t
            SET usage_count = (
                SELECT COUNT(*) 
                FROM topic_tags tt 
                JOIN topics tp ON tp.id = tt.topic_id 
                WHERE tt.tag_id = t.id AND tp.is_deleted = 0
            ), updated_at = @UpdatedAt";

        using var connection = await _connectionFactory.CreateConnectionAsync();
        var updatedCount = await connection.ExecuteAsync(sql, new { UpdatedAt = DateTime.UtcNow });
        
        _logger.LogInformation("Synced usage counts for {Count} tags", updatedCount);
    }
}
```

---

## 🔍 Day 3: MySQL 全文搜索实现

### 3.1 搜索仓储实现

**`Repositories/ISearchRepository.cs`**
```csharp
namespace Forum.Api.Repositories;

public interface ISearchRepository
{
    Task<SearchResults> SearchAsync(SearchQuery query);
    Task<List<SearchSuggestion>> GetSearchSuggestionsAsync(string query, int limit = 5);
    Task RecordSearchAsync(string query, long? userId, string ipAddress);
    Task<List<PopularSearch>> GetPopularSearchesAsync(int limit = 10);
}

public class SearchQuery
{
    public string Query { get; set; } = string.Empty;
    public long? CategoryId { get; set; }
    public List<string> TagSlugs { get; set; } = new();
    public string SearchType { get; set; } = "all"; // all, topics, posts
    public string SortBy { get; set; } = "relevance"; // relevance, date, activity
    public int Limit { get; set; } = 20;
    public int Offset { get; set; } = 0;
}

public class SearchResults
{
    public List<SearchResultItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public string Query { get; set; } = string.Empty;
    public TimeSpan SearchTime { get; set; }
}

public class SearchResultItem
{
    public string Type { get; set; } = string.Empty; // topic, post
    public long Id { get; set; }
    public long TopicId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Snippet { get; set; } = string.Empty;
    public float Relevance { get; set; }
    public string AuthorUsername { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Url { get; set; } = string.Empty;
}

public class SearchSuggestion
{
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // query, topic, user, tag
    public int Count { get; set; }
}

public class PopularSearch
{
    public string Query { get; set; } = string.Empty;
    public int SearchCount { get; set; }
    public DateTime LastSearched { get; set; }
}
```

**`Repositories/SearchRepository.cs`**
```csharp
using Dapper;
using Forum.Api.Infrastructure.Database;
using System.Diagnostics;
using System.Text;

namespace Forum.Api.Repositories;

public class SearchRepository : ISearchRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<SearchRepository> _logger;

    public SearchRepository(IDbConnectionFactory connectionFactory, ILogger<SearchRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<SearchResults> SearchAsync(SearchQuery query)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var results = new SearchResults
            {
                Query = query.Query,
                Items = new List<SearchResultItem>()
            };

            // 如果查询为空，返回空结果
            if (string.IsNullOrWhiteSpace(query.Query))
                return results;

            // 根据搜索类型执行不同的搜索
            if (query.SearchType == "all" || query.SearchType == "topics")
            {
                var topicResults = await SearchTopicsAsync(query);
                results.Items.AddRange(topicResults);
            }

            if (query.SearchType == "all" || query.SearchType == "posts")
            {
                var postResults = await SearchPostsAsync(query);
                results.Items.AddRange(postResults);
            }

            // 排序结果
            results.Items = SortResults(results.Items, query.SortBy).ToList();

            // 分页
            results.TotalCount = results.Items.Count;
            results.Items = results.Items.Skip(query.Offset).Take(query.Limit).ToList();

            return results;
        }
        finally
        {
            stopwatch.Stop();
            var searchTime = stopwatch.Elapsed;
            _logger.LogInformation("Search completed in {ElapsedMs}ms for query: {Query}", 
                searchTime.TotalMilliseconds, query.Query);
        }
    }

    private async Task<List<SearchResultItem>> SearchTopicsAsync(SearchQuery query)
    {
        var sqlBuilder = new StringBuilder(@"
            SELECT 
                'topic' as Type,
                t.id as Id,
                t.id as TopicId,
                t.title as Title,
                COALESCE(t.excerpt, SUBSTRING(t.title, 1, 200)) as Snippet,
                MATCH(t.title) AGAINST (@Query IN NATURAL LANGUAGE MODE) as Relevance,
                u.username as AuthorUsername,
                c.name as CategoryName,
                c.slug as CategorySlug,
                t.created_at as CreatedAt
            FROM topics t
            JOIN users u ON u.id = t.author_id
            JOIN categories c ON c.id = t.category_id
            WHERE t.is_deleted = 0 
            AND MATCH(t.title) AGAINST (@Query IN NATURAL LANGUAGE MODE)");

        var parameters = new DynamicParameters();
        parameters.Add("Query", query.Query);

        // 分类筛选
        if (query.CategoryId.HasValue)
        {
            sqlBuilder.Append(" AND t.category_id = @CategoryId");
            parameters.Add("CategoryId", query.CategoryId.Value);
        }

        // 标签筛选
        if (query.TagSlugs.Any())
        {
            sqlBuilder.Append(@"
                AND EXISTS (
                    SELECT 1 FROM topic_tags tt 
                    JOIN tags g ON g.id = tt.tag_id 
                    WHERE tt.topic_id = t.id AND g.slug IN @TagSlugs
                )");
            parameters.Add("TagSlugs", query.TagSlugs);
        }

        sqlBuilder.Append(" HAVING Relevance > 0");

        using var connection = await _connectionFactory.CreateConnectionAsync();
        var results = await connection.QueryAsync<SearchResultItem>(sqlBuilder.ToString(), parameters);
        
        return results.Select(r => 
        {
            r.Url = $"/t/{r.TopicId}";
            return r;
        }).ToList();
    }

    private async Task<List<SearchResultItem>> SearchPostsAsync(SearchQuery query)
    {
        var sqlBuilder = new StringBuilder(@"
            SELECT 
                'post' as Type,
                p.id as Id,
                p.topic_id as TopicId,
                t.title as Title,
                SUBSTRING(p.content_md, 1, 300) as Snippet,
                MATCH(p.content_md) AGAINST (@Query IN NATURAL LANGUAGE MODE) as Relevance,
                u.username as AuthorUsername,
                c.name as CategoryName,
                c.slug as CategorySlug,
                p.created_at as CreatedAt
            FROM posts p
            JOIN topics t ON t.id = p.topic_id
            JOIN users u ON u.id = p.author_id
            JOIN categories c ON c.id = t.category_id
            WHERE p.is_deleted = 0 AND t.is_deleted = 0
            AND MATCH(p.content_md) AGAINST (@Query IN NATURAL LANGUAGE MODE)");

        var parameters = new DynamicParameters();
        parameters.Add("Query", query.Query);

        // 分类筛选
        if (query.CategoryId.HasValue)
        {
            sqlBuilder.Append(" AND t.category_id = @CategoryId");
            parameters.Add("CategoryId", query.CategoryId.Value);
        }

        // 标签筛选
        if (query.TagSlugs.Any())
        {
            sqlBuilder.Append(@"
                AND EXISTS (
                    SELECT 1 FROM topic_tags tt 
                    JOIN tags g ON g.id = tt.tag_id 
                    WHERE tt.topic_id = t.id AND g.slug IN @TagSlugs
                )");
            parameters.Add("TagSlugs", query.TagSlugs);
        }

        sqlBuilder.Append(" HAVING Relevance > 0");

        using var connection = await _connectionFactory.CreateConnectionAsync();
        var results = await connection.QueryAsync<SearchResultItem>(sqlBuilder.ToString(), parameters);
        
        return results.Select(r => 
        {
            r.Url = $"/t/{r.TopicId}#post-{r.Id}";
            return r;
        }).ToList();
    }

    private static IEnumerable<SearchResultItem> SortResults(List<SearchResultItem> items, string sortBy)
    {
        return sortBy.ToLower() switch
        {
            "date" => items.OrderByDescending(x => x.CreatedAt),
            "activity" => items.OrderByDescending(x => x.CreatedAt), // 简化处理
            _ => items.OrderByDescending(x => x.Relevance).ThenByDescending(x => x.CreatedAt)
        };
    }

    public async Task<List<SearchSuggestion>> GetSearchSuggestionsAsync(string query, int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return new List<SearchSuggestion>();

        var suggestions = new List<SearchSuggestion>();
        var searchPattern = $"%{query}%";

        using var connection = await _connectionFactory.CreateConnectionAsync();

        // 主题标题建议
        var topicSuggestions = await connection.QueryAsync<SearchSuggestion>(@"
            SELECT DISTINCT t.title as Text, 'topic' as Type, 0 as Count
            FROM topics t
            WHERE t.title LIKE @Pattern AND t.is_deleted = 0
            ORDER BY t.view_count DESC
            LIMIT @Limit", new { Pattern = searchPattern, Limit = limit });

        suggestions.AddRange(topicSuggestions);

        // 标签建议
        if (suggestions.Count < limit)
        {
            var tagSuggestions = await connection.QueryAsync<SearchSuggestion>(@"
                SELECT g.name as Text, 'tag' as Type, g.usage_count as Count
                FROM tags g
                WHERE g.name LIKE @Pattern
                ORDER BY g.usage_count DESC
                LIMIT @Limit", new { Pattern = searchPattern, Limit = limit - suggestions.Count });

            suggestions.AddRange(tagSuggestions);
        }

        return suggestions.Take(limit).ToList();
    }

    public async Task RecordSearchAsync(string query, long? userId, string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(query))
            return;

        // 简化实现，实际应该创建搜索历史表
        _logger.LogInformation("Search recorded: {Query} by user {UserId} from {IpAddress}", 
            query, userId, ipAddress);
    }

    public async Task<List<PopularSearch>> GetPopularSearchesAsync(int limit = 10)
    {
        // 简化实现，返回空列表
        // 实际应该从搜索历史表统计热门搜索
        return new List<PopularSearch>();
    }
}
```

### 3.2 搜索服务实现

**`Services/ISearchService.cs`**
```csharp
using Forum.Api.Models.DTOs;

namespace Forum.Api.Services;

public interface ISearchService
{
    Task<SearchResultsDto> SearchAsync(SearchRequestDto request, long? userId = null, string? ipAddress = null);
    Task<List<SearchSuggestionDto>> GetSuggestionsAsync(string query);
    Task<List<PopularSearchDto>> GetPopularSearchesAsync();
}
```

**`Services/SearchService.cs`**
```csharp
using Forum.Api.Models.DTOs;
using Forum.Api.Repositories;

namespace Forum.Api.Services;

public class SearchService : ISearchService
{
    private readonly ISearchRepository _searchRepository;
    private readonly ILogger<SearchService> _logger;

    public SearchService(ISearchRepository searchRepository, ILogger<SearchService> logger)
    {
        _searchRepository = searchRepository;
        _logger = logger;
    }

    public async Task<SearchResultsDto> SearchAsync(SearchRequestDto request, long? userId = null, string? ipAddress = null)
    {
        // 记录搜索
        if (!string.IsNullOrEmpty(ipAddress))
        {
            await _searchRepository.RecordSearchAsync(request.Query, userId, ipAddress);
        }

        var searchQuery = new SearchQuery
        {
            Query = request.Query?.Trim() ?? string.Empty,
            CategoryId = request.CategoryId,
            TagSlugs = request.TagSlugs ?? new List<string>(),
            SearchType = request.SearchType ?? "all",
            SortBy = request.SortBy ?? "relevance",
            Limit = Math.Min(request.Limit ?? 20, 50),
            Offset = request.Offset ?? 0
        };

        var results = await _searchRepository.SearchAsync(searchQuery);

        return new SearchResultsDto
        {
            Items = results.Items.Select(item => new SearchResultItemDto
            {
                Type = item.Type,
                Id = item.Id,
                TopicId = item.TopicId,
                Title = item.Title,
                Snippet = item.Snippet,
                Relevance = item.Relevance,
                AuthorUsername = item.AuthorUsername,
                CategoryName = item.CategoryName,
                CategorySlug = item.CategorySlug,
                CreatedAt = item.CreatedAt,
                Url = item.Url
            }).ToList(),
            TotalCount = results.TotalCount,
            Query = results.Query,
            SearchTime = results.SearchTime
        };
    }

    public async Task<List<SearchSuggestionDto>> GetSuggestionsAsync(string query)
    {
        var suggestions = await _searchRepository.GetSearchSuggestionsAsync(query);
        
        return suggestions.Select(s => new SearchSuggestionDto
        {
            Text = s.Text,
            Type = s.Type,
            Count = s.Count
        }).ToList();
    }

    public async Task<List<PopularSearchDto>> GetPopularSearchesAsync()
    {
        var popularSearches = await _searchRepository.GetPopularSearchesAsync();
        
        return popularSearches.Select(p => new PopularSearchDto
        {
            Query = p.Query,
            SearchCount = p.SearchCount,
            LastSearched = p.LastSearched
        }).ToList();
    }
}
```

---

## 🎨 Day 4-5: 前端筛选与搜索界面

### 4.1 搜索 API 客户端

**`src/api/search.ts`**
```typescript
import { api } from './client';

export interface SearchRequest {
  query: string;
  categoryId?: number;
  tagSlugs?: string[];
  searchType?: 'all' | 'topics' | 'posts';
  sortBy?: 'relevance' | 'date' | 'activity';
  limit?: number;
  offset?: number;
}

export interface SearchResult {
  type: 'topic' | 'post';
  id: number;
  topicId: number;
  title: string;
  snippet: string;
  relevance: number;
  authorUsername: string;
  categoryName: string;
  categorySlug: string;
  createdAt: string;
  url: string;
}

export interface SearchResults {
  items: SearchResult[];
  totalCount: number;
  query: string;
  searchTime: string;
}

export interface SearchSuggestion {
  text: string;
  type: 'topic' | 'tag' | 'user';
  count: number;
}

export const searchApi = {
  search: (params: SearchRequest) =>
    api.get<SearchResults>('/search', { params }),
    
  getSuggestions: (query: string) =>
    api.get<SearchSuggestion[]>('/search/suggestions', { params: { q: query } }),
    
  getPopularSearches: () =>
    api.get<{ query: string; count: number }[]>('/search/popular'),
};
```

### 4.2 搜索组件

**`src/components/search/SearchBox.tsx`**
```tsx
import { useState, useRef, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Search, X } from 'lucide-react';
import { useQuery } from '@tanstack/react-query';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Command, CommandEmpty, CommandGroup, CommandItem, CommandList } from '@/components/ui/command';
import { searchApi } from '@/api/search';
import { useDebounce } from '@/hooks/useDebounce';

interface SearchBoxProps {
  placeholder?: string;
  onSearch?: (query: string) => void;
  className?: string;
}

export function SearchBox({ placeholder = '搜索主题和内容...', onSearch, className }: SearchBoxProps) {
  const [query, setQuery] = useState('');
  const [isOpen, setIsOpen] = useState(false);
  const navigate = useNavigate();
  const inputRef = useRef<HTMLInputElement>(null);
  
  const debouncedQuery = useDebounce(query, 300);

  // 获取搜索建议
  const { data: suggestions } = useQuery({
    queryKey: ['search', 'suggestions', debouncedQuery],
    queryFn: () => searchApi.getSuggestions(debouncedQuery),
    enabled: debouncedQuery.length >= 2 && isOpen,
    staleTime: 5 * 60 * 1000, // 5分钟缓存
  });

  const handleSearch = (searchQuery: string) => {
    if (!searchQuery.trim()) return;
    
    setIsOpen(false);
    setQuery('');
    onSearch?.(searchQuery);
    navigate(`/search?q=${encodeURIComponent(searchQuery)}`);
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      handleSearch(query);
    } else if (e.key === 'Escape') {
      setIsOpen(false);
    }
  };

  const handleSuggestionClick = (suggestion: string) => {
    handleSearch(suggestion);
  };

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (inputRef.current && !inputRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  return (
    <div className={`relative ${className}`}>
      <div className="relative">
        <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          ref={inputRef}
          type="text"
          placeholder={placeholder}
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          onKeyDown={handleKeyDown}
          onFocus={() => setIsOpen(true)}
          className="pl-9 pr-9"
        />
        {query && (
          <Button
            variant="ghost"
            size="sm"
            className="absolute right-1 top-1/2 h-7 w-7 -translate-y-1/2 p-0"
            onClick={() => setQuery('')}
          >
            <X className="h-4 w-4" />
          </Button>
        )}
      </div>

      {isOpen && (query.length >= 2 || suggestions?.data) && (
        <div className="absolute top-full z-50 w-full mt-1">
          <Command className="rounded-lg border shadow-md">
            <CommandList>
              {suggestions?.data?.length ? (
                <>
                  <CommandGroup heading="搜索建议">
                    {suggestions.data.slice(0, 5).map((suggestion, index) => (
                      <CommandItem
                        key={index}
                        onSelect={() => handleSuggestionClick(suggestion.text)}
                        className="cursor-pointer"
                      >
                        <Search className="mr-2 h-4 w-4" />
                        <span>{suggestion.text}</span>
                        {suggestion.type === 'tag' && (
                          <span className="ml-auto text-xs text-muted-foreground">
                            标签 ({suggestion.count})
                          </span>
                        )}
                      </CommandItem>
                    ))}
                  </CommandGroup>
                  
                  {query.trim() && (
                    <CommandGroup>
                      <CommandItem
                        onSelect={() => handleSearch(query)}
                        className="cursor-pointer font-medium"
                      >
                        <Search className="mr-2 h-4 w-4" />
                        搜索 "{query}"
                      </CommandItem>
                    </CommandGroup>
                  )}
                </>
              ) : query.length >= 2 ? (
                <CommandEmpty>
                  <div className="p-4 text-center">
                    <p className="text-sm text-muted-foreground mb-2">
                      没有找到相关建议
                    </p>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleSearch(query)}
                    >
                      搜索 "{query}"
                    </Button>
                  </div>
                </CommandEmpty>
              ) : null}
            </CommandList>
          </Command>
        </div>
      )}
    </div>
  );
}
```

### 4.3 搜索结果页面

**`src/pages/SearchPage.tsx`**
```tsx
import { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Filter, Clock, Zap, TrendingUp } from 'lucide-react';

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Separator } from '@/components/ui/separator';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { SearchBox } from '@/components/search/SearchBox';
import { TopicCard } from '@/components/topics/TopicCard';
import { PostCard } from '@/components/posts/PostCard';
import { LoadingSpinner } from '@/components/ui/loading-spinner';
import { EmptyState } from '@/components/ui/empty-state';

import { searchApi } from '@/api/search';
import { categoriesApi } from '@/api/categories';
import { tagsApi } from '@/api/tags';
import { formatRelativeTime } from '@/lib/utils';

export function SearchPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [filters, setFilters] = useState({
    query: searchParams.get('q') || '',
    categoryId: searchParams.get('category') ? parseInt(searchParams.get('category')!) : undefined,
    tagSlugs: searchParams.getAll('tag'),
    searchType: searchParams.get('type') || 'all',
    sortBy: searchParams.get('sort') || 'relevance',
  });

  // 搜索结果
  const { data: searchResults, isLoading, error } = useQuery({
    queryKey: ['search', filters],
    queryFn: () => searchApi.search({
      query: filters.query,
      categoryId: filters.categoryId,
      tagSlugs: filters.tagSlugs,
      searchType: filters.searchType as any,
      sortBy: filters.sortBy as any,
      limit: 20,
    }),
    enabled: !!filters.query,
    staleTime: 2 * 60 * 1000, // 2分钟缓存
  });

  // 分类和标签选项
  const { data: categories } = useQuery({
    queryKey: ['categories'],
    queryFn: categoriesApi.getAll,
  });

  const { data: popularTags } = useQuery({
    queryKey: ['tags', 'popular'],
    queryFn: () => tagsApi.getPopular(20),
  });

  useEffect(() => {
    const query = searchParams.get('q') || '';
    if (query !== filters.query) {
      setFilters(prev => ({ ...prev, query }));
    }
  }, [searchParams, filters.query]);

  const updateFilters = (newFilters: Partial<typeof filters>) => {
    const updated = { ...filters, ...newFilters };
    setFilters(updated);

    // 更新 URL
    const params = new URLSearchParams();
    if (updated.query) params.set('q', updated.query);
    if (updated.categoryId) params.set('category', updated.categoryId.toString());
    updated.tagSlugs.forEach(tag => params.append('tag', tag));
    if (updated.searchType !== 'all') params.set('type', updated.searchType);
    if (updated.sortBy !== 'relevance') params.set('sort', updated.sortBy);

    setSearchParams(params);
  };

  const handleSearch = (query: string) => {
    updateFilters({ query });
  };

  const toggleTag = (tagSlug: string) => {
    const newTagSlugs = filters.tagSlugs.includes(tagSlug)
      ? filters.tagSlugs.filter(t => t !== tagSlug)
      : [...filters.tagSlugs, tagSlug];
    updateFilters({ tagSlugs: newTagSlugs });
  };

  const clearFilters = () => {
    updateFilters({
      categoryId: undefined,
      tagSlugs: [],
      searchType: 'all',
      sortBy: 'relevance',
    });
  };

  const hasFilters = filters.categoryId || filters.tagSlugs.length > 0 || 
                     filters.searchType !== 'all' || filters.sortBy !== 'relevance';

  return (
    <div className="container mx-auto px-4 py-6">
      {/* 搜索框 */}
      <div className="mb-6">
        <SearchBox
          placeholder="搜索主题和内容..."
          onSearch={handleSearch}
          className="max-w-2xl mx-auto"
        />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
        {/* 侧边栏筛选 */}
        <div className="lg:col-span-1">
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="flex items-center text-sm">
                <Filter className="mr-2 h-4 w-4" />
                筛选条件
                {hasFilters && (
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={clearFilters}
                    className="ml-auto text-xs"
                  >
                    清除
                  </Button>
                )}
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              {/* 内容类型 */}
              <div>
                <label className="text-xs font-medium text-muted-foreground">内容类型</label>
                <Select value={filters.searchType} onValueChange={(value) => updateFilters({ searchType: value })}>
                  <SelectTrigger className="w-full mt-1">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">全部</SelectItem>
                    <SelectItem value="topics">主题</SelectItem>
                    <SelectItem value="posts">帖子</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              {/* 排序方式 */}
              <div>
                <label className="text-xs font-medium text-muted-foreground">排序方式</label>
                <Select value={filters.sortBy} onValueChange={(value) => updateFilters({ sortBy: value })}>
                  <SelectTrigger className="w-full mt-1">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="relevance">
                      <div className="flex items-center">
                        <Zap className="mr-2 h-3 w-3" />
                        相关度
                      </div>
                    </SelectItem>
                    <SelectItem value="date">
                      <div className="flex items-center">
                        <Clock className="mr-2 h-3 w-3" />
                        最新发布
                      </div>
                    </SelectItem>
                    <SelectItem value="activity">
                      <div className="flex items-center">
                        <TrendingUp className="mr-2 h-3 w-3" />
                        最新活跃
                      </div>
                    </SelectItem>
                  </SelectContent>
                </Select>
              </div>

              {/* 分类筛选 */}
              {categories?.data && (
                <div>
                  <label className="text-xs font-medium text-muted-foreground">分类</label>
                  <Select 
                    value={filters.categoryId?.toString() || 'all'} 
                    onValueChange={(value) => updateFilters({ categoryId: value === 'all' ? undefined : parseInt(value) })}
                  >
                    <SelectTrigger className="w-full mt-1">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="all">全部分类</SelectItem>
                      {categories.data.map((category) => (
                        <SelectItem key={category.id} value={category.id.toString()}>
                          <div className="flex items-center">
                            <div 
                              className="w-3 h-3 rounded-full mr-2"
                              style={{ backgroundColor: category.color }}
                            />
                            {category.name}
                          </div>
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              )}

              {/* 标签筛选 */}
              {popularTags?.data && (
                <div>
                  <label className="text-xs font-medium text-muted-foreground mb-2 block">热门标签</label>
                  <div className="flex flex-wrap gap-1">
                    {popularTags.data.slice(0, 10).map((tag) => (
                      <Badge
                        key={tag.id}
                        variant={filters.tagSlugs.includes(tag.slug) ? "default" : "secondary"}
                        className="cursor-pointer text-xs"
                        onClick={() => toggleTag(tag.slug)}
                      >
                        {tag.name}
                      </Badge>
                    ))}
                  </div>
                </div>
              )}
            </CardContent>
          </Card>
        </div>

        {/* 搜索结果 */}
        <div className="lg:col-span-3">
          {!filters.query ? (
            <EmptyState
              title="开始搜索"
              description="输入关键词搜索主题和帖子内容"
              icon="🔍"
            />
          ) : isLoading ? (
            <div className="flex justify-center py-8">
              <LoadingSpinner />
            </div>
          ) : error ? (
            <EmptyState
              title="搜索出错"
              description="请稍后重试"
              icon="❌"
            />
          ) : (
            <div className="space-y-4">
              {/* 搜索统计 */}
              <div className="flex items-center justify-between">
                <div className="text-sm text-muted-foreground">
                  找到 <span className="font-medium">{searchResults?.data.totalCount || 0}</span> 个结果
                  {searchResults?.data.searchTime && (
                    <span className="ml-2">
                      (用时 {searchResults.data.searchTime})
                    </span>
                  )}
                </div>
                
                {/* 已选择的筛选条件 */}
                {(filters.tagSlugs.length > 0 || filters.categoryId) && (
                  <div className="flex items-center gap-2">
                    {filters.tagSlugs.map((tagSlug) => (
                      <Badge key={tagSlug} variant="outline" className="text-xs">
                        {tagSlug}
                        <Button
                          variant="ghost"
                          size="sm"
                          className="ml-1 h-3 w-3 p-0"
                          onClick={() => toggleTag(tagSlug)}
                        >
                          ×
                        </Button>
                      </Badge>
                    ))}
                  </div>
                )}
              </div>

              <Separator />

              {/* 搜索结果列表 */}
              {searchResults?.data.items.length ? (
                <div className="space-y-4">
                  {searchResults.data.items.map((item) => (
                    <Card key={`${item.type}-${item.id}`} className="hover:shadow-md transition-shadow">
                      <CardContent className="p-4">
                        <div className="flex items-start justify-between">
                          <div className="flex-1">
                            <div className="flex items-center gap-2 mb-2">
                              <Badge variant="outline" className="text-xs">
                                {item.type === 'topic' ? '主题' : '帖子'}
                              </Badge>
                              <Badge variant="secondary" className="text-xs">
                                {item.categoryName}
                              </Badge>
                            </div>
                            
                            <h3 className="font-medium mb-2 line-clamp-2">
                              <a 
                                href={item.url}
                                className="text-blue-600 hover:text-blue-800 hover:underline"
                              >
                                {item.title}
                              </a>
                            </h3>
                            
                            <p className="text-sm text-muted-foreground mb-3 line-clamp-3">
                              {item.snippet}
                            </p>
                            
                            <div className="flex items-center gap-4 text-xs text-muted-foreground">
                              <span>@{item.authorUsername}</span>
                              <span>{formatRelativeTime(item.createdAt)}</span>
                              {item.relevance > 0 && (
                                <span>相关度: {(item.relevance * 100).toFixed(0)}%</span>
                              )}
                            </div>
                          </div>
                        </div>
                      </CardContent>
                    </Card>
                  ))}
                </div>
              ) : (
                <EmptyState
                  title="未找到相关内容"
                  description={`没有找到包含"${filters.query}"的内容，试试其他关键词？`}
                  icon="🔍"
                />
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
```

---

## ✅ M3 验收清单

### 分类管理系统
- [ ] **分类 CRUD 操作** (创建、编辑、删除、归档)
- [ ] **分类权限控制** (版主管理、权限检查)
- [ ] **分类统计信息** (主题数、帖子数、最新活动)
- [ ] **分类排序功能** (自定义排序、拖拽调整)

### 标签系统
- [ ] **动态标签创建** (自动创建、手动管理)
- [ ] **标签使用统计** (使用次数、热门标签)
- [ ] **标签搜索建议** (输入自动补全)
- [ ] **标签筛选功能** (多标签组合筛选)

### 搜索功能
- [ ] **全文搜索实现** (MySQL FULLTEXT 索引)
- [ ] **搜索结果排序** (相关度、时间、活跃度)
- [ ] **搜索建议功能** (实时建议、历史搜索)
- [ ] **高级筛选功能** (分类、标签、类型组合)

### 前端界面
- [ ] **搜索框组件** (自动建议、快捷键支持)
- [ ] **搜索结果页面** (结果展示、筛选器)
- [ ] **分类标签选择器** (可视化选择、批量操作)
- [ ] **响应式设计** (移动端适配)

### 性能优化
- [ ] **搜索缓存策略** (结果缓存、建议缓存)
- [ ] **数据库索引优化** (FULLTEXT 索引、复合索引)
- [ ] **分页性能** (搜索结果分页加载)
- [ ] **接口响应时间** (搜索 <500ms，建议 <200ms)

---

**预计完成时间**: 5 个工作日  
**关键阻塞点**: FULLTEXT 索引性能、中文分词效果  
**下一步**: M4 SignalR 实时功能开发