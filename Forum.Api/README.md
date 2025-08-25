# Forum API - ASP.NET Core 后端

Discourse 风格论坛的后端 API 服务，采用整洁架构和现代 .NET 技术栈。

## 技术栈

- **框架**: ASP.NET Core 8 Web API
- **数据库**: MySQL 8.x (utf8mb4)
- **ORM**: Dapper (轻量级微 ORM)
- **实时通信**: SignalR
- **认证**: JWT (Access + Refresh Token)
- **邮件**: MailKit
- **日志**: Serilog (结构化日志)
- **迁移**: DbUp (SQL 脚本管理)
- **验证**: FluentValidation
- **容器**: Docker

## 开发命令

```bash
# 安装和恢复
dotnet restore          # 恢复 NuGet 包依赖

# 开发运行  
dotnet run              # 启动开发服务器 http://localhost:4000
dotnet watch run        # 热重载开发模式

# 构建和测试
dotnet build            # 构建项目
dotnet test             # 运行单元测试
dotnet publish          # 发布生产版本

# 代码质量
dotnet format           # 代码格式化
dotnet build --verbosity normal  # 详细构建信息
```

## 项目结构

```
Forum.Api/
├── Controllers/         # 🔌 API 控制器层
│   ├── AuthController.cs           # 认证 API (/api/auth)
│   ├── CategoriesController.cs     # 分类 API (/api/categories)
│   ├── TopicsController.cs         # 主题 API (/api/topics) 
│   ├── PostsController.cs          # 帖子 API (/api/posts)
│   ├── HealthController.cs         # 健康检查
│   └── TestController.cs           # 开发测试端点
│
├── Services/           # 💼 业务逻辑层
│   ├── AuthService.cs              # 认证业务逻辑
│   ├── CategoryService.cs          # 分类业务逻辑
│   ├── TopicService.cs             # 主题业务逻辑
│   ├── PostService.cs              # 帖子业务逻辑
│   ├── SignalRService.cs           # 实时通信服务
│   └── I*.cs                       # 对应接口定义
│
├── Repositories/       # 🗄️ 数据访问层 (Dapper)
│   ├── UserRepository.cs           # 用户数据操作
│   ├── CategoryRepository.cs       # 分类数据操作
│   ├── TopicRepository.cs          # 主题数据操作
│   ├── PostRepository.cs           # 帖子数据操作
│   ├── RefreshTokenRepository.cs   # 刷新令牌操作
│   └── I*.cs                       # 对应接口定义
│
├── Models/             # 📋 数据模型
│   ├── Entities/       # 数据库实体 (与表结构对应)
│   │   ├── User.cs, Category.cs, Topic.cs, Post.cs
│   │   ├── Tag.cs, RefreshToken.cs
│   └── DTOs/          # API 数据传输对象
│       ├── AuthDTOs.cs             # 认证相关 DTO
│       ├── CategoryDTOs.cs, TopicDTOs.cs, PostDTOs.cs
│       ├── ApiResponse.cs          # 统一 API 响应格式
│       └── PaginationQuery.cs      # 分页查询模型
│
├── Infrastructure/     # 🏗️ 基础设施层
│   ├── Database/       # 数据库基础设施
│   │   ├── IDbConnectionFactory.cs      # 数据库连接抽象
│   │   ├── MySqlConnectionFactory.cs    # MySQL 连接实现
│   │   ├── DatabaseMigrator.cs          # 数据库迁移器
│   │   └── DapperTypeHandlers.cs        # Dapper 类型处理
│   ├── Auth/          # 认证基础设施
│   │   ├── IJwtTokenService.cs          # JWT 服务接口
│   │   ├── JwtTokenService.cs           # JWT 服务实现
│   │   ├── IPasswordService.cs          # 密码服务接口
│   │   ├── PasswordService.cs           # 密码哈希服务
│   │   └── JwtSettings.cs               # JWT 配置
│   └── Email/         # 邮件基础设施
│       ├── IEmailService.cs             # 邮件服务接口
│       ├── EmailService.cs              # MailKit 邮件实现
│       └── EmailSettings.cs             # 邮件配置
│
├── Hubs/              # 🔄 SignalR 实时通信
│   └── TopicsHub.cs                     # 主题房间广播中心
│
├── Middleware/        # 🛡️ 中间件
│   ├── ErrorHandlingMiddleware.cs       # 全局异常处理
│   └── RequestLoggingMiddleware.cs      # 请求日志记录
│
├── Migrations/        # 🗃️ 数据库迁移 (DbUp)
│   ├── 001_CreateUserTables.sql         # 用户和角色表
│   ├── 002_CreateCategoriesAndTags.sql  # 分类和标签表
│   └── 003_CreateTopicsAndPosts.sql     # 主题和帖子表
│
├── Extensions/        # ⚙️ 扩展配置
│   ├── ServiceCollectionExtensions.cs   # DI 容器配置
│   └── WebApplicationExtensions.cs      # 应用管道配置
│
├── Properties/        # 配置文件
│   └── launchSettings.json              # 启动配置
│
├── Program.cs         # 应用程序入口点
├── Forum.Api.csproj   # 项目文件和依赖
├── appsettings.json   # 应用配置
└── Dockerfile         # 容器化配置
```

