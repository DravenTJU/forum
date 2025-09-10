# Forum.Api 测试工程师分工方案

> **基于**: TDD_GUIDE.md  
> **目标**: 最大程度避免两人之间的阻塞  
> **原则**: 垂直分工 + 并行开发 + 独立测试

## 1. 分工策略总览

### 1.1 核心原则
- **垂直切分**：按业务领域完整划分，减少跨领域依赖
- **接口隔离**：通过Mock和接口设计实现独立开发
- **并行优先**：80%时间可以并行工作，20%时间协作集成
- **责任明确**：每个人对自己领域的质量完全负责

### 1.2 协作边界
```
工程师A (用户与内容)     ←→  协作接口  ←→     工程师B (分类与基础设施)
├── AuthService                            ├── CategoryService
├── TopicService        ←→  Mock依赖  ←→   ├── TagService  
├── PostService                           ├── EmailService
└── 用户相关Repository                     └── 基础设施Repository
```

## 2. 工程师A：用户体系与内容管理

### 2.1 核心职责
**业务领域**：用户生命周期 + 内容生产消费链路

**服务层测试**：
- `AuthService`: 完整的用户认证授权体系
- `TopicService`: 主题管理的完整生命周期  
- `PostService`: 帖子交互的完整功能链
- `UserService`: 用户管理和资料维护

**数据层测试**：
- `UserRepository`: 用户数据访问和查询优化
- `TopicRepository`: 主题数据管理和分页查询
- `PostRepository`: 帖子数据和关系维护
- `RefreshTokenRepository`: 会话令牌管理

**API层测试**：
- `AuthController`: `/api/v1/auth/*` 全部端点
- `TopicsController`: `/api/v1/topics/*` 全部端点  
- `PostsController`: `/api/v1/topics/*/posts/*` 全部端点

### 2.2 TDD开发时序

#### Phase 1: 用户认证体系 (Week 1-2)
**优先级1：用户注册流程TDD**
```csharp
// Red-Green-Refactor 迭代顺序
1. RegisterUser_WithValidInput_ShouldCreateUser
2. RegisterUser_WithDuplicateEmail_ShouldThrowConflictException  
3. RegisterUser_WithWeakPassword_ShouldThrowValidationException
4. RegisterUser_ShouldSendVerificationEmail
5. RegisterUser_ShouldHashPassword
```

**优先级2：用户登录和JWT**
```csharp
1. LoginUser_WithValidCredentials_ShouldReturnJwtToken
2. LoginUser_WithInvalidPassword_ShouldThrowAuthException
3. LoginUser_WithUnverifiedEmail_ShouldThrowVerificationException
4. GenerateJwtToken_ShouldIncludeUserClaims
5. ValidateJwtToken_WithExpiredToken_ShouldThrowException
```

#### Phase 2: 主题管理体系 (Week 3-4)
**依赖处理策略**：Mock `ICategoryService` 和 `ITagService`
```csharp
// Mock依赖示例
_mockCategoryService.Setup(x => x.GetByIdAsync(It.IsAny<long>()))
                   .ReturnsAsync(new Category { Id = 1, Name = "Test Category" });
```

**TDD开发顺序**：
1. 主题创建基本功能
2. 分类关联和验证（Mock分类服务）
3. 权限控制和用户验证
4. 编辑和删除功能
5. 统计信息更新

#### Phase 3: 帖子交互体系 (Week 5-6)
**关键功能**：
1. 帖子创建和回复链
2. @提及解析和通知
3. Markdown渲染和XSS防护
4. 帖子编辑历史追踪
5. 删除和恢复机制

### 2.3 测试工具建设
**测试数据构建器**：
```csharp
public class UserTestBuilder
public class TopicTestBuilder  
public class PostTestBuilder
public class RegisterRequestBuilder
public class LoginRequestBuilder
```

