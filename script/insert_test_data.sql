-- =====================================================================
-- 论坛系统测试数据脚本
-- 
-- 描述：为 Discourse 风格论坛生成全面的测试数据
-- 版本：1.0
-- 创建日期：2025-08-25
-- 
-- 包含数据：
-- - 用户账户（不同角色：管理员、版主、普通用户）
-- - 分类和标签
-- - 主题帖子（包含不同状态：置顶、锁定、正常）
-- - 回帖（包含引用和提及）
-- - 用户角色权限
-- =====================================================================

SET FOREIGN_KEY_CHECKS = 0;
SET AUTOCOMMIT = 0;

START TRANSACTION;

-- 清理现有测试数据（保留系统默认数据）
DELETE FROM post_mentions WHERE post_id > 0;
DELETE FROM posts WHERE id > 0;
DELETE FROM topic_tags WHERE topic_id > 0;
DELETE FROM topics WHERE id > 0;
DELETE FROM category_moderators WHERE user_id > 0;
DELETE FROM user_roles WHERE user_id > 0;
DELETE FROM refresh_tokens WHERE user_id > 0;
DELETE FROM email_verification_tokens WHERE user_id > 0;
DELETE FROM users WHERE id > 0;

-- 重置自增 ID
ALTER TABLE users AUTO_INCREMENT = 1;
ALTER TABLE topics AUTO_INCREMENT = 1;
ALTER TABLE posts AUTO_INCREMENT = 1;

-- =====================================================================
-- 1. 插入测试用户
-- =====================================================================

-- 管理员用户
INSERT INTO users (id, username, email, password_hash, status, email_verified, avatar_url, bio, last_seen_at, created_at) VALUES
(1, 'admin', 'admin@forum.example.com', 'u+IPdC4Ogn8BC5Bgfz1auYMCOnjuFtqX1IsBlfHee0Q=:GmwBJNmFNtydcoLxMZg++A==', 'active', 1, 'https://api.dicebear.com/7.x/avataaars/svg?seed=admin', '系统管理员，负责论坛运营和管理', NOW(), NOW() - INTERVAL 30 DAY),
(2, 'moderator', 'mod@forum.example.com', 'u+IPdC4Ogn8BC5Bgfz1auYMCOnjuFtqX1IsBlfHee0Q=:GmwBJNmFNtydcoLxMZg++A==', 'active', 1, 'https://api.dicebear.com/7.x/avataaars/svg?seed=moderator', '版主，维护论坛秩序', NOW(), NOW() - INTERVAL 25 DAY);

-- 活跃用户
INSERT INTO users (id, username, email, password_hash, status, email_verified, avatar_url, bio, last_seen_at, created_at) VALUES
(3, 'developer_jane', 'jane@example.com', 'u+IPdC4Ogn8BC5Bgfz1auYMCOnjuFtqX1IsBlfHee0Q=:GmwBJNmFNtydcoLxMZg++A==', 'active', 1, 'https://api.dicebear.com/7.x/avataaars/svg?seed=jane', '全栈开发工程师，专注于现代Web技术', NOW(), NOW() - INTERVAL 20 DAY),
(4, 'backend_bob', 'bob@example.com', 'u+IPdC4Ogn8BC5Bgfz1auYMCOnjuFtqX1IsBlfHee0Q=:GmwBJNmFNtydcoLxMZg++A==', 'active', 1, 'https://api.dicebear.com/7.x/avataaars/svg?seed=bob', '后端开发专家，熟悉微服务架构', NOW() - INTERVAL 30 MINUTE, NOW() - INTERVAL 18 DAY),
(5, 'frontend_alice', 'alice@example.com', 'u+IPdC4Ogn8BC5Bgfz1auYMCOnjuFtqX1IsBlfHee0Q=:GmwBJNmFNtydcoLxMZg++A==', 'active', 1, 'https://api.dicebear.com/7.x/avataaars/svg?seed=alice', 'UI/UX设计师兼前端开发', NOW() - INTERVAL 2 HOUR, NOW() - INTERVAL 15 DAY),
(6, 'devops_charlie', 'charlie@example.com', 'u+IPdC4Ogn8BC5Bgfz1auYMCOnjuFtqX1IsBlfHee0Q=:GmwBJNmFNtydcoLxMZg++A==', 'active', 1, 'https://api.dicebear.com/7.x/avataaars/svg?seed=charlie', 'DevOps工程师，容器化和云原生专家', NOW() - INTERVAL 1 DAY, NOW() - INTERVAL 12 DAY);

