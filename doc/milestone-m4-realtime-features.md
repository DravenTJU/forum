# M4: SignalR 实时功能详细实现步骤

**时间估算**: 1周 (5个工作日)  
**优先级**: 高 (核心实时体验)  
**负责人**: 全栈开发团队

## 📋 任务总览

- ✅ SignalR Hub 完整实现 (房间管理 + 事件广播)
- ✅ 实时帖子同步 (创建/编辑/删除)
- ✅ 用户输入状态指示 ("正在输入")
- ✅ 连接管理与重连机制
- ✅ 前端实时 UI 更新
- ✅ 性能优化与扩展方案

---

## 🌐 Day 1: SignalR Hub 完整实现

### 1.1 扩展 TopicsHub 功能

**`Hubs/TopicsHub.cs`** - 完整版本
```csharp
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Collections.Concurrent;
using Forum.Api.Services;

namespace Forum.Api.Hubs;

[Authorize]
public class TopicsHub : Hub
{
    private readonly ILogger<TopicsHub> _logger;
    private readonly IUserService _userService;
    private readonly ITopicService _topicService;
    
    // 用户连接映射 (UserId -> ConnectionIds)
    private static readonly ConcurrentDictionary<long, HashSet<string>> UserConnections = new();
    // 主题房间映射 (TopicId -> UserIds)
    private static readonly ConcurrentDictionary<long, HashSet<long>> TopicRooms = new();
    // 用户输入状态 (TopicId -> TypingUsers)
    private static readonly ConcurrentDictionary<long, ConcurrentDictionary<long, TypingInfo>> TypingUsers = new();

    public TopicsHub(ILogger<TopicsHub> logger, IUserService userService, ITopicService topicService)
    {
        _logger = logger;
        _userService = userService;
        _topicService = topicService;
    }

    public async Task JoinTopic(long topicId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return;

        // 验证主题存在且用户有权限访问
        var topic = await _topicService.GetTopicAsync(topicId);
        if (topic == null)
        {
            await Clients.Caller.SendAsync("Error", new { message = "主题不存在" });
            return;
        }

        var groupName = GetTopicGroupName(topicId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        // 更新用户连接映射
        UserConnections.AddOrUpdate(userId.Value, 
            new HashSet<string> { Context.ConnectionId },
            (key, existing) => { existing.Add(Context.ConnectionId); return existing; });

        // 更新主题房间映射
        TopicRooms.AddOrUpdate(topicId,
            new HashSet<long> { userId.Value },
            (key, existing) => { existing.Add(userId.Value); return existing; });

        // 获取当前在线用户列表
        var onlineUsers = await GetTopicOnlineUsersAsync(topicId);
        
        // 通知房间内其他用户有新用户加入
        await Clients.OthersInGroup(groupName).SendAsync("UserJoined", new
        {
            TopicId = topicId,
            UserId = userId.Value,
            Username = Context.User?.FindFirst(ClaimTypes.Name)?.Value,
            JoinedAt = DateTime.UtcNow,
            OnlineCount = onlineUsers.Count
        });

        // 向当前用户发送房间信息
        await Clients.Caller.SendAsync("TopicJoined", new
        {
            TopicId = topicId,
            OnlineUsers = onlineUsers,
            OnlineCount = onlineUsers.Count
        });

        _logger.LogInformation("User {UserId} joined topic {TopicId}", userId, topicId);
    }

    public async Task LeaveTopic(long topicId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return;

        var groupName = GetTopicGroupName(topicId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        // 更新映射
        if (UserConnections.TryGetValue(userId.Value, out var connections))
        {
            connections.Remove(Context.ConnectionId);
            if (!connections.Any())
            {
                UserConnections.TryRemove(userId.Value, out _);
            }
        }

        if (TopicRooms.TryGetValue(topicId, out var users))
        {
            users.Remove(userId.Value);
            if (!users.Any())
            {
                TopicRooms.TryRemove(topicId, out _);
                TypingUsers.TryRemove(topicId, out _);
            }
        }

        // 清除输入状态
        if (TypingUsers.TryGetValue(topicId, out var typingUsers))
        {
            typingUsers.TryRemove(userId.Value, out _);
        }

        // 通知其他用户
        var onlineUsers = await GetTopicOnlineUsersAsync(topicId);
        await Clients.OthersInGroup(groupName).SendAsync("UserLeft", new
        {
            TopicId = topicId,
            UserId = userId.Value,
            LeftAt = DateTime.UtcNow,
            OnlineCount = onlineUsers.Count
        });

        _logger.LogInformation("User {UserId} left topic {TopicId}", userId, topicId);
    }

    public async Task StartTyping(long topicId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return;

        var username = Context.User?.FindFirst(ClaimTypes.Name)?.Value;
        var groupName = GetTopicGroupName(topicId);

        // 更新输入状态
        var typingInfo = new TypingInfo
        {
            UserId = userId.Value,
            Username = username ?? "Unknown",
            StartedAt = DateTime.UtcNow
        };

        TypingUsers.AddOrUpdate(topicId,
            new ConcurrentDictionary<long, TypingInfo> { [userId.Value] = typingInfo },
            (key, existing) => { existing[userId.Value] = typingInfo; return existing; });

        // 通知其他用户
        await Clients.OthersInGroup(groupName).SendAsync("UserStartedTyping", new
        {
            TopicId = topicId,
            UserId = userId.Value,
            Username = username,
            StartedAt = typingInfo.StartedAt
        });

        _logger.LogDebug("User {UserId} started typing in topic {TopicId}", userId, topicId);
    }

    public async Task StopTyping(long topicId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return;

        var groupName = GetTopicGroupName(topicId);

        // 移除输入状态
        if (TypingUsers.TryGetValue(topicId, out var typingUsers))
        {
            typingUsers.TryRemove(userId.Value, out _);
        }

        // 通知其他用户
        await Clients.OthersInGroup(groupName).SendAsync("UserStoppedTyping", new
        {
            TopicId = topicId,
            UserId = userId.Value,
            StoppedAt = DateTime.UtcNow
        });

        _logger.LogDebug("User {UserId} stopped typing in topic {TopicId}", userId, topicId);
    }

    public async Task GetTypingUsers(long topicId)
    {
        if (TypingUsers.TryGetValue(topicId, out var typingUsers))
        {
            // 清理过期的输入状态 (超过30秒)
            var expiredUsers = typingUsers.Where(kvp => 
                DateTime.UtcNow - kvp.Value.StartedAt > TimeSpan.FromSeconds(30))
                .Select(kvp => kvp.Key).ToList();

            foreach (var expiredUserId in expiredUsers)
            {
                typingUsers.TryRemove(expiredUserId, out _);
            }

            var activeTypingUsers = typingUsers.Values.ToList();
            await Clients.Caller.SendAsync("TypingUsers", new
            {
                TopicId = topicId,
                Users = activeTypingUsers
            });
        }
    }

    public async Task RequestOnlineUsers(long topicId)
    {
        var onlineUsers = await GetTopicOnlineUsersAsync(topicId);
        await Clients.Caller.SendAsync("OnlineUsers", new
        {
            TopicId = topicId,
            Users = onlineUsers,
            Count = onlineUsers.Count
        });
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        var connectionId = Context.ConnectionId;

        if (userId.HasValue)
        {
            // 更新用户最后在线时间
            await _userService.UpdateLastSeenAsync(userId.Value);

            _logger.LogInformation("User {UserId} connected with connection {ConnectionId}", 
                userId, connectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetCurrentUserId();
        var connectionId = Context.ConnectionId;

        if (userId.HasValue)
        {
            // 清理用户连接
            if (UserConnections.TryGetValue(userId.Value, out var connections))
            {
                connections.Remove(connectionId);
                if (!connections.Any())
                {
                    UserConnections.TryRemove(userId.Value, out _);
                    
                    // 从所有主题房间移除用户
                    await RemoveUserFromAllTopicsAsync(userId.Value);
                }
            }

            _logger.LogInformation("User {UserId} disconnected from connection {ConnectionId}", 
                userId, connectionId);
        }

        if (exception != null)
        {
            _logger.LogWarning(exception, "Connection {ConnectionId} disconnected with error", connectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private async Task RemoveUserFromAllTopicsAsync(long userId)
    {
        var topicsToRemove = new List<long>();

        foreach (var (topicId, users) in TopicRooms)
        {
            if (users.Remove(userId))
            {
                var groupName = GetTopicGroupName(topicId);
                var onlineUsers = await GetTopicOnlineUsersAsync(topicId);
                
                await Clients.Group(groupName).SendAsync("UserLeft", new
                {
                    TopicId = topicId,
                    UserId = userId,
                    LeftAt = DateTime.UtcNow,
                    OnlineCount = onlineUsers.Count
                });

                if (!users.Any())
                {
                    topicsToRemove.Add(topicId);
                }
            }
        }

        // 清理空的主题房间
        foreach (var topicId in topicsToRemove)
        {
            TopicRooms.TryRemove(topicId, out _);
            TypingUsers.TryRemove(topicId, out _);
        }
    }

    private async Task<List<OnlineUserInfo>> GetTopicOnlineUsersAsync(long topicId)
    {
        if (!TopicRooms.TryGetValue(topicId, out var userIds))
        {
            return new List<OnlineUserInfo>();
        }

        var onlineUsers = new List<OnlineUserInfo>();
        foreach (var userId in userIds)
        {
            try
            {
                var user = await _userService.GetUserAsync(userId);
                if (user != null)
                {
                    onlineUsers.Add(new OnlineUserInfo
                    {
                        UserId = user.Id,
                        Username = user.Username,
                        AvatarUrl = user.AvatarUrl
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get user info for user {UserId}", userId);
            }
        }

        return onlineUsers;
    }

    private long? GetCurrentUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static string GetTopicGroupName(long topicId) => $"topic:{topicId}";
}

public class TypingInfo
{
    public long UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
}

public class OnlineUserInfo
{
    public long UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}
```

