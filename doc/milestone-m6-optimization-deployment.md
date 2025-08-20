# 里程碑 M6：优化与生产部署

**工期：7天**  
**前置条件：M5 通知管理完成**

## 目标

完成生产环境部署准备，包括性能优化、安全加固、监控告警、CI/CD 流程，确保系统稳定运行。

## 技术要求

- 前后端性能优化
- 生产环境配置
- 安全防护加强
- 监控告警体系
- 自动化部署流程

---

## Day 1: 性能优化 - 数据库与缓存

### 数据库索引优化

```sql
-- 分析并优化关键查询索引
-- 主题列表查询优化
EXPLAIN SELECT t.id, t.title, t.slug, t.reply_count, t.view_count, t.last_posted_at,
       u.id AS author_id, u.username AS author_username,
       c.id AS category_id, c.name AS category_name
FROM topics t
JOIN users u ON u.id = t.author_id
JOIN categories c ON c.id = t.category_id
WHERE t.is_deleted = 0
  AND t.category_id = 1
ORDER BY t.is_pinned DESC, t.last_posted_at DESC, t.id DESC
LIMIT 20;

-- 优化索引
ALTER TABLE topics 
ADD INDEX idx_topic_list_optimized (category_id, is_deleted, is_pinned DESC, last_posted_at DESC, id DESC);

-- 帖子分页查询优化
ALTER TABLE posts 
ADD INDEX idx_posts_pagination (topic_id, is_deleted, created_at, id);

-- 搜索性能优化
ALTER TABLE topics 
ADD FULLTEXT INDEX ftx_topic_title_content (title);

ALTER TABLE posts 
ADD FULLTEXT INDEX ftx_posts_content_optimized (content_md);

-- 用户活跃度统计优化
ALTER TABLE posts 
ADD INDEX idx_posts_user_stats (author_id, created_at DESC);

-- 通知查询优化
ALTER TABLE notifications 
ADD INDEX idx_notifications_user_unread (user_id, read_at, created_at DESC);
```

### Redis 缓存层实现

```csharp
// Services/CacheService.cs
public class CacheService : ICacheService
{
    private readonly IDatabase _database;
    private readonly ILogger<CacheService> _logger;
    private readonly TimeSpan _defaultExpiry = TimeSpan.FromMinutes(30);

    public CacheService(IConnectionMultiplexer redis, ILogger<CacheService> logger)
    {
        _database = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            var value = await _database.StringGetAsync(key);
            if (!value.HasValue) return null;
            
            return JsonSerializer.Deserialize<T>(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cache key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            await _database.StringSetAsync(key, json, expiry ?? _defaultExpiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set cache key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _database.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove cache key: {Key}", key);
        }
    }

    public async Task RemovePatternAsync(string pattern)
    {
        try
        {
            var server = _database.Multiplexer.GetServer(_database.Multiplexer.GetEndPoints().First());
            var keys = server.Keys(database: _database.Database, pattern: pattern);
            await _database.KeyDeleteAsync(keys.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove cache pattern: {Pattern}", pattern);
        }
    }
}
```

### 缓存集成到仓储层

```csharp
// Repositories/CachedTopicRepository.cs
public class CachedTopicRepository : ITopicRepository
{
    private readonly TopicRepository _inner;
    private readonly ICacheService _cache;

    public async Task<IEnumerable<TopicListItem>> GetTopicsAsync(TopicQuery query)
    {
        var cacheKey = $"topics:list:{JsonSerializer.Serialize(query)}";
        
        var cached = await _cache.GetAsync<IEnumerable<TopicListItem>>(cacheKey);
        if (cached != null) return cached;

        var topics = await _inner.GetTopicsAsync(query);
        await _cache.SetAsync(cacheKey, topics, TimeSpan.FromMinutes(5));
        
        return topics;
    }

    public async Task<TopicDetail?> GetTopicDetailAsync(long id)
    {
        var cacheKey = $"topic:detail:{id}";
        
        var cached = await _cache.GetAsync<TopicDetail>(cacheKey);
        if (cached != null) return cached;

        var topic = await _inner.GetTopicDetailAsync(id);
        if (topic != null)
        {
            await _cache.SetAsync(cacheKey, topic, TimeSpan.FromMinutes(15));
        }
        
        return topic;
    }

    public async Task<long> CreateAsync(Topic topic)
    {
        var id = await _inner.CreateAsync(topic);
        
        // 清除相关缓存
        await _cache.RemovePatternAsync("topics:list:*");
        await _cache.RemoveAsync($"category:stats:{topic.CategoryId}");
        
        return id;
    }

    public async Task UpdateAsync(long id, Topic topic)
    {
        await _inner.UpdateAsync(id, topic);
        
        // 清除相关缓存
        await _cache.RemoveAsync($"topic:detail:{id}");
        await _cache.RemovePatternAsync("topics:list:*");
    }
}
```

### 应用层缓存策略

```csharp
// Services/TopicService.cs 增强版本
public class TopicService : ITopicService
{
    private readonly ITopicRepository _repository;
    private readonly ICacheService _cache;
    private readonly IMemoryCache _localCache;

    public async Task<IEnumerable<Category>> GetCategoriesAsync()
    {
        // 本地缓存热点数据
        return await _localCache.GetOrCreateAsync("categories", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(30);
            return await _repository.GetCategoriesAsync();
        });
    }

    public async Task<TopicStats> GetTopicStatsAsync(long topicId)
    {
        var cacheKey = $"topic:stats:{topicId}";
        
        var cached = await _cache.GetAsync<TopicStats>(cacheKey);
        if (cached != null) return cached;

        var stats = await _repository.GetTopicStatsAsync(topicId);
        await _cache.SetAsync(cacheKey, stats, TimeSpan.FromMinutes(10));
        
        return stats;
    }
}
```

### 验证要点

- [ ] 数据库查询性能提升显著
- [ ] Redis 缓存正常工作
- [ ] 缓存失效策略正确
- [ ] 热点数据命中率高

---

## Day 2: 前端性能优化

### 构建优化配置

```typescript
// vite.config.ts 生产优化
import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';
import { fileURLToPath, URL } from 'node:url';
import { visualizer } from 'rollup-plugin-visualizer';

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  
  return {
    plugins: [
      react(),
      // 生产环境包分析
      mode === 'production' && visualizer({
        filename: 'dist/stats.html',
        open: true,
        gzipSize: true,
      }),
    ].filter(Boolean),
    
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url))
      }
    },
    
    build: {
      // 生产优化配置
      target: 'es2020',
      minify: 'terser',
      terserOptions: {
        compress: {
          drop_console: true,
          drop_debugger: true,
        },
      },
      rollupOptions: {
        output: {
          // 代码分割策略
          manualChunks: {
            // 框架核心
            'react-vendor': ['react', 'react-dom', 'react-router-dom'],
            // UI 组件库
            'ui-vendor': ['@radix-ui/react-dialog', '@radix-ui/react-dropdown-menu'],
            // 工具库
            'utils-vendor': ['clsx', 'tailwind-merge', 'date-fns'],
            // 状态管理
            'query-vendor': ['@tanstack/react-query'],
            // 编辑器相关
            'editor-vendor': ['react-hook-form', 'zod'],
          },
        },
      },
      // 压缩配置
      cssCodeSplit: true,
      sourcemap: false,
      // 块大小警告阈值
      chunkSizeWarningLimit: 1000,
    },
    
    server: {
      proxy: {
        '/api': {
          target: env.VITE_API_BASE_URL || 'http://localhost:4000',
          changeOrigin: true,
        },
        '/hubs': {
          target: env.VITE_API_BASE_URL || 'http://localhost:4000',
          changeOrigin: true,
          ws: true,
        },
      },
    },
  };
});
```