-- 普通用户
INSERT INTO users (id, username, email, password_hash, status, email_verified, avatar_url, bio, last_seen_at, created_at) VALUES
(7, 'newbie_david', 'david@example.com', 'u+IPdC4Ogn8BC5Bgfz1auYMCOnjuFtqX1IsBlfHee0Q=:GmwBJNmFNtydcoLxMZg++A==', 'active', 1, 'https://api.dicebear.com/7.x/avataaars/svg?seed=david', '编程新手，正在学习Web开发', NOW() - INTERVAL 3 HOUR, NOW() - INTERVAL 10 DAY),
(8, 'student_emma', 'emma@example.com', 'u+IPdC4Ogn8BC5Bgfz1auYMCOnjuFtqX1IsBlfHee0Q=:GmwBJNmFNtydcoLxMZg++A==', 'active', 1, 'https://api.dicebear.com/7.x/avataaars/svg?seed=emma', '计算机科学学生，对AI和机器学习感兴趣', NOW() - INTERVAL 6 HOUR, NOW() - INTERVAL 8 DAY),
(9, 'freelancer_frank', 'frank@example.com', 'u+IPdC4Ogn8BC5Bgfz1auYMCOnjuFtqX1IsBlfHee0Q=:GmwBJNmFNtydcoLxMZg++A==', 'active', 1, 'https://api.dicebear.com/7.x/avataaars/svg?seed=frank', '自由职业者，全栈独立开发', NOW() - INTERVAL 12 HOUR, NOW() - INTERVAL 6 DAY),
(10, 'senior_grace', 'grace@example.com', 'u+IPdC4Ogn8BC5Bgfz1auYMCOnjuFtqX1IsBlfHee0Q=:GmwBJNmFNtydcoLxMZg++A==', 'active', 1, 'https://api.dicebear.com/7.x/avataaars/svg?seed=grace', '资深技术架构师，15年开发经验', NOW() - INTERVAL 1 DAY, NOW() - INTERVAL 4 DAY);

-- 不活跃用户和测试状态
INSERT INTO users (id, username, email, password_hash, status, email_verified, avatar_url, bio, last_seen_at, created_at) VALUES
(11, 'inactive_henry', 'henry@example.com', 'u+IPdC4Ogn8BC5Bgfz1auYMCOnjuFtqX1IsBlfHee0Q=:GmwBJNmFNtydcoLxMZg++A==', 'active', 0, NULL, '测试账户 - 未验证邮箱', NULL, NOW() - INTERVAL 3 DAY),
(12, 'suspended_user', 'suspended@example.com', 'u+IPdC4Ogn8BC5Bgfz1auYMCOnjuFtqX1IsBlfHee0Q=:GmwBJNmFNtydcoLxMZg++A==', 'suspended', 1, NULL, '因违规被暂时停用的账户', NOW() - INTERVAL 7 DAY, NOW() - INTERVAL 2 DAY);

-- =====================================================================
-- 2. 分配用户角色
-- =====================================================================

INSERT INTO user_roles (user_id, role) VALUES
-- 管理员
(1, 'admin'),
(1, 'mod'),
(1, 'user'),

-- 版主
(2, 'mod'),
(2, 'user'),

-- 普通用户
(3, 'user'),
(4, 'user'),
(5, 'user'),
(6, 'user'),
(7, 'user'),
(8, 'user'),
(9, 'user'),
(10, 'user'),
(11, 'user'),
(12, 'user');

-- =====================================================================
-- 3. 分配版主权限（分类管理）
-- =====================================================================

INSERT INTO category_moderators (category_id, user_id) VALUES
-- admin 管理所有分类
(1, 1), (2, 1), (3, 1), (4, 1),
-- moderator 管理技术交流和产品反馈
(2, 2), (3, 2);

-- =====================================================================
-- 4. 更新标签使用次数（为后续主题数据做准备）
-- =====================================================================

UPDATE tags SET usage_count = 0;

-- =====================================================================
-- 5. 创建主题帖子
-- =====================================================================