**测试基类**：
```csharp
public class AuthenticatedTestBase : TestBase
public class UserContextTestBase : TestBase  
public class ContentManagementTestBase : TestBase
```

## 3. 工程师B：分类体系与基础设施

### 3.1 核心职责
**业务领域**：内容组织体系 + 系统基础设施

**服务层测试**：
- `CategoryService`: 分类层次结构和管理
- `TagService`: 标签体系和关联关系
- `EmailService`: 邮件服务和模板系统
- `SearchService`: 全文搜索和索引管理
- `NotificationService`: 通知系统和推送

**数据层测试**：  
- `CategoryRepository`: 分类数据和层次查询
- `TagRepository`: 标签数据和使用统计
- `NotificationRepository`: 通知数据和状态管理
- 搜索相关的数据访问逻辑

**API层测试**：
- `CategoriesController`: `/api/v1/categories/*` 全部端点
- `TagsController`: `/api/v1/tags/*` 全部端点
- `SearchController`: `/api/v1/search/*` 全部端点
- `NotificationsController`: `/api/v1/notifications/*` 全部端点

### 3.2 TDD开发时序

#### Phase 1: 分类标签体系 (Week 1-2) 
**优先级1：分类管理TDD**
```csharp
1. CreateCategory_WithValidData_ShouldCreateCategory
2. CreateCategory_WithDuplicateSlug_ShouldThrowConflictException
3. GetCategoriesHierarchy_ShouldReturnOrderedCategories
4. UpdateCategoryOrder_ShouldUpdateSequence
5. DeleteCategory_WithTopics_ShouldPreventDeletion
```

**优先级2：标签系统TDD**
```csharp  
1. CreateTag_WithValidData_ShouldCreateTag
2. AssignTagToTopic_ShouldCreateAssociation
3. GetPopularTags_ShouldReturnByUsageCount
4. DeleteTag_ShouldRemoveAssociations
5. SearchTags_WithQuery_ShouldReturnMatches
```

#### Phase 2: 基础设施服务 (Week 3-4)
**邮件服务TDD**：
```csharp
1. SendVerificationEmail_WithValidUser_ShouldSendEmail
2. SendEmail_WithInvalidTemplate_ShouldThrowException  
3. SendBulkEmails_ShouldHandleBatchProcessing
4. SendEmail_WithSmtpFailure_ShouldRetryWithExponentialBackoff
```

**数据库迁移TDD**：
```csharp
1. RunMigrations_WithValidScripts_ShouldUpdateSchema
2. RunMigrations_WithFailedScript_ShouldRollback
3. GetPendingMigrations_ShouldReturnUnexecutedScripts
```

#### Phase 3: 搜索通知体系 (Week 5-6)
**搜索服务TDD**：
```csharp
1. SearchTopics_WithKeyword_ShouldReturnRelevantResults
2. SearchPosts_WithQuery_ShouldRankByRelevance
3. SearchWithFilters_ShouldApplyCategoryAndTagFilters
4. FullTextSearch_WithSpecialCharacters_ShouldEscapeProperly
```

**通知服务TDD**：
```csharp
1. CreateMentionNotification_WhenUserMentioned_ShouldNotifyUser
2. CreateReplyNotification_WhenPostReplied_ShouldNotifyAuthor
3. MarkNotificationAsRead_ShouldUpdateReadStatus
4. GetUnreadNotifications_ShouldReturnOnlyUnread
```

### 3.3 测试工具建设
**测试数据构建器**：
```csharp
public class CategoryTestBuilder
public class TagTestBuilder
public class EmailTestBuilder  
public class NotificationTestBuilder
public class SearchQueryBuilder
```

**测试基类**：
```csharp
public class DatabaseTestBase : TestBase
public class CategoryHierarchyTestBase : TestBase
public class SearchTestBase : TestBase
public class EmailServiceTestBase : TestBase
```

## 4. 协作开发：实时通信系统