### 1.2 SignalR 服务增强

**`Services/ISignalRService.cs`** - 增强版本
```csharp
namespace Forum.Api.Services;

public interface ISignalRService
{
    // 帖子相关事件
    Task NotifyPostCreatedAsync(long topicId, PostCreatedPayload payload);
    Task NotifyPostEditedAsync(long topicId, PostEditedPayload payload);
    Task NotifyPostDeletedAsync(long topicId, PostDeletedPayload payload);
    
    // 主题相关事件
    Task NotifyTopicUpdatedAsync(long topicId, TopicUpdatedPayload payload);
    Task NotifyTopicStatsUpdatedAsync(long topicId, TopicStatsPayload payload);
    
    // 用户相关事件
    Task NotifyUserMentionedAsync(long userId, MentionPayload payload);
    Task NotifyUserPresenceAsync(long userId, UserPresencePayload payload);
    
    // 系统事件
    Task NotifySystemMessageAsync(string message, SystemMessageType type = SystemMessageType.Info);
    Task NotifyMaintenanceAsync(MaintenancePayload payload);
}

public enum SystemMessageType
{
    Info,
    Warning,
    Error,
    Maintenance
}
```

**`Services/SignalRService.cs`** - 增强版本
```csharp
using Microsoft.AspNetCore.SignalR;
using Forum.Api.Hubs;
using Forum.Api.Models.DTOs;

namespace Forum.Api.Services;

public class SignalRService : ISignalRService
{
    private readonly IHubContext<TopicsHub> _hubContext;
    private readonly ILogger<SignalRService> _logger;

    public SignalRService(IHubContext<TopicsHub> hubContext, ILogger<SignalRService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyPostCreatedAsync(long topicId, PostCreatedPayload payload)
    {
        var groupName = $"topic:{topicId}";
        
        try
        {
            await _hubContext.Clients.Group(groupName).SendAsync("PostCreated", new
            {
                TopicId = topicId,
                Post = payload,
                Timestamp = DateTime.UtcNow
            });

            _logger.LogDebug("Notified PostCreated for topic {TopicId}, post {PostId}", 
                topicId, payload.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify PostCreated for topic {TopicId}", topicId);
        }
    }

    public async Task NotifyPostEditedAsync(long topicId, PostEditedPayload payload)
    {
        var groupName = $"topic:{topicId}";
        
        try
        {
            await _hubContext.Clients.Group(groupName).SendAsync("PostEdited", new
            {
                TopicId = topicId,
                Post = payload,
                Timestamp = DateTime.UtcNow
            });

            _logger.LogDebug("Notified PostEdited for topic {TopicId}, post {PostId}", 
                topicId, payload.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify PostEdited for topic {TopicId}", topicId);
        }
    }

    public async Task NotifyPostDeletedAsync(long topicId, PostDeletedPayload payload)
    {
        var groupName = $"topic:{topicId}";
        
        try
        {
            await _hubContext.Clients.Group(groupName).SendAsync("PostDeleted", new
            {
                TopicId = topicId,
                PostId = payload.PostId,
                DeletedBy = payload.DeletedBy,
                Reason = payload.Reason,
                Timestamp = DateTime.UtcNow
            });

            _logger.LogDebug("Notified PostDeleted for topic {TopicId}, post {PostId}", 
                topicId, payload.PostId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify PostDeleted for topic {TopicId}", topicId);
        }
    }

    public async Task NotifyTopicUpdatedAsync(long topicId, TopicUpdatedPayload payload)
    {
        var groupName = $"topic:{topicId}";
        
        try
        {
            await _hubContext.Clients.Group(groupName).SendAsync("TopicUpdated", new
            {
                TopicId = topicId,
                Topic = payload,
                Timestamp = DateTime.UtcNow
            });

            _logger.LogDebug("Notified TopicUpdated for topic {TopicId}", topicId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify TopicUpdated for topic {TopicId}", topicId);
        }
    }

    public async Task NotifyTopicStatsUpdatedAsync(long topicId, TopicStatsPayload payload)
    {
        var groupName = $"topic:{topicId}";
        
        try
        {
            await _hubContext.Clients.Group(groupName).SendAsync("TopicStatsUpdated", new
            {
                TopicId = topicId,
                Stats = payload,
                Timestamp = DateTime.UtcNow
            });

            _logger.LogDebug("Notified TopicStatsUpdated for topic {TopicId}", topicId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify TopicStatsUpdated for topic {TopicId}", topicId);
        }
    }

    public async Task NotifyUserMentionedAsync(long userId, MentionPayload payload)
    {
        try
        {
            // 通知特定用户
            await _hubContext.Clients.User(userId.ToString()).SendAsync("UserMentioned", new
            {
                Mention = payload,
                Timestamp = DateTime.UtcNow
            });

            _logger.LogDebug("Notified user {UserId} of mention in post {PostId}", 
                userId, payload.PostId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify user {UserId} of mention", userId);
        }
    }

    public async Task NotifyUserPresenceAsync(long userId, UserPresencePayload payload)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("UserPresenceUpdated", new
            {
                UserId = userId,
                Presence = payload,
                Timestamp = DateTime.UtcNow
            });

            _logger.LogDebug("Notified user presence update for user {UserId}: {Status}", 
                userId, payload.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify user presence for user {UserId}", userId);
        }
    }

    public async Task NotifySystemMessageAsync(string message, SystemMessageType type = SystemMessageType.Info)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("SystemMessage", new
            {
                Message = message,
                Type = type.ToString().ToLower(),
                Timestamp = DateTime.UtcNow
            });

            _logger.LogInformation("Broadcasted system message: {Message} (Type: {Type})", message, type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast system message");
        }
    }

    public async Task NotifyMaintenanceAsync(MaintenancePayload payload)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("MaintenanceNotification", new
            {
                Maintenance = payload,
                Timestamp = DateTime.UtcNow
            });

            _logger.LogInformation("Broadcasted maintenance notification: {Type}", payload.Type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast maintenance notification");
        }
    }
}

// 负载类型定义
public class PostCreatedPayload
{
    public long Id { get; set; }
    public long TopicId { get; set; }
    public long AuthorId { get; set; }
    public string AuthorUsername { get; set; } = string.Empty;
    public string? AuthorAvatarUrl { get; set; }
    public string ContentHtml { get; set; } = string.Empty;
    public long? ReplyToPostId { get; set; }
    public int PostNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> MentionedUsers { get; set; } = new();
}

public class PostEditedPayload
{
    public long Id { get; set; }
    public long TopicId { get; set; }
    public string ContentHtml { get; set; } = string.Empty;
    public string? EditReason { get; set; }
    public long EditorId { get; set; }
    public string EditorUsername { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public bool IsEdited { get; set; } = true;
}

public class PostDeletedPayload
{
    public long PostId { get; set; }
    public long DeletedBy { get; set; }
    public string DeletedByUsername { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

public class TopicUpdatedPayload
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsPinned { get; set; }
    public bool IsLocked { get; set; }
    public string? EditReason { get; set; }
    public long? EditorId { get; set; }
    public string? EditorUsername { get; set; }
}

public class TopicStatsPayload
{
    public int ReplyCount { get; set; }
    public int ViewCount { get; set; }
    public DateTime? LastPostedAt { get; set; }
    public long? LastPosterId { get; set; }
    public string? LastPosterUsername { get; set; }
}

public class MentionPayload
{
    public long PostId { get; set; }
    public long TopicId { get; set; }
    public string TopicTitle { get; set; } = string.Empty;
    public long MentionedBy { get; set; }
    public string MentionedByUsername { get; set; } = string.Empty;
    public string ContentSnippet { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class UserPresencePayload
{
    public string Status { get; set; } = string.Empty; // online, away, offline
    public DateTime LastSeenAt { get; set; }
}

public class MaintenancePayload
{
    public string Type { get; set; } = string.Empty; // scheduled, emergency
    public string Message { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool AffectsPosting { get; set; }
}
```

