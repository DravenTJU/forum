# 论坛系统前后端接口对接文档

> **版本**: 1.0  
> **日期**: 2025-08-21  
> **技术栈**: Vite + React + TypeScript / ASP.NET Core + MySQL + Dapper

## 1. 概述

本文档定义了基于 Discourse 风格论坛系统的前后端 API 接口规范，涵盖用户认证、主题管理、回帖、实时通信等核心功能。

### 1.1 基本约定

- **Base URL**: `/api/v1`
- **内容类型**: `application/json`
- **字符编码**: UTF-8
- **时间格式**: ISO 8601 (`YYYY-MM-DDTHH:mm:ss.fffZ`)
- **认证方式**: JWT Token (HttpOnly Cookie)
- **CSRF 保护**: 非 GET 请求需要 CSRF Token

### 1.2 HTTP 状态码

| 状态码 | 说明 | 使用场景 |
|--------|------|----------|
| 200 | OK | 请求成功 |
| 201 | Created | 资源创建成功 |
| 204 | No Content | 删除成功，无返回内容 |
| 400 | Bad Request | 请求参数错误 |
| 401 | Unauthorized | 未认证 |
| 403 | Forbidden | 无权限 |
| 404 | Not Found | 资源不存在 |
| 409 | Conflict | 乐观并发冲突 |
| 429 | Too Many Requests | 超出限流 |
| 500 | Internal Server Error | 服务器错误 |

## 2. 公共数据结构

### 2.1 通用响应格式

```typescript
interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: {
    code: string;
    message: string;
    details?: Record<string, string[]>; // 字段级错误
  };
  meta?: {
    total?: number;
    hasNext?: boolean;
    nextCursor?: string;
  };
}
```

### 2.2 分页参数

```typescript
interface PaginationQuery {
  cursor?: string;  // Keyset 分页游标
  limit?: number;   // 每页数量，默认 20，最大 100
}
```

### 2.3 用户信息

```typescript
interface User {
  id: string;
  username: string;
  email: string;
  emailVerified: boolean;
  avatarUrl?: string;
  bio?: string;
  status: 'active' | 'suspended';
  roles: Array<'user' | 'mod' | 'admin'>;
  lastSeenAt?: string;
  createdAt: string;
}

interface UserProfile extends User {
  topicCount: number;
  postCount: number;
}
```

### 2.4 分类与标签

```typescript
interface Category {
  id: string;
  name: string;
  slug: string;
  description?: string;
  order: number;
  isArchived: boolean;
  topicCount?: number;
}

interface Tag {
  id: string;
  name: string;
  slug: string;
  description?: string;
  color?: string;
  topicCount?: number;
}
```

### 2.5 主题信息

```typescript
interface Topic {
  id: string;
  title: string;
  slug: string;
  author: {
    id: string;
    username: string;
    avatarUrl?: string;
  };
  category: {
    id: string;
    name: string;
    slug: string;
  };
  tags: Tag[];
  isPinned: boolean;
  isLocked: boolean;
  isDeleted: boolean;
  replyCount: number;
  viewCount: number;
  lastPostedAt?: string;
  lastPoster?: {
    id: string;
    username: string;
  };
  createdAt: string;
  updatedAt: string;
}

interface TopicDetail extends Topic {
  firstPost: Post;
}
```

### 2.6 帖子信息

```typescript
interface Post {
  id: string;
  topicId: string;
  author: {
    id: string;
    username: string;
    avatarUrl?: string;
  };
  contentMd: string;
  contentHtml?: string; // 服务端渲染的 HTML（可选缓存）
  replyToPost?: {
    id: string;
    author: string;
    excerpt: string;
  };
  mentions: string[]; // 被 @ 的用户名列表
  isEdited: boolean;
  isDeleted: boolean;
  createdAt: string;
  updatedAt: string;
}
```

## 3. 认证 API

### 3.1 用户注册

```http
POST /api/v1/auth/register
Content-Type: application/json

{
  "username": "john_doe",
  "email": "john@example.com",
  "password": "SecurePass123"
}
```

**响应**:
```json
{
  "success": true,
  "data": {
    "message": "注册成功，请查看邮箱验证邮件"
  }
}
```