-- 技术交流分类的主题
INSERT INTO topics (id, title, slug, author_id, category_id, is_pinned, is_locked, reply_count, view_count, last_posted_at, last_poster_id, created_at, updated_at) VALUES
(1, '欢迎来到论坛！新手必读指南', 'welcome-guide', 1, 1, 1, 0, 8, 245, NOW() - INTERVAL 2 HOUR, 7, NOW() - INTERVAL 25 DAY, NOW() - INTERVAL 2 HOUR),
(2, 'React 18 新特性深度解析', 'react-18-features', 3, 2, 1, 0, 12, 156, NOW() - INTERVAL 3 HOUR, 5, NOW() - INTERVAL 20 DAY, NOW() - INTERVAL 3 HOUR),
(3, 'ASP.NET Core 性能优化实践', 'aspnet-core-performance', 4, 2, 0, 0, 15, 189, NOW() - INTERVAL 1 HOUR, 10, NOW() - INTERVAL 18 DAY, NOW() - INTERVAL 1 HOUR),
(4, 'Docker 容器化部署完整指南', 'docker-deployment-guide', 6, 2, 0, 0, 9, 134, NOW() - INTERVAL 5 HOUR, 3, NOW() - INTERVAL 15 DAY, NOW() - INTERVAL 5 HOUR),
(5, '前端状态管理库对比分析', 'frontend-state-management', 5, 2, 0, 0, 7, 98, NOW() - INTERVAL 8 HOUR, 9, NOW() - INTERVAL 12 DAY, NOW() - INTERVAL 8 HOUR),
(6, 'MySQL 索引优化策略', 'mysql-index-optimization', 10, 2, 0, 0, 6, 87, NOW() - INTERVAL 12 HOUR, 4, NOW() - INTERVAL 10 DAY, NOW() - INTERVAL 12 HOUR),
(7, '新手求助：如何开始学习编程？', 'beginner-programming-help', 7, 1, 0, 0, 11, 67, NOW() - INTERVAL 4 HOUR, 8, NOW() - INTERVAL 8 DAY, NOW() - INTERVAL 4 HOUR),
(8, '关于论坛新功能的建议', 'forum-feature-suggestions', 9, 3, 0, 1, 5, 45, NOW() - INTERVAL 2 DAY, 2, NOW() - INTERVAL 6 DAY, NOW() - INTERVAL 2 DAY),
(9, '分享：我的开发工具链配置', 'my-dev-toolchain', 3, 1, 0, 0, 3, 89, NOW() - INTERVAL 1 DAY, 6, NOW() - INTERVAL 4 DAY, NOW() - INTERVAL 1 DAY),
(10, '【已解决】TypeScript 类型推断问题', 'typescript-type-inference', 8, 2, 0, 0, 8, 76, NOW() - INTERVAL 18 HOUR, 3, NOW() - INTERVAL 3 DAY, NOW() - INTERVAL 18 HOUR);

-- =====================================================================
-- 6. 主题标签关联
-- =====================================================================

INSERT INTO topic_tags (topic_id, tag_id) VALUES
-- 欢迎指南：讨论
(1, 2),
-- React 18：讨论、分享
(2, 2), (2, 3),
-- ASP.NET Core：分享
(3, 3),
-- Docker指南：分享、讨论
(4, 3), (4, 2),
-- 状态管理：讨论
(5, 2),
-- MySQL优化：分享
(6, 3),
-- 新手求助：问题
(7, 1),
-- 功能建议：建议
(8, 4),
-- 工具链：分享
(9, 3),
-- TypeScript问题：问题、分享
(10, 1), (10, 3);

-- 更新标签使用计数
UPDATE tags SET usage_count = (
  SELECT COUNT(*) FROM topic_tags WHERE topic_tags.tag_id = tags.id
);

-- =====================================================================
-- 7. 创建帖子内容（首帖和回帖）
-- =====================================================================

