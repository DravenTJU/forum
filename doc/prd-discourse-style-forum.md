---
title: "PRD｜Discourse 风格论坛（Vite + React + TypeScript / C# + MySQL + Dapper）"
version: "1.0"
date: "2025-08-18"
---

# PRD｜Vite + React + TypeScript（前端）/ **C# + MySQL + Dapper**（后端） 的 Discourse 风格论坛

> 目标：以 Discourse 的信息架构与交互为参考，实现一个现代、轻量、支持实时回帖的论坛应用。前端选用 **Vite + React + TypeScript**，UI 采用 **shadcn**（Tailwind 驱动），后端为 **ASP.NET Core（C#）+ MySQL**，**不使用 ORM（EF/Core）**，数据访问采用 **Dapper**，实时通信 **SignalR**（前端使用 `@microsoft/signalr`）。

## 1. 背景 & 目标
- **业务目标**
  - 降低发帖与回帖门槛，提供 Markdown 编辑与引用、@ 提及。
  - 按 **分类（Category）** 与 **标签（Tag）** 组织主题内容，便于筛选与发现。
  - **实时**：主题详情页内的新回帖、编辑、删除在 ≤1s 内同步到在线用户。
- **技术目标**
  - 前端基于 **Vite + TypeScript** 的快速开发体验（HMR、构建产物小）。
  - UI 采用 **shadcn** 组件库（与 Radix 生态兼容），保持一致的设计语言。
  - 后端框架：**ASP.NET Core 8 Web API**  
  - 数据库：**MySQL 8.x（InnoDB, utf8mb4）**  
  - 数据访问：**Dapper**（参数化 SQL，仓储/Query 对象模式），**不使用 ORM**  
  - 实时：**SignalR Hub**（Topic 房间）；前端由 `socket.io-client` 切换为 `@microsoft/signalr`  
  - 搜索：MySQL **FULLTEXT**（`topics.title`、`posts.content_md`），`MATCH ... AGAINST`；中文分词建议后续接入 ElasticSearch 或 Meilisearch  
  - 分页：**Keyset**（基于 `(created_at, id)`）避免深分页  
  - 事务一致性：发帖写入与统计更新使用 **事务**；并发采用 **乐观并发**（`updated_at` 校验）

## 2. 术语
- **主题（Topic）**：一级内容，包含标题与正文（首帖），可被分类/打标签。
- **帖子（Post）**：主题下的楼层（含首帖与回帖），支持引用、@ 提及。
- **分类（Category）**：板块；**标签（Tag）**：更细粒度的话题标记。
- **用户（User）**：角色分为访客、注册用户、版主（Moderator）、管理员（Admin）。

## 3. 用户角色与权限
| 角色 | 能力 |
|---|---|
| 访客 | 浏览主题/帖子、按分类/标签筛选、搜索 |
| 注册用户 | 注册、登录、发主题、回帖、在限时内编辑/删除自己的内容、@ 提及、订阅、收到站内通知 |
| 版主 | 置顶/锁定/移动主题、编辑任意主题与帖子、管理标签 |
| 管理员 | 管理分类/标签/用户、封禁、系统设置 |

> **编辑限时窗口**：用户发布后 10 分钟内可编辑；超时需要版主/管理员操作。

## 4. 成功指标（KPI）
- 注册转化率、首帖转化率、DAU/WAU。
- **实时延迟 P95 ≤ 1s**（同主题房间内的消息广播端到端）。
- 内容生产率（每 DAU 主题/回帖数）。
- 搜索点击率与回访率。

## 5. 范围（MVP）
**包含**
- 注册/登录（邮箱+密码，邮箱验证），退出、重置密码。
- 主题：创建、编辑、删除、浏览、分页/无限加载。
- 帖子：回帖、编辑、删除、引用、@ 提及。
- 分类与标签：创建（Admin/Mod）、列表、筛选。
- **实时**：主题详情页内的创建/编辑/删除广播；“正在输入”指示。
- 通知：被 @/被回复时站内通知（页内铃铛入口）。
- 搜索：标题+正文的全文检索（MySQL FULLTEXT 文本索引）。
- 基础管理：分类/标签 CRUD、用户封禁（Admin）。