### 代码分割与懒加载

```tsx
// router/index.tsx 路由懒加载
import { lazy, Suspense } from 'react';
import { createBrowserRouter } from 'react-router-dom';
import { LoadingSpinner } from '@/components/ui/loading-spinner';
import Layout from '@/components/Layout';
import ErrorBoundary from '@/components/ErrorBoundary';

// 懒加载页面组件
const HomePage = lazy(() => import('@/pages/Home'));
const TopicDetailPage = lazy(() => import('@/pages/TopicDetail'));
const CreateTopicPage = lazy(() => import('@/pages/CreateTopic'));
const UserProfilePage = lazy(() => import('@/pages/UserProfile'));
const AdminLayout = lazy(() => import('@/components/admin/AdminLayout'));
const AdminUsersPage = lazy(() => import('@/pages/admin/Users'));
const AdminCategoriesPage = lazy(() => import('@/pages/admin/Categories'));

const LazyWrapper = ({ children }: { children: React.ReactNode }) => (
  <Suspense fallback={<LoadingSpinner />}>
    <ErrorBoundary>
      {children}
    </ErrorBoundary>
  </Suspense>
);

export const router = createBrowserRouter([
  {
    path: '/',
    element: <Layout />,
    children: [
      {
        index: true,
        element: <LazyWrapper><HomePage /></LazyWrapper>,
      },
      {
        path: 't/:id/:slug?',
        element: <LazyWrapper><TopicDetailPage /></LazyWrapper>,
      },
      {
        path: 'new',
        element: <LazyWrapper><CreateTopicPage /></LazyWrapper>,
      },
      {
        path: 'u/:username',
        element: <LazyWrapper><UserProfilePage /></LazyWrapper>,
      },
    ],
  },
  {
    path: '/admin',
    element: <LazyWrapper><AdminLayout /></LazyWrapper>,
    children: [
      {
        path: 'users',
        element: <LazyWrapper><AdminUsersPage /></LazyWrapper>,
      },
      {
        path: 'categories',
        element: <LazyWrapper><AdminCategoriesPage /></LazyWrapper>,
      },
    ],
  },
]);
```

### React Query 优化配置

```tsx
// lib/query-client.ts 查询客户端优化
import { QueryClient } from '@tanstack/react-query';

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // 缓存时间 5 分钟
      staleTime: 5 * 60 * 1000,
      // 垃圾回收时间 10 分钟
      gcTime: 10 * 60 * 1000,
      // 重试配置
      retry: (failureCount, error: any) => {
        // 客户端错误不重试
        if (error?.response?.status >= 400 && error?.response?.status < 500) {
          return false;
        }
        return failureCount < 3;
      },
      // 重试延迟
      retryDelay: attemptIndex => Math.min(1000 * 2 ** attemptIndex, 30000),
    },
    mutations: {
      // 突变重试
      retry: 1,
    },
  },
});

// 预取关键数据
export const prefetchEssentialData = async () => {
  await Promise.all([
    // 预取分类列表
    queryClient.prefetchQuery({
      queryKey: ['categories'],
      queryFn: () => api.get('/categories').then(res => res.data.data),
    }),
    // 预取用户信息（如果已登录）
    queryClient.prefetchQuery({
      queryKey: ['auth', 'me'],
      queryFn: () => api.get('/auth/me').then(res => res.data),
    }),
  ]);
};
```

### 虚拟滚动优化

```tsx
// components/VirtualTopicList.tsx 虚拟滚动列表
import { FixedSizeList as List } from 'react-window';
import { useInfiniteQuery } from '@tanstack/react-query';
import { TopicListItem } from './TopicListItem';

interface VirtualTopicListProps {
  categoryId?: number;
  tag?: string;
}

export function VirtualTopicList({ categoryId, tag }: VirtualTopicListProps) {
  const {
    data,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
  } = useInfiniteQuery({
    queryKey: ['topics', { categoryId, tag }],
    queryFn: ({ pageParam = null }) =>
      api.get('/topics', {
        params: {
          cursor: pageParam,
          limit: 50,
          categoryId,
          tag,
        },
      }).then(res => res.data),
    initialPageParam: null,
    getNextPageParam: (lastPage) => lastPage.nextCursor,
  });

  const allTopics = data?.pages.flatMap(page => page.data) ?? [];

  // 虚拟滚动渲染函数
  const Row = ({ index, style }: { index: number; style: React.CSSProperties }) => {
    const topic = allTopics[index];
    
    // 接近末尾时预加载
    if (index === allTopics.length - 10 && hasNextPage && !isFetchingNextPage) {
      fetchNextPage();
    }

    return (
      <div style={style}>
        <TopicListItem topic={topic} />
      </div>
    );
  };

  return (
    <div className="h-[600px]">
      <List
        height={600}
        itemCount={allTopics.length}
        itemSize={80}
        width="100%"
      >
        {Row}
      </List>
    </div>
  );
}
```

### 图片优化与懒加载

```tsx
// components/OptimizedImage.tsx 图片优化组件
import { useState, useRef, useEffect } from 'react';
import { cn } from '@/lib/utils';

interface OptimizedImageProps {
  src: string;
  alt: string;
  className?: string;
  width?: number;
  height?: number;
  placeholder?: string;
}

export function OptimizedImage({
  src,
  alt,
  className,
  width,
  height,
  placeholder = 'data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjAiIGhlaWdodD0iMjAiIHZpZXdCb3g9IjAgMCAyMCAyMCIgZmlsbD0ibm9uZSIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj4KPHJlY3Qgd2lkdGg9IjIwIiBoZWlnaHQ9IjIwIiBmaWxsPSIjRjVGNUY1Ii8+Cjwvc3ZnPgo=',
}: OptimizedImageProps) {
  const [isLoaded, setIsLoaded] = useState(false);
  const [isInView, setIsInView] = useState(false);
  const imgRef = useRef<HTMLImageElement>(null);

  useEffect(() => {
    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting) {
          setIsInView(true);
          observer.disconnect();
        }
      },
      { threshold: 0.1 }
    );

    if (imgRef.current) {
      observer.observe(imgRef.current);
    }

    return () => observer.disconnect();
  }, []);

  return (
    <div className={cn('relative overflow-hidden', className)}>
      <img
        ref={imgRef}
        src={isInView ? src : placeholder}
        alt={alt}
        width={width}
        height={height}
        className={cn(
          'transition-opacity duration-300',
          isLoaded ? 'opacity-100' : 'opacity-0'
        )}
        onLoad={() => setIsLoaded(true)}
        loading="lazy"
        decoding="async"
      />
      {!isLoaded && (
        <div className="absolute inset-0 bg-gray-200 animate-pulse" />
      )}
    </div>
  );
}
```

