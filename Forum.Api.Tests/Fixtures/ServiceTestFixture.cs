using Forum.Api.Infrastructure.Auth;
using Forum.Api.Infrastructure.Database;
using Forum.Api.Infrastructure.Email;
using Forum.Api.Repositories;
using Forum.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace Forum.Api.Tests.Fixtures;

/// <summary>
/// 服务层测试夹具
/// 配置业务服务和仓储层的Mock依赖
/// </summary>
public class ServiceTestFixture : BaseUnitTestFixture
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        // JWT设置
        services.Configure<JwtSettings>(Configuration.GetSection("JwtSettings"));

        // 基础设施服务（真实实现）
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordService, PasswordService>();

        // 外部依赖Mock
        ConfigureMockServices(services);

        // 仓储层Mock
        ConfigureMockRepositories(services);

        // 业务服务（真实实现）
        ConfigureBusinessServices(services);
    }

    /// <summary>
    /// 配置Mock服务
    /// </summary>
    private static void ConfigureMockServices(IServiceCollection services)
    {
        // 邮件服务Mock
        var mockEmailService = new Mock<IEmailService>();
        mockEmailService
            .Setup(x => x.SendEmailVerificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        mockEmailService
            .Setup(x => x.SendPasswordResetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        services.AddSingleton(mockEmailService.Object);

        // 数据库连接工厂Mock（用于内存数据库）
        var mockDbFactory = new Mock<IDbConnectionFactory>();
        services.AddSingleton(mockDbFactory.Object);
    }

    /// <summary>
    /// 配置Mock仓储
    /// </summary>
    private static void ConfigureMockRepositories(IServiceCollection services)
    {
        // 用户仓储Mock
        services.AddSingleton<Mock<IUserRepository>>();
        services.AddSingleton<IUserRepository>(provider => provider.GetRequiredService<Mock<IUserRepository>>().Object);

        // 主题仓储Mock
        services.AddSingleton<Mock<ITopicRepository>>();
        services.AddSingleton<ITopicRepository>(provider => provider.GetRequiredService<Mock<ITopicRepository>>().Object);

        // 帖子仓储Mock
        services.AddSingleton<Mock<IPostRepository>>();
        services.AddSingleton<IPostRepository>(provider => provider.GetRequiredService<Mock<IPostRepository>>().Object);

        // 分类仓储Mock
        services.AddSingleton<Mock<ICategoryRepository>>();
        services.AddSingleton<ICategoryRepository>(provider => provider.GetRequiredService<Mock<ICategoryRepository>>().Object);

        // 标签仓储Mock
        services.AddSingleton<Mock<ITagRepository>>();
        services.AddSingleton<ITagRepository>(provider => provider.GetRequiredService<Mock<ITagRepository>>().Object);

        // 刷新令牌仓储Mock
        services.AddSingleton<Mock<IRefreshTokenRepository>>();
        services.AddSingleton<IRefreshTokenRepository>(provider => provider.GetRequiredService<Mock<IRefreshTokenRepository>>().Object);
    }

    /// <summary>
    /// 配置业务服务
    /// </summary>
    private static void ConfigureBusinessServices(IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ITopicService, TopicService>();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<ISignalRService, SignalRService>();
    }

    /// <summary>
    /// 获取Mock仓储
    /// </summary>
    /// <typeparam name="T">仓储接口类型</typeparam>
    /// <returns>Mock对象</returns>
    public Mock<T> GetMockRepository<T>() where T : class
    {
        return ServiceProvider.GetRequiredService<Mock<T>>();
    }

    /// <summary>
    /// 重置所有Mock对象
    /// </summary>
    public void ResetMocks()
    {
        GetMockRepository<IUserRepository>().Reset();
        GetMockRepository<ITopicRepository>().Reset();
        GetMockRepository<IPostRepository>().Reset();
        GetMockRepository<ICategoryRepository>().Reset();
        GetMockRepository<ITagRepository>().Reset();
        GetMockRepository<IRefreshTokenRepository>().Reset();
    }
}