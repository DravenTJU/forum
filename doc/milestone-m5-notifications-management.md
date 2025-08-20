# 里程碑 M5：通知系统与基础管理功能

**工期：7天**  
**前置条件：M4 实时功能完成**

## 目标

实现完整的通知系统（@ 提及、回复通知）以及基础的管理功能（分类/标签管理、用户管理、内容审核）。

## 技术要求

- 通知生成与分发机制
- 站内通知 UI 组件
- 管理后台权限控制
- 内容审核工作流
- 邮件通知集成

---

## Day 1: 通知系统基础架构

### 后端：通知服务层

```csharp
// Services/NotificationService.cs
public class NotificationService
{
    private readonly INotificationRepository _repository;
    private readonly IHubContext<TopicsHub> _hubContext;
    private readonly IEmailService _emailService;

    public async Task CreateMentionNotificationAsync(long postId, IEnumerable<long> mentionedUserIds)
    {
        var post = await _postRepository.GetAsync(postId);
        if (post == null) return;

        foreach (var userId in mentionedUserIds)
        {
            var notification = new Notification
            {
                UserId = userId,
                Type = NotificationType.Mention,
                TopicId = post.TopicId,
                PostId = postId,
                ByUserId = post.AuthorId,
                Snippet = TruncateContent(post.ContentMd, 100),
                CreatedAt = DateTime.UtcNow
            };

            var id = await _repository.CreateAsync(notification);
            
            // 实时推送
            await _hubContext.Clients.User(userId.ToString())
                .SendAsync("NotificationReceived", new {
                    Id = id,
                    Type = "mention",
                    Snippet = notification.Snippet,
                    TopicId = post.TopicId,
                    PostId = postId,
                    CreatedAt = notification.CreatedAt
                });

            // 可选：邮件通知
            var user = await _userRepository.GetAsync(userId);
            if (user?.EmailNotificationEnabled == true)
            {
                await _emailService.SendMentionNotificationAsync(user.Email, post);
            }
        }
    }

    public async Task CreateReplyNotificationAsync(long postId, long topicAuthorId)
    {
        var post = await _postRepository.GetAsync(postId);
        if (post == null || post.AuthorId == topicAuthorId) return;

        var notification = new Notification
        {
            UserId = topicAuthorId,
            Type = NotificationType.Reply,
            TopicId = post.TopicId,
            PostId = postId,
            ByUserId = post.AuthorId,
            Snippet = TruncateContent(post.ContentMd, 100),
            CreatedAt = DateTime.UtcNow
        };

        var id = await _repository.CreateAsync(notification);
        
        await _hubContext.Clients.User(topicAuthorId.ToString())
            .SendAsync("NotificationReceived", new {
                Id = id,
                Type = "reply",
                Snippet = notification.Snippet,
                TopicId = post.TopicId,
                PostId = postId,
                CreatedAt = notification.CreatedAt
            });
    }

    private string TruncateContent(string content, int maxLength)
    {
        if (content.Length <= maxLength) return content;
        return content.Substring(0, maxLength) + "...";
    }
}
```

### 数据访问层

```csharp
// Repositories/NotificationRepository.cs
public class NotificationRepository : INotificationRepository
{
    private readonly string _connectionString;

    public async Task<long> CreateAsync(Notification notification)
    {
        const string sql = @"
            INSERT INTO notifications (user_id, type, topic_id, post_id, by_user_id, snippet, created_at)
            VALUES (@UserId, @Type, @TopicId, @PostId, @ByUserId, @Snippet, @CreatedAt);
            SELECT LAST_INSERT_ID();";

        await using var connection = new MySqlConnection(_connectionString);
        return await connection.ExecuteScalarAsync<long>(sql, notification);
    }

    public async Task<IEnumerable<NotificationItem>> GetUserNotificationsAsync(long userId, int limit = 20, long? cursorId = null)
    {
        const string sql = @"
            SELECT n.id, n.type, n.snippet, n.read_at, n.created_at,
                   n.topic_id, t.title AS topic_title,
                   n.post_id,
                   n.by_user_id, u.username AS by_username, u.avatar_url AS by_avatar_url
            FROM notifications n
            LEFT JOIN topics t ON t.id = n.topic_id
            LEFT JOIN users u ON u.id = n.by_user_id
            WHERE n.user_id = @UserId
              AND (@CursorId IS NULL OR n.id < @CursorId)
            ORDER BY n.created_at DESC, n.id DESC
            LIMIT @Limit";

        await using var connection = new MySqlConnection(_connectionString);
        return await connection.QueryAsync<NotificationItem>(sql, new { UserId = userId, CursorId = cursorId, Limit = limit });
    }

    public async Task<int> GetUnreadCountAsync(long userId)
    {
        const string sql = "SELECT COUNT(*) FROM notifications WHERE user_id = @UserId AND read_at IS NULL";
        
        await using var connection = new MySqlConnection(_connectionString);
        return await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
    }

    public async Task MarkAsReadAsync(long notificationId, long userId)
    {
        const string sql = @"
            UPDATE notifications 
            SET read_at = NOW(3) 
            WHERE id = @Id AND user_id = @UserId AND read_at IS NULL";

        await using var connection = new MySqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new { Id = notificationId, UserId = userId });
    }
}
```

### API 控制器

```csharp
// Controllers/NotificationsController.cs
[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationRepository _repository;
    
    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] bool unreadOnly = false, [FromQuery] long? cursor = null)
    {
        var userId = GetCurrentUserId();
        var notifications = await _repository.GetUserNotificationsAsync(userId, cursor: cursor);
        
        if (unreadOnly)
        {
            notifications = notifications.Where(n => n.ReadAt == null);
        }
        
        return Ok(new { Data = notifications });
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetCurrentUserId();
        var count = await _repository.GetUnreadCountAsync(userId);
        return Ok(new { Count = count });
    }

    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkAsRead(long id)
    {
        var userId = GetCurrentUserId();
        await _repository.MarkAsReadAsync(id, userId);
        return NoContent();
    }
}
```