### 3.2 用户登录

```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "email": "john@example.com",
  "password": "SecurePass123"
}
```

**响应**:
```json
{
  "success": true,
  "data": {
    "user": {
      "id": "u_123",
      "username": "john_doe",
      "email": "john@example.com",
      "emailVerified": true,
      "roles": ["user"]
    },
    "csrfToken": "abc123..."
  }
}
```

### 3.3 获取当前用户

```http
GET /api/v1/auth/me
```

**响应**:
```json
{
  "success": true,
  "data": {
    "id": "u_123",
    "username": "john_doe",
    "email": "john@example.com",
    "emailVerified": true,
    "roles": ["user"],
    "lastSeenAt": "2025-08-21T10:30:00.000Z"
  }
}
```

### 3.4 邮箱验证

```http
POST /api/v1/auth/verify
Content-Type: application/json

{
  "token": "verification_token_here"
}
```

### 3.5 退出登录

```http
POST /api/v1/auth/logout
```

## 4. 主题 API

### 4.1 主题列表

```http
GET /api/v1/topics?cursor=&limit=20&categoryId=c_dev&tag=typescript&sort=latest
```

**查询参数**:
- `cursor`: 分页游标
- `limit`: 每页数量 (1-100, 默认 20)
- `categoryId`: 分类 ID 过滤
- `tag`: 标签 slug 过滤
- `sort`: 排序方式 (`latest`, `hot`)

**响应**:
```json
{
  "success": true,
  "data": [
    {
      "id": "t_123",
      "title": "TypeScript 最佳实践讨论",
      "slug": "typescript-best-practices",
      "author": {
        "id": "u_456",
        "username": "developer",
        "avatarUrl": "https://example.com/avatar.jpg"
      },
      "category": {
        "id": "c_dev",
        "name": "开发讨论",
        "slug": "development"
      },
      "tags": [
        {
          "id": "tag_1",
          "name": "TypeScript",
          "slug": "typescript",
          "color": "#3178c6"
        }
      ],
      "isPinned": false,
      "isLocked": false,
      "replyCount": 15,
      "viewCount": 234,
      "lastPostedAt": "2025-08-21T09:30:00.000Z",
      "lastPoster": {
        "id": "u_789",
        "username": "expert_dev"
      },
      "createdAt": "2025-08-20T14:00:00.000Z"
    }
  ],
  "meta": {
    "hasNext": true,
    "nextCursor": "eyJjcmVhdGVkX2F0IjoiMjAyNS0wOC0yMFQxNDowMDowMC4wMDBaIiwiaWQiOiJ0XzEyMyJ9"
  }
}
```

### 4.2 创建主题

```http
POST /api/v1/topics
Content-Type: application/json
X-CSRF-Token: abc123...

{
  "title": "新的讨论主题",
  "contentMd": "# 主题内容\n\n这是一个关于 TypeScript 的讨论...",
  "categoryId": "c_dev",
  "tagSlugs": ["typescript", "best-practices"]
}
```

**响应**:
```json
{
  "success": true,
  "data": {
    "id": "t_124",
    "title": "新的讨论主题",
    "slug": "new-discussion-topic",
    "categoryId": "c_dev",
    "tagSlugs": ["typescript", "best-practices"],
    "stats": {
      "replyCount": 0,
      "viewCount": 1
    },
    "createdAt": "2025-08-21T10:45:00.000Z"
  }
}
```

### 4.3 获取主题详情

```http
GET /api/v1/topics/t_123
```