---

## 🔄 Day 2: 实时帖子同步实现

### 2.1 集成 SignalR 到帖子服务

**`Services/PostService.cs`** - 添加实时广播
```csharp
// 在现有 PostService 中添加 SignalR 集成

public class PostService : IPostService
{
    private readonly IPostRepository _postRepository;
    private readonly ITopicRepository _topicRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMarkdownService _markdownService;
    private readonly ISignalRService _signalRService; // 新增
    private readonly ILogger<PostService> _logger;

    public PostService(
        IPostRepository postRepository,
        ITopicRepository topicRepository,
        IUserRepository userRepository,
        IMarkdownService markdownService,
        ISignalRService signalRService, // 新增
        ILogger<PostService> logger)
    {
        _postRepository = postRepository;
        _topicRepository = topicRepository;
        _userRepository = userRepository;
        _markdownService = markdownService;
        _signalRService = signalRService; // 新增
        _logger = logger;
    }

    public async Task<PostDto> CreatePostAsync(CreatePostRequest request, long authorId)
    {
        // 验证主题存在且未锁定
        var topic = await _topicRepository.GetByIdAsync(request.TopicId);
        if (topic == null)
            throw new ArgumentException("主题不存在");
        
        if (topic.IsLocked)
            throw new InvalidOperationException("主题已锁定，无法回帖");

        // 处理 Markdown 和提及
        var processedContent = _markdownService.ProcessMentions(request.ContentMd, new Dictionary<string, long>());
        var safeHtml = _markdownService.ToSafeHtml(processedContent);
        var mentionedUsernames = _markdownService.ExtractMentions(request.ContentMd);

        // 获取提及的用户ID
        var mentionedUserIds = new List<long>();
        if (mentionedUsernames.Any())
        {
            foreach (var username in mentionedUsernames)
            {
                var mentionedUser = await _userRepository.GetByUsernameAsync(username);
                if (mentionedUser != null)
                {
                    mentionedUserIds.Add(mentionedUser.Id);
                }
            }
        }

        var post = new Post
        {
            TopicId = request.TopicId,
            AuthorId = authorId,
            ContentMd = processedContent,
            RawContentMd = request.ContentMd,
            ReplyToPostId = request.ReplyToPostId
        };

        var postId = await _postRepository.CreateAsync(post, mentionedUserIds);
        post.Id = postId;

        // 获取作者信息和帖子编号
        var author = await _userRepository.GetByIdAsync(authorId);
        var postCount = await _postRepository.GetTopicPostCountAsync(request.TopicId);
        
        // **实时广播 - 新帖创建**
        var postCreatedPayload = new PostCreatedPayload
        {
            Id = postId,
            TopicId = request.TopicId,
            AuthorId = authorId,
            AuthorUsername = author?.Username ?? "Unknown",
            AuthorAvatarUrl = author?.AvatarUrl,
            ContentHtml = safeHtml,
            ReplyToPostId = request.ReplyToPostId,
            PostNumber = postCount,
            CreatedAt = DateTime.UtcNow,
            MentionedUsers = mentionedUsernames
        };

        await _signalRService.NotifyPostCreatedAsync(request.TopicId, postCreatedPayload);

        // 发送提及通知
        foreach (var mentionedUserId in mentionedUserIds)
        {
            var mentionPayload = new MentionPayload
            {
                PostId = postId,
                TopicId = request.TopicId,
                TopicTitle = topic.Title,
                MentionedBy = authorId,
                MentionedByUsername = author?.Username ?? "Unknown",
                ContentSnippet = _markdownService.ExtractPlainText(request.ContentMd, 100),
                Url = $"/t/{request.TopicId}#post-{postId}"
            };

            await _signalRService.NotifyUserMentionedAsync(mentionedUserId, mentionPayload);
        }

        // 更新主题统计并广播
        var statsPayload = new TopicStatsPayload
        {
            ReplyCount = topic.ReplyCount + 1,
            ViewCount = topic.ViewCount,
            LastPostedAt = DateTime.UtcNow,
            LastPosterId = authorId,
            LastPosterUsername = author?.Username
        };

        await _signalRService.NotifyTopicStatsUpdatedAsync(request.TopicId, statsPayload);

        _logger.LogInformation("Post created: {PostId} in topic {TopicId} by user {AuthorId}", 
            postId, request.TopicId, authorId);

        return MapToPostDto(post, author);
    }

    public async Task<PostDto> UpdatePostAsync(long id, UpdatePostRequest request, long editorId)
    {
        var post = await _postRepository.GetByIdAsync(id);
        if (post == null)
            throw new ArgumentException("帖子不存在");

        // 检查编辑权限
        var canEdit = post.AuthorId == editorId || post.CanEdit || await CanModeratePostAsync(editorId, post.TopicId);
        if (!canEdit)
            throw new UnauthorizedAccessException("没有编辑权限或编辑时间已过");

        var oldContent = post.ContentMd;
        var processedContent = _markdownService.ProcessMentions(request.ContentMd, new Dictionary<string, long>());
        var safeHtml = _markdownService.ToSafeHtml(processedContent);

        post.ContentMd = processedContent;
        post.RawContentMd = request.ContentMd;
        post.IsEdited = true;
        post.EditReason = request.EditReason;

        await _postRepository.UpdateAsync(post);

        // 获取编辑者信息
        var editor = await _userRepository.GetByIdAsync(editorId);

        // **实时广播 - 帖子编辑**
        var postEditedPayload = new PostEditedPayload
        {
            Id = id,
            TopicId = post.TopicId,
            ContentHtml = safeHtml,
            EditReason = request.EditReason,
            EditorId = editorId,
            EditorUsername = editor?.Username ?? "Unknown",
            UpdatedAt = DateTime.UtcNow,
            IsEdited = true
        };

        await _signalRService.NotifyPostEditedAsync(post.TopicId, postEditedPayload);

        _logger.LogInformation("Post edited: {PostId} by user {EditorId}", id, editorId);

        return MapToPostDto(post, editor);
    }

    public async Task DeletePostAsync(long id, long deleterId)
    {
        var post = await _postRepository.GetByIdAsync(id);
        if (post == null)
            throw new ArgumentException("帖子不存在");

        // 检查删除权限
        var canDelete = post.AuthorId == deleterId || await CanModeratePostAsync(deleterId, post.TopicId);
        if (!canDelete)
            throw new UnauthorizedAccessException("没有删除权限");

        await _postRepository.DeleteAsync(id, deleterId);

        // 获取删除者信息
        var deleter = await _userRepository.GetByIdAsync(deleterId);

        // **实时广播 - 帖子删除**
        var postDeletedPayload = new PostDeletedPayload
        {
            PostId = id,
            DeletedBy = deleterId,
            DeletedByUsername = deleter?.Username ?? "Unknown",
            Reason = "用户删除" // 可以扩展为支持删除原因
        };

        await _signalRService.NotifyPostDeletedAsync(post.TopicId, postDeletedPayload);

        // 更新主题统计
        var topic = await _topicRepository.GetByIdAsync(post.TopicId);
        if (topic != null)
        {
            var statsPayload = new TopicStatsPayload
            {
                ReplyCount = Math.Max(topic.ReplyCount - 1, 0),
                ViewCount = topic.ViewCount,
                LastPostedAt = topic.LastPostedAt,
                LastPosterId = topic.LastPosterId,
                LastPosterUsername = null // 简化处理
            };

            await _signalRService.NotifyTopicStatsUpdatedAsync(post.TopicId, statsPayload);
        }

        _logger.LogInformation("Post deleted: {PostId} by user {DeleterId}", id, deleterId);
    }

    private async Task<bool> CanModeratePostAsync(long userId, long topicId)
    {
        // 简化实现，实际应该检查用户角色和分类权限
        return false;
    }

    private static PostDto MapToPostDto(Post post, User? author)
    {
        return new PostDto
        {
            Id = post.Id,
            TopicId = post.TopicId,
            AuthorId = post.AuthorId,
            ContentMd = post.ContentMd,
            ReplyToPostId = post.ReplyToPostId,
            IsEdited = post.IsEdited,
            EditReason = post.EditReason,
            EditCount = post.EditCount,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            PostNumber = post.PostNumber,
            Author = author != null ? new UserSummaryDto
            {
                Id = author.Id,
                Username = author.Username,
                AvatarUrl = author.AvatarUrl
            } : null
        };
    }
}
```