### 验证要点

- [ ] 通知创建接口正常工作
- [ ] 通知列表 API 返回正确格式
- [ ] 未读计数准确
- [ ] 标记已读功能正常

---

## Day 2: @ 提及检测与通知触发

### 提及解析服务

```csharp
// Services/MentionService.cs
public class MentionService
{
    private readonly IUserRepository _userRepository;
    private static readonly Regex MentionRegex = new(@"@(\w{2,20})", RegexOptions.Compiled);

    public async Task<IEnumerable<long>> ExtractMentionedUserIdsAsync(string content)
    {
        var matches = MentionRegex.Matches(content);
        if (!matches.Any()) return Enumerable.Empty<long>();

        var usernames = matches.Select(m => m.Groups[1].Value).Distinct().ToList();
        var users = await _userRepository.GetByUsernamesAsync(usernames);
        
        return users.Select(u => u.Id);
    }

    public string HighlightMentions(string content)
    {
        return MentionRegex.Replace(content, match =>
        {
            var username = match.Groups[1].Value;
            return $"<span class=\"mention\">@{username}</span>";
        });
    }
}
```

### 集成到帖子创建流程

```csharp
// Controllers/PostsController.cs 修改
[HttpPost]
public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest request)
{
    // 验证和权限检查...
    
    var post = new Post
    {
        TopicId = request.TopicId,
        AuthorId = GetCurrentUserId(),
        ContentMd = request.ContentMd,
        ReplyToPostId = request.ReplyToPostId,
        CreatedAt = DateTime.UtcNow
    };

    await using var connection = new MySqlConnection(_connectionString);
    await using var transaction = await connection.BeginTransactionAsync();

    try
    {
        // 1. 创建帖子
        var postId = await _postRepository.CreateAsync(post, transaction);
        
        // 2. 更新主题统计
        await _topicRepository.IncrementReplyCountAsync(request.TopicId, GetCurrentUserId(), transaction);
        
        // 3. 检测并创建 @ 提及通知
        var mentionedUserIds = await _mentionService.ExtractMentionedUserIdsAsync(request.ContentMd);
        if (mentionedUserIds.Any())
        {
            await _notificationService.CreateMentionNotificationAsync(postId, mentionedUserIds);
        }

        // 4. 创建回复通知（如果不是主题作者自己回复）
        var topic = await _topicRepository.GetAsync(request.TopicId);
        if (topic != null && topic.AuthorId != GetCurrentUserId())
        {
            await _notificationService.CreateReplyNotificationAsync(postId, topic.AuthorId);
        }

        await transaction.CommitAsync();

        // 5. 实时广播
        await _hubContext.Clients.Group($"topic:{request.TopicId}")
            .SendAsync("PostCreated", new {
                Id = postId,
                TopicId = request.TopicId,
                AuthorId = GetCurrentUserId(),
                ContentMd = request.ContentMd,
                CreatedAt = post.CreatedAt
            });

        return CreatedAtAction(nameof(GetPost), new { id = postId }, new { Id = postId });
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### 用户查询优化

```csharp
// Repositories/UserRepository.cs 添加方法
public async Task<IEnumerable<User>> GetByUsernamesAsync(IEnumerable<string> usernames)
{
    if (!usernames.Any()) return Enumerable.Empty<User>();

    const string sql = @"
        SELECT id, username, email, avatar_url, status
        FROM users 
        WHERE username IN @Usernames AND status = 'active'";

    await using var connection = new MySqlConnection(_connectionString);
    return await connection.QueryAsync<User>(sql, new { Usernames = usernames });
}
```

### 验证要点

- [ ] @ 提及正确识别用户名
- [ ] 通知发送给正确的用户
- [ ] 回复通知正常触发
- [ ] 不给自己发送通知

---

## Day 3: 前端通知 UI 组件

### 通知下拉组件

```tsx
// components/notifications/NotificationDropdown.tsx
import { useState, useEffect } from 'react';
import { Bell, BellRing } from 'lucide-react';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { ScrollArea } from '@/components/ui/scroll-area';
import { useNotifications } from '@/hooks/useNotifications';
import { NotificationItem } from './NotificationItem';