**响应**:
```json
{
  "success": true,
  "data": {
    "id": "t_123",
    "title": "TypeScript 最佳实践讨论",
    "slug": "typescript-best-practices",
    "author": {
      "id": "u_456",
      "username": "developer",
      "avatarUrl": "https://example.com/avatar.jpg"
    },
    "category": {
      "id": "c_dev",
      "name": "开发讨论",
      "slug": "development"
    },
    "tags": [
      {
        "id": "tag_1",
        "name": "TypeScript",
        "slug": "typescript",
        "color": "#3178c6"
      }
    ],
    "isPinned": false,
    "isLocked": false,
    "replyCount": 15,
    "viewCount": 235,
    "createdAt": "2025-08-20T14:00:00.000Z",
    "updatedAt": "2025-08-21T09:30:00.000Z",
    "firstPost": {
      "id": "p_1",
      "contentMd": "# TypeScript 最佳实践\n\n大家来讨论一下...",
      "contentHtml": "<h1>TypeScript 最佳实践</h1><p>大家来讨论一下...</p>",
      "author": {
        "id": "u_456",
        "username": "developer"
      },
      "createdAt": "2025-08-20T14:00:00.000Z"
    }
  }
}
```

### 4.4 编辑主题

```http
PATCH /api/v1/topics/t_123
Content-Type: application/json
X-CSRF-Token: abc123...

{
  "title": "更新后的标题",
  "categoryId": "c_general",
  "tagSlugs": ["general-discussion"],
  "updatedAt": "2025-08-21T09:30:00.000Z"
}
```

**注意**: `updatedAt` 字段用于乐观并发控制，必须传递当前版本的时间戳。

### 4.5 删除主题

```http
DELETE /api/v1/topics/t_123
X-CSRF-Token: abc123...
```

### 4.6 主题管理操作

#### 置顶主题
```http
POST /api/v1/topics/t_123/pin
X-CSRF-Token: abc123...
```

#### 锁定主题
```http
POST /api/v1/topics/t_123/lock
X-CSRF-Token: abc123...
```

#### 移动主题
```http
POST /api/v1/topics/t_123/move
Content-Type: application/json
X-CSRF-Token: abc123...

{
  "categoryId": "c_general"
}
```

## 5. 帖子 API

### 5.1 获取主题下的帖子列表

```http
GET /api/v1/topics/t_123/posts?cursor=&limit=20
```

**响应**:
```json
{
  "success": true,
  "data": [
    {
      "id": "p_2",
      "topicId": "t_123",
      "author": {
        "id": "u_789",
        "username": "expert_dev",
        "avatarUrl": "https://example.com/avatar2.jpg"
      },
      "contentMd": "我觉得使用 **严格模式** 是最重要的...",
      "contentHtml": "<p>我觉得使用 <strong>严格模式</strong> 是最重要的...</p>",
      "replyToPost": {
        "id": "p_1",
        "author": "developer",
        "excerpt": "大家来讨论一下..."
      },
      "mentions": ["developer"],
      "isEdited": false,
      "isDeleted": false,
      "createdAt": "2025-08-20T15:30:00.000Z",
      "updatedAt": "2025-08-20T15:30:00.000Z"
    }
  ],
  "meta": {
    "hasNext": true,
    "nextCursor": "eyJjcmVhdGVkX2F0IjoiMjAyNS0wOC0yMFQxNTozMDowMC4wMDBaIiwiaWQiOiJwXzIifQ"
  }
}
```

### 5.2 创建回帖

```http
POST /api/v1/topics/t_123/posts
Content-Type: application/json
X-CSRF-Token: abc123...

{
  "contentMd": "我同意楼上的观点，另外补充一点...\n\n@developer 你觉得呢？",
  "replyToPostId": "p_2"
}
```

**响应**:
```json
{
  "success": true,
  "data": {
    "id": "p_3",
    "topicId": "t_123",
    "author": {
      "id": "u_999",
      "username": "current_user"
    },
    "contentMd": "我同意楼上的观点...",
    "mentions": ["developer"],
    "createdAt": "2025-08-21T10:50:00.000Z"
  }
}
```

### 5.3 编辑帖子

```http
PATCH /api/v1/posts/p_3
Content-Type: application/json
X-CSRF-Token: abc123...

{
  "contentMd": "更新后的帖子内容...",
  "updatedAt": "2025-08-21T10:50:00.000Z"
}
```

### 5.4 删除帖子

```http
DELETE /api/v1/posts/p_3
X-CSRF-Token: abc123...
```

## 6. 分类与标签 API

### 6.1 分类列表

```http
GET /api/v1/categories
```