## 开发规范

### 架构原则
- **整洁架构**: 依赖倒置，核心业务逻辑独立于框架
- **SOLID 原则**: 单一职责、开闭、里氏替换、接口隔离、依赖倒置
- **Repository 模式**: 数据访问抽象，便于测试和切换数据源
- **依赖注入**: 构造函数注入，接口与实现分离

### 命名约定
```csharp
// 类和接口
public class UserService : IUserService          // PascalCase
public interface IUserRepository                 // I + PascalCase

// 方法和属性
public async Task<User> GetUserByIdAsync(long id)  // PascalCase + Async 后缀
public string Username { get; set; }               // 属性 PascalCase

// 字段和变量
private readonly IUserRepository _userRepository;  // _camelCase (私有字段)
var userId = request.UserId;                        // camelCase (局部变量)

// 常量
public const int MaxUsernameLength = 50;            // PascalCase
```

### 异步编程规范
```csharp
// 正确：所有 I/O 操作使用异步
public async Task<User> GetUserByIdAsync(long id)
{
    const string sql = "SELECT * FROM users WHERE id = @Id";
    return await _connection.QuerySingleOrDefaultAsync<User>(sql, new { Id = id });
}

// 正确：异步方法命名以 Async 结尾
public async Task<bool> SendEmailAsync(string to, string subject, string body)

// 正确：避免 async void，使用 async Task
public async Task HandleEventAsync() // 不是 async void
```

### 数据访问模式
```csharp
// Repository 实现示例
public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _dbFactory;
    
    public async Task<User?> GetByIdAsync(long id)
    {
        using var connection = await _dbFactory.CreateConnectionAsync();
        
        const string sql = @"
            SELECT id, username, email, password_hash, created_at, updated_at
            FROM users 
            WHERE id = @Id AND is_deleted = 0";
            
        return await connection.QuerySingleOrDefaultAsync<User>(sql, new { Id = id });
    }
}
```

### API 设计规范
```csharp
// Controller 设计示例
[ApiController]
[Route("api/[controller]")]
public class TopicsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<TopicListDto>>>> GetTopics(
        [FromQuery] PaginationQuery query,
        [FromQuery] long? categoryId = null)
    {
        var result = await _topicService.GetTopicsAsync(query, categoryId);
        return Ok(ApiResponse<PaginatedResult<TopicListDto>>.Success(result));
    }
    
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ApiResponse<TopicDto>>> CreateTopic(
        [FromBody] CreateTopicRequest request)
    {
        var topic = await _topicService.CreateTopicAsync(request, GetCurrentUserId());
        return CreatedAtAction(nameof(GetTopic), new { id = topic.Id }, 
            ApiResponse<TopicDto>.Success(topic));
    }
}
```

### 错误处理策略
```csharp
// 自定义异常
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string resource, object key) 
        : base($"{resource} with key '{key}' was not found.") { }
}

// 全局错误处理中间件
public async Task InvokeAsync(HttpContext context, RequestDelegate next)
{
    try
    {
        await next(context);
    }
    catch (Exception ex)
    {
        await HandleExceptionAsync(context, ex);
    }
}
```

### 依赖注入配置
```csharp
// ServiceCollectionExtensions.cs
public static IServiceCollection AddApplicationServices(this IServiceCollection services)
{
    // 业务服务 - Scoped
    services.AddScoped<IAuthService, AuthService>();
    services.AddScoped<ITopicService, TopicService>();
    
    // 数据访问 - Scoped  
    services.AddScoped<IUserRepository, UserRepository>();
    services.AddScoped<ITopicRepository, TopicRepository>();
    
    // 基础设施服务 - Singleton
    services.AddSingleton<IDbConnectionFactory, MySqlConnectionFactory>();
    services.AddSingleton<IJwtTokenService, JwtTokenService>();
    
    return services;
}
```

## 数据库管理

### 迁移脚本规范
```sql
-- 001_CreateUserTables.sql
-- 严格按照数字前缀顺序命名
-- 包含 DDL 和必要的初始数据

CREATE TABLE users (
    id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
    username VARCHAR(20) NOT NULL UNIQUE,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(100) NOT NULL,
    -- 更多字段...
    created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    updated_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

### DbUp 迁移管理
- 启动时自动执行未应用的迁移
- 迁移脚本不可逆，谨慎设计
- 开发环境测试后才能提交
- 生产部署前备份数据库

## 实时通信 (SignalR)

### Hub 实现
```csharp
[Authorize]
public class TopicsHub : Hub
{
    public async Task JoinTopic(long topicId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"topic:{topicId}");
    }
    
    public async Task LeaveTopic(long topicId)  
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"topic:{topicId}");
    }
}
```

### 服务端事件广播
```csharp
public class SignalRService : ISignalRService
{
    private readonly IHubContext<TopicsHub> _hubContext;
    