-- 主题 1: 欢迎指南的帖子
INSERT INTO posts (id, topic_id, author_id, content_md, reply_to_post_id, created_at, updated_at) VALUES
(1, 1, 1, '# 欢迎来到我们的技术论坛！🎉

各位开发者朋友们好！

非常欢迎大家加入我们的技术社区。这里是一个专为开发者打造的交流平台，无论你是刚刚入门的新手，还是经验丰富的资深工程师，都能在这里找到属于自己的位置。

## 论坛功能介绍

- **分类讨论**：我们有不同的技术分类，方便大家找到感兴趣的话题
- **标签系统**：通过标签快速筛选和定位相关内容  
- **实时互动**：支持实时回帖和通知，让讨论更加流畅
- **个人主页**：展示你的技术专长和贡献

## 发帖规范

1. **选择合适的分类**：确保你的帖子发在正确的分类下
2. **使用恰当的标签**：帮助其他人更容易找到你的内容
3. **标题要清晰**：让人一眼就能明白你要讨论的内容
4. **内容要详实**：提供足够的背景信息和具体的问题描述

期待大家的积极参与，让我们一起构建一个高质量的技术交流社区！', NULL, NOW() - INTERVAL 25 DAY, NOW() - INTERVAL 25 DAY);

-- 回帖
INSERT INTO posts (id, topic_id, author_id, content_md, reply_to_post_id, created_at, updated_at) VALUES
(2, 1, 3, '感谢管理员的详细介绍！论坛的功能看起来很完善，期待能在这里学到更多东西。', 1, NOW() - INTERVAL 24 DAY, NOW() - INTERVAL 24 DAY),
(3, 1, 4, '界面设计得很不错，用起来很流畅 👍', 1, NOW() - INTERVAL 23 DAY, NOW() - INTERVAL 23 DAY),
(4, 1, 5, '特别喜欢实时通知功能，这样讨论起来更有效率！', 1, NOW() - INTERVAL 22 DAY, NOW() - INTERVAL 22 DAY),
(5, 1, 7, '作为新手，这个指南对我很有帮助。请问有没有推荐的学习路径？', 1, NOW() - INTERVAL 20 DAY, NOW() - INTERVAL 20 DAY),
(6, 1, 2, '@newbie_david 我们会陆续发布一些学习资源，可以关注技术交流分类。', 5, NOW() - INTERVAL 19 DAY, NOW() - INTERVAL 19 DAY),
(7, 1, 8, '论坛的 Markdown 支持很棒，可以方便地分享代码片段！', 1, NOW() - INTERVAL 18 DAY, NOW() - INTERVAL 18 DAY),
(8, 1, 6, '希望能看到更多关于 DevOps 和云原生的讨论。', 1, NOW() - INTERVAL 15 DAY, NOW() - INTERVAL 15 DAY),
(9, 1, 7, '谢谢 @moderator 的回复！我会多关注学习资源的。', 6, NOW() - INTERVAL 2 HOUR, NOW() - INTERVAL 2 HOUR);

-- 主题 2: React 18 的帖子
INSERT INTO posts (id, topic_id, author_id, content_md, reply_to_post_id, created_at, updated_at) VALUES
(10, 2, 3, '# React 18 新特性深度解析

React 18 带来了许多令人兴奋的新特性，今天我想和大家分享一下我的使用体验。

## 主要新特性

### 1. Concurrent Features
React 18 引入了并发特性，这是最大的变化：

```jsx
// 新的 createRoot API
import { createRoot } from ''react-dom/client'';
const root = createRoot(container);
root.render(<App />);
```

### 2. Automatic Batching
现在所有的状态更新都会自动批处理：

```jsx
// React 18 中，这些更新会被自动批处理
setTimeout(() => {
  setCount(c => c + 1);
  setFlag(f => !f);
}, 1000);
```

### 3. Suspense 改进
Suspense 现在支持更多场景，包括数据获取：

```jsx
<Suspense fallback={<Loading />}>
  <UserProfile userId={userId} />
</Suspense>
```

## 升级建议

1. **逐步迁移**：不需要一次性重写所有代码
2. **测试充分**：新的并发特性可能影响组件行为
3. **关注性能**：利用新特性优化用户体验

大家在升级过程中遇到什么问题吗？', NULL, NOW() - INTERVAL 20 DAY, NOW() - INTERVAL 20 DAY);

INSERT INTO posts (id, topic_id, author_id, content_md, reply_to_post_id, created_at, updated_at) VALUES
(11, 2, 5, '非常详细的总结！我在项目中已经开始使用 React 18 了，Automatic Batching 确实提升了性能。

不过要注意一个问题，如果你的代码依赖于同步的状态更新，可能需要调整：

```jsx
// 如果需要强制同步更新，可以使用 flushSync
import { flushSync } from ''react-dom'';

flushSync(() => {
  setCount(count + 1);
});
```', 10, NOW() - INTERVAL 19 DAY, NOW() - INTERVAL 19 DAY),
(12, 2, 4, '我们团队还在评估升级的风险。@developer_jane 你们在升级过程中遇到了什么兼容性问题吗？', 10, NOW() - INTERVAL 18 DAY, NOW() - INTERVAL 18 DAY),
(13, 2, 3, '@backend_bob 总体来说兼容性很好，主要是一些第三方库可能需要更新。建议先在测试环境试试。

另外，StrictMode 在开发环境下会更严格，可能会发现一些之前隐藏的问题。', 12, NOW() - INTERVAL 17 DAY, NOW() - INTERVAL 17 DAY),
(14, 2, 7, '作为新手问一下，React 18 对学习曲线有什么影响吗？是否建议直接从 18 开始学？', 10, NOW() - INTERVAL 15 DAY, NOW() - INTERVAL 15 DAY),
(15, 2, 10, '@newbie_david 建议直接学 React 18，新的特性设计得更加直观，而且是未来的趋势。', 14, NOW() - INTERVAL 14 DAY, NOW() - INTERVAL 14 DAY),
(16, 2, 9, 'Concurrent Features 确实很强大，特别是对于复杂应用的性能优化很有帮助。', 10, NOW() - INTERVAL 10 DAY, NOW() - INTERVAL 10 DAY),
(17, 2, 6, '我们在生产环境使用了几个月，稳定性很好。推荐大家升级！', 10, NOW() - INTERVAL 8 DAY, NOW() - INTERVAL 8 DAY),
(18, 2, 8, '期待更多关于 React 18 性能优化的实战案例分享！', 10, NOW() - INTERVAL 6 DAY, NOW() - INTERVAL 6 DAY),
(19, 2, 5, '@student_emma 我准备写一篇关于 React 18 性能优化的详细文章，敬请期待！', 18, NOW() - INTERVAL 3 HOUR, NOW() - INTERVAL 3 HOUR);

-- 主题 3: ASP.NET Core 性能优化
INSERT INTO posts (id, topic_id, author_id, content_md, reply_to_post_id, created_at, updated_at) VALUES
(20, 3, 4, '# ASP.NET Core 性能优化实践分享

最近在项目中进行了一轮性能优化，想和大家分享一些实用的经验。

## 数据库优化

### 1. 使用 Dapper 替代 EF Core
在高性能场景下，Dapper 的表现确实更好：

```csharp
// Dapper 查询示例
var users = await connection.QueryAsync<User>(
    "SELECT * FROM users WHERE status = @status",
    new { status = "active" });
```

### 2. 连接池优化
```csharp
// 配置连接池
services.AddScoped<IDbConnectionFactory>(_ => 
    new MySqlConnectionFactory(connectionString, new MySqlConnectionPoolSettings
    {
        MaxPoolSize = 100,
        MinPoolSize = 10,
        ConnectionTimeout = 30
    }));
```

## 缓存策略

### 1. Memory Cache
```csharp
services.AddMemoryCache(options =>
{
    options.SizeLimit = 1000;
    options.CompactionPercentage = 0.25;
});
```

### 2. Distributed Cache (Redis)
```csharp
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
    options.InstanceName = "ForumApp";
});
```

## API 性能优化

### 1. Response Caching
```csharp
[ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "categoryId" })]
public async Task<IActionResult> GetTopics(int categoryId)
{
    // 实现逻辑
}
```

### 2. 异步编程最佳实践
```csharp
// 避免阻塞调用
public async Task<List<Topic>> GetTopicsAsync()
{
    return await _repository.GetTopicsAsync().ConfigureAwait(false);
}
```

通过这些优化，我们的 API 响应时间从平均 300ms 降到了 50ms 以下！', NULL, NOW() - INTERVAL 18 DAY, NOW() - INTERVAL 18 DAY);

-- 继续添加更多回帖...
INSERT INTO posts (id, topic_id, author_id, content_md, reply_to_post_id, created_at, updated_at) VALUES
(21, 3, 10, '非常实用的分享！我们也在考虑从 EF Core 迁移到 Dapper，性能提升确实很明显。

补充一个小技巧，在使用 Dapper 时可以考虑使用 `QueryFirstOrDefaultAsync` 来避免不必要的内存分配：

```csharp
var user = await connection.QueryFirstOrDefaultAsync<User>(
    "SELECT * FROM users WHERE id = @id", 
    new { id });
```', 20, NOW() - INTERVAL 17 DAY, NOW() - INTERVAL 17 DAY),
(22, 3, 6, '关于缓存策略，我建议还要考虑缓存穿透和缓存雪崩的问题。可以使用布隆过滤器或者设置随机的过期时间。', 20, NOW() - INTERVAL 16 DAY, NOW() - INTERVAL 16 DAY),
(23, 3, 3, '@backend_bob 你们有没有做过压力测试？想了解一下具体的 QPS 提升数据。', 20, NOW() - INTERVAL 15 DAY, NOW() - INTERVAL 15 DAY),
(24, 3, 4, '@developer_jane 我们的压测结果：优化前 QPS 约 500，优化后能达到 2000+，延迟也从 P95 300ms 降到了 P95 80ms。', 23, NOW() - INTERVAL 14 DAY, NOW() - INTERVAL 14 DAY),
(25, 3, 8, '学到了很多！请问在微服务架构下，这些优化策略需要做哪些调整？', 20, NOW() - INTERVAL 12 DAY, NOW() - INTERVAL 12 DAY),
(26, 3, 4, '@student_emma 微服务下主要关注服务间通信的优化，比如使用 HTTP/2、gRPC，还有熔断器模式等。', 25, NOW() - INTERVAL 11 DAY, NOW() - INTERVAL 11 DAY),
(27, 3, 5, '想补充一点关于前端的优化：配合后端的 Response Caching，前端也要做好缓存策略，比如使用 ETags。', 20, NOW() - INTERVAL 8 DAY, NOW() - INTERVAL 8 DAY),
(28, 3, 9, '这个分享太有价值了！我们项目正好遇到性能瓶颈，准备按照这个思路优化。', 20, NOW() - INTERVAL 6 DAY, NOW() - INTERVAL 6 DAY),
(29, 3, 10, '有计划写一个完整的性能优化系列文章吗？这种实战经验分享对大家都很有帮助。', 20, NOW() - INTERVAL 1 HOUR, NOW() - INTERVAL 1 HOUR);

-- 主题 7: 新手求助帖子
INSERT INTO posts (id, topic_id, author_id, content_md, reply_to_post_id, created_at, updated_at) VALUES
(30, 7, 7, '# 新手求助：如何开始学习编程？

大家好！我是编程完全零基础的新手，最近想开始学习Web开发，但是面对这么多技术栈感到有些迷茫。

## 我的情况
- 完全没有编程经验
- 对网站和应用开发比较感兴趣  
- 数学基础一般，但学习能力还可以
- 每天能投入 2-3 小时学习时间

## 我的疑问
1. **应该从哪门语言开始？** JavaScript、Python 还是其他？
2. **学习路径该怎么规划？** 前端先学还是后端先学？
3. **有什么好的学习资源推荐？** 
4. **如何判断自己的学习进度？**
5. **什么时候可以开始做项目？**

希望各位前辈能给一些建议，谢谢大家！🙏', NULL, NOW() - INTERVAL 8 DAY, NOW() - INTERVAL 8 DAY);

INSERT INTO posts (id, topic_id, author_id, content_md, reply_to_post_id, created_at, updated_at) VALUES
(31, 7, 3, '欢迎加入编程的世界！@newbie_david 

作为过来人，我建议：

## 学习路径建议
1. **从前端开始**：HTML → CSS → JavaScript，因为能看到直观的效果，有成就感
2. **循序渐进**：不要急于求成，把基础打牢
3. **边学边做**：理论+实践，每学一个概念就动手试试

## 推荐资源
- **MDN Web Docs**：权威的Web技术文档
- **freeCodeCamp**：免费的交互式课程
- **YouTube**：有很多优质的编程教程

建议先花1个月学HTML/CSS，再花2个月学JavaScript基础。有问题随时问！', 30, NOW() - INTERVAL 7 DAY, NOW() - INTERVAL 7 DAY),
(32, 7, 10, '@newbie_david 我也是从零开始学的！分享几个经验：

## 学习心得
1. **不要追求完美**：先做出来，再优化
2. **做笔记很重要**：记录学习过程和问题
3. **加入社区**：多和其他开发者交流

## 项目建议
- 第1个月：静态网页（个人简历）
- 第2-3月：交互式网页（计算器、待办事项）
- 第4-6月：完整的小项目

编程是个马拉松，保持耐心和热情最重要！💪', 30, NOW() - INTERVAL 6 DAY, NOW() - INTERVAL 6 DAY),
(33, 7, 2, '作为版主，我整理了一个新手学习路线图，大家可以参考：

## 阶段1：Web基础（1-2月）
- HTML5 语义化标签
- CSS3 布局和动画
- JavaScript ES6+ 基础

## 阶段2：前端框架（2-3月）  
- 选择一个框架深入学习（推荐React或Vue）
- 状态管理和路由

## 阶段3：后端入门（3-4月）
- Node.js + Express 或者其他后端语言
- 数据库基础（MySQL/MongoDB）

## 阶段4：项目实战（持续）
- 完整的全栈项目
- 部署和运维基础

学习编程最重要的是**持续性**，每天进步一点点！', 30, NOW() - INTERVAL 5 DAY, NOW() - INTERVAL 5 DAY),
(34, 7, 5, '@newbie_david 从设计师转前端的角度给你一些建议：

## 学习方法
1. **视觉化学习**：多看优秀的网站设计，分析实现方法
2. **工具使用**：熟悉浏览器开发者工具
3. **设计+代码**：学会从设计稿到代码的转换

推荐先做几个漂亮的静态页面，培养审美和代码感觉！', 30, NOW() - INTERVAL 4 DAY, NOW() - INTERVAL 4 DAY),
(35, 7, 8, '我现在也在学习中，我们可以互相鼓励！最近在学JavaScript，确实有点难度但很有意思。

@newbie_david 要不我们建个学习小组？', 30, NOW() - INTERVAL 3 DAY, NOW() - INTERVAL 3 DAY),
(36, 7, 7, '感谢大家的热心回复！@developer_jane @senior_grace @moderator @frontend_alice @student_emma 

我已经开始按照大家的建议学习HTML了，确实很有成就感！@student_emma 建学习小组的想法很好，我们可以私聊讨论一下。

再次感谢各位前辈的指导！🙏', 31, NOW() - INTERVAL 2 DAY, NOW() - INTERVAL 2 DAY),
(37, 7, 6, '看到这么多热心的回复真的很感动！这就是技术社区的魅力。

@newbie_david 如果将来想学DevOps相关的知识，也欢迎来找我交流！', 30, NOW() - INTERVAL 1 DAY, NOW() - INTERVAL 1 DAY),
(38, 7, 9, '这个帖子对我也很有启发！虽然我已经工作了，但看到新手的学习热情，让我想起了刚开始学编程的时候。

学习永无止境，大家一起加油！🚀', 30, NOW() - INTERVAL 18 HOUR, NOW() - INTERVAL 18 HOUR),
(39, 7, 1, '看到大家的互动很暖心！这就是我们希望营造的社区氛围。

我会整理一个新手资源合集发到公告区，敬请期待！', 30, NOW() - INTERVAL 12 HOUR, NOW() - INTERVAL 12 HOUR),
(40, 7, 8, '太好了！期待管理员的资源合集。学习路上有大家陪伴真的很幸福～', 39, NOW() - INTERVAL 4 HOUR, NOW() - INTERVAL 4 HOUR);

-- =====================================================================
-- 8. 处理提及关系
-- =====================================================================

INSERT INTO post_mentions (post_id, mentioned_user_id) VALUES
-- 帖子中的@提及
(6, 7),   -- moderator 提及 newbie_david
(9, 2),   -- newbie_david 提及 moderator  
(12, 3),  -- backend_bob 提及 developer_jane
(13, 4),  -- developer_jane 提及 backend_bob
(14, 7),  -- newbie_david 被提及
(15, 7),  -- senior_grace 提及 newbie_david
(19, 8),  -- frontend_alice 提及 student_emma
(23, 4),  -- developer_jane 提及 backend_bob
(24, 3),  -- backend_bob 提及 developer_jane
(25, 8),  -- student_emma 被提及
(26, 8),  -- backend_bob 提及 student_emma
(31, 7),  -- developer_jane 提及 newbie_david
(32, 7),  -- senior_grace 提及 newbie_david
(34, 7),  -- frontend_alice 提及 newbie_david
(35, 7),  -- student_emma 提及 newbie_david
(36, 3),  -- newbie_david 提及多人
(36, 10), 
(36, 2), 
(36, 5), 
(36, 8),
(37, 7),  -- devops_charlie 提及 newbie_david
(39, 30); -- admin 在最后的回复中

-- =====================================================================
-- 9. 更新主题统计数据
-- =====================================================================

-- 更新主题的回帖数和最后发帖时间
UPDATE topics t SET 
  reply_count = (SELECT COUNT(*) - 1 FROM posts p WHERE p.topic_id = t.id AND p.is_deleted = 0),
  last_posted_at = (SELECT MAX(p.created_at) FROM posts p WHERE p.topic_id = t.id AND p.is_deleted = 0),
  last_poster_id = (SELECT p.author_id FROM posts p WHERE p.topic_id = t.id AND p.is_deleted = 0 ORDER BY p.created_at DESC LIMIT 1);

-- =====================================================================
-- 10. 增加更多测试主题（简化版）
-- =====================================================================

INSERT INTO topics (title, slug, author_id, category_id, is_pinned, is_locked, reply_count, view_count, last_posted_at, last_poster_id, created_at, updated_at) VALUES
('微服务架构最佳实践', 'microservices-best-practices', 6, 2, 0, 0, 0, 23, NULL, NULL, NOW() - INTERVAL 2 DAY, NOW() - INTERVAL 2 DAY),
('Vue 3 Composition API 使用心得', 'vue3-composition-api', 5, 2, 0, 0, 0, 34, NULL, NULL, NOW() - INTERVAL 1 DAY, NOW() - INTERVAL 1 DAY),
('数据库设计原则讨论', 'database-design-principles', 10, 2, 0, 0, 0, 45, NULL, NULL, NOW() - INTERVAL 18 HOUR, NOW() - INTERVAL 18 HOUR),
('GitHub Actions CI/CD 配置分享', 'github-actions-cicd', 6, 2, 0, 0, 0, 67, NULL, NULL, NOW() - INTERVAL 12 HOUR, NOW() - INTERVAL 12 HOUR),
('移动端响应式设计技巧', 'mobile-responsive-design', 5, 2, 0, 0, 0, 89, NULL, NULL, NOW() - INTERVAL 6 HOUR, NOW() - INTERVAL 6 HOUR);

-- 为新主题创建首帖
INSERT INTO posts (topic_id, author_id, content_md, created_at, updated_at) VALUES
(11, 6, '# 微服务架构最佳实践

在实施微服务架构时，有一些关键的原则和实践...', NOW() - INTERVAL 2 DAY, NOW() - INTERVAL 2 DAY),
(12, 5, '# Vue 3 Composition API 使用心得

从 Options API 迁移到 Composition API 的经验分享...', NOW() - INTERVAL 1 DAY, NOW() - INTERVAL 1 DAY),
(13, 10, '# 数据库设计原则讨论

好的数据库设计是系统成功的基础，让我们讨论一下...', NOW() - INTERVAL 18 HOUR, NOW() - INTERVAL 18 HOUR),
(14, 6, '# GitHub Actions CI/CD 配置分享

分享一套完整的前后端项目CI/CD配置...', NOW() - INTERVAL 12 HOUR, NOW() - INTERVAL 12 HOUR),
(15, 5, '# 移动端响应式设计技巧

移动优先的响应式设计实践经验...', NOW() - INTERVAL 6 HOUR, NOW() - INTERVAL 6 HOUR);

-- 为一些新主题添加标签
INSERT INTO topic_tags (topic_id, tag_id) VALUES
(11, 2), (11, 3),  -- 微服务：讨论、分享
(12, 3),           -- Vue3：分享  
(13, 2),           -- 数据库：讨论
(14, 3),           -- CI/CD：分享
(15, 3);           -- 响应式：分享

-- 更新标签使用计数
UPDATE tags SET usage_count = (
  SELECT COUNT(*) FROM topic_tags WHERE topic_tags.tag_id = tags.id
);

-- =====================================================================
-- 11. 创建一些邮箱验证令牌（测试用）
-- =====================================================================

INSERT INTO email_verification_tokens (user_id, token, expires_at, created_at) VALUES
(11, 'verify_token_henry_' || UNIX_TIMESTAMP(), NOW() + INTERVAL 24 HOUR, NOW());

-- =====================================================================
-- 12. 提交事务
-- =====================================================================

COMMIT;
SET AUTOCOMMIT = 1;
SET FOREIGN_KEY_CHECKS = 1;

-- =====================================================================
-- 验证数据插入结果
-- =====================================================================

SELECT 'Users created:' as Info, COUNT(*) as Count FROM users
UNION ALL
SELECT 'Topics created:' as Info, COUNT(*) as Count FROM topics
UNION ALL  
SELECT 'Posts created:' as Info, COUNT(*) as Count FROM posts
UNION ALL
SELECT 'User roles assigned:' as Info, COUNT(*) as Count FROM user_roles
UNION ALL
SELECT 'Topic-tag relations:' as Info, COUNT(*) as Count FROM topic_tags
UNION ALL
SELECT 'Post mentions:' as Info, COUNT(*) as Count FROM post_mentions;

-- =====================================================================
-- 脚本执行完成
-- =====================================================================

SELECT '🎉 测试数据插入完成！' as Status;
SELECT '📊 数据统计：12个用户，15个主题，45个帖子' as Summary;
SELECT '🔗 包含完整的关联关系：用户角色、主题标签、帖子引用、用户提及' as Features;