**响应**:
```json
{
  "success": true,
  "data": [
    {
      "id": "c_dev",
      "name": "开发讨论",
      "slug": "development",
      "description": "技术开发相关讨论",
      "order": 1,
      "isArchived": false,
      "topicCount": 156
    }
  ]
}
```

### 6.2 标签列表

```http
GET /api/v1/tags
```

**响应**:
```json
{
  "success": true,
  "data": [
    {
      "id": "tag_1",
      "name": "TypeScript",
      "slug": "typescript",
      "description": "TypeScript 相关讨论",
      "color": "#3178c6",
      "topicCount": 42
    }
  ]
}
```

### 6.3 创建分类（管理员）

```http
POST /api/v1/categories
Content-Type: application/json
X-CSRF-Token: abc123...

{
  "name": "新分类",
  "slug": "new-category",
  "description": "分类描述",
  "order": 10
}
```

## 7. 搜索 API

### 7.1 全文搜索

```http
GET /api/v1/search?q=TypeScript&tag=development&categoryId=c_dev&limit=20
```

**查询参数**:
- `q`: 搜索关键词
- `tag`: 标签过滤
- `categoryId`: 分类过滤
- `limit`: 结果数量限制

**响应**:
```json
{
  "success": true,
  "data": [
    {
      "type": "topic",
      "topicId": "t_123",
      "postId": null,
      "title": "TypeScript 最佳实践讨论",
      "snippet": null,
      "score": 2.5,
      "lastPostedAt": "2025-08-21T09:30:00.000Z"
    },
    {
      "type": "post",
      "topicId": "t_123",
      "postId": "p_2",
      "title": null,
      "snippet": "我觉得使用严格模式是最重要的 TypeScript 特性...",
      "score": 1.8,
      "lastPostedAt": "2025-08-20T15:30:00.000Z"
    }
  ]
}
```

## 8. 通知 API

### 8.1 获取通知列表

```http
GET /api/v1/notifications?unreadOnly=true&limit=20
```

**响应**:
```json
{
  "success": true,
  "data": [
    {
      "id": "n_1",
      "type": "mention",
      "topicId": "t_123",
      "postId": "p_3",
      "byUser": {
        "id": "u_999",
        "username": "other_user"
      },
      "snippet": "...@developer 你觉得呢？",
      "readAt": null,
      "createdAt": "2025-08-21T10:50:00.000Z"
    }
  ]
}
```

### 8.2 标记通知为已读

```http
POST /api/v1/notifications/n_1/read
X-CSRF-Token: abc123...
```

## 9. 实时通信 (SignalR)

### 9.1 连接与认证

```typescript
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
  .withUrl("/hubs/topics", {
    accessTokenFactory: () => getCsrfToken() // 或从 Cookie 获取
  })
  .withAutomaticReconnect()
  .build();

await connection.start();
```

### 9.2 Hub 方法

#### 客户端 → 服务端

```typescript
// 加入主题房间
await connection.invoke("JoinTopic", topicId);

// 离开主题房间  
await connection.invoke("LeaveTopic", topicId);

// 发送正在输入状态
await connection.invoke("Typing", topicId, true);
```

#### 服务端 → 客户端

```typescript
// 监听新帖子创建
connection.on("PostCreated", (payload: {
  topicId: string;
  post: Post;
}) => {
  // 在 UI 中插入新帖子
});

// 监听帖子编辑
connection.on("PostEdited", (payload: {
  topicId: string;
  postId: string;
  contentMd: string;
  contentHtml: string;
  updatedAt: string;
}) => {
  // 更新帖子内容
});

// 监听帖子删除
connection.on("PostDeleted", (payload: {
  topicId: string;
  postId: string;
}) => {
  // 标记帖子为已删除
});

// 监听主题统计更新
connection.on("TopicStats", (payload: {
  topicId: string;
  replyCount: number;
  viewCount: number;
}) => {
  // 更新主题统计
});

// 监听用户输入状态
connection.on("UserTyping", (payload: {
  topicId: string;
  userId: string;
  username: string;
  isTyping: boolean;
}) => {
  // 显示/隐藏输入指示器
});
```

## 10. 错误处理

### 10.1 字段验证错误