---

## 💬 Day 3: 前端 SignalR 客户端

### 3.1 SignalR 连接管理

**`src/hooks/useSignalR.ts`**
```typescript
import { useEffect, useRef, useState, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';
import { useAuth } from './useAuth';
import { toast } from 'sonner';

export interface SignalRConnection {
  connection: signalR.HubConnection | null;
  connectionState: signalR.HubConnectionState;
  isConnected: boolean;
  isConnecting: boolean;
  joinTopic: (topicId: number) => Promise<void>;
  leaveTopic: (topicId: number) => Promise<void>;
  startTyping: (topicId: number) => Promise<void>;
  stopTyping: (topicId: number) => Promise<void>;
  getTypingUsers: (topicId: number) => Promise<void>;
  getOnlineUsers: (topicId: number) => Promise<void>;
}

export function useSignalR(): SignalRConnection {
  const { user, isAuthenticated } = useAuth();
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  const [connectionState, setConnectionState] = useState<signalR.HubConnectionState>(
    signalR.HubConnectionState.Disconnected
  );
  const reconnectTimeoutRef = useRef<NodeJS.Timeout>();
  const reconnectAttemptsRef = useRef(0);
  const maxReconnectAttempts = 5;

  const isConnected = connectionState === signalR.HubConnectionState.Connected;
  const isConnecting = connectionState === signalR.HubConnectionState.Connecting;

  const createConnection = useCallback(() => {
    if (!isAuthenticated) return null;

    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/topics', {
        accessTokenFactory: () => {
          return localStorage.getItem('accessToken') || '';
        },
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets,
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          // 指数退避策略
          const delay = Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
          console.log(`SignalR reconnect attempt ${retryContext.previousRetryCount + 1}, delay: ${delay}ms`);
          return delay;
        },
      })
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // 连接状态变化监听
    newConnection.onclose((error) => {
      console.log('SignalR connection closed', error);
      setConnectionState(signalR.HubConnectionState.Disconnected);
      
      if (error) {
        toast.error('实时连接断开，正在重新连接...');
      }
    });

    newConnection.onreconnecting((error) => {
      console.log('SignalR reconnecting', error);
      setConnectionState(signalR.HubConnectionState.Reconnecting);
      toast.info('正在重新连接实时服务...');
    });

    newConnection.onreconnected((connectionId) => {
      console.log('SignalR reconnected', connectionId);
      setConnectionState(signalR.HubConnectionState.Connected);
      reconnectAttemptsRef.current = 0;
      toast.success('实时连接已恢复');
    });

    return newConnection;
  }, [isAuthenticated]);

  const startConnection = useCallback(async () => {
    if (!connection || connectionState !== signalR.HubConnectionState.Disconnected) {
      return;
    }

    try {
      setConnectionState(signalR.HubConnectionState.Connecting);
      await connection.start();
      setConnectionState(signalR.HubConnectionState.Connected);
      reconnectAttemptsRef.current = 0;
      console.log('SignalR connected successfully');
    } catch (error) {
      console.error('SignalR connection failed:', error);
      setConnectionState(signalR.HubConnectionState.Disconnected);
      
      // 手动重连逻辑
      reconnectAttemptsRef.current++;
      if (reconnectAttemptsRef.current <= maxReconnectAttempts) {
        const delay = Math.min(1000 * Math.pow(2, reconnectAttemptsRef.current), 30000);
        console.log(`Scheduling reconnect attempt ${reconnectAttemptsRef.current} in ${delay}ms`);
        
        reconnectTimeoutRef.current = setTimeout(() => {
          startConnection();
        }, delay);
      } else {
        toast.error('无法连接到实时服务，请刷新页面重试');
      }
    }
  }, [connection, connectionState]);

  const stopConnection = useCallback(async () => {
    if (reconnectTimeoutRef.current) {
      clearTimeout(reconnectTimeoutRef.current);
    }

    if (connection && connectionState !== signalR.HubConnectionState.Disconnected) {
      try {
        await connection.stop();
        setConnectionState(signalR.HubConnectionState.Disconnected);
        console.log('SignalR connection stopped');
      } catch (error) {
        console.error('Error stopping SignalR connection:', error);
      }
    }
  }, [connection, connectionState]);

  // 初始化连接
  useEffect(() => {
    if (isAuthenticated && !connection) {
      const newConnection = createConnection();
      if (newConnection) {
        setConnection(newConnection);
      }
    } else if (!isAuthenticated && connection) {
      stopConnection();
      setConnection(null);
    }
  }, [isAuthenticated, connection, createConnection, stopConnection]);

  // 启动连接
  useEffect(() => {
    if (connection && connectionState === signalR.HubConnectionState.Disconnected) {
      startConnection();
    }
  }, [connection, connectionState, startConnection]);

  // 清理
  useEffect(() => {
    return () => {
      if (reconnectTimeoutRef.current) {
        clearTimeout(reconnectTimeoutRef.current);
      }
      stopConnection();
    };
  }, [stopConnection]);

  // Hub 方法包装
  const joinTopic = useCallback(async (topicId: number) => {
    if (!connection || !isConnected) {
      console.warn('Cannot join topic: SignalR not connected');
      return;
    }

    try {
      await connection.invoke('JoinTopic', topicId);
      console.log(`Joined topic ${topicId}`);
    } catch (error) {
      console.error(`Failed to join topic ${topicId}:`, error);
    }
  }, [connection, isConnected]);

  const leaveTopic = useCallback(async (topicId: number) => {
    if (!connection || !isConnected) {
      return;
    }

    try {
      await connection.invoke('LeaveTopic', topicId);
      console.log(`Left topic ${topicId}`);
    } catch (error) {
      console.error(`Failed to leave topic ${topicId}:`, error);
    }
  }, [connection, isConnected]);

  const startTyping = useCallback(async (topicId: number) => {
    if (!connection || !isConnected) return;

    try {
      await connection.invoke('StartTyping', topicId);
    } catch (error) {
      console.error('Failed to start typing:', error);
    }
  }, [connection, isConnected]);

  const stopTyping = useCallback(async (topicId: number) => {
    if (!connection || !isConnected) return;

    try {
      await connection.invoke('StopTyping', topicId);
    } catch (error) {
      console.error('Failed to stop typing:', error);
    }
  }, [connection, isConnected]);

  const getTypingUsers = useCallback(async (topicId: number) => {
    if (!connection || !isConnected) return;

    try {
      await connection.invoke('GetTypingUsers', topicId);
    } catch (error) {
      console.error('Failed to get typing users:', error);
    }
  }, [connection, isConnected]);

  const getOnlineUsers = useCallback(async (topicId: number) => {
    if (!connection || !isConnected) return;

    try {
      await connection.invoke('RequestOnlineUsers', topicId);
    } catch (error) {
      console.error('Failed to get online users:', error);
    }
  }, [connection, isConnected]);

  return {
    connection,
    connectionState,
    isConnected,
    isConnecting,
    joinTopic,
    leaveTopic,
    startTyping,
    stopTyping,
    getTypingUsers,
    getOnlineUsers,
  };
}
```

