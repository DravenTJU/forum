using Bogus;
using Forum.Api.Models.Entities;

namespace Forum.Api.Tests.Builders;

/// <summary>
/// 用户测试数据构建器
/// 使用Builder模式和Bogus库生成不同类型的用户测试数据
/// </summary>
public class UserTestDataBuilder
{
    private readonly Faker<User> _faker;
    private User _user;

    public UserTestDataBuilder()
    {
        _faker = new Faker<User>()
            .RuleFor(u => u.Id, f => f.Random.Guid().ToString())
            .RuleFor(u => u.Username, f => f.Internet.UserName())
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.PasswordHash, f => f.Random.Hash())
            .RuleFor(u => u.EmailVerified, f => f.Random.Bool(0.8f))
            .RuleFor(u => u.AvatarUrl, f => f.Internet.Avatar())
            .RuleFor(u => u.Bio, f => f.Lorem.Paragraph())
            .RuleFor(u => u.Status, f => f.PickRandom("active", "suspended"))
            .RuleFor(u => u.LastSeenAt, f => f.Date.Recent(30))
            .RuleFor(u => u.CreatedAt, f => f.Date.Past(2))
            .RuleFor(u => u.UpdatedAt, (f, u) => u.CreatedAt.AddDays(f.Random.Int(0, 30)));

        _user = _faker.Generate();
    }

    /// <summary>
    /// 创建管理员用户
    /// </summary>
    public static UserTestDataBuilder AdminUser()
    {
        return new UserTestDataBuilder()
            .WithUsername("admin")
            .WithEmail("admin@example.com")
            .WithStatus("active")
            .WithEmailVerified(true);
    }

    /// <summary>
    /// 创建普通用户
    /// </summary>
    public static UserTestDataBuilder RegularUser()
    {
        return new UserTestDataBuilder()
            .WithStatus("active")
            .WithEmailVerified(true);
    }

    /// <summary>
    /// 创建未验证用户
    /// </summary>
    public static UserTestDataBuilder UnverifiedUser()
    {
        return new UserTestDataBuilder()
            .WithEmailVerified(false)
            .WithStatus("active");
    }

    /// <summary>
    /// 创建被封禁用户
    /// </summary>
    public static UserTestDataBuilder SuspendedUser()
    {
        return new UserTestDataBuilder()
            .WithStatus("suspended")
            .WithEmailVerified(true);
    }

    /// <summary>
    /// 设置用户ID
    /// </summary>
    public UserTestDataBuilder WithId(string id)
    {
        _user.Id = id;
        return this;
    }

    /// <summary>
    /// 设置用户名
    /// </summary>
    public UserTestDataBuilder WithUsername(string username)
    {
        _user.Username = username;
        return this;
    }

    /// <summary>
    /// 设置邮箱
    /// </summary>
    public UserTestDataBuilder WithEmail(string email)
    {
        _user.Email = email;
        return this;
    }

    /// <summary>
    /// 设置密码哈希
    /// </summary>
    public UserTestDataBuilder WithPasswordHash(string passwordHash)
    {
        _user.PasswordHash = passwordHash;
        return this;
    }

    /// <summary>
    /// 设置邮箱验证状态
    /// </summary>
    public UserTestDataBuilder WithEmailVerified(bool verified)
    {
        _user.EmailVerified = verified;
        return this;
    }

    /// <summary>
    /// 设置用户状态
    /// </summary>
    public UserTestDataBuilder WithStatus(string status)
    {
        _user.Status = status;
        return this;
    }

    /// <summary>
    /// 设置头像URL
    /// </summary>
    public UserTestDataBuilder WithAvatarUrl(string? avatarUrl)
    {
        _user.AvatarUrl = avatarUrl;
        return this;
    }

    /// <summary>
    /// 设置个人简介
    /// </summary>
    public UserTestDataBuilder WithBio(string? bio)
    {
        _user.Bio = bio;
        return this;
    }

    /// <summary>
    /// 设置创建时间
    /// </summary>
    public UserTestDataBuilder WithCreatedAt(DateTime createdAt)
    {
        _user.CreatedAt = createdAt;
        return this;
    }

    /// <summary>
    /// 设置最后登录时间
    /// </summary>
    public UserTestDataBuilder WithLastSeenAt(DateTime? lastSeenAt)
    {
        _user.LastSeenAt = lastSeenAt;
        return this;
    }

    /// <summary>
    /// 构建用户对象
    /// </summary>
    public User Build()
    {
        // 确保时间戳一致性
        if (_user.UpdatedAt <= _user.CreatedAt)
        {
            _user.UpdatedAt = _user.CreatedAt;
        }

        return _user;
    }

    /// <summary>
    /// 批量生成用户
    /// </summary>
    public static List<User> GenerateUsers(int count)
    {
        var users = new List<User>();
        for (int i = 0; i < count; i++)
        {
            users.Add(new UserTestDataBuilder().Build());
        }
        return users;
    }

    /// <summary>
    /// 生成特定类型用户列表
    /// </summary>
    public static List<User> GenerateUsersByType(int adminCount, int regularCount, int suspendedCount = 0)
    {
        var users = new List<User>();

        // 管理员用户
        for (int i = 0; i < adminCount; i++)
        {
            users.Add(AdminUser()
                .WithUsername($"admin_{i + 1}")
                .WithEmail($"admin{i + 1}@example.com")
                .Build());
        }

        // 普通用户
        for (int i = 0; i < regularCount; i++)
        {
            users.Add(RegularUser().Build());
        }

        // 被封禁用户
        for (int i = 0; i < suspendedCount; i++)
        {
            users.Add(SuspendedUser().Build());
        }

        return users;
    }

    /// <summary>
    /// 隐式转换为User对象
    /// </summary>
    public static implicit operator User(UserTestDataBuilder builder)
    {
        return builder.Build();
    }
}