```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_FAILED",
    "message": "请求参数验证失败",
    "details": {
      "email": ["邮箱格式不正确"],
      "password": ["密码长度至少 8 位"]
    }
  }
}
```

### 10.2 业务逻辑错误

```json
{
  "success": false,
  "error": {
    "code": "TOPIC_LOCKED",
    "message": "主题已被锁定，无法回帖"
  }
}
```

### 10.3 乐观并发冲突

```json
{
  "success": false,
  "error": {
    "code": "CONFLICT",
    "message": "数据已被其他用户修改，请刷新后重试"
  }
}
```

### 10.4 权限错误

```json
{
  "success": false,
  "error": {
    "code": "INSUFFICIENT_PERMISSIONS",
    "message": "权限不足"
  }
}
```

## 11. 安全考虑

### 11.1 CSRF 保护

所有非 GET 请求都需要在请求头中包含 CSRF Token:

```http
X-CSRF-Token: abc123...
```

### 11.2 输入验证与 XSS 防护

- 服务端对所有 Markdown 内容进行消毒处理
- 前端只渲染经过消毒的 HTML
- @ 提及和链接的白名单检查

### 11.3 限流规则

| 操作类型 | 限制 |
|----------|------|
| 登录尝试 | 5 次/分钟/IP |
| 注册 | 3 次/小时/IP |
| 发主题 | 10 次/小时/用户 |
| 发回帖 | 30 次/小时/用户 |
| 搜索 | 60 次/分钟/用户 |

## 12. 部署与环境配置

### 12.1 环境变量

```bash
# 应用配置
APP_URL=https://forum.example.com
ASPNETCORE_ENVIRONMENT=Production

# 数据库配置
MYSQL_HOST=localhost
MYSQL_PORT=3306
MYSQL_DB=forum_db
MYSQL_USER=forum_user
MYSQL_PASSWORD=secure_password

# JWT 配置
JWT_SECRET=your-secret-key
JWT_EXPIRES=15m
REFRESH_EXPIRES=7d

# SMTP 配置
SMTP_HOST=smtp.example.com
SMTP_PORT=587
SMTP_USER=noreply@example.com
SMTP_PASS=smtp_password
EMAIL_FROM=Forum <noreply@example.com>

# CORS 配置
CORS_ORIGINS=https://forum.example.com,https://admin.forum.example.com

# SignalR (多实例)
SIGNALR_REDIS_URL=redis://localhost:6379

# 安全配置
CSRF_COOKIE_NAME=__Secure-csrf-token
CSRF_HEADER_NAME=X-CSRF-Token
```

### 12.2 前端代理配置

```typescript
// vite.config.ts
export default defineConfig({
  server: {
    proxy: {
      "/api": {
        target: "http://localhost:5000",
        changeOrigin: true
      },
      "/hubs": {
        target: "http://localhost:5000",
        ws: true
      }
    }
  }
});
```

## 13. 测试

### 13.1 API 测试示例

```typescript
// 使用 Jest + Supertest
describe('Topics API', () => {
  it('should create a new topic', async () => {
    const response = await request(app)
      .post('/api/v1/topics')
      .set('X-CSRF-Token', csrfToken)
      .send({
        title: 'Test Topic',
        contentMd: '# Test Content',
        categoryId: 'c_test',
        tagSlugs: ['test']
      })
      .expect(201);

    expect(response.body.success).toBe(true);
    expect(response.body.data.title).toBe('Test Topic');
  });
});
```

### 13.2 SignalR 测试

```typescript
// 实时通信测试
describe('SignalR Hub', () => {
  it('should broadcast new post to topic room', async () => {
    const connection1 = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/topics')
      .build();
    
    await connection1.start();
    await connection1.invoke('JoinTopic', topicId);
    
    const messageReceived = new Promise(resolve => {
      connection1.on('PostCreated', resolve);
    });
    
    // 模拟其他用户发帖
    await createPost(topicId, postData);
    
    const message = await messageReceived;
    expect(message.post.contentMd).toBe(postData.contentMd);
  });
});
```

---

**文档维护**: 本文档随 API 变更同步更新，版本变更记录见项目 CHANGELOG。