### 验证要点

- [ ] 打包体积显著减少
- [ ] 页面加载速度提升
- [ ] 懒加载正常工作
- [ ] 虚拟滚动性能良好

---

## Day 3: 安全加固

### HTTPS 和安全头配置

```csharp
// Program.cs 安全配置增强
var builder = WebApplication.CreateBuilder(args);

// 安全服务配置
builder.Services.Configure<SecuritySettings>(builder.Configuration.GetSection("Security"));

// HSTS 配置
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

var app = builder.Build();

// 生产环境安全中间件
if (app.Environment.IsProduction())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// 安全头中间件
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    
    // CSP 策略
    var csp = "default-src 'self'; " +
              "script-src 'self' 'unsafe-inline'; " +
              "style-src 'self' 'unsafe-inline'; " +
              "img-src 'self' data: https:; " +
              "font-src 'self'; " +
              "connect-src 'self' ws: wss:; " +
              "frame-ancestors 'none';";
    context.Response.Headers.Add("Content-Security-Policy", csp);
    
    await next();
});
```

### 限流和防护

```csharp
// Services/RateLimitService.cs
public class RateLimitService : IRateLimitService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<RateLimitService> _logger;

    public async Task<bool> IsAllowedAsync(string key, int maxRequests, TimeSpan window)
    {
        var cacheKey = $"rate_limit:{key}";
        var now = DateTime.UtcNow;
        
        var requests = _cache.Get<List<DateTime>>(cacheKey) ?? new List<DateTime>();
        
        // 清理过期记录
        requests.RemoveAll(r => now - r > window);
        
        if (requests.Count >= maxRequests)
        {
            _logger.LogWarning("Rate limit exceeded for key: {Key}", key);
            return false;
        }
        
        requests.Add(now);
        _cache.Set(cacheKey, requests, window);
        
        return true;
    }
}

// Attributes/RateLimitAttribute.cs
public class RateLimitAttribute : Attribute, IAsyncActionFilter
{
    private readonly int _maxRequests;
    private readonly TimeSpan _window;
    private readonly string _keyPattern;

    public RateLimitAttribute(int maxRequests, int windowSeconds, string keyPattern = "ip")
    {
        _maxRequests = maxRequests;
        _window = TimeSpan.FromSeconds(windowSeconds);
        _keyPattern = keyPattern;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var rateLimitService = context.HttpContext.RequestServices.GetRequiredService<IRateLimitService>();
        
        var key = _keyPattern switch
        {
            "ip" => context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            "user" => context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous",
            _ => context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
        };

        if (!await rateLimitService.IsAllowedAsync(key, _maxRequests, _window))
        {
            context.Result = new StatusCodeResult(429); // Too Many Requests
            return;
        }

        await next();
    }
}
```

### 输入验证增强

```csharp
// Validators/TopicValidator.cs
public class CreateTopicValidator : AbstractValidator<CreateTopicRequest>
{
    public CreateTopicValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("标题不能为空")
            .Length(2, 200).WithMessage("标题长度必须在2-200字符之间")
            .Must(BeValidTitle).WithMessage("标题包含禁用字符");

        RuleFor(x => x.ContentMd)
            .NotEmpty().WithMessage("内容不能为空")
            .MaximumLength(50000).WithMessage("内容长度不能超过50000字符")
            .Must(BeValidContent).WithMessage("内容包含禁用内容");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("必须选择分类");
    }

    private bool BeValidTitle(string title)
    {
        // 检查禁用词汇
        var bannedWords = new[] { "spam", "test123" }; // 实际从配置加载
        return !bannedWords.Any(word => title.Contains(word, StringComparison.OrdinalIgnoreCase));
    }

    private bool BeValidContent(string content)
    {
        // 检查恶意内容
        return !content.Contains("<script", StringComparison.OrdinalIgnoreCase) &&
               !content.Contains("javascript:", StringComparison.OrdinalIgnoreCase);
    }
}

// Services/ContentValidationService.cs
public class ContentValidationService : IContentValidationService
{
    private readonly ILogger<ContentValidationService> _logger;
    private readonly ContentValidationSettings _settings;

    public async Task<ValidationResult> ValidateContentAsync(string content, ContentType type)
    {
        var result = new ValidationResult();

        // 1. 长度检查
        if (content.Length > _settings.MaxContentLength)
        {
            result.AddError("内容长度超出限制");
        }

        // 2. 恶意代码检查
        if (ContainsMaliciousCode(content))
        {
            result.AddError("内容包含恶意代码");
            _logger.LogWarning("Malicious content detected: {Content}", content);
        }

        // 3. 垃圾内容检测
        if (await IsSpamAsync(content))
        {
            result.AddError("内容被识别为垃圾信息");
        }

        // 4. 敏感词检查
        var bannedWords = await GetBannedWordsAsync();
        foreach (var word in bannedWords)
        {
            if (content.Contains(word, StringComparison.OrdinalIgnoreCase))
            {
                result.AddError($"内容包含禁用词汇: {word}");
            }
        }

        return result;
    }

    private bool ContainsMaliciousCode(string content)
    {
        var patterns = new[]
        {
            @"<script[^>]*>.*?</script>",
            @"javascript:",
            @"vbscript:",
            @"onload\s*=",
            @"onerror\s*=",
            @"onclick\s*=",
        };

        return patterns.Any(pattern => 
            Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase));
    }
}
```

### 数据库安全

```sql
-- 创建只读用户（用于报表等查询）
CREATE USER 'forum_readonly'@'%' IDENTIFIED BY 'strong_readonly_password';
GRANT SELECT ON forum.* TO 'forum_readonly'@'%';

-- 创建备份用户
CREATE USER 'forum_backup'@'localhost' IDENTIFIED BY 'strong_backup_password';
GRANT SELECT, LOCK TABLES, SHOW VIEW, EVENT, TRIGGER ON forum.* TO 'forum_backup'@'localhost';

-- 删除测试数据
DELETE FROM users WHERE username LIKE 'test%';
DELETE FROM topics WHERE title LIKE '%test%';

-- 生产环境安全配置
-- 禁用 LOAD DATA LOCAL INFILE
SET GLOBAL local_infile = 0;

-- 设置慢查询日志
SET GLOBAL slow_query_log = 1;
SET GLOBAL long_query_time = 2;
SET GLOBAL slow_query_log_file = '/var/log/mysql/slow.log';

-- 启用错误日志
SET GLOBAL log_error = '/var/log/mysql/error.log';
```

### 验证要点