export function NotificationDropdown() {
  const { 
    notifications, 
    unreadCount, 
    isLoading, 
    markAsRead,
    refetch 
  } = useNotifications();
  
  const [isOpen, setIsOpen] = useState(false);

  useEffect(() => {
    // 监听实时通知
    const handleNotification = (notification: any) => {
      refetch();
    };

    // SignalR 连接监听
    const connection = getSignalRConnection();
    connection?.on('NotificationReceived', handleNotification);

    return () => {
      connection?.off('NotificationReceived', handleNotification);
    };
  }, [refetch]);

  const handleItemClick = async (notificationId: number) => {
    await markAsRead(notificationId);
    setIsOpen(false);
  };

  return (
    <DropdownMenu open={isOpen} onOpenChange={setIsOpen}>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="sm" className="relative">
          {unreadCount > 0 ? (
            <BellRing className="h-5 w-5" />
          ) : (
            <Bell className="h-5 w-5" />
          )}
          {unreadCount > 0 && (
            <Badge 
              variant="destructive" 
              className="absolute -top-1 -right-1 h-5 w-5 p-0 text-xs"
            >
              {unreadCount > 99 ? '99+' : unreadCount}
            </Badge>
          )}
        </Button>
      </DropdownMenuTrigger>
      
      <DropdownMenuContent align="end" className="w-80">
        <div className="p-2">
          <h3 className="font-medium text-sm mb-2">通知</h3>
          {isLoading ? (
            <div className="text-center py-4 text-muted-foreground">
              加载中...
            </div>
          ) : notifications.length === 0 ? (
            <div className="text-center py-4 text-muted-foreground">
              暂无通知
            </div>
          ) : (
            <ScrollArea className="h-80">
              <div className="space-y-1">
                {notifications.map((notification) => (
                  <NotificationItem
                    key={notification.id}
                    notification={notification}
                    onClick={() => handleItemClick(notification.id)}
                  />
                ))}
              </div>
            </ScrollArea>
          )}
        </div>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
```

### 通知项组件

```tsx
// components/notifications/NotificationItem.tsx
import { formatDistanceToNow } from 'date-fns';
import { zhCN } from 'date-fns/locale';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { cn } from '@/lib/utils';

interface NotificationItemProps {
  notification: {
    id: number;
    type: 'mention' | 'reply' | 'system';
    snippet: string;
    readAt: string | null;
    createdAt: string;
    topicId?: number;
    topicTitle?: string;
    postId?: number;
    byUserId?: number;
    byUsername?: string;
    byAvatarUrl?: string;
  };
  onClick: () => void;
}

export function NotificationItem({ notification, onClick }: NotificationItemProps) {
  const isUnread = !notification.readAt;
  
  const getNotificationText = () => {
    switch (notification.type) {
      case 'mention':
        return `${notification.byUsername} 在主题中提到了你`;
      case 'reply':
        return `${notification.byUsername} 回复了你的主题`;
      case 'system':
        return '系统通知';
      default:
        return '';
    }
  };

  const handleClick = () => {
    onClick();
    // 跳转到对应帖子
    if (notification.topicId && notification.postId) {
      window.location.href = `/t/${notification.topicId}#post-${notification.postId}`;
    }
  };

  return (
    <div
      className={cn(
        "flex gap-3 p-3 rounded-lg cursor-pointer hover:bg-accent transition-colors",
        isUnread && "bg-accent/50"
      )}
      onClick={handleClick}
    >
      <Avatar className="h-8 w-8">
        <AvatarImage src={notification.byAvatarUrl} />
        <AvatarFallback>
          {notification.byUsername?.charAt(0).toUpperCase()}
        </AvatarFallback>
      </Avatar>
      
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2">
          <p className="text-sm font-medium truncate">
            {getNotificationText()}
          </p>
          {isUnread && (
            <Badge variant="secondary" className="h-2 w-2 p-0 rounded-full" />
          )}
        </div>
        
        {notification.topicTitle && (
          <p className="text-sm text-muted-foreground truncate">
            {notification.topicTitle}
          </p>
        )}
        
        <p className="text-xs text-muted-foreground mt-1 line-clamp-2">
          {notification.snippet}
        </p>
        
        <p className="text-xs text-muted-foreground mt-1">
          {formatDistanceToNow(new Date(notification.createdAt), {
            addSuffix: true,
            locale: zhCN
          })}
        </p>
      </div>
    </div>
  );
}
```

### 通知 Hook

```tsx
// hooks/useNotifications.ts
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/api';

export function useNotifications() {
  const queryClient = useQueryClient();

  const { data: notifications = [], isLoading } = useQuery({
    queryKey: ['notifications'],
    queryFn: () => api.get('/notifications').then(res => res.data.data),
    refetchInterval: 30000, // 30秒轮询
  });

  const { data: unreadCount = 0 } = useQuery({
    queryKey: ['notifications', 'unread-count'],
    queryFn: () => api.get('/notifications/unread-count').then(res => res.data.count),
    refetchInterval: 10000, // 10秒轮询
  });

  const markAsReadMutation = useMutation({
    mutationFn: (notificationId: number) =>
      api.post(`/notifications/${notificationId}/read`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
      queryClient.invalidateQueries({ queryKey: ['notifications', 'unread-count'] });
    },
  });

  return {
    notifications,
    unreadCount,
    isLoading,
    markAsRead: markAsReadMutation.mutate,
    refetch: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
      queryClient.invalidateQueries({ queryKey: ['notifications', 'unread-count'] });
    },
  };
}
```

### 验证要点

- [ ] 通知下拉正确显示
- [ ] 未读数量显示准确
- [ ] 点击通知跳转到正确位置
- [ ] 实时通知正常接收

---

## Day 4: 管理后台基础架构

### 权限验证中间件

```csharp
// Attributes/RequireRoleAttribute.cs
public class RequireRoleAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _roles;

    public RequireRoleAttribute(params string[] roles)
    {
        _roles = roles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (!user.Identity?.IsAuthenticated == true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var userRoles = user.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        if (!_roles.Any(role => userRoles.Contains(role)))
        {
            context.Result = new ForbidResult();
        }
    }
}
```

### 用户管理控制器

```csharp
// Controllers/Admin/UsersController.cs
[ApiController]
[Route("api/v1/admin/users")]
[RequireRole("admin")]
public class AdminUsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditService _auditService;

    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1, 
        [FromQuery] int size = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null)
    {
        var result = await _userRepository.GetUsersAsync(new UserQuery
        {
            Page = page,
            Size = size,
            Search = search,
            Status = status
        });

        return Ok(result);
    }

    [HttpPost("{id}/suspend")]
    public async Task<IActionResult> SuspendUser(long id, [FromBody] SuspendUserRequest request)
    {
        var currentUserId = GetCurrentUserId();
        var user = await _userRepository.GetAsync(id);
        
        if (user == null)
            return NotFound();

        var oldStatus = user.Status;
        await _userRepository.UpdateStatusAsync(id, UserStatus.Suspended);
        
        await _auditService.LogAsync(new AuditLog
        {
            ActorUserId = currentUserId,
            Action = "user.suspend",
            TargetType = "user",
            TargetId = id,
            BeforeJson = JsonSerializer.Serialize(new { Status = oldStatus }),
            AfterJson = JsonSerializer.Serialize(new { Status = "suspended", Reason = request.Reason }),
        });

        return NoContent();
    }

    [HttpPost("{id}/activate")]
    public async Task<IActionResult> ActivateUser(long id)
    {
        var currentUserId = GetCurrentUserId();
        var user = await _userRepository.GetAsync(id);
        
        if (user == null)
            return NotFound();

        var oldStatus = user.Status;
        await _userRepository.UpdateStatusAsync(id, UserStatus.Active);
        
        await _auditService.LogAsync(new AuditLog
        {
            ActorUserId = currentUserId,
            Action = "user.activate",
            TargetType = "user",
            TargetId = id,
            BeforeJson = JsonSerializer.Serialize(new { Status = oldStatus }),
            AfterJson = JsonSerializer.Serialize(new { Status = "active" }),
        });

        return NoContent();
    }
}
```

### 分类管理控制器

```csharp
// Controllers/Admin/CategoriesController.cs
[ApiController]
[Route("api/v1/admin/categories")]
[RequireRole("admin", "mod")]
public class AdminCategoriesController : ControllerBase
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IAuditService _auditService;

    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _categoryRepository.GetAllAsync();
        return Ok(new { Data = categories });
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        var category = new Category
        {
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            Order = request.Order,
            CreatedAt = DateTime.UtcNow
        };

        var id = await _categoryRepository.CreateAsync(category);
        
        await _auditService.LogAsync(new AuditLog
        {
            ActorUserId = GetCurrentUserId(),
            Action = "category.create",
            TargetType = "category",
            TargetId = id,
            AfterJson = JsonSerializer.Serialize(category),
        });

        return CreatedAtAction(nameof(GetCategory), new { id }, new { Id = id });
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateCategory(long id, [FromBody] UpdateCategoryRequest request)
    {
        var existingCategory = await _categoryRepository.GetAsync(id);
        if (existingCategory == null)
            return NotFound();

        var beforeJson = JsonSerializer.Serialize(existingCategory);
        
        await _categoryRepository.UpdateAsync(id, new Category
        {
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            Order = request.Order,
            UpdatedAt = DateTime.UtcNow
        });

        var afterCategory = await _categoryRepository.GetAsync(id);
        
        await _auditService.LogAsync(new AuditLog
        {
            ActorUserId = GetCurrentUserId(),
            Action = "category.update",
            TargetType = "category",
            TargetId = id,
            BeforeJson = beforeJson,
            AfterJson = JsonSerializer.Serialize(afterCategory),
        });

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(long id)
    {
        var category = await _categoryRepository.GetAsync(id);
        if (category == null)
            return NotFound();

        // 检查是否有主题使用此分类
        var topicCount = await _categoryRepository.GetTopicCountAsync(id);
        if (topicCount > 0)
        {
            return BadRequest(new { Error = "Cannot delete category with existing topics" });
        }

        await _categoryRepository.DeleteAsync(id);
        
        await _auditService.LogAsync(new AuditLog
        {
            ActorUserId = GetCurrentUserId(),
            Action = "category.delete",
            TargetType = "category",
            TargetId = id,
            BeforeJson = JsonSerializer.Serialize(category),
        });

        return NoContent();
    }
}
```

### 验证要点

- [ ] 权限验证正确拦截
- [ ] 用户列表和搜索功能正常
- [ ] 用户封禁/解封功能正常
- [ ] 分类 CRUD 操作正常
- [ ] 审计日志正确记录

---

## Day 5: 管理后台前端界面

### 管理后台布局

```tsx
// components/admin/AdminLayout.tsx
import { useState } from 'react';
import { Outlet, Link, useLocation } from 'react-router-dom';
import { Users, FolderOpen, Tags, BarChart3, Settings } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Sheet, SheetContent, SheetTrigger } from '@/components/ui/sheet';

