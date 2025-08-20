# 开发指南

## 📋 目录结构说明

### 后端架构 (Forum.Api)

```
Forum.Api/
├── Controllers/          # Web API 控制器
│   ├── AuthController.cs      # 用户认证 API
│   ├── CategoriesController.cs # 分类管理 API
│   ├── TopicsController.cs     # 主题管理 API
│   └── PostsController.cs      # 帖子管理 API
├── Hubs/                # SignalR 实时通信
│   └── TopicsHub.cs           # 主题实时通信中枢
├── Models/              # 数据模型定义
│   ├── Entities/        # 数据库实体类
│   │   ├── User.cs           # 用户实体
│   │   ├── Category.cs       # 分类实体
│   │   ├── Topic.cs          # 主题实体
│   │   └── Post.cs           # 帖子实体
│   ├── DTOs/           # 数据传输对象
│   └── Requests/       # API 请求模型
├── Services/           # 业务逻辑层
│   ├── IAuthService.cs       # 认证服务接口
│   ├── AuthService.cs        # 认证服务实现
│   └── ...其他服务
├── Repositories/       # 数据访问层
│   ├── IUserRepository.cs    # 用户数据访问接口
│   ├── UserRepository.cs     # 用户数据访问实现
│   └── ...其他仓储
├── Infrastructure/     # 基础设施层
│   ├── Database/       # 数据库相关
│   │   ├── IDbConnectionFactory.cs  # 数据库连接工厂
│   │   └── MySqlConnectionFactory.cs
│   ├── Auth/           # 认证相关
│   │   ├── IJwtTokenService.cs      # JWT 服务接口
│   │   └── JwtTokenService.cs       # JWT 服务实现
│   └── Email/          # 邮件服务
├── Middleware/         # 中间件
│   ├── ErrorHandlingMiddleware.cs   # 错误处理
│   └── RequestLoggingMiddleware.cs  # 请求日志
├── Migrations/         # 数据库迁移脚本
│   ├── 001_CreateUserTables.sql     # 用户相关表
│   ├── 002_CreateCategoriesAndTags.sql # 分类标签表
│   └── 003_CreateTopicsAndPosts.sql    # 主题帖子表
└── Extensions/         # 扩展方法
    ├── ServiceCollectionExtensions.cs  # 服务注册
    └── WebApplicationExtensions.cs     # 应用配置
```

## 🔧 代码规范

### 命名约定

- **类名**: PascalCase (UserService, CategoryRepository)
- **方法名**: PascalCase (GetUserByIdAsync, CreateTopicAsync)
- **变量名**: camelCase (userId, topicTitle)
- **常量**: PascalCase (MaxFileSize, DefaultPageSize)
- **接口**: I + PascalCase (IUserService, IRepository<T>)

### 异步编程

- 所有 I/O 操作必须使用异步方法
- 异步方法名称以 `Async` 结尾
- 使用 `ConfigureAwait(false)` (在库代码中)

```csharp
public async Task<User> GetUserByIdAsync(long id)
{
    // 正确的异步实现
    return await _repository.GetByIdAsync(id);
}
```

### 依赖注入

- 使用构造函数注入
- 接口和实现分离
- 服务生命周期管理

```csharp
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository userRepository, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }
}
```

### 错误处理

- 使用自定义异常类型
- 统一错误响应格式
- 记录详细错误日志

```csharp
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
```

### 数据访问

- 使用 Dapper 进行数据访问
- SQL 语句使用常量字符串
- 参数化查询防止 SQL 注入

```csharp
const string sql = @"
    SELECT id, username, email 
    FROM users 
    WHERE id = @Id AND is_deleted = 0";

return await connection.QuerySingleOrDefaultAsync<User>(sql, new { Id = id });
```

## 🧪 测试规范

### 单元测试

- 测试方法命名: `MethodName_Scenario_ExpectedResult`
- 使用 AAA 模式 (Arrange, Act, Assert)
- Mock 外部依赖

```csharp
[Test]
public async Task GetUserByIdAsync_UserExists_ReturnsUser()
{
    // Arrange
    var userId = 1L;
    var expectedUser = new User { Id = userId, Username = "test" };
    _mockRepository.Setup(x => x.GetByIdAsync(userId))
                  .ReturnsAsync(expectedUser);

    // Act
    var result = await _userService.GetUserByIdAsync(userId);

    // Assert
    Assert.That(result, Is.EqualTo(expectedUser));
}
```

## 🔐 安全规范

### 认证授权

- JWT Token 有效期限制
- Refresh Token 一次性使用
- 敏感操作需要重新验证

### 数据验证

- 输入参数验证
- 业务规则验证
- 输出数据清理

### 日志安全

- 不记录敏感信息 (密码、token)
- 使用结构化日志
- 记录安全相关事件

## 📚 开发流程

### 新功能开发

1. 创建 feature 分支
2. 编写单元测试
3. 实现功能代码
4. 更新文档
5. 提交 Pull Request

### 数据库变更

1. 创建迁移脚本 (按编号顺序)
2. 在开发环境测试
3. 更新实体类
4. 更新仓储方法
5. 运行集成测试

### API 设计

1. 遵循 RESTful 原则
2. 使用标准 HTTP 状态码
3. 统一响应格式
4. 版本控制考虑

## 🛠️ 工具配置

### 开发环境

- Visual Studio 2022 / VS Code
- SQL Server Management Studio
- Postman (API 测试)
- Git (版本控制)

### 代码质量工具

- EditorConfig (代码格式)
- SonarLint (代码质量)
- StyleCop (代码风格)

## 📋 检查清单

### 提交前检查

- [ ] 代码编译无警告
- [ ] 单元测试全部通过
- [ ] 代码覆盖率达标
- [ ] 文档已更新
- [ ] 安全检查通过