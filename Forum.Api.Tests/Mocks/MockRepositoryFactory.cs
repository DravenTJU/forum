using Forum.Api.Models.Entities;
using Forum.Api.Repositories;
using Moq;

namespace Forum.Api.Tests.Mocks;

/// <summary>
/// Mock仓储工厂
/// 统一创建和配置各种仓储的Mock对象
/// </summary>
public class MockRepositoryFactory
{
    /// <summary>
    /// 创建用户仓储Mock
    /// </summary>
    public static Mock<IUserRepository> CreateUserRepositoryMock()
    {
        var mock = new Mock<IUserRepository>();

        // 默认设置：根据ID查找用户
        mock.Setup(x => x.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => null);

        // 默认设置：根据邮箱查找用户
        mock.Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((string email) => null);

        // 默认设置：根据用户名查找用户
        mock.Setup(x => x.GetByUsernameAsync(It.IsAny<string>()))
            .ReturnsAsync((string username) => null);

        // 默认设置：检查邮箱是否存在
        mock.Setup(x => x.ExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        // 默认设置：检查用户名是否存在
        mock.Setup(x => x.UsernameExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        // 默认设置：创建用户
        mock.Setup(x => x.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User user) => user.Id);

        // 默认设置：更新用户
        mock.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(true);

        // 默认设置：验证邮箱
        mock.Setup(x => x.VerifyEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        return mock;
    }

    /// <summary>
    /// 创建主题仓储Mock
    /// </summary>
    public static Mock<ITopicRepository> CreateTopicRepositoryMock()
    {
        var mock = new Mock<ITopicRepository>();

        // 默认设置：根据ID查找主题
        mock.Setup(x => x.GetByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((long id) => null);

        // 默认设置：获取主题列表
        mock.Setup(x => x.GetTopicsAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<long?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync((new List<Topic>(), false));

        // 默认设置：创建主题
        mock.Setup(x => x.CreateAsync(It.IsAny<Topic>()))
            .ReturnsAsync((Topic topic) => topic.Id);

        // 默认设置：更新主题
        mock.Setup(x => x.UpdateAsync(It.IsAny<Topic>()))
            .ReturnsAsync(true);

        // 默认设置：删除主题
        mock.Setup(x => x.DeleteAsync(It.IsAny<long>()))
            .ReturnsAsync(true);

        // 默认设置：更新统计
        mock.Setup(x => x.UpdateStatsAsync(It.IsAny<long>()))
            .ReturnsAsync(true);

        // 默认设置：增加浏览数
        mock.Setup(x => x.IncrementViewCountAsync(It.IsAny<long>()))
            .ReturnsAsync(true);

        return mock;
    }

    /// <summary>
    /// 创建帖子仓储Mock
    /// </summary>
    public static Mock<IPostRepository> CreatePostRepositoryMock()
    {
        var mock = new Mock<IPostRepository>();

        // 默认设置：根据ID查找帖子
        mock.Setup(x => x.GetByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((long id) => null);

        // 默认设置：获取主题下的帖子
        mock.Setup(x => x.GetPostsByTopicAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<string?>()))
            .ReturnsAsync((new List<Post>(), false));

        // 默认设置：创建帖子
        mock.Setup(x => x.CreateAsync(It.IsAny<Post>()))
            .ReturnsAsync((Post post) => post.Id);

        // 默认设置：更新帖子
        mock.Setup(x => x.UpdateAsync(It.IsAny<Post>()))
            .ReturnsAsync(true);

        // 默认设置：删除帖子
        mock.Setup(x => x.DeleteAsync(It.IsAny<long>()))
            .ReturnsAsync(true);

        return mock;
    }

    /// <summary>
    /// 创建分类仓储Mock
    /// </summary>
    public static Mock<ICategoryRepository> CreateCategoryRepositoryMock()
    {
        var mock = new Mock<ICategoryRepository>();

        // 默认设置：获取所有分类
        mock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Category>());

        // 默认设置：根据ID查找分类
        mock.Setup(x => x.GetByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((long id) => null);

        // 默认设置：根据Slug查找分类
        mock.Setup(x => x.GetBySlugAsync(It.IsAny<string>()))
            .ReturnsAsync((string slug) => null);

        // 默认设置：创建分类
        mock.Setup(x => x.CreateAsync(It.IsAny<Category>()))
            .ReturnsAsync((Category category) => category.Id);

        // 默认设置：更新分类
        mock.Setup(x => x.UpdateAsync(It.IsAny<Category>()))
            .ReturnsAsync(true);

        return mock;
    }

    /// <summary>
    /// 创建标签仓储Mock
    /// </summary>
    public static Mock<ITagRepository> CreateTagRepositoryMock()
    {
        var mock = new Mock<ITagRepository>();

        // 默认设置：获取所有标签
        mock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Tag>());

        // 默认设置：根据Slug查找标签
        mock.Setup(x => x.GetBySlugAsync(It.IsAny<string>()))
            .ReturnsAsync((string slug) => null);

        // 默认设置：批量获取标签
        mock.Setup(x => x.GetBySlugListAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(new List<Tag>());

        // 默认设置：创建标签
        mock.Setup(x => x.CreateAsync(It.IsAny<Tag>()))
            .ReturnsAsync((Tag tag) => tag.Id);

        return mock;
    }

    /// <summary>
    /// 创建刷新令牌仓储Mock
    /// </summary>
    public static Mock<IRefreshTokenRepository> CreateRefreshTokenRepositoryMock()
    {
        var mock = new Mock<IRefreshTokenRepository>();

        // 默认设置：根据令牌查找
        mock.Setup(x => x.GetByTokenAsync(It.IsAny<string>()))
            .ReturnsAsync((string token) => null);

        // 默认设置：创建令牌
        mock.Setup(x => x.CreateAsync(It.IsAny<RefreshToken>()))
            .ReturnsAsync((RefreshToken token) => token.Id);

        // 默认设置：撤销令牌
        mock.Setup(x => x.RevokeAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // 默认设置：清理过期令牌
        mock.Setup(x => x.CleanupExpiredAsync())
            .ReturnsAsync(0);

        return mock;
    }

    /// <summary>
    /// 设置用户仓储常见场景
    /// </summary>
    public static void SetupUserRepositoryScenarios(Mock<IUserRepository> mock, List<User> users)
    {
        // 设置根据ID查找
        mock.Setup(x => x.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => users.FirstOrDefault(u => u.Id == id));

        // 设置根据邮箱查找
        mock.Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((string email) => users.FirstOrDefault(u => u.Email == email));

        // 设置根据用户名查找
        mock.Setup(x => x.GetByUsernameAsync(It.IsAny<string>()))
            .ReturnsAsync((string username) => users.FirstOrDefault(u => u.Username == username));

        // 设置邮箱存在性检查
        mock.Setup(x => x.ExistsAsync(It.IsAny<string>()))
            .ReturnsAsync((string email) => users.Any(u => u.Email == email));

        // 设置用户名存在性检查
        mock.Setup(x => x.UsernameExistsAsync(It.IsAny<string>()))
            .ReturnsAsync((string username) => users.Any(u => u.Username == username));
    }

    /// <summary>
    /// 设置主题仓储常见场景
    /// </summary>
    public static void SetupTopicRepositoryScenarios(Mock<ITopicRepository> mock, List<Topic> topics)
    {
        // 设置根据ID查找
        mock.Setup(x => x.GetByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((long id) => topics.FirstOrDefault(t => t.Id == id && !t.IsDeleted));

        // 设置获取主题列表
        mock.Setup(x => x.GetTopicsAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<long?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync((int limit, string? cursor, long? categoryId, string? tagSlug, string? sortBy) =>
            {
                var filteredTopics = topics.Where(t => !t.IsDeleted);
                
                if (categoryId.HasValue)
                {
                    filteredTopics = filteredTopics.Where(t => t.CategoryId == categoryId.Value);
                }

                var result = filteredTopics.Take(limit).ToList();
                var hasNext = filteredTopics.Count() > limit;
                
                return (result, hasNext);
            });
    }
}