### 3.2 主题页面实时功能

**`src/hooks/useTopicRealtime.ts`**
```typescript
import { useEffect, useCallback, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { useSignalR } from './useSignalR';
import { toast } from 'sonner';

export interface TypingUser {
  userId: number;
  username: string;
  startedAt: string;
}

export interface OnlineUser {
  userId: number;
  username: string;
  avatarUrl?: string;
}

export interface TopicRealtimeState {
  typingUsers: TypingUser[];
  onlineUsers: OnlineUser[];
  onlineCount: number;
}

export function useTopicRealtime(topicId: number) {
  const { connection, isConnected, joinTopic, leaveTopic, startTyping, stopTyping } = useSignalR();
  const queryClient = useQueryClient();
  const [realtimeState, setRealtimeState] = useState<TopicRealtimeState>({
    typingUsers: [],
    onlineUsers: [],
    onlineCount: 0,
  });

  // 加入主题房间
  useEffect(() => {
    if (isConnected && topicId) {
      joinTopic(topicId);

      return () => {
        leaveTopic(topicId);
      };
    }
  }, [isConnected, topicId, joinTopic, leaveTopic]);

  // 设置事件监听器
  useEffect(() => {
    if (!connection) return;

    // 新帖子创建
    const handlePostCreated = (data: any) => {
      console.log('Post created:', data);
      
      // 更新帖子列表缓存
      queryClient.invalidateQueries({ queryKey: ['posts', topicId] });
      
      // 更新主题统计
      queryClient.invalidateQueries({ queryKey: ['topic', topicId] });
      
      // 显示通知（如果不是当前用户发的）
      const currentUser = queryClient.getQueryData<any>(['auth', 'user']);
      if (currentUser && data.Post.AuthorId !== currentUser.id) {
        toast.info(`${data.Post.AuthorUsername} 发表了新回复`);
      }
    };

    // 帖子编辑
    const handlePostEdited = (data: any) => {
      console.log('Post edited:', data);
      
      // 更新特定帖子缓存
      queryClient.setQueryData(['post', data.Post.Id], (oldData: any) => {
        if (oldData) {
          return {
            ...oldData,
            contentHtml: data.Post.ContentHtml,
            isEdited: data.Post.IsEdited,
            editReason: data.Post.EditReason,
            updatedAt: data.Post.UpdatedAt,
          };
        }
        return oldData;
      });

      // 刷新帖子列表
      queryClient.invalidateQueries({ queryKey: ['posts', topicId] });
    };

    // 帖子删除
    const handlePostDeleted = (data: any) => {
      console.log('Post deleted:', data);
      
      // 从缓存中移除或标记为删除
      queryClient.invalidateQueries({ queryKey: ['posts', topicId] });
      queryClient.invalidateQueries({ queryKey: ['topic', topicId] });
      
      toast.info('一条回复已被删除');
    };

    // 主题更新
    const handleTopicUpdated = (data: any) => {
      console.log('Topic updated:', data);
      
      // 更新主题缓存
      queryClient.setQueryData(['topic', topicId], (oldData: any) => {
        if (oldData) {
          return {
            ...oldData,
            ...data.Topic,
          };
        }
        return oldData;
      });

      if (data.Topic.IsPinned !== undefined) {
        toast.info(data.Topic.IsPinned ? '主题已置顶' : '主题已取消置顶');
      }
      
      if (data.Topic.IsLocked !== undefined) {
        toast.info(data.Topic.IsLocked ? '主题已锁定' : '主题已解锁');
      }
    };

    // 主题统计更新
    const handleTopicStatsUpdated = (data: any) => {
      console.log('Topic stats updated:', data);
      
      // 更新主题统计
      queryClient.setQueryData(['topic', topicId], (oldData: any) => {
        if (oldData) {
          return {
            ...oldData,
            replyCount: data.Stats.ReplyCount,
            viewCount: data.Stats.ViewCount,
            lastPostedAt: data.Stats.LastPostedAt,
            lastPosterId: data.Stats.LastPosterId,
            lastPosterUsername: data.Stats.LastPosterUsername,
          };
        }
        return oldData;
      });
    };

    // 用户加入
    const handleUserJoined = (data: any) => {
      console.log('User joined:', data);
      setRealtimeState(prev => ({
        ...prev,
        onlineCount: data.OnlineCount,
      }));
    };

    // 用户离开
    const handleUserLeft = (data: any) => {
      console.log('User left:', data);
      setRealtimeState(prev => ({
        ...prev,
        onlineCount: data.OnlineCount,
        typingUsers: prev.typingUsers.filter(u => u.userId !== data.UserId),
      }));
    };

    // 用户开始输入
    const handleUserStartedTyping = (data: any) => {
      console.log('User started typing:', data);
      setRealtimeState(prev => ({
        ...prev,
        typingUsers: [
          ...prev.typingUsers.filter(u => u.userId !== data.UserId),
          {
            userId: data.UserId,
            username: data.Username,
            startedAt: data.StartedAt,
          },
        ],
      }));
    };

    // 用户停止输入
    const handleUserStoppedTyping = (data: any) => {
      console.log('User stopped typing:', data);
      setRealtimeState(prev => ({
        ...prev,
        typingUsers: prev.typingUsers.filter(u => u.userId !== data.UserId),
      }));
    };

    // 输入用户列表
    const handleTypingUsers = (data: any) => {
      console.log('Typing users:', data);
      setRealtimeState(prev => ({
        ...prev,
        typingUsers: data.Users || [],
      }));
    };

    // 在线用户列表
    const handleOnlineUsers = (data: any) => {
      console.log('Online users:', data);
      setRealtimeState(prev => ({
        ...prev,
        onlineUsers: data.Users || [],
        onlineCount: data.Count,
      }));
    };

    // 主题加入成功
    const handleTopicJoined = (data: any) => {
      console.log('Topic joined:', data);
      setRealtimeState(prev => ({
        ...prev,
        onlineUsers: data.OnlineUsers || [],
        onlineCount: data.OnlineCount,
      }));
    };

    // 错误处理
    const handleError = (data: any) => {
      console.error('SignalR error:', data);
      toast.error(data.message || '发生了未知错误');
    };

    // 注册事件监听器
    connection.on('PostCreated', handlePostCreated);
    connection.on('PostEdited', handlePostEdited);
    connection.on('PostDeleted', handlePostDeleted);
    connection.on('TopicUpdated', handleTopicUpdated);
    connection.on('TopicStatsUpdated', handleTopicStatsUpdated);
    connection.on('UserJoined', handleUserJoined);
    connection.on('UserLeft', handleUserLeft);
    connection.on('UserStartedTyping', handleUserStartedTyping);
    connection.on('UserStoppedTyping', handleUserStoppedTyping);
    connection.on('TypingUsers', handleTypingUsers);
    connection.on('OnlineUsers', handleOnlineUsers);
    connection.on('TopicJoined', handleTopicJoined);
    connection.on('Error', handleError);

    // 清理事件监听器
    return () => {
      connection.off('PostCreated', handlePostCreated);
      connection.off('PostEdited', handlePostEdited);
      connection.off('PostDeleted', handlePostDeleted);
      connection.off('TopicUpdated', handleTopicUpdated);
      connection.off('TopicStatsUpdated', handleTopicStatsUpdated);
      connection.off('UserJoined', handleUserJoined);
      connection.off('UserLeft', handleUserLeft);
      connection.off('UserStartedTyping', handleUserStartedTyping);
      connection.off('UserStoppedTyping', handleUserStoppedTyping);
      connection.off('TypingUsers', handleTypingUsers);
      connection.off('OnlineUsers', handleOnlineUsers);
      connection.off('TopicJoined', handleTopicJoined);
      connection.off('Error', handleError);
    };
  }, [connection, topicId, queryClient]);

  // 输入状态管理
  const handleStartTyping = useCallback(() => {
    if (isConnected && topicId) {
      startTyping(topicId);
    }
  }, [isConnected, topicId, startTyping]);

  const handleStopTyping = useCallback(() => {
    if (isConnected && topicId) {
      stopTyping(topicId);
    }
  }, [isConnected, topicId, stopTyping]);

  return {
    ...realtimeState,
    isConnected,
    handleStartTyping,
    handleStopTyping,
  };
}
```

