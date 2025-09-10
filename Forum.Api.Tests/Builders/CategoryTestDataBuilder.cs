using Bogus;
using Forum.Api.Models.Entities;

namespace Forum.Api.Tests.Builders;

/// <summary>
/// 分类测试数据构建器
/// 生成分类层次结构和不同状态的分类数据
/// </summary>
public class CategoryTestDataBuilder
{
    private readonly Faker<Category> _faker;
    private Category _category;

    public CategoryTestDataBuilder()
    {
        _faker = new Faker<Category>()
            .RuleFor(c => c.Id, f => f.Random.Long(1, long.MaxValue))
            .RuleFor(c => c.Name, f => f.Commerce.Categories(1)[0])
            .RuleFor(c => c.Slug, (f, c) => GenerateSlug(c.Name))
            .RuleFor(c => c.Description, f => f.Lorem.Sentence(5, 10))
            .RuleFor(c => c.Color, f => f.Internet.Color())
            .RuleFor(c => c.Order, f => f.Random.Int(1, 100))
            .RuleFor(c => c.IsArchived, f => f.Random.Bool(0.1f))
            .RuleFor(c => c.CreatedAt, f => f.Date.Past(2))
            .RuleFor(c => c.UpdatedAt, (f, c) => c.CreatedAt.AddDays(f.Random.Int(0, 60)));

        _category = _faker.Generate();
    }

    /// <summary>
    /// 创建开发讨论分类
    /// </summary>
    public static CategoryTestDataBuilder DevelopmentCategory()
    {
        return new CategoryTestDataBuilder()
            .WithName("开发讨论")
            .WithSlug("development")
            .WithDescription("技术开发相关讨论")
            .WithColor("#007acc")
            .WithOrder(1)
            .WithIsArchived(false);
    }

    /// <summary>
    /// 创建一般讨论分类
    /// </summary>
    public static CategoryTestDataBuilder GeneralCategory()
    {
        return new CategoryTestDataBuilder()
            .WithName("一般讨论")
            .WithSlug("general")
            .WithDescription("一般话题讨论")
            .WithColor("#28a745")
            .WithOrder(2)
            .WithIsArchived(false);
    }

    /// <summary>
    /// 创建公告分类
    /// </summary>
    public static CategoryTestDataBuilder AnnouncementCategory()
    {
        return new CategoryTestDataBuilder()
            .WithName("公告")
            .WithSlug("announcements")
            .WithDescription("重要公告和通知")
            .WithColor("#dc3545")
            .WithOrder(0)
            .WithIsArchived(false);
    }

    /// <summary>
    /// 创建已归档分类
    /// </summary>
    public static CategoryTestDataBuilder ArchivedCategory()
    {
        return new CategoryTestDataBuilder()
            .WithIsArchived(true)
            .WithOrder(999);
    }

    /// <summary>
    /// 设置分类ID
    /// </summary>
    public CategoryTestDataBuilder WithId(long id)
    {
        _category.Id = id;
        return this;
    }

    /// <summary>
    /// 设置分类名称
    /// </summary>
    public CategoryTestDataBuilder WithName(string name)
    {
        _category.Name = name;
        _category.Slug = GenerateSlug(name);
        return this;
    }

    /// <summary>
    /// 设置Slug
    /// </summary>
    public CategoryTestDataBuilder WithSlug(string slug)
    {
        _category.Slug = slug;
        return this;
    }

    /// <summary>
    /// 设置描述
    /// </summary>
    public CategoryTestDataBuilder WithDescription(string? description)
    {
        _category.Description = description;
        return this;
    }

    /// <summary>
    /// 设置颜色
    /// </summary>
    public CategoryTestDataBuilder WithColor(string color)
    {
        _category.Color = color;
        return this;
    }

    /// <summary>
    /// 设置排序
    /// </summary>
    public CategoryTestDataBuilder WithOrder(int order)
    {
        _category.Order = order;
        return this;
    }

    /// <summary>
    /// 设置归档状态
    /// </summary>
    public CategoryTestDataBuilder WithIsArchived(bool isArchived)
    {
        _category.IsArchived = isArchived;
        return this;
    }

    /// <summary>
    /// 设置创建时间
    /// </summary>
    public CategoryTestDataBuilder WithCreatedAt(DateTime createdAt)
    {
        _category.CreatedAt = createdAt;
        return this;
    }

    /// <summary>
    /// 设置更新时间
    /// </summary>
    public CategoryTestDataBuilder WithUpdatedAt(DateTime updatedAt)
    {
        _category.UpdatedAt = updatedAt;
        return this;
    }

    /// <summary>
    /// 构建分类对象
    /// </summary>
    public Category Build()
    {
        // 确保时间戳一致性
        if (_category.UpdatedAt < _category.CreatedAt)
        {
            _category.UpdatedAt = _category.CreatedAt;
        }

        return _category;
    }

    /// <summary>
    /// 批量生成分类
    /// </summary>
    public static List<Category> GenerateCategories(int count)
    {
        var categories = new List<Category>();
        for (int i = 0; i < count; i++)
        {
            categories.Add(new CategoryTestDataBuilder()
                .WithOrder(i + 1)
                .WithIsArchived(false)
                .Build());
        }
        return categories;
    }

    /// <summary>
    /// 生成默认分类集合
    /// </summary>
    public static List<Category> GenerateDefaultCategories()
    {
        return new List<Category>
        {
            AnnouncementCategory().WithId(1).Build(),
            DevelopmentCategory().WithId(2).Build(),
            GeneralCategory().WithId(3).Build(),
            new CategoryTestDataBuilder()
                .WithId(4)
                .WithName("求助问答")
                .WithSlug("help")
                .WithDescription("技术求助和问答")
                .WithColor("#ffc107")
                .WithOrder(3)
                .Build(),
            new CategoryTestDataBuilder()
                .WithId(5)
                .WithName("资源分享")
                .WithSlug("resources")
                .WithDescription("学习资源和工具分享")
                .WithColor("#17a2b8")
                .WithOrder(4)
                .Build()
        };
    }

    /// <summary>
    /// 生成分层分类结构（父子关系）
    /// </summary>
    public static List<Category> GenerateHierarchicalCategories()
    {
        var categories = new List<Category>();

        // 顶级分类
        categories.Add(DevelopmentCategory().WithId(1).WithOrder(1).Build());
        categories.Add(GeneralCategory().WithId(2).WithOrder(2).Build());

        // 开发讨论子分类
        categories.Add(new CategoryTestDataBuilder()
            .WithId(10)
            .WithName("前端开发")
            .WithSlug("frontend")
            .WithColor("#61dafb")
            .WithOrder(11)
            .Build());

        categories.Add(new CategoryTestDataBuilder()
            .WithId(11)
            .WithName("后端开发")
            .WithSlug("backend")
            .WithColor("#68217a")
            .WithOrder(12)
            .Build());

        categories.Add(new CategoryTestDataBuilder()
            .WithId(12)
            .WithName("数据库")
            .WithSlug("database")
            .WithColor("#336791")
            .WithOrder(13)
            .Build());

        return categories;
    }

    /// <summary>
    /// 生成URL友好的slug
    /// </summary>
    private static string GenerateSlug(string name)
    {
        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace(".", "")
            .Replace(",", "")
            .Replace("?", "")
            .Replace("!", "")
            .Replace("：", "")
            .Replace("；", "")
            .Trim('-');
    }

    /// <summary>
    /// 隐式转换为Category对象
    /// </summary>
    public static implicit operator Category(CategoryTestDataBuilder builder)
    {
        return builder.Build();
    }
}