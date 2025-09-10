using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Forum.Api.Tests.Fixtures;

/// <summary>
/// 单元测试基础夹具
/// 提供依赖注入容器配置和通用测试基础设施
/// </summary>
public abstract class BaseUnitTestFixture : IDisposable
{
    protected IServiceProvider ServiceProvider { get; }
    protected IConfiguration Configuration { get; }
    protected ITestOutputHelper? Output { get; set; }

    protected BaseUnitTestFixture()
    {
        // 构建配置
        Configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        // 构建服务容器
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// 配置测试专用服务
    /// </summary>
    /// <param name="services">服务集合</param>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // 配置
        services.AddSingleton(Configuration);

        // 日志
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            if (Output != null)
            {
                builder.AddXUnit(Output);
            }
            builder.SetMinimumLevel(LogLevel.Warning);
        });

        // 基础服务配置将在子类中实现
    }

    /// <summary>
    /// 获取服务实例
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>服务实例</returns>
    protected T GetService<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// 获取可选服务实例
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>服务实例或null</returns>
    protected T? GetOptionalService<T>() where T : class
    {
        return ServiceProvider.GetService<T>();
    }

    public virtual void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// xUnit测试日志提供器扩展
/// </summary>
public static class LoggingExtensions
{
    public static ILoggingBuilder AddXUnit(this ILoggingBuilder builder, ITestOutputHelper output)
    {
        builder.Services.AddSingleton<ILoggerProvider>(new XUnitLoggerProvider(output));
        return builder;
    }
}

/// <summary>
/// xUnit测试日志提供器
/// </summary>
public class XUnitLoggerProvider : ILoggerProvider
{
    private readonly ITestOutputHelper _output;

    public XUnitLoggerProvider(ITestOutputHelper output)
    {
        _output = output;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new XUnitLogger(_output, categoryName);
    }

    public void Dispose() { }
}

/// <summary>
/// xUnit测试日志器
/// </summary>
public class XUnitLogger : ILogger
{
    private readonly ITestOutputHelper _output;
    private readonly string _categoryName;

    public XUnitLogger(ITestOutputHelper output, string categoryName)
    {
        _output = output;
        _categoryName = categoryName;
    }

    public IDisposable BeginScope<TState>(TState state) => new NoOpDisposable();

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        try
        {
            var message = formatter(state, exception);
            _output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{logLevel}] [{_categoryName}] {message}");
            
            if (exception != null)
            {
                _output.WriteLine($"Exception: {exception}");
            }
        }
        catch
        {
            // 忽略日志输出异常
        }
    }

    private class NoOpDisposable : IDisposable
    {
        public void Dispose() { }
    }
}