using Forum.Api.Infrastructure.Database;
using Forum.Api.Infrastructure.Auth;
using Forum.Api.Infrastructure.Email;
using Forum.Api.Services;
using Forum.Api.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace Forum.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDbConnectionFactory, MySqlConnectionFactory>();
        
        // 注册仓储
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<ITopicRepository, TopicRepository>();
        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        
        return services;
    }

    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");

        services.Configure<JwtSettings>(jwtSettings);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ClockSkew = TimeSpan.Zero
                };

                // 支持从Cookie获取Token（符合API规范）
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // 从Cookie获取JWT token
                        var token = context.Request.Cookies["auth-token"];
                        if (!string.IsNullOrEmpty(token))
                        {
                            context.Token = token;
                        }
                        else
                        {
                            // SignalR 支持：从查询参数获取token
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                            {
                                context.Token = accessToken;
                            }
                        }
                        
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                        logger.LogInformation("JWT token validated successfully for user: {UserId}", 
                            context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                        logger.LogWarning("JWT authentication failed: {Exception}", context.Exception.Message);
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordService, PasswordService>();

        return services;
    }

    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<ITopicService, TopicService>();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<ISignalRService, SignalRService>();

        return services;
    }

    public static IServiceCollection AddEmailServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }
}