- [ ] HTTPS 配置正确
- [ ] 安全头设置完整
- [ ] 限流机制生效
- [ ] 输入验证严格
- [ ] 数据库权限最小化

---

## Day 4: 监控告警体系

### 应用性能监控

```csharp
// Services/MetricsService.cs
public class MetricsService : IMetricsService
{
    private readonly ILogger<MetricsService> _logger;
    private readonly IConfiguration _configuration;
    
    // 计数器
    private readonly Counter<long> _requestCounter;
    private readonly Counter<long> _errorCounter;
    private readonly Counter<long> _signalrConnectionCounter;
    
    // 直方图
    private readonly Histogram<double> _requestDuration;
    private readonly Histogram<double> _databaseQueryDuration;

    public MetricsService(IMeterFactory meterFactory, ILogger<MetricsService> logger)
    {
        var meter = meterFactory.Create("Forum.Api");
        
        _requestCounter = meter.CreateCounter<long>("http_requests_total", "Total HTTP requests");
        _errorCounter = meter.CreateCounter<long>("http_errors_total", "Total HTTP errors");
        _signalrConnectionCounter = meter.CreateCounter<long>("signalr_connections_total", "SignalR connections");
        
        _requestDuration = meter.CreateHistogram<double>("http_request_duration_seconds", "HTTP request duration");
        _databaseQueryDuration = meter.CreateHistogram<double>("database_query_duration_seconds", "Database query duration");
        
        _logger = logger;
    }

    public void RecordRequest(string method, string path, int statusCode, double duration)
    {
        _requestCounter.Add(1, new KeyValuePair<string, object?>("method", method),
                               new KeyValuePair<string, object?>("path", path),
                               new KeyValuePair<string, object?>("status_code", statusCode));
        
        _requestDuration.Record(duration, new KeyValuePair<string, object?>("method", method),
                                         new KeyValuePair<string, object?>("path", path));
        
        if (statusCode >= 400)
        {
            _errorCounter.Add(1, new KeyValuePair<string, object?>("status_code", statusCode));
        }
    }

    public void RecordDatabaseQuery(string operation, double duration)
    {
        _databaseQueryDuration.Record(duration, new KeyValuePair<string, object?>("operation", operation));
    }

    public void RecordSignalRConnection(string action)
    {
        _signalrConnectionCounter.Add(1, new KeyValuePair<string, object?>("action", action));
    }
}

// Middleware/MetricsMiddleware.cs
public class MetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMetricsService _metrics;

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            _metrics.RecordRequest(
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.Elapsed.TotalSeconds
            );
        }
    }
}
```

### 健康检查

```csharp
// HealthChecks/DatabaseHealthCheck.cs
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly string _connectionString;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            
            var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync(cancellationToken);
            
            return HealthCheckResult.Healthy("Database is healthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database is unhealthy", ex);
        }
    }
}

// HealthChecks/RedisHealthCheck.cs
public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var database = _redis.GetDatabase();
            await database.PingAsync();
            
            return HealthCheckResult.Healthy("Redis is healthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis is unhealthy", ex);
        }
    }
}

// Program.cs 健康检查配置
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database")
    .AddCheck<RedisHealthCheck>("redis")
    .AddCheck("signalr", () => HealthCheckResult.Healthy("SignalR is healthy"))
    .AddCheck("storage", () => 
    {
        var freeSpace = new DriveInfo("/").AvailableFreeSpace;
        return freeSpace > 1_000_000_000 // 1GB
            ? HealthCheckResult.Healthy($"Free space: {freeSpace / 1_000_000_000}GB")
            : HealthCheckResult.Unhealthy($"Low disk space: {freeSpace / 1_000_000_000}GB");
    });

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

### 日志聚合配置

```csharp
// Program.cs Serilog 配置
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentUserName()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .Enrich.WithProperty("Application", "Forum.Api")
        .WriteTo.Console(outputTemplate: 
            "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File("logs/forum-.log", 
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.Conditional(evt => context.HostingEnvironment.IsProduction(),
            wt => wt.File("logs/errors-.log", 
                restrictedToMinimumLevel: LogEventLevel.Warning,
                rollingInterval: RollingInterval.Day));
});
```

### Docker 监控配置

```yaml
# docker-compose.monitoring.yml
version: '3.8'

services:
  prometheus:
    image: prom/prometheus:latest
    ports:
      - "9090:9090"
    volumes:
      - ./monitoring/prometheus.yml:/etc/prometheus/prometheus.yml
      - prometheus_data:/prometheus
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.path=/prometheus'
      - '--web.console.libraries=/etc/prometheus/console_libraries'
      - '--web.console.templates=/etc/prometheus/consoles'
      - '--storage.tsdb.retention.time=200h'
      - '--web.enable-lifecycle'

  grafana:
    image: grafana/grafana:latest
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin123
    volumes:
      - grafana_data:/var/lib/grafana
      - ./monitoring/grafana/dashboards:/etc/grafana/provisioning/dashboards
      - ./monitoring/grafana/datasources:/etc/grafana/provisioning/datasources

  node-exporter:
    image: prom/node-exporter:latest
    ports:
      - "9100:9100"
    volumes:
      - /proc:/host/proc:ro
      - /sys:/host/sys:ro
      - /:/rootfs:ro
    command:
      - '--path.procfs=/host/proc'
      - '--path.rootfs=/rootfs'
      - '--path.sysfs=/host/sys'
      - '--collector.filesystem.mount-points-exclude=^/(sys|proc|dev|host|etc)($$|/)'

  alertmanager:
    image: prom/alertmanager:latest
    ports:
      - "9093:9093"
    volumes:
      - ./monitoring/alertmanager.yml:/etc/alertmanager/alertmanager.yml
      - alertmanager_data:/alertmanager

volumes:
  prometheus_data:
  grafana_data:
  alertmanager_data:
```

### 验证要点

- [ ] 应用指标正确收集
- [ ] 健康检查端点正常
- [ ] 日志格式化正确
- [ ] 监控面板显示数据

---

## Day 5: CI/CD 自动化部署

### GitHub Actions 工作流

```yaml
# .github/workflows/ci-cd.yml
name: CI/CD Pipeline

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

env:
  REGISTRY: docker.io
  IMAGE_NAME: forum-app