### 4.1 分工协作策略
**共同责任**：SignalR Hub 和实时消息服务

**工程师A负责**：
- 用户连接和认证验证
- 用户状态管理（在线/离线/输入中）
- 用户权限验证（房间准入控制）
- 用户相关的实时事件

**工程师B负责**：
- 消息广播基础设施  
- 房间管理和连接池
- 消息持久化和重试机制
- 系统级实时事件

### 4.2 接口协作设计
**统一接口定义**：
```csharp
public interface IRealtimeMessageService
{
    // 工程师A实现
    Task AuthenticateConnectionAsync(string connectionId, string token);
    Task JoinTopicRoomAsync(string connectionId, long topicId);  
    Task UpdateUserTypingStatusAsync(long topicId, long userId, bool isTyping);
    
    // 工程师B实现
    Task BroadcastPostCreatedAsync(long topicId, PostCreatedEvent eventData);
    Task BroadcastPostEditedAsync(long topicId, PostEditedEvent eventData);
    Task BroadcastTopicStatsAsync(long topicId, TopicStatsEvent eventData);
}
```

**Mock依赖策略**：
```csharp
// 工程师A测试时Mock工程师B的部分
_mockMessageService.Setup(x => x.BroadcastPostCreatedAsync(It.IsAny<long>(), It.IsAny<PostCreatedEvent>()))
                  .Returns(Task.CompletedTask);

// 工程师B测试时Mock工程师A的部分  
_mockMessageService.Setup(x => x.AuthenticateConnectionAsync(It.IsAny<string>(), It.IsAny<string>()))
                  .ReturnsAsync(true);
```

### 4.3 集成测试协作
**集成测试责任**：
- **Week 7**: 共同编写 `SignalRHubIntegrationTests`
- **测试场景**: 端到端的消息流测试
- **协作方式**: 结对编程或轮流编写测试用例

## 5. 测试基础设施共建

### 5.1 共享测试工具
**数据库测试基础**：
```csharp
public class DatabaseTestFixture : IDisposable
{
    public MySqlContainer MySqlContainer { get; }
    public IDbConnectionFactory ConnectionFactory { get; }
    
    // 工程师A和B共同维护
}
```

**API测试基础**：
```csharp
public abstract class WebApplicationTestBase : IClassFixture<WebApplicationFactory<Program>>
{
    // 统一的HTTP客户端配置
    // 认证令牌管理
    // 响应格式验证
}
```

### 5.2 并行测试配置
**测试隔离机制**：
```csharp
// 工程师A的测试配置
[Collection("AuthTests")]  
public class AuthServiceTests : TestBase
{
    protected override string TestDatabase => "ForumTest_Auth";
}

// 工程师B的测试配置  
[Collection("CategoryTests")]
public class CategoryServiceTests : TestBase  
{
    protected override string TestDatabase => "ForumTest_Category";
}
```

**容器配置隔离**：
```csharp
// 使用不同端口避免冲突
public class AuthTestContainerConfig  
{
    public static MySqlContainer CreateContainer() 
        => new MySqlBuilder().WithPortBinding(3307).Build();
}

public class CategoryTestContainerConfig
{
    public static MySqlContainer CreateContainer()
        => new MySqlBuilder().WithPortBinding(3308).Build();  
}
```

## 6. 时间线和里程碑

### 6.1 并行开发时间线

| Week | 工程师A | 工程师B | 协作内容 |
|------|---------|---------|----------|
| **W1** | AuthService TDD | CategoryService + TagService TDD | 接口设计讨论 |
| **W2** | JWT + 权限 TDD | 分类层次 + 标签关联 TDD | 测试基础设施搭建 |
| **W3** | TopicService TDD | EmailService + Migration TDD | Mock依赖接口确定 |
| **W4** | 主题管理完整流程 | 基础设施服务集成 | 中期代码审查 |
| **W5** | PostService TDD | SearchService TDD | SignalR接口设计 |
| **W6** | 帖子交互完整功能 | NotificationService TDD | SignalR分工确定 |
| **W7** | API Controller测试 | API Controller测试 | SignalR集成开发 |
| **W8** | 完整集成测试 | 完整集成测试 | 最终集成验证 |