    public async Task NotifyPostCreated(long topicId, PostDto post)
    {
        await _hubContext.Clients.Group($"topic:{topicId}")
            .SendAsync("PostCreated", post);
    }
}
```

## 安全规范

### 认证授权
```csharp
// JWT 配置
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            // 配置参数...
        };
    });

// Controller 授权
[Authorize] // 需要认证
[Authorize(Roles = "Admin")] // 需要特定角色
```

### 输入验证
```csharp
// 使用 FluentValidation
public class CreateTopicRequestValidator : AbstractValidator<CreateTopicRequest>
{
    public CreateTopicRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("标题不能为空")
            .MaximumLength(200).WithMessage("标题长度不能超过200个字符");
            
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("内容不能为空")
            .MaximumLength(10000).WithMessage("内容长度不能超过10000个字符");
    }
}
```

### 安全日志
```csharp
// 不记录敏感信息
_logger.LogInformation("User {UserId} attempted login", userId); // ✅
_logger.LogInformation("Login attempt: {Password}", password);   // ❌

// 记录安全事件
_logger.LogWarning("Failed login attempt for email {Email} from IP {IpAddress}", 
    email, ipAddress);
```

## 测试策略

### 单元测试
```csharp
[TestFixture]
public class UserServiceTests
{
    private Mock<IUserRepository> _mockUserRepository;
    private UserService _userService;
    
    [SetUp]
    public void Setup()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _userService = new UserService(_mockUserRepository.Object);
    }
    
    [Test]
    public async Task GetUserByIdAsync_UserExists_ReturnsUser()
    {
        // Arrange
        var userId = 1L;
        var expectedUser = new User { Id = userId, Username = "testuser" };
        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
                          .ReturnsAsync(expectedUser);
        
        // Act
        var result = await _userService.GetUserByIdAsync(userId);
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Username, Is.EqualTo("testuser"));
    }
}
```

### 集成测试
```csharp
[TestFixture]
public class AuthControllerIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task Register_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Password123!"
        };
        
        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", request);
        
        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
        Assert.That(result.Success, Is.True);
    }
}
```

## 配置管理

### appsettings.json 结构
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=forum;Uid=root;Pwd=password;"
  },
  "JwtSettings": {
    "Secret": "your-super-secret-jwt-key-must-be-at-least-256-bits",
    "Issuer": "ForumApi",
    "Audience": "ForumClient", 
    "ExpirationInMinutes": 60,
    "RefreshExpirationInDays": 7
  },
  "EmailSettings": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "FromEmail": "noreply@forum.com",
    "FromName": "Forum System"
  },
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File", "Args": { "path": "logs/log-.txt", "rollingInterval": "Day" } }
    ]
  }
}
```

### 环境变量支持
- `ASPNETCORE_ENVIRONMENT`: Development/Staging/Production
- `DB_CONNECTION_STRING`: 数据库连接字符串
- `JWT_SECRET`: JWT 密钥
- `SMTP_*`: 邮件服务配置

## 部署和运维

### Docker 支持
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "Forum.Api.dll"]
```

### 健康检查
- `/api/health` - 基础健康检查
- `/api/health/ready` - 就绪检查（数据库连接等）
- 支持 Kubernetes liveness 和 readiness 探针

### 日志管理
- 结构化日志 (JSON 格式)
- 日志级别：Information/Warning/Error
- 按天分割日志文件
- 生产环境日志聚合 (ELK Stack 等)

## 开发工作流

### 新功能开发
1. **实体设计**: 在 `Models/Entities/` 定义数据库实体
2. **迁移脚本**: 创建对应的 SQL 迁移文件
3. **Repository**: 在 `Repositories/` 实现数据访问
4. **Service**: 在 `Services/` 实现业务逻辑
5. **DTO**: 在 `Models/DTOs/` 定义 API 数据传输对象
6. **Controller**: 在 `Controllers/` 实现 API 端点
7. **测试**: 编写单元测试和集成测试

### API 开发流程
1. **设计 API**: 定义 RESTful 接口和数据格式
2. **DTO 定义**: 请求和响应对象的类型定义
3. **验证规则**: FluentValidation 验证器
4. **业务逻辑**: Service 层实现
5. **控制器实现**: Controller 路由和处理
6. **文档更新**: Swagger 注释和 API 文档

### 数据库变更流程
1. **设计变更**: 分析表结构和数据迁移需求
2. **编写迁移**: 创建编号的 SQL 脚本
3. **更新实体**: 修改对应的实体类
4. **更新 Repository**: 调整数据访问方法
5. **测试验证**: 开发环境测试迁移
6. **集成测试**: 验证相关功能正常

## 相关文档

- [项目整体架构](../CLAUDE.md)
- [前端开发指南](../forum-frontend/README.md)
- [编码规范详解](../doc/coding_standards_and_principles.md)
- [实现工作流](../doc/implementation-workflow.md)
- [产品需求文档](../doc/prd-discourse-style-forum.md)