jobs:
  test:
    runs-on: ubuntu-latest
    
    services:
      mysql:
        image: mysql:8.0
        env:
          MYSQL_ROOT_PASSWORD: root
          MYSQL_DATABASE: forum_test
        ports:
          - 3306:3306
        options: >-
          --health-cmd="mysqladmin ping"
          --health-interval=10s
          --health-timeout=5s
          --health-retries=3
      
      redis:
        image: redis:7
        ports:
          - 6379:6379
        options: >-
          --health-cmd="redis-cli ping"
          --health-interval=10s
          --health-timeout=5s
          --health-retries=3

    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Setup Node.js
      uses: actions/setup-node@v4
      with:
        node-version: '20'
        cache: 'npm'
        cache-dependency-path: 'frontend/package-lock.json'
    
    - name: Restore backend dependencies
      run: dotnet restore src/Forum.Api/Forum.Api.csproj
    
    - name: Install frontend dependencies
      run: |
        cd frontend
        npm ci
    
    - name: Run backend tests
      run: |
        cd src
        dotnet test --no-restore --verbosity normal
      env:
        ConnectionStrings__DefaultConnection: "Server=localhost;Port=3306;Database=forum_test;Uid=root;Pwd=root;"
        ConnectionStrings__Redis: "localhost:6379"
    
    - name: Run frontend tests
      run: |
        cd frontend
        npm run test:ci
    
    - name: Run frontend lint
      run: |
        cd frontend
        npm run lint
    
    - name: Build frontend
      run: |
        cd frontend
        npm run build

  build-and-push:
    needs: test
    runs-on: ubuntu-latest
    if: github.event_name == 'push' && github.ref == 'refs/heads/main'
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Set up Docker Buildx
      uses: docker/setup-buildx-action@v3
    
    - name: Log in to Docker Hub
      uses: docker/login-action@v3
      with:
        username: ${{ secrets.DOCKER_USERNAME }}
        password: ${{ secrets.DOCKER_PASSWORD }}
    
    - name: Extract metadata
      id: meta
      uses: docker/metadata-action@v5
      with:
        images: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}
        tags: |
          type=ref,event=branch
          type=sha,prefix={{branch}}-
          type=raw,value=latest,enable={{is_default_branch}}
    
    - name: Build and push Docker image
      uses: docker/build-push-action@v5
      with:
        context: .
        push: true
        tags: ${{ steps.meta.outputs.tags }}
        labels: ${{ steps.meta.outputs.labels }}
        cache-from: type=gha
        cache-to: type=gha,mode=max

  deploy:
    needs: build-and-push
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Deploy to production
      uses: appleboy/ssh-action@v1.0.0
      with:
        host: ${{ secrets.HOST }}
        username: ${{ secrets.USERNAME }}
        key: ${{ secrets.SSH_KEY }}
        script: |
          cd /opt/forum
          docker-compose pull
          docker-compose up -d --remove-orphans
          docker system prune -f
```

### Docker 配置优化

```dockerfile
# Dockerfile 多阶段构建优化
FROM node:20-alpine AS frontend-build
WORKDIR /app/frontend

# 复制依赖文件并安装
COPY frontend/package*.json ./
RUN npm ci --only=production

# 复制源代码并构建
COPY frontend/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend-build
WORKDIR /app