**不包含（MVP 之外）**
- 私信系统、复杂勋章/积分体系、富媒体上传（图片/附件）、SSO、移动 App、邮件群发。

## 6. 关键用户故事
1. 新用户注册并验证邮箱，创建首个主题。
2. 多用户同时在主题页中，任意用户回帖，其他人 **≤1s** 内看到更新。
3. 用户通过分类与标签筛选主题，或用关键词搜索并跳转到对应楼层。
4. 版主可锁定/置顶/移动主题；被锁定主题不允许新增回帖。
5. 被 @ 的用户在站内收到通知并可一键定位。

## 7. 信息架构（IA）与导航
- **主导航**：最新 / 热门 / 分类 / 标签 / 搜索 / 登录（或用户菜单）
- **页面**
  - `/` 最新主题流（支持分类/标签筛选）
  - `/t/:topicId/:slug` 主题详情（首帖 + 回帖列表，实时）
  - `/new` 发主题（选择分类、标签）
  - `/c` 分类列表，`/tags` 标签列表
  - `/u/:username` 个人页
  - `/admin` 管理后台：分类/标签/用户
  - `/login` `/register` `/reset-password`

## 8. 交互 & UI（基于 shadcn）
- **通用**：`Button` `Avatar` `Badge` `Card` `DropdownMenu` `Tooltip` `Dialog` `AlertDialog` `Form` `Input` `Textarea` `Tabs` `Breadcrumb` `Separator` `ScrollArea` `Skeleton` `Pagination` `Toast` `Sheet` `Command`（命令面板式搜索）
- **主题列表**：标题 + 标签 `Badge`、作者头像、分类、最后回帖时间、回帖数。
- **主题详情**：首帖置顶；楼层 `Card`；引用块；楼层锚点；回帖编辑区（`Textarea` + Markdown 预览）；`Toast` 提示。
- **编辑器**：Markdown（粗体/斜体/代码/引用/列表/链接），实时字数；离开未保存内容前 `Dialog` 确认。
- **通知**：右上角铃铛 `DropdownMenu`，未读数 `Badge`。
- **搜索**：顶部搜索框 + `Command` 快速跳转（主题/用户/标签）。

### 8.1 shadcn 与 Vite 约定
- 采用 **Tailwind CSS**；组件通过 CLI 拉取到本地 `components` 目录（不依赖 RSC）。
- 建议安装：`clsx`、`tailwind-merge`、`lucide-react`、`sonner`（Toast）。
- 在 Vite 环境按需调整导入路径与别名。

## 9. 技术架构（选择 **Vite** 的实现细节）
### 9.1 前端
- **技术栈**：Vite + React + TypeScript + React Router + TanStack Query + Axios/Fetch
- **表单与校验**：`react-hook-form` + `zod`
- **状态**：以 **TanStack Query** 为服务端状态主导；组件内局部 `useState`；尽量避免全局 store。
- **实时**：用 **`@microsoft/signalr`** 客户端；进入主题详情后加入房间。
- **安全**：HttpOnly Cookie 承载 Token；Markdown 渲染后进行 **服务端消毒 + 前端只读渲染**。

**路径别名与配置示例**
```ts
// tsconfig.json（片段）
{
  "compilerOptions": {
    "baseUrl": ".",
    "paths": { "@/*": ["src/*"] },
    "strict": true
  }
}
```

```ts
// vite.config.ts（片段）
import { fileURLToPath, URL } from "node:url";
import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      "@": fileURLToPath(new URL("./src", import.meta.url))
    }
  },
  server: {
    proxy: {
      "/api": "http://localhost:4000",
      "/hubs/*": {
        target: "http://localhost:4000",
        ws: true
      }
    }
  }
});
```

**Tailwind 与 shadcn（片段）**
```ts
// tailwind.config.ts（片段）
import { fontFamily } from "tailwindcss/defaultTheme";
export default {
  content: ["./index.html", "./src/**/*.{ts,tsx}"],
  theme: {
    extend: {
      fontFamily: {
        sans: ["Inter", ...fontFamily.sans]
      }
    }
  },
  plugins: []
};
```