const navigation = [
  { name: '用户管理', href: '/admin/users', icon: Users },
  { name: '分类管理', href: '/admin/categories', icon: FolderOpen },
  { name: '标签管理', href: '/admin/tags', icon: Tags },
  { name: '数据统计', href: '/admin/stats', icon: BarChart3 },
  { name: '系统设置', href: '/admin/settings', icon: Settings },
];

export function AdminLayout() {
  const location = useLocation();
  const [sidebarOpen, setSidebarOpen] = useState(false);

  const NavItems = () => (
    <>
      {navigation.map((item) => {
        const Icon = item.icon;
        return (
          <Link
            key={item.name}
            to={item.href}
            className={cn(
              "flex items-center gap-3 px-3 py-2 text-sm font-medium rounded-md transition-colors",
              location.pathname === item.href
                ? "bg-accent text-accent-foreground"
                : "text-muted-foreground hover:text-foreground hover:bg-accent"
            )}
          >
            <Icon className="h-4 w-4" />
            {item.name}
          </Link>
        );
      })}
    </>
  );

  return (
    <div className="flex h-screen bg-background">
      {/* Desktop Sidebar */}
      <div className="hidden md:flex md:w-64 md:flex-col">
        <div className="flex flex-col flex-1 min-h-0 border-r bg-card">
          <div className="flex flex-col flex-1 pt-5 pb-4 overflow-y-auto">
            <div className="flex items-center flex-shrink-0 px-4">
              <h1 className="text-lg font-semibold">管理后台</h1>
            </div>
            <nav className="mt-5 flex-1 px-2 space-y-1">
              <NavItems />
            </nav>
          </div>
        </div>
      </div>

      {/* Mobile Sidebar */}
      <Sheet open={sidebarOpen} onOpenChange={setSidebarOpen}>
        <SheetTrigger asChild>
          <Button variant="outline" size="sm" className="md:hidden">
            菜单
          </Button>
        </SheetTrigger>
        <SheetContent side="left" className="w-64">
          <div className="flex flex-col h-full">
            <div className="flex items-center px-4 py-6">
              <h1 className="text-lg font-semibold">管理后台</h1>
            </div>
            <nav className="flex-1 px-2 space-y-1">
              <NavItems />
            </nav>
          </div>
        </SheetContent>
      </Sheet>

      {/* Main Content */}
      <div className="flex flex-col flex-1 overflow-hidden">
        <main className="flex-1 relative overflow-y-auto focus:outline-none">
          <div className="py-6">
            <div className="max-w-7xl mx-auto px-4 sm:px-6 md:px-8">
              <Outlet />
            </div>
          </div>
        </main>
      </div>
    </div>
  );
}
```

### 用户管理页面

```tsx
// pages/admin/Users.tsx
import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { MoreHorizontal, Search, UserX, UserCheck } from 'lucide-react';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { useToast } from '@/hooks/use-toast';
import { adminApi } from '@/lib/admin-api';