# 复制项目文件并还原依赖
COPY src/*.sln ./
COPY src/Forum.Api/*.csproj ./Forum.Api/
COPY src/Forum.Core/*.csproj ./Forum.Core/
COPY src/Forum.Infrastructure/*.csproj ./Forum.Infrastructure/
RUN dotnet restore

# 复制源代码并发布
COPY src/ ./
RUN dotnet publish Forum.Api/Forum.Api.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# 创建非 root 用户
RUN addgroup --system --gid 1001 forum && \
    adduser --system --uid 1001 --ingroup forum forum

# 复制构建结果
COPY --from=backend-build /app/out ./
COPY --from=frontend-build /app/frontend/dist ./wwwroot

# 设置权限
RUN chown -R forum:forum /app
USER forum

# 健康检查
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

EXPOSE 8080
ENTRYPOINT ["dotnet", "Forum.Api.dll"]
```

### Docker Compose 生产配置

```yaml
# docker-compose.prod.yml
version: '3.8'

services:
  app:
    image: forum-app:latest
    container_name: forum-app
    restart: unless-stopped
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__DefaultConnection=${MYSQL_CONNECTION_STRING}
      - ConnectionStrings__Redis=${REDIS_CONNECTION_STRING}
      - JWT__Secret=${JWT_SECRET}
      - SMTP__Host=${SMTP_HOST}
      - SMTP__Username=${SMTP_USERNAME}
      - SMTP__Password=${SMTP_PASSWORD}
    depends_on:
      - mysql
      - redis
    networks:
      - forum-network
    volumes:
      - app-logs:/app/logs
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

  mysql:
    image: mysql:8.0
    container_name: forum-mysql
    restart: unless-stopped
    environment:
      - MYSQL_ROOT_PASSWORD=${MYSQL_ROOT_PASSWORD}
      - MYSQL_DATABASE=forum
      - MYSQL_USER=forum
      - MYSQL_PASSWORD=${MYSQL_PASSWORD}
    volumes:
      - mysql-data:/var/lib/mysql
      - ./scripts/init.sql:/docker-entrypoint-initdb.d/init.sql:ro
    networks:
      - forum-network
    command: >
      --default-authentication-plugin=mysql_native_password
      --innodb-buffer-pool-size=256M
      --max-connections=200
      --slow-query-log=1
      --long-query-time=2

  redis:
    image: redis:7-alpine
    container_name: forum-redis
    restart: unless-stopped
    command: redis-server --appendonly yes --maxmemory 256mb --maxmemory-policy allkeys-lru
    volumes:
      - redis-data:/data
    networks:
      - forum-network

  nginx:
    image: nginx:alpine
    container_name: forum-nginx
    restart: unless-stopped
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf:ro
      - ./nginx/ssl:/etc/nginx/ssl:ro
      - nginx-cache:/var/cache/nginx
    depends_on:
      - app
    networks:
      - forum-network

volumes:
  mysql-data:
  redis-data:
  app-logs:
  nginx-cache:

networks:
  forum-network:
    driver: bridge
```

### Nginx 配置

```nginx
# nginx/nginx.conf
upstream forum-api {
    server app:8080;
}

# 限流配置
limit_req_zone $binary_remote_addr zone=api:10m rate=10r/s;
limit_req_zone $binary_remote_addr zone=login:10m rate=1r/s;

server {
    listen 80;
    server_name forum.example.com;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name forum.example.com;

    # SSL 配置
    ssl_certificate /etc/nginx/ssl/forum.crt;
    ssl_certificate_key /etc/nginx/ssl/forum.key;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-RSA-AES256-GCM-SHA512:DHE-RSA-AES256-GCM-SHA512:ECDHE-RSA-AES256-GCM-SHA384;
    ssl_prefer_server_ciphers off;
    ssl_session_cache shared:SSL:10m;
    ssl_session_timeout 10m;

    # 安全头
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    add_header X-Content-Type-Options nosniff always;
    add_header X-Frame-Options DENY always;
    add_header X-XSS-Protection "1; mode=block" always;

    # Gzip 压缩
    gzip on;
    gzip_vary on;
    gzip_min_length 1024;
    gzip_types text/plain text/css text/xml text/javascript application/javascript application/xml+rss application/json;

    # 静态文件缓存
    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
        location ~ \.(js|css)$ {
            gzip_static on;
        }
    }

    # API 路由
    location /api/ {
        limit_req zone=api burst=20 nodelay;
        proxy_pass http://forum-api;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        
        # 超时配置
        proxy_connect_timeout 5s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }

    # 登录限流
    location /api/auth/login {
        limit_req zone=login burst=5 nodelay;
        proxy_pass http://forum-api;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # WebSocket (SignalR)
    location /hubs/ {
        proxy_pass http://forum-api;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }

    # 前端应用
    location / {
        try_files $uri $uri/ /index.html;
        root /var/www/html;
        index index.html;
        
        # 缓存配置
        location = /index.html {
            expires -1;
            add_header Cache-Control "no-cache, no-store, must-revalidate";
        }
    }
}
```

### 验证要点

- [ ] CI/CD 流程正常执行
- [ ] Docker 镜像构建成功
- [ ] 生产环境部署正常
- [ ] Nginx 配置生效

---

## Day 6: 性能测试与优化

### 压力测试脚本

```javascript
// tests/load/basic-load.js - k6 压力测试
import http from 'k6/http';
import ws from 'k6/ws';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

export let errorRate = new Rate('errors');

export let options = {
  stages: [
    { duration: '2m', target: 10 }, // 预热
    { duration: '5m', target: 50 }, // 正常负载
    { duration: '2m', target: 100 }, // 峰值负载
    { duration: '5m', target: 50 }, // 回到正常
    { duration: '2m', target: 0 }, // 降压
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'], // 95% 请求 < 500ms
    errors: ['rate<0.1'], // 错误率 < 10%
  },
};

const BASE_URL = 'http://localhost:8080';
let authToken = '';

export function setup() {
  // 获取认证令牌
  const loginResponse = http.post(`${BASE_URL}/api/auth/login`, {
    email: 'test@example.com',
    password: 'password123',
  });
  
  if (loginResponse.status === 200) {
    authToken = loginResponse.cookies.access_token[0].value;
  }
  
  return { authToken };
}

export default function(data) {
  const headers = {
    'Authorization': `Bearer ${data.authToken}`,
    'Content-Type': 'application/json',
  };

  // 测试主题列表
  let response = http.get(`${BASE_URL}/api/topics`, { headers });
  check(response, {
    'topics list status is 200': (r) => r.status === 200,
    'topics list response time < 500ms': (r) => r.timings.duration < 500,
  }) || errorRate.add(1);

  sleep(1);

  // 测试主题详情
  response = http.get(`${BASE_URL}/api/topics/1`, { headers });
  check(response, {
    'topic detail status is 200': (r) => r.status === 200,
    'topic detail response time < 300ms': (r) => r.timings.duration < 300,
  }) || errorRate.add(1);

  sleep(1);

  // 测试创建帖子
  const postData = {
    topicId: 1,
    contentMd: `测试帖子内容 ${Math.random()}`,
  };

  response = http.post(`${BASE_URL}/api/topics/1/posts`, JSON.stringify(postData), { headers });
  check(response, {
    'create post status is 201': (r) => r.status === 201,
    'create post response time < 1000ms': (r) => r.timings.duration < 1000,
  }) || errorRate.add(1);

  sleep(2);
}

export function websocketTest() {
  // WebSocket 连接测试
  const url = 'ws://localhost:8080/hubs/topics';
  const response = ws.connect(url, {}, function (socket) {
    socket.on('open', () => {
      socket.send(JSON.stringify({
        protocol: 'json',
        version: 1,
      }));
      
      // 加入主题房间
      socket.send(JSON.stringify({
        type: 1,
        target: 'JoinTopic',
        arguments: [1],
      }));
    });

    socket.on('message', (data) => {
      check(data, {
        'websocket message received': (msg) => msg.length > 0,
      });
    });

    sleep(30);
  });

  check(response, {
    'websocket connection status is 101': (r) => r && r.status === 101,
  });
}
```

### 数据库性能优化

```sql
-- 分析慢查询并优化
-- 查看慢查询
SELECT 
    query_time,
    lock_time,
    rows_sent,
    rows_examined,
    sql_text
FROM mysql.slow_log 
ORDER BY query_time DESC 
LIMIT 10;

-- 优化主题列表查询
EXPLAIN ANALYZE 
SELECT t.id, t.title, t.slug, t.reply_count, t.view_count, t.last_posted_at,
       u.username AS author_username,
       c.name AS category_name
FROM topics t
INNER JOIN users u ON u.id = t.author_id
INNER JOIN categories c ON c.id = t.category_id
WHERE t.is_deleted = 0
  AND t.category_id = 1
ORDER BY t.is_pinned DESC, t.last_posted_at DESC, t.id DESC
LIMIT 20;

-- 创建覆盖索引
ALTER TABLE topics 
ADD INDEX idx_topic_list_covering (category_id, is_deleted, is_pinned, last_posted_at, id, title, slug, reply_count, view_count, author_id);

-- 优化搜索查询
ALTER TABLE topics 
ADD INDEX idx_topic_search (is_deleted, category_id, last_posted_at DESC);

-- 添加分区（如果数据量大）
-- ALTER TABLE posts PARTITION BY RANGE (YEAR(created_at)) (
--     PARTITION p2024 VALUES LESS THAN (2025),
--     PARTITION p2025 VALUES LESS THAN (2026),
--     PARTITION p_future VALUES LESS THAN MAXVALUE
-- );

-- 统计信息更新
ANALYZE TABLE topics, posts, users, categories;

-- 查询缓存配置（MySQL 8.0 中已移除，使用应用层缓存）
-- 确保 InnoDB 缓冲池大小合理
SHOW VARIABLES LIKE 'innodb_buffer_pool_size';

-- 连接池配置
SHOW VARIABLES LIKE 'max_connections';
SHOW STATUS LIKE 'Threads_connected';
```

### 应用性能优化

```csharp
// Services/OptimizedTopicService.cs
public class OptimizedTopicService : ITopicService
{
    private readonly ITopicRepository _repository;
    private readonly ICacheService _cache;
    private readonly IMemoryCache _localCache;
    private readonly ILogger<OptimizedTopicService> _logger;

    public async Task<PagedResult<TopicListItem>> GetTopicsAsync(TopicQuery query)
    {
        // 多级缓存策略
        var cacheKey = $"topics:list:{JsonSerializer.Serialize(query)}";
        
        // L1: 内存缓存（热点数据）
        if (_localCache.TryGetValue(cacheKey, out PagedResult<TopicListItem> cachedResult))
        {
            return cachedResult;
        }

        // L2: Redis 缓存
        var redisCached = await _cache.GetAsync<PagedResult<TopicListItem>>(cacheKey);
        if (redisCached != null)
        {
            // 回填内存缓存
            _localCache.Set(cacheKey, redisCached, TimeSpan.FromMinutes(2));
            return redisCached;
        }

        // 数据库查询
        var result = await _repository.GetTopicsAsync(query);
        
        // 写入缓存
        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
        _localCache.Set(cacheKey, result, TimeSpan.FromMinutes(2));
        
        return result;
    }

    public async Task<IEnumerable<TopicListItem>> GetTopicsBatchAsync(IEnumerable<long> topicIds)
    {
        var topics = new List<TopicListItem>();
        var uncachedIds = new List<long>();

        // 批量检查缓存
        foreach (var id in topicIds)
        {
            var cacheKey = $"topic:detail:{id}";
            var cached = await _cache.GetAsync<TopicListItem>(cacheKey);
            
            if (cached != null)
            {
                topics.Add(cached);
            }
            else
            {
                uncachedIds.Add(id);
            }
        }

        // 批量查询未缓存的数据
        if (uncachedIds.Any())
        {
            var uncachedTopics = await _repository.GetTopicsByIdsAsync(uncachedIds);
            
            // 批量写入缓存
            var cacheTasks = uncachedTopics.Select(async topic =>
            {
                var cacheKey = $"topic:detail:{topic.Id}";
                await _cache.SetAsync(cacheKey, topic, TimeSpan.FromMinutes(15));
            });
            
            await Task.WhenAll(cacheTasks);
            topics.AddRange(uncachedTopics);
        }

        return topics.OrderBy(t => topicIds.ToList().IndexOf(t.Id));
    }
}

// 连接池优化
public class OptimizedDbConnectionFactory
{
    private readonly string _connectionString;
    private readonly ObjectPool<MySqlConnection> _pool;

    public OptimizedDbConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
        
        var provider = new DefaultObjectPoolProvider();
        _pool = provider.Create(new MySqlConnectionPooledObjectPolicy(connectionString));
    }

    public MySqlConnection GetConnection()
    {
        return _pool.Get();
    }

    public void ReturnConnection(MySqlConnection connection)
    {
        _pool.Return(connection);
    }
}
```

### 前端性能监控

```tsx
// hooks/usePerformanceMonitoring.ts
import { useEffect } from 'react';

export function usePerformanceMonitoring() {
  useEffect(() => {
    // Core Web Vitals 监控
    if ('web-vital' in window) {
      import('web-vitals').then(({ getCLS, getFID, getFCP, getLCP, getTTFB }) => {
        getCLS(sendToAnalytics);
        getFID(sendToAnalytics);
        getFCP(sendToAnalytics);
        getLCP(sendToAnalytics);
        getTTFB(sendToAnalytics);
      });
    }

    // 资源加载监控
    const observer = new PerformanceObserver((list) => {
      list.getEntries().forEach((entry) => {
        if (entry.entryType === 'navigation') {
          const nav = entry as PerformanceNavigationTiming;
          sendToAnalytics({
            name: 'page_load_time',
            value: nav.loadEventEnd - nav.loadEventStart,
            url: window.location.pathname,
          });
        }

        if (entry.entryType === 'resource') {
          const resource = entry as PerformanceResourceTiming;
          if (resource.duration > 1000) { // 超过1秒的资源
            sendToAnalytics({
              name: 'slow_resource',
              value: resource.duration,
              resource: resource.name,
            });
          }
        }
      });
    });

    observer.observe({ entryTypes: ['navigation', 'resource'] });

    return () => observer.disconnect();
  }, []);
}

function sendToAnalytics(metric: any) {
  // 发送到分析服务
  fetch('/api/analytics/metrics', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(metric),
  }).catch(console.error);
}

// components/PerformanceOptimizedApp.tsx
import { lazy, Suspense, memo } from 'react';
import { usePerformanceMonitoring } from '@/hooks/usePerformanceMonitoring';

const MemoizedTopicList = memo(TopicList);

export function PerformanceOptimizedApp() {
  usePerformanceMonitoring();

  return (
    <Suspense fallback={<div>Loading...</div>}>
      <MemoizedTopicList />
    </Suspense>
  );
}
```

### 验证要点

- [ ] 压力测试通过预期指标
- [ ] 数据库查询性能提升
- [ ] 缓存命中率 > 80%
- [ ] Core Web Vitals 达标

---

## Day 7: 最终部署与文档

### 生产环境检查清单

```bash
#!/bin/bash
# scripts/production-checklist.sh

echo "=== 生产环境部署检查清单 ==="

# 1. 环境变量检查
echo "1. 检查环境变量..."
required_vars=(
  "MYSQL_CONNECTION_STRING"
  "REDIS_CONNECTION_STRING"
  "JWT_SECRET"
  "SMTP_HOST"
  "SMTP_USERNAME"
  "SMTP_PASSWORD"
)

for var in "${required_vars[@]}"; do
  if [[ -z "${!var}" ]]; then
    echo "❌ 缺少环境变量: $var"
    exit 1
  else
    echo "✅ $var: 已设置"
  fi
done

# 2. 数据库连接检查
echo "2. 检查数据库连接..."
mysql -h "${MYSQL_HOST}" -u "${MYSQL_USER}" -p"${MYSQL_PASSWORD}" -e "SELECT 1;" > /dev/null 2>&1
if [ $? -eq 0 ]; then
  echo "✅ 数据库连接正常"
else
  echo "❌ 数据库连接失败"
  exit 1
fi

# 3. Redis 连接检查
echo "3. 检查 Redis 连接..."
redis-cli -h "${REDIS_HOST}" ping > /dev/null 2>&1
if [ $? -eq 0 ]; then
  echo "✅ Redis 连接正常"
else
  echo "❌ Redis 连接失败"
  exit 1
fi

# 4. SSL 证书检查
echo "4. 检查 SSL 证书..."
if [[ -f "/etc/nginx/ssl/forum.crt" && -f "/etc/nginx/ssl/forum.key" ]]; then
  echo "✅ SSL 证书存在"
  
  # 检查证书有效期
  cert_expiry=$(openssl x509 -enddate -noout -in /etc/nginx/ssl/forum.crt | cut -d= -f2)
  expiry_timestamp=$(date -d "$cert_expiry" +%s)
  current_timestamp=$(date +%s)
  days_until_expiry=$(( ($expiry_timestamp - $current_timestamp) / 86400 ))
  
  if [ $days_until_expiry -lt 30 ]; then
    echo "⚠️  SSL 证书将在 $days_until_expiry 天后过期"
  else
    echo "✅ SSL 证书有效期: $days_until_expiry 天"
  fi
else
  echo "❌ SSL 证书缺失"
  exit 1
fi

# 5. 磁盘空间检查
echo "5. 检查磁盘空间..."
available_space=$(df / | awk 'NR==2 {print $4}')
if [ $available_space -lt 1048576 ]; then  # 1GB
  echo "❌ 磁盘空间不足: $(($available_space / 1024))MB"
  exit 1
else
  echo "✅ 磁盘空间充足: $(($available_space / 1024 / 1024))GB"
fi

# 6. 服务健康检查
echo "6. 检查服务健康状态..."
health_response=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:8080/health)
if [ "$health_response" = "200" ]; then
  echo "✅ 应用健康检查通过"
else
  echo "❌ 应用健康检查失败: HTTP $health_response"
  exit 1
fi

# 7. 性能基准测试
echo "7. 运行性能基准测试..."
response_time=$(curl -w "%{time_total}" -o /dev/null -s http://localhost:8080/api/topics)
if (( $(echo "$response_time < 0.5" | bc -l) )); then
  echo "✅ API 响应时间: ${response_time}s"
else
  echo "⚠️  API 响应时间较慢: ${response_time}s"
fi

echo "=== 检查完成 ==="
```

### 运维文档

```markdown
# 论坛系统运维手册

## 服务管理

### 启动服务
```bash
docker-compose -f docker-compose.prod.yml up -d
```

### 停止服务
```bash
docker-compose -f docker-compose.prod.yml down
```

### 查看日志
```bash
# 应用日志
docker-compose logs -f app

# 数据库日志
docker-compose logs -f mysql

# Nginx 日志
docker-compose logs -f nginx
```

### 服务重启
```bash
# 重启应用（零停机）
docker-compose -f docker-compose.prod.yml up -d --no-deps app

# 重启所有服务
docker-compose -f docker-compose.prod.yml restart
```

## 数据库维护

### 备份数据库
```bash
# 创建备份
docker exec forum-mysql mysqldump -u root -p forum > backup_$(date +%Y%m%d_%H%M%S).sql

# 自动备份脚本
0 2 * * * /opt/forum/scripts/backup.sh
```

### 恢复数据库
```bash
# 恢复备份
docker exec -i forum-mysql mysql -u root -p forum < backup_20241120_020000.sql
```

### 数据库优化
```bash
# 清理过期数据
docker exec forum-mysql mysql -u root -p -e "
DELETE FROM refresh_tokens WHERE expires_at < NOW() - INTERVAL 30 DAY;
DELETE FROM notifications WHERE created_at < NOW() - INTERVAL 90 DAY AND read_at IS NOT NULL;
DELETE FROM audit_logs WHERE created_at < NOW() - INTERVAL 1 YEAR;
"

# 优化表
docker exec forum-mysql mysql -u root -p -e "OPTIMIZE TABLE topics, posts, users;"
```

## 监控和告警

### 关键指标
- 应用响应时间 < 500ms
- 数据库连接数 < 150
- CPU 使用率 < 80%
- 内存使用率 < 85%
- 磁盘使用率 < 80%

### 日志监控
```bash
# 错误日志监控
tail -f /opt/forum/logs/error.log | grep -i "error\|exception\|failed"

# 慢查询监控
docker exec forum-mysql mysqladmin -u root -p processlist
```

## 常见问题排查

### 应用无法启动
1. 检查环境变量配置
2. 检查数据库连接
3. 查看应用日志
4. 检查端口占用

### 数据库连接问题
1. 检查 MySQL 服务状态
2. 验证连接字符串
3. 检查用户权限
4. 查看连接池状态

### Redis 连接问题
1. 检查 Redis 服务状态
2. 验证连接配置
3. 检查内存使用情况
4. 查看连接数

### 性能问题
1. 检查慢查询日志
2. 分析数据库索引
3. 监控缓存命中率
4. 检查资源使用情况

## 扩容方案

### 垂直扩容
- 增加 CPU 和内存
- 优化数据库配置
- 调整连接池大小

### 水平扩容
- 部署多个应用实例
- 配置负载均衡
- 启用 Redis Backplane
- 数据库读写分离

## 安全维护

### SSL 证书更新
```bash
# 更新证书
certbot renew --nginx

# 自动更新
0 0 1 * * certbot renew --nginx --quiet
```

### 安全检查
```bash
# 检查开放端口
nmap localhost

# 检查 SSL 配置
testssl.sh https://forum.example.com
```
```

### API 文档生成

```csharp
// Program.cs Swagger 生产配置
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Forum API V1");
        c.RoutePrefix = "api-docs";
        c.DisplayRequestDuration();
        c.EnableDeepLinking();
        c.EnableFilter();
        c.ShowExtensions();
    });
}
```

### 用户使用手册

```markdown
# 论坛使用指南

## 新用户指南

### 注册和验证
1. 访问论坛首页，点击"注册"
2. 填写用户名、邮箱和密码
3. 检查邮箱中的验证邮件
4. 点击验证链接完成注册

### 发布主题
1. 点击"发布新主题"
2. 选择合适的分类
3. 填写标题和内容（支持 Markdown）
4. 添加相关标签
5. 点击"发布"

### 回复和互动
1. 在主题页面点击"回复"
2. 使用 @ 符号提及其他用户
3. 使用引用功能回复特定内容
4. 支持 Markdown 格式化

## 管理员指南

### 用户管理
1. 访问管理后台
2. 查看用户列表
3. 封禁或解封用户
4. 分配版主权限

### 内容管理
1. 管理分类和标签
2. 处理举报内容
3. 置顶或锁定主题
4. 删除违规内容

### 系统设置
1. 配置邮件通知
2. 调整权限设置
3. 监控系统状态
4. 查看审计日志
```

### 部署脚本

```bash
#!/bin/bash
# scripts/deploy.sh

set -e

echo "开始部署论坛系统..."

# 1. 检查环境
echo "1. 检查部署环境..."
./scripts/production-checklist.sh

# 2. 拉取最新代码
echo "2. 拉取最新镜像..."
docker-compose -f docker-compose.prod.yml pull

# 3. 数据库迁移
echo "3. 执行数据库迁移..."
docker-compose -f docker-compose.prod.yml run --rm app dotnet Forum.Api.dll --migrate

# 4. 更新服务
echo "4. 更新服务..."
docker-compose -f docker-compose.prod.yml up -d --remove-orphans

# 5. 健康检查
echo "5. 等待服务启动..."
sleep 30

# 6. 验证部署
echo "6. 验证部署结果..."
health_check=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:8080/health)
if [ "$health_check" = "200" ]; then
  echo "✅ 部署成功！"
else
  echo "❌ 部署失败！健康检查返回: $health_check"
  exit 1
fi

# 7. 清理旧镜像
echo "7. 清理旧镜像..."
docker system prune -f

echo "部署完成！"
```

### 验证要点

- [ ] 生产环境检查清单通过
- [ ] 运维文档完整
- [ ] API 文档生成正确
- [ ] 用户手册清晰
- [ ] 部署脚本测试通过

---

## 最终交付物

### 系统组件
- 完整的生产环境配置
- CI/CD 自动化流程
- 监控告警体系
- 备份恢复机制

### 文档体系
- 运维手册
- API 文档
- 用户使用指南
- 故障排查手册

### 性能指标
- API 响应时间 P95 < 500ms
- 数据库查询优化 50%+
- 缓存命中率 > 80%
- 系统可用性 > 99.9%

### 安全保障
- HTTPS 全站加密
- 安全头配置完整
- 输入验证严格
- 访问控制精确

## 成功标准

1. **性能达标**：核心 API P95 响应时间 < 500ms
2. **稳定运行**：连续运行 72 小时无重大故障
3. **安全合规**：通过安全扫描，无高危漏洞
4. **易于维护**：文档完整，运维流程清晰

M6 完成后，论坛系统将具备生产环境运行的所有条件，可以支持正式上线和长期稳定运营。