**SignalR 客户端片段**
```ts
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
  .withUrl("/hubs/topics", {
    accessTokenFactory: () => getAccessTokenFromCookie()
  })
  .withAutomaticReconnect()
  .build();

await connection.start();
await connection.invoke("JoinTopic", topicId);

connection.on("PostCreated", (payload) => {/* 插入新楼层 */});
connection.on("PostEdited", (payload) => {/* 更新楼层 */});
connection.on("PostDeleted", (payload) => {/* 标记删除 */});
connection.on("UserTyping", (payload) => {/* 展示 typing 指示 */});
```

### 9.2 后端（C# / ASP.NET Core / Dapper）
- **框架**：ASP.NET Core 8 Web API（Kestrel）
- **数据库**：MySQL 8.x（InnoDB, utf8mb4）
- **数据访问**：Dapper（参数化 SQL；`IDbConnection` 基于 `MySqlConnector`）
- **实时**：SignalR（`TopicsHub`）；多实例扩展可用 **Redis Backplane** 或 Azure SignalR
- **认证**：JWT（Access + Refresh），HttpOnly + Secure Cookie；Refresh Token 存库（哈希）
- **邮件**：MailKit/SMTP
- **输入校验**：FluentValidation 或最小 API + DataAnnotations
- **安全**：`UseCors` 白名单、`UseRateLimiter`、`X-Content-Type-Options` 等安全头
- **迁移**：SQL 脚本 + **DbUp**（启动迁移）或 **Flyway/Liquibase**（外部）

**Dapper 访问片段（示例）**
```csharp
using Dapper;
using MySqlConnector;

public async Task<IEnumerable<TopicListItem>> GetTopicsAsync(TopicQuery q) {
    const string sql = @"
    SELECT t.id, t.title, t.slug, t.reply_count, t.view_count, t.last_posted_at,
           u.id AS author_id, u.username AS author_username,
           c.id AS category_id, c.name AS category_name
    FROM topics t
    JOIN users u ON u.id = t.author_id
    JOIN categories c ON c.id = t.category_id
    LEFT JOIN topic_tags tt ON tt.topic_id = t.id
    LEFT JOIN tags g ON g.id = tt.tag_id
    WHERE t.is_deleted = 0
      AND (@categoryId IS NULL OR t.category_id = @categoryId)
      AND (@tagSlug IS NULL OR g.slug = @tagSlug)
      AND (@cursorLast IS NULL OR 
           (t.last_posted_at < @cursorLast OR 
            (t.last_posted_at = @cursorLast AND t.id < @cursorId)))
    GROUP BY t.id
    ORDER BY t.is_pinned DESC, t.last_posted_at DESC, t.id DESC
    LIMIT @limit;";

    await using var conn = new MySqlConnection(_connStr);
    return await conn.QueryAsync<TopicListItem>(sql, new {
        q.CategoryId, q.TagSlug, q.CursorLast, q.CursorId, limit = q.Limit
    });
}
```

## 10. 数据模型（关系型 / MySQL DDL）

> 字符集 `utf8mb4`, 排序规则建议 `utf8mb4_0900_ai_ci`；所有时间戳使用 `DATETIME(3)`；布尔使用 `TINYINT(1)`。