export function UsersPage() {
  const [search, setSearch] = useState('');
  const { toast } = useToast();
  const queryClient = useQueryClient();

  const { data: users = [], isLoading } = useQuery({
    queryKey: ['admin', 'users', search],
    queryFn: () => adminApi.getUsers({ search }),
  });

  const suspendMutation = useMutation({
    mutationFn: ({ userId, reason }: { userId: number; reason: string }) =>
      adminApi.suspendUser(userId, reason),
    onSuccess: () => {
      toast({ title: '用户已封禁' });
      queryClient.invalidateQueries({ queryKey: ['admin', 'users'] });
    },
  });

  const activateMutation = useMutation({
    mutationFn: (userId: number) => adminApi.activateUser(userId),
    onSuccess: () => {
      toast({ title: '用户已激活' });
      queryClient.invalidateQueries({ queryKey: ['admin', 'users'] });
    },
  });

  const handleSuspend = (userId: number) => {
    const reason = prompt('请输入封禁原因：');
    if (reason) {
      suspendMutation.mutate({ userId, reason });
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">用户管理</h1>
      </div>

      <div className="flex items-center space-x-2">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="搜索用户名或邮箱..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="pl-8"
          />
        </div>
      </div>

      <div className="border rounded-lg">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>用户</TableHead>
              <TableHead>邮箱</TableHead>
              <TableHead>状态</TableHead>
              <TableHead>角色</TableHead>
              <TableHead>注册时间</TableHead>
              <TableHead>最后活跃</TableHead>
              <TableHead className="w-[50px]"></TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableRow>
                <TableCell colSpan={7} className="text-center py-4">
                  加载中...
                </TableCell>
              </TableRow>
            ) : users.length === 0 ? (
              <TableRow>
                <TableCell colSpan={7} className="text-center py-4">
                  暂无用户
                </TableCell>
              </TableRow>
            ) : (
              users.map((user: any) => (
                <TableRow key={user.id}>
                  <TableCell>
                    <div className="flex items-center gap-3">
                      <Avatar className="h-8 w-8">
                        <AvatarImage src={user.avatarUrl} />
                        <AvatarFallback>
                          {user.username.charAt(0).toUpperCase()}
                        </AvatarFallback>
                      </Avatar>
                      <span className="font-medium">{user.username}</span>
                    </div>
                  </TableCell>
                  <TableCell>{user.email}</TableCell>
                  <TableCell>
                    <Badge
                      variant={user.status === 'active' ? 'default' : 'destructive'}
                    >
                      {user.status === 'active' ? '正常' : '已封禁'}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    <div className="flex gap-1">
                      {user.roles.map((role: string) => (
                        <Badge key={role} variant="outline">
                          {role === 'admin' ? '管理员' : role === 'mod' ? '版主' : '用户'}
                        </Badge>
                      ))}
                    </div>
                  </TableCell>
                  <TableCell>
                    {new Date(user.createdAt).toLocaleDateString()}
                  </TableCell>
                  <TableCell>
                    {user.lastSeenAt 
                      ? new Date(user.lastSeenAt).toLocaleDateString()
                      : '从未活跃'
                    }
                  </TableCell>
                  <TableCell>
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild>
                        <Button variant="ghost" size="sm">
                          <MoreHorizontal className="h-4 w-4" />
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end">
                        {user.status === 'active' ? (
                          <DropdownMenuItem
                            onClick={() => handleSuspend(user.id)}
                            className="text-destructive"
                          >
                            <UserX className="mr-2 h-4 w-4" />
                            封禁用户
                          </DropdownMenuItem>
                        ) : (
                          <DropdownMenuItem
                            onClick={() => activateMutation.mutate(user.id)}
                          >
                            <UserCheck className="mr-2 h-4 w-4" />
                            解封用户
                          </DropdownMenuItem>
                        )}
                      </DropdownMenuContent>
                    </DropdownMenu>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}
```

### 分类管理页面

```tsx
// pages/admin/Categories.tsx
import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, Edit, Trash2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import { CategoryForm } from '@/components/admin/CategoryForm';
import { useToast } from '@/hooks/use-toast';
import { adminApi } from '@/lib/admin-api';

export function CategoriesPage() {
  const [editingCategory, setEditingCategory] = useState<any>(null);
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const { toast } = useToast();
  const queryClient = useQueryClient();

  const { data: categories = [], isLoading } = useQuery({
    queryKey: ['admin', 'categories'],
    queryFn: () => adminApi.getCategories(),
  });

  const deleteMutation = useMutation({
    mutationFn: (categoryId: number) => adminApi.deleteCategory(categoryId),
    onSuccess: () => {
      toast({ title: '分类已删除' });
      queryClient.invalidateQueries({ queryKey: ['admin', 'categories'] });
    },
    onError: (error: any) => {
      toast({
        title: '删除失败',
        description: error.response?.data?.error || '未知错误',
        variant: 'destructive',
      });
    },
  });

  const handleDelete = (category: any) => {
    if (confirm(`确定要删除分类"${category.name}"吗？`)) {
      deleteMutation.mutate(category.id);
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">分类管理</h1>
        <Dialog open={isCreateOpen} onOpenChange={setIsCreateOpen}>
          <DialogTrigger asChild>
            <Button>
              <Plus className="mr-2 h-4 w-4" />
              新建分类
            </Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>创建分类</DialogTitle>
            </DialogHeader>
            <CategoryForm
              onSuccess={() => {
                setIsCreateOpen(false);
                queryClient.invalidateQueries({ queryKey: ['admin', 'categories'] });
              }}
            />
          </DialogContent>
        </Dialog>
      </div>

      <div className="border rounded-lg">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>名称</TableHead>
              <TableHead>Slug</TableHead>
              <TableHead>描述</TableHead>
              <TableHead>排序</TableHead>
              <TableHead>状态</TableHead>
              <TableHead>主题数量</TableHead>
              <TableHead className="w-[100px]">操作</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableRow>
                <TableCell colSpan={7} className="text-center py-4">
                  加载中...
                </TableCell>
              </TableRow>
            ) : categories.length === 0 ? (
              <TableRow>
                <TableCell colSpan={7} className="text-center py-4">
                  暂无分类
                </TableCell>
              </TableRow>
            ) : (
              categories.map((category: any) => (
                <TableRow key={category.id}>
                  <TableCell className="font-medium">{category.name}</TableCell>
                  <TableCell className="font-mono text-sm">
                    {category.slug}
                  </TableCell>
                  <TableCell>{category.description}</TableCell>
                  <TableCell>{category.order}</TableCell>
                  <TableCell>
                    <Badge variant={category.isArchived ? 'secondary' : 'default'}>
                      {category.isArchived ? '已归档' : '正常'}
                    </Badge>
                  </TableCell>
                  <TableCell>{category.topicCount || 0}</TableCell>
                  <TableCell>
                    <div className="flex items-center gap-2">
                      <Dialog>
                        <DialogTrigger asChild>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => setEditingCategory(category)}
                          >
                            <Edit className="h-4 w-4" />
                          </Button>
                        </DialogTrigger>
                        <DialogContent>
                          <DialogHeader>
                            <DialogTitle>编辑分类</DialogTitle>
                          </DialogHeader>
                          <CategoryForm
                            category={editingCategory}
                            onSuccess={() => {
                              setEditingCategory(null);
                              queryClient.invalidateQueries({ queryKey: ['admin', 'categories'] });
                            }}
                          />
                        </DialogContent>
                      </Dialog>
                      
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleDelete(category)}
                        disabled={category.topicCount > 0}
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}
```

### 验证要点

- [ ] 管理后台布局正确显示
- [ ] 用户管理功能正常
- [ ] 分类管理功能正常
- [ ] 权限控制有效
- [ ] 操作反馈清晰

---

## Day 6: 内容审核与权限细化

### 内容举报系统

```csharp
// Models/Report.cs
public class Report
{
    public long Id { get; set; }
    public long ReporterId { get; set; }
    public string TargetType { get; set; } // "topic" | "post"
    public long TargetId { get; set; }
    public string Reason { get; set; }
    public string Description { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.Pending;
    public long? HandlerId { get; set; }
    public string? HandlerAction { get; set; }
    public string? HandlerNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? HandledAt { get; set; }
}

public enum ReportStatus
{
    Pending,
    Resolved,
    Dismissed
}
```

### 举报控制器

```csharp
// Controllers/ReportsController.cs
[ApiController]
[Route("api/v1/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateReport([FromBody] CreateReportRequest request)
    {
        var report = new Report
        {
            ReporterId = GetCurrentUserId(),
            TargetType = request.TargetType,
            TargetId = request.TargetId,
            Reason = request.Reason,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow
        };

        var id = await _reportRepository.CreateAsync(report);
        return CreatedAtAction(nameof(GetReport), new { id }, new { Id = id });
    }
}

// Controllers/Admin/ReportsController.cs
[ApiController]
[Route("api/v1/admin/reports")]
[RequireRole("admin", "mod")]
public class AdminReportsController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetReports([FromQuery] ReportStatus? status = null)
    {
        var reports = await _reportRepository.GetReportsAsync(status);
        return Ok(new { Data = reports });
    }

    [HttpPost("{id}/resolve")]
    public async Task<IActionResult> ResolveReport(long id, [FromBody] ResolveReportRequest request)
    {
        var currentUserId = GetCurrentUserId();
        await _reportRepository.ResolveAsync(id, currentUserId, request.Action, request.Note);
        
        // 执行相应的处理动作
        switch (request.Action)
        {
            case "delete_content":
                await HandleDeleteContent(request.TargetType, request.TargetId);
                break;
            case "warn_user":
                await HandleWarnUser(request.TargetUserId, request.Note);
                break;
            case "suspend_user":
                await HandleSuspendUser(request.TargetUserId, request.Note);
                break;
        }

        return NoContent();
    }
}
```

### 版主权限管理

```csharp
// Services/PermissionService.cs
public class PermissionService
{
    public async Task<bool> CanModerateTopicAsync(long userId, long topicId)
    {
        var userRoles = await GetUserRolesAsync(userId);
        
        // 管理员可以管理所有内容
        if (userRoles.Contains("admin"))
            return true;

        // 版主可以管理分配的分类
        if (userRoles.Contains("mod"))
        {
            var topic = await _topicRepository.GetAsync(topicId);
            if (topic != null)
            {
                return await IsCategoryModeratorAsync(userId, topic.CategoryId);
            }
        }

        return false;
    }

    public async Task<bool> CanEditPostAsync(long userId, long postId)
    {
        var post = await _postRepository.GetAsync(postId);
        if (post == null) return false;

        // 作者在限时内可以编辑
        if (post.AuthorId == userId)
        {
            var editWindow = TimeSpan.FromMinutes(10);
            return DateTime.UtcNow - post.CreatedAt <= editWindow;
        }

        // 版主和管理员可以编辑
        return await CanModerateTopicAsync(userId, post.TopicId);
    }

    private async Task<bool> IsCategoryModeratorAsync(long userId, long categoryId)
    {
        const string sql = @"
            SELECT COUNT(*) FROM category_moderators 
            WHERE user_id = @UserId AND category_id = @CategoryId";

        await using var connection = new MySqlConnection(_connectionString);
        var count = await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId, CategoryId = categoryId });
        return count > 0;
    }
}
```

### 前端举报组件

```tsx
// components/ReportDialog.tsx
import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { Flag } from 'lucide-react';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';
import { Textarea } from '@/components/ui/textarea';
import { useToast } from '@/hooks/use-toast';
import { api } from '@/lib/api';

interface ReportDialogProps {
  targetType: 'topic' | 'post';
  targetId: number;
  children?: React.ReactNode;
}

const reportReasons = [
  { value: 'spam', label: '垃圾信息' },
  { value: 'harassment', label: '骚扰他人' },
  { value: 'inappropriate', label: '不当内容' },
  { value: 'copyright', label: '版权侵犯' },
  { value: 'other', label: '其他' },
];

export function ReportDialog({ targetType, targetId, children }: ReportDialogProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [reason, setReason] = useState('');
  const [description, setDescription] = useState('');
  const { toast } = useToast();

  const reportMutation = useMutation({
    mutationFn: () => api.post('/reports', {
      targetType,
      targetId,
      reason,
      description,
    }),
    onSuccess: () => {
      toast({ title: '举报已提交，我们会尽快处理' });
      setIsOpen(false);
      setReason('');
      setDescription('');
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!reason) return;
    reportMutation.mutate();
  };

  return (
    <Dialog open={isOpen} onOpenChange={setIsOpen}>
      <DialogTrigger asChild>
        {children || (
          <Button variant="ghost" size="sm">
            <Flag className="h-4 w-4 mr-2" />
            举报
          </Button>
        )}
      </DialogTrigger>
      
      <DialogContent>
        <DialogHeader>
          <DialogTitle>举报内容</DialogTitle>
        </DialogHeader>
        
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <Label>举报原因</Label>
            <RadioGroup value={reason} onValueChange={setReason} className="mt-2">
              {reportReasons.map((option) => (
                <div key={option.value} className="flex items-center space-x-2">
                  <RadioGroupItem value={option.value} id={option.value} />
                  <Label htmlFor={option.value}>{option.label}</Label>
                </div>
              ))}
            </RadioGroup>
          </div>
          
          <div>
            <Label htmlFor="description">详细说明（可选）</Label>
            <Textarea
              id="description"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="请描述具体问题..."
              className="mt-1"
            />
          </div>
          
          <div className="flex justify-end gap-2">
            <Button 
              type="button" 
              variant="outline" 
              onClick={() => setIsOpen(false)}
            >
              取消
            </Button>
            <Button 
              type="submit" 
              disabled={!reason || reportMutation.isPending}
            >
              提交举报
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
```

### 验证要点

- [ ] 举报功能正常工作
- [ ] 版主权限正确控制
- [ ] 内容审核流程完整
- [ ] 编辑时间窗口生效

---

## Day 7: 邮件通知与最终测试

### 邮件通知服务

```csharp
// Services/EmailService.cs
public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public async Task SendMentionNotificationAsync(string toEmail, Post post)
    {
        var topic = await _topicRepository.GetAsync(post.TopicId);
        var author = await _userRepository.GetAsync(post.AuthorId);
        
        var subject = $"你在主题「{topic.Title}」中被提及";
        var body = GenerateMentionEmailBody(author.Username, topic.Title, post.ContentMd, GetTopicUrl(topic.Id, post.Id));
        
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendReplyNotificationAsync(string toEmail, Post post)
    {
        var topic = await _topicRepository.GetAsync(post.TopicId);
        var author = await _userRepository.GetAsync(post.AuthorId);
        
        var subject = $"{author.Username} 回复了你的主题「{topic.Title}」";
        var body = GenerateReplyEmailBody(author.Username, topic.Title, post.ContentMd, GetTopicUrl(topic.Id, post.Id));
        
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendVerificationEmailAsync(string toEmail, string token)
    {
        var subject = "验证您的邮箱地址";
        var verifyUrl = $"{_settings.BaseUrl}/auth/verify?token={token}";
        var body = GenerateVerificationEmailBody(verifyUrl);
        
        await SendEmailAsync(toEmail, subject, body);
    }

    private async Task SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port, _settings.UseSsl);
            
            if (!string.IsNullOrEmpty(_settings.Username))
            {
                await client.AuthenticateAsync(_settings.Username, _settings.Password);
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(new MailboxAddress("", to));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = body };

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            
            _logger.LogInformation("Email sent successfully to {Email}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", to);
            throw;
        }
    }

    private string GenerateMentionEmailBody(string authorName, string topicTitle, string content, string url)
    {
        return $@"
        <html>
        <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
            <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                <h2>你在论坛中被提及</h2>
                <p><strong>{authorName}</strong> 在主题 <strong>「{topicTitle}」</strong> 中提到了你：</p>
                <div style='background: #f5f5f5; padding: 15px; border-left: 4px solid #007bff; margin: 20px 0;'>
                    {TruncateContent(content, 200)}
                </div>
                <p><a href='{url}' style='background: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>查看完整内容</a></p>
                <hr style='margin: 30px 0; border: none; border-top: 1px solid #ddd;'>
                <p style='color: #666; font-size: 12px;'>
                    如果你不想接收此类邮件，可以在个人设置中关闭邮件通知。
                </p>
            </div>
        </body>
        </html>";
    }
}
```

### 用户邮件偏好设置

```csharp
// Models/UserEmailPreferences.cs
public class UserEmailPreferences
{
    public long UserId { get; set; }
    public bool MentionNotification { get; set; } = true;
    public bool ReplyNotification { get; set; } = true;
    public bool SystemNotification { get; set; } = true;
    public DateTime UpdatedAt { get; set; }
}

// Controllers/UserController.cs 添加方法
[HttpGet("email-preferences")]
public async Task<IActionResult> GetEmailPreferences()
{
    var userId = GetCurrentUserId();
    var preferences = await _userRepository.GetEmailPreferencesAsync(userId);
    return Ok(preferences);
}

[HttpPatch("email-preferences")]
public async Task<IActionResult> UpdateEmailPreferences([FromBody] UpdateEmailPreferencesRequest request)
{
    var userId = GetCurrentUserId();
    await _userRepository.UpdateEmailPreferencesAsync(userId, new UserEmailPreferences
    {
        MentionNotification = request.MentionNotification,
        ReplyNotification = request.ReplyNotification,
        SystemNotification = request.SystemNotification,
        UpdatedAt = DateTime.UtcNow
    });
    
    return NoContent();
}
```

### 前端邮件偏好设置

```tsx
// components/settings/EmailPreferences.tsx
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Switch } from '@/components/ui/switch';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { useToast } from '@/hooks/use-toast';
import { api } from '@/lib/api';

export function EmailPreferences() {
  const { toast } = useToast();
  const queryClient = useQueryClient();

  const { data: preferences, isLoading } = useQuery({
    queryKey: ['user', 'email-preferences'],
    queryFn: () => api.get('/user/email-preferences').then(res => res.data),
  });

  const updateMutation = useMutation({
    mutationFn: (data: any) => api.patch('/user/email-preferences', data),
    onSuccess: () => {
      toast({ title: '设置已保存' });
      queryClient.invalidateQueries({ queryKey: ['user', 'email-preferences'] });
    },
  });

  const handleToggle = (field: string, value: boolean) => {
    updateMutation.mutate({
      ...preferences,
      [field]: value,
    });
  };

  if (isLoading) {
    return <div>加载中...</div>;
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>邮件通知设置</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="flex items-center justify-between">
          <Label htmlFor="mention-notification">@ 提及通知</Label>
          <Switch
            id="mention-notification"
            checked={preferences?.mentionNotification}
            onCheckedChange={(checked) => handleToggle('mentionNotification', checked)}
          />
        </div>
        
        <div className="flex items-center justify-between">
          <Label htmlFor="reply-notification">回复通知</Label>
          <Switch
            id="reply-notification"
            checked={preferences?.replyNotification}
            onCheckedChange={(checked) => handleToggle('replyNotification', checked)}
          />
        </div>
        
        <div className="flex items-center justify-between">
          <Label htmlFor="system-notification">系统通知</Label>
          <Switch
            id="system-notification"
            checked={preferences?.systemNotification}
            onCheckedChange={(checked) => handleToggle('systemNotification', checked)}
          />
        </div>
      </CardContent>
    </Card>
  );
}
```

### 最终集成测试

```bash
# 创建测试脚本
# scripts/test-notifications.sh
#!/bin/bash

echo "=== M5 通知系统测试 ==="

echo "1. 测试 @ 提及通知..."
curl -X POST http://localhost:4000/api/v1/topics/1/posts \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"contentMd": "测试 @testuser 提及功能"}'

echo "2. 测试回复通知..."
curl -X POST http://localhost:4000/api/v1/topics/1/posts \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"contentMd": "这是一个回复测试"}'

echo "3. 检查通知列表..."
curl -X GET http://localhost:4000/api/v1/notifications \
  -H "Authorization: Bearer $TOKEN"

echo "4. 检查未读计数..."
curl -X GET http://localhost:4000/api/v1/notifications/unread-count \
  -H "Authorization: Bearer $TOKEN"

echo "5. 测试管理员功能..."
curl -X GET http://localhost:4000/api/v1/admin/users \
  -H "Authorization: Bearer $ADMIN_TOKEN"

echo "=== 测试完成 ==="
```

### 验证检查清单

**通知系统验证**
- [ ] @ 提及通知正确生成和发送
- [ ] 回复通知正确触发
- [ ] 实时通知推送正常
- [ ] 邮件通知发送成功
- [ ] 通知列表正确显示
- [ ] 未读计数准确
- [ ] 标记已读功能正常

**管理功能验证**
- [ ] 管理后台访问权限正确
- [ ] 用户封禁/解封功能正常
- [ ] 分类管理 CRUD 操作正常
- [ ] 标签管理功能正常
- [ ] 举报系统工作正常
- [ ] 审计日志记录完整

**权限控制验证**
- [ ] 普通用户无法访问管理功能
- [ ] 版主权限正确限制在分配的分类
- [ ] 内容编辑时间窗口正确生效
- [ ] 锁定主题不能回帖
- [ ] 封禁用户不能发帖

**性能验证**
- [ ] 通知生成性能良好
- [ ] 邮件发送不阻塞主流程
- [ ] 管理后台响应速度正常
- [ ] 大量通知下系统稳定

---

## 交付物

### 后端代码
- NotificationService 和相关 Repository
- 管理后台 API 控制器
- 权限验证中间件
- 邮件服务实现
- 举报和审核系统

### 前端代码
- 通知下拉组件和相关 UI
- 管理后台完整界面
- 邮件偏好设置页面
- 举报对话框组件

### 数据库更新
- 邮件偏好设置表
- 举报表结构
- 相关索引优化

### 文档
- 管理员操作手册
- API 文档更新
- 部署说明更新

## 风险与注意事项

1. **邮件发送稳定性**：配置可靠的 SMTP 服务，监控邮件发送状态
2. **通知性能**：大量通知时考虑异步处理和队列
3. **权限安全**：严格验证管理操作权限
4. **垃圾举报**：防止恶意举报，考虑举报频率限制

M5 完成后，论坛将具备完整的通知系统和基础管理功能，为正式运营做好准备。