---

## ✅ M4 验收清单

### SignalR Hub 功能
- [ ] **房间管理** (加入/离开主题房间)
- [ ] **用户连接追踪** (在线用户列表、连接状态)
- [ ] **事件广播** (帖子创建/编辑/删除)
- [ ] **输入状态指示** ("正在输入" 功能)
- [ ] **错误处理** (连接失败、权限验证)

### 实时同步功能
- [ ] **帖子实时同步** (新帖出现 ≤1s)
- [ ] **编辑实时更新** (内容修改同步显示)
- [ ] **删除状态同步** (删除标记实时更新)
- [ ] **统计数据同步** (回帖数、浏览量更新)
- [ ] **主题状态同步** (置顶/锁定状态)

### 前端实时体验
- [ ] **连接状态指示** (连接/断开/重连状态)
- [ ] **自动重连机制** (指数退避、最大重试次数)
- [ ] **输入状态显示** (显示正在输入的用户)
- [ ] **实时通知** (新消息提醒、系统通知)
- [ ] **在线用户列表** (当前主题在线用户)

### 性能与稳定性
- [ ] **连接池管理** (多实例支持、Redis Backplane)
- [ ] **消息幂等性** (防止重复处理)
- [ ] **内存泄漏防护** (连接清理、事件解绑)
- [ ] **错误恢复** (自动重连、状态恢复)
- [ ] **性能监控** (连接数、消息延迟统计)

### 用户体验
- [ ] **无感知重连** (后台自动重连、用户无感知)
- [ ] **离线状态处理** (离线消息缓存、重连后同步)
- [ ] **输入防抖** (输入状态合理防抖处理)
- [ ] **错误提示友好** (网络异常、权限错误等)
- [ ] **移动端适配** (移动端 WebSocket 稳定性)

---

**预计完成时间**: 5 个工作日  
**关键阻塞点**: WebSocket 连接稳定性、多实例扩展  
**下一步**: M5 通知系统 + 基础管理功能开发