```sql
-- Users & Roles
CREATE TABLE users (
  id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  username VARCHAR(20) NOT NULL UNIQUE,
  email VARCHAR(255) NOT NULL UNIQUE,
  password_hash VARCHAR(100) NOT NULL,
  status ENUM('active','suspended') NOT NULL DEFAULT 'active',
  email_verified TINYINT(1) NOT NULL DEFAULT 0,
  avatar_url VARCHAR(500),
  bio TEXT,
  last_seen_at DATETIME(3) NULL,
  created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  updated_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE user_roles (
  user_id BIGINT UNSIGNED NOT NULL,
  role ENUM('user','mod','admin') NOT NULL,
  PRIMARY KEY (user_id, role),
  CONSTRAINT fk_user_roles_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Categories, Tags, Moderators
CREATE TABLE categories (
  id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  name VARCHAR(100) NOT NULL,
  slug VARCHAR(100) NOT NULL UNIQUE,
  description TEXT,
  `order` INT NOT NULL DEFAULT 0,
  is_archived TINYINT(1) NOT NULL DEFAULT 0,
  created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  updated_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE category_moderators (
  category_id BIGINT UNSIGNED NOT NULL,
  user_id BIGINT UNSIGNED NOT NULL,
  PRIMARY KEY (category_id, user_id),
  CONSTRAINT fk_cat_mod_cat FOREIGN KEY (category_id) REFERENCES categories(id) ON DELETE CASCADE,
  CONSTRAINT fk_cat_mod_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE tags (
  id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  name VARCHAR(100) NOT NULL,
  slug VARCHAR(100) NOT NULL UNIQUE,
  description VARCHAR(255),
  color VARCHAR(20),
  created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  updated_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Topics & Posts
CREATE TABLE topics (
  id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  title VARCHAR(200) NOT NULL,
  slug VARCHAR(220) NOT NULL,
  author_id BIGINT UNSIGNED NOT NULL,
  category_id BIGINT UNSIGNED NOT NULL,
  is_pinned TINYINT(1) NOT NULL DEFAULT 0,
  is_locked TINYINT(1) NOT NULL DEFAULT 0,
  is_deleted TINYINT(1) NOT NULL DEFAULT 0,
  reply_count INT NOT NULL DEFAULT 0,
  view_count INT NOT NULL DEFAULT 0,
  last_posted_at DATETIME(3) NULL,
  last_poster_id BIGINT UNSIGNED NULL,
  created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  updated_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
  CONSTRAINT fk_topics_author FOREIGN KEY (author_id) REFERENCES users(id),
  CONSTRAINT fk_topics_category FOREIGN KEY (category_id) REFERENCES categories(id),
  CONSTRAINT fk_topics_lastposter FOREIGN KEY (last_poster_id) REFERENCES users(id),
  UNIQUE KEY uq_topic_slug (category_id, slug),
  INDEX idx_topic_cat_last (category_id, is_deleted, last_posted_at DESC, id DESC),
  FULLTEXT KEY ftx_topic_title (title)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE topic_tags (
  topic_id BIGINT UNSIGNED NOT NULL,
  tag_id BIGINT UNSIGNED NOT NULL,
  PRIMARY KEY (topic_id, tag_id),
  CONSTRAINT fk_tt_topic FOREIGN KEY (topic_id) REFERENCES topics(id) ON DELETE CASCADE,
  CONSTRAINT fk_tt_tag FOREIGN KEY (tag_id) REFERENCES tags(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE posts (
  id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  topic_id BIGINT UNSIGNED NOT NULL,
  author_id BIGINT UNSIGNED NOT NULL,
  content_md MEDIUMTEXT NOT NULL,
  content_html MEDIUMTEXT NOT NULL,
  reply_to_post_id BIGINT UNSIGNED NULL,
  is_edited TINYINT(1) NOT NULL DEFAULT 0,
  is_deleted TINYINT(1) NOT NULL DEFAULT 0,
  deleted_at DATETIME(3) NULL,
  created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  updated_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
  CONSTRAINT fk_posts_topic FOREIGN KEY (topic_id) REFERENCES topics(id) ON DELETE CASCADE,
  CONSTRAINT fk_posts_author FOREIGN KEY (author_id) REFERENCES users(id),
  CONSTRAINT fk_posts_reply_to FOREIGN KEY (reply_to_post_id) REFERENCES posts(id),
  INDEX idx_posts_topic_created (topic_id, created_at, id),
  FULLTEXT KEY ftx_posts_content (content_md)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE post_mentions (
  post_id BIGINT UNSIGNED NOT NULL,
  mentioned_user_id BIGINT UNSIGNED NOT NULL,
  PRIMARY KEY (post_id, mentioned_user_id),
  CONSTRAINT fk_pm_post FOREIGN KEY (post_id) REFERENCES posts(id) ON DELETE CASCADE,
  CONSTRAINT fk_pm_user FOREIGN KEY (mentioned_user_id) REFERENCES users(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Notifications & Tokens & Audit
CREATE TABLE notifications (
  id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  user_id BIGINT UNSIGNED NOT NULL,
  type ENUM('mention','reply','system') NOT NULL,
  topic_id BIGINT UNSIGNED NULL,
  post_id BIGINT UNSIGNED NULL,
  by_user_id BIGINT UNSIGNED NULL,
  snippet VARCHAR(200),
  read_at DATETIME(3) NULL,
  created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  CONSTRAINT fk_n_user FOREIGN KEY (user_id) REFERENCES users(id),
  CONSTRAINT fk_n_topic FOREIGN KEY (topic_id) REFERENCES topics(id),
  CONSTRAINT fk_n_post FOREIGN KEY (post_id) REFERENCES posts(id),
  CONSTRAINT fk_n_byuser FOREIGN KEY (by_user_id) REFERENCES users(id),
  INDEX idx_n_user_created (user_id, created_at DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE refresh_tokens (
  id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  user_id BIGINT UNSIGNED NOT NULL,
  token_hash BINARY(32) NOT NULL,
  expires_at DATETIME(3) NOT NULL,
  revoked_at DATETIME(3) NULL,
  ua VARCHAR(200) NULL,
  ip VARCHAR(45) NULL,
  created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  UNIQUE KEY uq_token_hash (token_hash),
  INDEX idx_rt_user (user_id),
  INDEX idx_rt_expires (expires_at),
  CONSTRAINT fk_rt_user FOREIGN KEY (user_id) REFERENCES users(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE audit_logs (
  id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  actor_user_id BIGINT UNSIGNED NOT NULL,
  action VARCHAR(50) NOT NULL,
  target_type VARCHAR(50) NOT NULL,
  target_id BIGINT UNSIGNED NULL,
  before_json JSON NULL,
  after_json JSON NULL,
  created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  CONSTRAINT fk_audit_user FOREIGN KEY (actor_user_id) REFERENCES users(id),
  INDEX idx_audit_created (created_at DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

**索引与分页说明**
- 主题列表：`ORDER BY is_pinned DESC, last_posted_at DESC, id DESC` + 索引 `idx_topic_cat_last`
- 回帖列表：`WHERE topic_id=? AND (created_at, id) > (cursorCreated, cursorId)` + 索引 `idx_posts_topic_created`
- 搜索：`FULLTEXT` 命中优先；必要时回落 `LIKE`（小数据集或低相关度）

## 11. API 设计（REST + JSON，/api/v1）
> 非 GET 请求需 CSRF 令牌（双提交）与 Access Token（HttpOnly Cookie）。

### 11.1 Auth
- `POST /auth/register`  `{username,email,password}`
- `POST /auth/login`     `{email,password}`
- `POST /auth/logout`
- `POST /auth/refresh`
- `POST /auth/verify-request`  `{email}`  → 发送验证邮件
- `POST /auth/verify`          `{token}`
- `POST /auth/forgot`          `{email}`
- `POST /auth/reset`           `{token,newPassword}`
- `GET  /auth/me`              → 当前用户信息

### 11.2 Category & Tag
- `GET  /categories` / `POST /categories`（Admin）
- `PATCH /categories/:id` / `DELETE /categories/:id`（Admin）
- `GET  /tags` / `POST /tags`（Mod+）
- `PATCH /tags/:id` / `DELETE /tags/:id`（Mod+）

### 11.3 Topic
- `GET  /topics?cursor=&limit=&categoryId=&tag=&sort=latest|hot`
- `POST /topics`  `{title, contentMd, categoryId, tagSlugs[]}`
- `GET  /topics/:id`
- `PATCH /topics/:id`（作者限时或 Mod+，**必须携带 updatedAt 字段用于乐观并发控制**）
- `DELETE /topics/:id`（作者限时或 Mod+）
- 管理：`POST /topics/:id/pin` / `POST /topics/:id/lock` / `POST /topics/:id/move`

**创建主题响应示例**
```json
{
  "id": "t_123",
  "title": "TypeScript 最佳实践",
  "categoryId": "c_dev",
  "tagSlugs": ["typescript","best-practices"],
  "stats": {"replyCount":0,"viewCount":0},
  "createdAt":"2025-08-18T00:00:00Z"
}
```

### 11.4 Post
- `GET  /topics/:topicId/posts?cursor=&limit=`
- `POST /topics/:topicId/posts` `{contentMd, replyToPostId?}`
- `PATCH /posts/:id`（**必须携带 updatedAt 字段，否则返回 400 错误**）
- `DELETE /posts/:id`

### 11.5 Search & Notifications
- `GET /search?q=&tag=&categoryId=`
- `GET /notifications?unreadOnly=true`
- `POST /notifications/:id/read`

**事务与并发（关键路径）**
- 发帖：`BEGIN; INSERT post; UPDATE topics SET reply_count=reply_count+1,last_posted_at=NOW(3),last_poster_id=@uid WHERE id=@topicId; COMMIT;`
- 删帖：逻辑删除后需事务内同步更新 `reply_count`；定期任务校正统计字段防止不一致
- 编辑：PATCH 请求必须携带 `updatedAt` 字段，与数据库 `updated_at` 匹配（乐观并发），不匹配返回 409；缺少字段返回 400。

## 12. 实时通信（**SignalR**）

**连接与认证**
- 使用 `Bearer`（从 HttpOnly Cookie 交换成短期 AccessToken）或使用带 Cookie 的协商端点
- Hub：`/hubs/topics`

**Hub 方法（示例）**
- 客户端 → 服务端
  - `JoinTopic(topicId: long)` / `LeaveTopic(topicId: long)`
  - `Typing(topicId: long, isTyping: bool)`
- 服务端 → 客户端（组播到 `topic:{id}`）
  - `PostCreated(payload)`
  - `PostEdited(payload)`
  - `PostDeleted(payload)`
  - `TopicStats(payload)`
  - `UserTyping(payload)`

**扩展**
- 多实例部署使用 **Redis backplane**（`AddSignalR().AddStackExchangeRedis(...)`）。

## 13. 权限与安全
- **认证**：JWT（Access 短期、Refresh 长期），HttpOnly + `Secure` Cookie；Refresh Token 存库可撤销。
- **邮箱验证**：未验证邮箱禁止发帖。
- **输入校验**：后端 `zod`；前端同构模型与类型。
- **XSS**：后端 Markdown 渲染后 HTML 消毒；前端只读渲染；禁用行内事件与脚本。
- **限流**：基于 IP+用户维度（登录、发帖、搜索）。
- **CSRF**：双提交 Cookie；仅允许同源请求。
- **审计**：版主/管理员敏感操作写入审计日志（AuditLog）。
- **内容政策**：屏蔽词；被举报阈值自动隐藏（后续版本）。

## 14. 性能与可扩展性
- Keyset 分页；热点列表 LRU 缓存（内存/Redis）
- 监控：ASP.NET Metrics（Prometheus 可选）、日志：Serilog（JSON）
- SLO：API P95 < 200ms；实时广播到达 P95 < 1s
- 索引：`idx_topic_cat_last`, `idx_posts_topic_created`，以及 FULLTEXT

**索引**
```sql
ALTER TABLE topics ADD FULLTEXT ftx_topic_title (title);
ALTER TABLE posts  ADD FULLTEXT ftx_posts_content (content_md);
```

**查询（示例）**
```sql
-- 组合搜索（主题标题 + 回帖正文），按相关度排序
(SELECT 'topic' AS type, t.id AS topic_id, NULL AS post_id,
        MATCH(t.title) AGAINST (@q IN NATURAL LANGUAGE MODE) AS score,
        t.title AS title, NULL AS snippet, t.last_posted_at
 FROM topics t
 WHERE MATCH(t.title) AGAINST (@q IN NATURAL LANGUAGE MODE))
