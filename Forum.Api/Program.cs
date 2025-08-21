using Forum.Api.Extensions;
using Forum.Api.Hubs;
using Forum.Api.Middleware;
using Forum.Api.Infrastructure.Database;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 加载 .env 文件
var envFiles = new[] { ".env", ".env.docker" };
foreach (var envFile in envFiles)
{
    if (File.Exists(envFile))
    {
        var envLines = File.ReadAllLines(envFile);
        foreach (var line in envLines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                continue;

            var parts = line.Split('=', 2);
            if (parts.Length == 2)
            {
                Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
            }
        }
        break; // 只加载第一个找到的文件
    }
}

// 构建连接字符串
var dbServer = Environment.GetEnvironmentVariable("DB_SERVER") ?? "127.0.0.1";
var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "3306";
var dbDatabase = Environment.GetEnvironmentVariable("DB_DATABASE") ?? "forum";
var dbUsername = Environment.GetEnvironmentVariable("DB_USERNAME") ?? "root";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "1105";
var dbCharset = Environment.GetEnvironmentVariable("DB_CHARSET") ?? "utf8mb4";
var dbSslMode = Environment.GetEnvironmentVariable(builder.Environment.IsDevelopment() ? "DEV_DB_SSL_MODE" : "DB_SSL_MODE") ?? 
                (builder.Environment.IsDevelopment() ? "None" : "Preferred");
var dbAllowPublicKey = Environment.GetEnvironmentVariable("DB_ALLOW_PUBLIC_KEY_RETRIEVAL") ?? "true";

var connectionString = $"Server={dbServer};Port={dbPort};Database={dbDatabase};Uid={dbUsername};Pwd={dbPassword};CharSet={dbCharset};SslMode={dbSslMode};AllowPublicKeyRetrieval={dbAllowPublicKey};";

// 设置配置值
builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;
builder.Configuration["JwtSettings:Secret"] = Environment.GetEnvironmentVariable(builder.Environment.IsDevelopment() ? "DEV_JWT_SECRET" : "JWT_SECRET") ?? "default-secret-key-32-chars-minimum";
builder.Configuration["JwtSettings:ExpirationInMinutes"] = Environment.GetEnvironmentVariable("JWT_EXPIRATION_MINUTES") ?? "60";
builder.Configuration["JwtSettings:RefreshExpirationInDays"] = Environment.GetEnvironmentVariable("JWT_REFRESH_EXPIRATION_DAYS") ?? "7";
builder.Configuration["JwtSettings:Issuer"] = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "ForumApi";
builder.Configuration["JwtSettings:Audience"] = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "ForumClient";

// 邮件设置
builder.Configuration["EmailSettings:SmtpHost"] = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "smtp.gmail.com";
builder.Configuration["EmailSettings:SmtpPort"] = Environment.GetEnvironmentVariable("SMTP_PORT") ?? "587";
builder.Configuration["EmailSettings:SmtpUsername"] = Environment.GetEnvironmentVariable("SMTP_USERNAME") ?? "";
builder.Configuration["EmailSettings:SmtpPassword"] = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? "";
builder.Configuration["EmailSettings:FromEmail"] = Environment.GetEnvironmentVariable("FROM_EMAIL") ?? "noreply@forum.com";
builder.Configuration["EmailSettings:FromName"] = Environment.GetEnvironmentVariable("FROM_NAME") ?? "Forum System";

// CORS 设置
var corsOrigins = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS");
if (!string.IsNullOrEmpty(corsOrigins))
{
    var origins = corsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(o => o.Trim())
                            .ToArray();
    builder.Configuration["CorsSettings:AllowedOrigins:0"] = origins.Length > 0 ? origins[0] : "http://localhost:5173";
    for (int i = 1; i < origins.Length; i++)
    {
        builder.Configuration[$"CorsSettings:AllowedOrigins:{i}"] = origins[i];
    }
}

// Serilog 配置
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// 配置 Dapper 类型处理器
DapperConfiguration.Configure();

// 添加服务
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 自定义服务注册
builder.Services.AddDatabaseServices(builder.Configuration);
builder.Services.AddAuthenticationServices(builder.Configuration);
builder.Services.AddBusinessServices();
builder.Services.AddEmailServices(builder.Configuration);
builder.Services.AddSignalR();

// CORS 配置
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>();
        policy.WithOrigins(allowedOrigins ?? new[] { "http://localhost:5173" })
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// 配置管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseCors("DefaultPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.MapControllers();
app.MapHub<TopicsHub>("/hubs/topics");

// 数据库迁移
await app.RunDatabaseMigrationsAsync();

try
{
    Log.Information("Starting web application");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
