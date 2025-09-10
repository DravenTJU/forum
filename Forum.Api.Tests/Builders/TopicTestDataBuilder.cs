using Bogus;
using Forum.Api.Models.Entities;

namespace Forum.Api.Tests.Builders;

/// <summary>
/// 主题测试数据构建器
/// 生成各种状态的主题测试数据
/// </summary>
public class TopicTestDataBuilder
{
    private readonly Faker<Topic> _faker;
    private Topic _topic;

    public TopicTestDataBuilder()
    {
        _faker = new Faker<Topic>()
            .RuleFor(t => t.Id, f => f.Random.Long(1, long.MaxValue))
            .RuleFor(t => t.Title, f => f.Lorem.Sentence(3, 8))
            .RuleFor(t => t.Slug, (f, t) => GenerateSlug(t.Title))
            .RuleFor(t => t.AuthorId, f => f.Random.Long(1, 1000))
            .RuleFor(t => t.CategoryId, f => f.Random.Long(1, 20))
            .RuleFor(t => t.IsPinned, f => f.Random.Bool(0.1f))
            .RuleFor(t => t.IsLocked, f => f.Random.Bool(0.05f))
            .RuleFor(t => t.IsDeleted, f => false)
            .RuleFor(t => t.ReplyCount, f => f.Random.Int(0, 100))
            .RuleFor(t => t.ViewCount, f => f.Random.Int(1, 1000))
            .RuleFor(t => t.CreatedAt, f => f.Date.Past(1))
            .RuleFor(t => t.UpdatedAt, (f, t) => t.CreatedAt.AddMinutes(f.Random.Int(0, 60 * 24 * 30)))
            .RuleFor(t => t.LastPostedAt, (f, t) => t.ReplyCount > 0 ? t.UpdatedAt : null)
            .RuleFor(t => t.LastPosterId, (f, t) => t.ReplyCount > 0 ? f.Random.Long(1, 1000) : null);

        _topic = _faker.Generate();
    }

    /// <summary>
    /// 创建新主题（无回复）
    /// </summary>
    public static TopicTestDataBuilder NewTopic()
    {
        return new TopicTestDataBuilder()
            .WithReplyCount(0)
            .WithViewCount(1)
            .WithLastPostedAt(null)
            .WithLastPosterId(null)
            .WithIsPinned(false)
            .WithIsLocked(false);
    }

    /// <summary>
    /// 创建热门主题
    /// </summary>
    public static TopicTestDataBuilder PopularTopic()
    {
        var faker = new Faker();
        return new TopicTestDataBuilder()
            .WithReplyCount(faker.Random.Int(20, 100))
            .WithViewCount(faker.Random.Int(200, 2000))
            .WithLastPostedAt(DateTime.UtcNow.AddMinutes(-faker.Random.Int(5, 60)));
    }

    /// <summary>
    /// 创建置顶主题
    /// </summary>
    public static TopicTestDataBuilder PinnedTopic()
    {
        return new TopicTestDataBuilder()
            .WithIsPinned(true)
            .WithIsLocked(false);
    }

    /// <summary>
    /// 创建锁定主题
    /// </summary>
    public static TopicTestDataBuilder LockedTopic()
    {
        return new TopicTestDataBuilder()
            .WithIsLocked(true)
            .WithIsPinned(false);
    }

    /// <summary>
    /// 创建已删除主题
    /// </summary>
    public static TopicTestDataBuilder DeletedTopic()
    {
        return new TopicTestDataBuilder()
            .WithIsDeleted(true);
    }

    /// <summary>
    /// 设置主题ID
    /// </summary>
    public TopicTestDataBuilder WithId(long id)
    {
        _topic.Id = id;
        return this;
    }

    /// <summary>
    /// 设置标题
    /// </summary>
    public TopicTestDataBuilder WithTitle(string title)
    {
        _topic.Title = title;
        _topic.Slug = GenerateSlug(title);
        return this;
    }

    /// <summary>
    /// 设置Slug
    /// </summary>
    public TopicTestDataBuilder WithSlug(string slug)
    {
        _topic.Slug = slug;
        return this;
    }

    /// <summary>
    /// 设置作者ID
    /// </summary>
    public TopicTestDataBuilder WithAuthorId(long authorId)
    {
        _topic.AuthorId = authorId;
        return this;
    }

    /// <summary>
    /// 设置分类ID
    /// </summary>
    public TopicTestDataBuilder WithCategoryId(long categoryId)
    {
        _topic.CategoryId = categoryId;
        return this;
    }