### 6.2 关键里程碑
**M1 (Week 2)**: 核心服务层TDD完成，接口稳定
**M2 (Week 4)**: 复杂业务逻辑完成，Mock依赖验证
**M3 (Week 6)**: 所有单独模块完成，准备集成
**M4 (Week 8)**: 完整系统集成，所有测试通过

## 7. 冲突预防和协作机制

### 7.1 代码协作规范
**分支策略**：
```bash
# 工程师A使用的分支前缀
feature/auth-*
feature/topic-*  
feature/post-*

# 工程师B使用的分支前缀
feature/category-*
feature/infrastructure-*
feature/search-*
```

**命名空间隔离**：
```csharp
// 工程师A的命名空间
Forum.Api.Tests.Unit.Auth
Forum.Api.Tests.Unit.Content
Forum.Api.Tests.Integration.UserFlow

// 工程师B的命名空间  
Forum.Api.Tests.Unit.Category
Forum.Api.Tests.Unit.Infrastructure
Forum.Api.Tests.Integration.Search
```

### 7.2 沟通协作机制
**每日同步** (5分钟)：
- 昨日进展和今日计划
- 依赖阻塞点识别
- 接口变更通知

**Weekly Review** (30分钟)：
- 代码质量相互审查
- 测试覆盖率对比
- 下周协作点确认

**关键决策点**：
- 接口设计变更需双方确认
- 共享测试工具修改需协商
- 数据库Schema变更需同步

### 7.3 质量保证机制
**交叉审查**：
- 服务接口设计相互审查
- 测试用例覆盖度交叉检查  
- Mock使用合理性相互验证

**集成验证**：
- 每周集成测试运行
- 接口兼容性自动检查
- 性能基准自动验证

## 8. 成功指标和风险控制

### 8.1 成功指标
**效率指标**：
- 并行工作时间比例 ≥ 80%
- 相互等待时间 ≤ 10%
- 接口集成一次成功率 ≥ 90%

**质量指标**：
- 各自领域测试覆盖率 ≥ 95%
- 跨领域集成测试覆盖率 ≥ 85%
- Mock使用合理性 ≥ 90%

**协作指标**：
- 代码冲突解决时间 ≤ 1小时
- 接口变更响应时间 ≤ 4小时  
- 协作满意度 ≥ 4.5/5

### 8.2 风险控制
**技术风险**：
- **依赖阻塞**: 通过Mock和接口隔离预防
- **集成失败**: 早期接口设计和持续集成
- **性能问题**: 独立性能测试和基准验证

**协作风险**：
- **沟通不畅**: 建立定期同步机制
- **标准不一**: 统一代码规范和质量标准
- **进度不协调**: 里程碑检查和及时调整

**缓解措施**：
- 接口优先设计，减少后期返工
- 自动化测试保证集成质量
- 文档化决策过程，避免重复讨论

## 9. 总结

这个分工方案通过垂直领域划分和接口隔离设计，确保两个工程师在80%的时间内可以并行工作，只在关键集成点需要协作。每个工程师对自己负责的业务领域拥有完整的控制权，通过Mock机制实现独立开发和测试。

**关键成功因素**：
1. **清晰的责任边界**：每人负责完整的业务领域
2. **有效的接口隔离**：通过Mock实现独立开发  
3. **持续的沟通协作**：及时同步和冲突解决
4. **统一的质量标准**：代码规范和测试要求一致
5. **灵活的调整机制**：根据实际情况动态优化分工

通过遵循这个分工方案，两个测试工程师可以高效协作，在确保代码质量的同时最大化开发效率。