UNION ALL
(SELECT 'post' AS type, p.topic_id, p.id,
        MATCH(p.content_md) AGAINST (@q IN NATURAL LANGUAGE MODE) AS score,
        NULL AS title, SUBSTRING(p.content_md,1,200) AS snippet, p.created_at AS last_posted_at
 FROM posts p
 WHERE MATCH(p.content_md) AGAINST (@q IN NATURAL LANGUAGE MODE))
ORDER BY score DESC, last_posted_at DESC
LIMIT @limit OFFSET @offset;
```

## 15. 可用性、i18n 与 SEO
- **无障碍（a11y）**：键盘可达、语义化 HTML、ARIA 属性、对比度达 WCAG AA。
- **i18n**：默认中文，预留 i18next 方案。
- **SEO**：默认 CSR；如需提升抓取，可使用预渲染（Prerender）或后续引入 Vite SSR/SSG。

## 16. 日志、监控与告警
- **日志**：Serilog（结构化，按请求/事件）
- **指标**：请求时延 P95/99、错误率、SignalR 消息延迟分布
- **告警**：阈值告警（错误率、队列堆积、连接数异常）

## 17. 环境变量（示例）
```
APP_URL
ASPNETCORE_ENVIRONMENT              # Development/Staging/Production
MYSQL_HOST, MYSQL_PORT, MYSQL_DB, MYSQL_USER, MYSQL_PASSWORD
MYSQL_MAX_POOL                      # 连接池大小
JWT_SECRET, JWT_EXPIRES, REFRESH_EXPIRES
SMTP_HOST, SMTP_PORT, SMTP_USER, SMTP_PASS, EMAIL_FROM
CORS_ORIGINS
SIGNALR_REDIS_URL                   # 多实例可选
CSRF_COOKIE_NAME, CSRF_HEADER_NAME
```

## 18. 里程碑（增量交付）
- **M0**：脚手架（Vite + TS、ESLint/Prettier、Tailwind、shadcn、ASP.NET Core、Dapper/连接池、SignalR 基座）
- **M1**：认证（注册/登录/邮箱验证/重置密码）
- **M2**：主题/帖子 CRUD + Markdown 渲染与消毒
- **M3**：分类/标签与筛选、搜索（FULLTEXT）
- **M4**：**实时**（创建/编辑/删除广播、typing 指示）
- **M5**：通知与基础管理（分类/标签、封禁）
- **M6**：优化与打磨（索引、缓存、监控、a11y、SEO/预渲染）

## 19. 验收标准（节选）
- **注册与登录**：验证邮件链路可用；错误反馈清晰。
- **发主题与回帖**：另一浏览器页签 ≤1s 收到新帖；Markdown 正确渲染；脚本被过滤。
- **分类与标签**：创建主题时可选；列表页筛选正确。
- **实时**：A/B 同页，A 回帖/编辑/删除，B 在 1s 内收到相应事件并正确渲染。
- **权限**：非作者不可越权编辑/删除；封禁用户无法发帖；被锁定主题不可回帖。
- **搜索**：关键词检索返回准确结果；可跳转定位楼层。
- **通知**：@ 他人会产生通知并可跳转定位。
- **安全性能**：XSS/CSRF/限流生效；API P95 < 200ms。

## 20. 风险与对策
- **邮件送达率**：使用可靠 SMTP/供应商并监控退信。
- **SQL 注入风险**：Dapper 全量使用参数化；拒绝字符串拼接
- **迁移管理**：采用 DbUp/Flyway，流水化执行 SQL，防止漂移
- **FULLTEXT 相关度**：MySQL FULLTEXT 在中文环境分词效果有限，建议生产环境接入 ElasticSearch 或 Meilisearch；结合时间衰减或二次排序优化结果质量
- **SignalR 扩展**：生产环境启用 Redis backplane；Nginx 保持 WS 升级
- **Markdown XSS**：强制后端消毒 + 白名单渲染；增加安全测试。
- **shadcn 在 Vite 的适配**：固定组件导入策略，抽离 UI 基础层，提供团队规范文档。

## 21. 附录
### 附录 A：发帖事务（伪代码）
```csharp
await using var conn = new MySqlConnection(cs);
await conn.OpenAsync();
await using var tx = await conn.BeginTransactionAsync();

var postId = await conn.ExecuteScalarAsync<long>(@"
  INSERT INTO posts (topic_id, author_id, content_md, content_html)
  VALUES (@TopicId, @UserId, @Md, @Html);
  SELECT LAST_INSERT_ID();", new { TopicId, UserId, Md, Html }, tx);

await conn.ExecuteAsync(@"
  UPDATE topics
  SET reply_count = reply_count + 1,
      last_posted_at = NOW(3),
      last_poster_id = @UserId,
      updated_at = NOW(3)
  WHERE id = @TopicId;", new { TopicId, UserId }, tx);

await tx.CommitAsync();
```

### 附录 B：SignalR Hub（片段）
```csharp
public class TopicsHub : Hub
{
    public Task JoinTopic(long topicId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, $"topic:{topicId}");

    public Task LeaveTopic(long topicId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, $"topic:{topicId}");
}
```

### 附录 C：搜索降级（示意）
```sql
-- 当 FULLTEXT 无法命中或 QPS 波动时，可回落到 LIKE（仅小范围）
SELECT id, title FROM topics WHERE title LIKE CONCAT('%', @q, '%') LIMIT 50;
```