    /// <summary>
    /// 设置置顶状态
    /// </summary>
    public TopicTestDataBuilder WithIsPinned(bool isPinned)
    {
        _topic.IsPinned = isPinned;
        return this;
    }

    /// <summary>
    /// 设置锁定状态
    /// </summary>
    public TopicTestDataBuilder WithIsLocked(bool isLocked)
    {
        _topic.IsLocked = isLocked;
        return this;
    }

    /// <summary>
    /// 设置删除状态
    /// </summary>
    public TopicTestDataBuilder WithIsDeleted(bool isDeleted)
    {
        _topic.IsDeleted = isDeleted;
        return this;
    }

    /// <summary>
    /// 设置回复数量
    /// </summary>
    public TopicTestDataBuilder WithReplyCount(int replyCount)
    {
        _topic.ReplyCount = replyCount;
        return this;
    }

    /// <summary>
    /// 设置浏览数量
    /// </summary>
    public TopicTestDataBuilder WithViewCount(int viewCount)
    {
        _topic.ViewCount = viewCount;
        return this;
    }

    /// <summary>
    /// 设置最后回复时间
    /// </summary>
    public TopicTestDataBuilder WithLastPostedAt(DateTime? lastPostedAt)
    {
        _topic.LastPostedAt = lastPostedAt;
        return this;
    }

    /// <summary>
    /// 设置最后回复人ID
    /// </summary>
    public TopicTestDataBuilder WithLastPosterId(long? lastPosterId)
    {
        _topic.LastPosterId = lastPosterId;
        return this;
    }

    /// <summary>
    /// 设置创建时间
    /// </summary>
    public TopicTestDataBuilder WithCreatedAt(DateTime createdAt)
    {
        _topic.CreatedAt = createdAt;
        return this;
    }

    /// <summary>
    /// 设置更新时间
    /// </summary>
    public TopicTestDataBuilder WithUpdatedAt(DateTime updatedAt)
    {
        _topic.UpdatedAt = updatedAt;
        return this;
    }

    /// <summary>
    /// 构建主题对象
    /// </summary>
    public Topic Build()
    {
        // 确保时间戳一致性
        if (_topic.UpdatedAt < _topic.CreatedAt)
        {
            _topic.UpdatedAt = _topic.CreatedAt;
        }

        // 确保最后回复时间不早于创建时间
        if (_topic.LastPostedAt.HasValue && _topic.LastPostedAt < _topic.CreatedAt)
        {
            _topic.LastPostedAt = _topic.UpdatedAt;
        }

        return _topic;
    }

    /// <summary>
    /// 批量生成主题
    /// </summary>
    public static List<Topic> GenerateTopics(int count, long categoryId = 1)
    {
        var topics = new List<Topic>();
        for (int i = 0; i < count; i++)
        {
            topics.Add(new TopicTestDataBuilder()
                .WithCategoryId(categoryId)
                .Build());
        }
        return topics;
    }

    /// <summary>
    /// 生成特定作者的主题列表
    /// </summary>
    public static List<Topic> GenerateTopicsForAuthor(long authorId, int count)
    {
        var topics = new List<Topic>();
        for (int i = 0; i < count; i++)
        {
            topics.Add(new TopicTestDataBuilder()
                .WithAuthorId(authorId)
                .Build());
        }
        return topics;
    }

    /// <summary>
    /// 生成不同状态的主题混合列表
    /// </summary>
    public static List<Topic> GenerateMixedTopics(int normalCount, int pinnedCount = 2, int lockedCount = 1)
    {
        var topics = new List<Topic>();

        // 普通主题
        for (int i = 0; i < normalCount; i++)
        {
            topics.Add(new TopicTestDataBuilder().Build());
        }

        // 置顶主题
        for (int i = 0; i < pinnedCount; i++)
        {
            topics.Add(PinnedTopic().Build());
        }

        // 锁定主题
        for (int i = 0; i < lockedCount; i++)
        {
            topics.Add(LockedTopic().Build());
        }

        return topics;
    }

    /// <summary>
    /// 生成URL友好的slug
    /// </summary>
    private static string GenerateSlug(string title)
    {
        return title.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace(".", "")
            .Replace(",", "")
            .Replace("?", "")
            .Replace("!", "")
            .Replace(":", "")
            .Replace(";", "")
            .Trim('-');
    }

    /// <summary>
    /// 隐式转换为Topic对象
    /// </summary>
    public static implicit operator Topic(TopicTestDataBuilder builder)
    {
        return builder.Build();
    }
}