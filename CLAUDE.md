# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

这是一个 Discourse 风格的论坛应用，采用前后端分离架构。详细的项目背景、需求和技术目标请参见：
- [产品需求文档 (PRD)](doc/prd-discourse-style-forum.md)
- [实现工作流](doc/implementation-workflow.md)

**技术栈：**
- 前端：Vite + React + TypeScript + shadcn/ui + Tailwind CSS
- 后端：ASP.NET Core 8 + MySQL + Dapper + SignalR
- 数据库：MySQL 8.x，使用 utf8mb4 编码
- 实时通信：SignalR Hub 主题房间机制

## 快速开始

开发环境搭建和运行命令请参见：
- [后端 API 开发指南](Forum.Api/README.md)
- [前端开发指南](forum-frontend/README.md)
- [后端开发详细说明](Forum.Api/DEVELOPMENT.md)

## 架构概览

### 整体架构
```
forum/
├── Forum.Api/          # 后端 ASP.NET Core Web API
├── forum-frontend/     # 前端 React + TypeScript
├── doc/               # 项目文档
├── database/          # 数据库初始化脚本
└── docker-compose.yml # 开发环境容器配置
```

**架构模式**：前后端分离 + 整洁架构 + Repository 模式

### 后端结构（Forum.Api/）
```
Forum.Api/
├── Controllers/         # 🔌 API 控制器层
│   ├── AuthController.cs           # 认证相关 (/api/auth)
│   ├── CategoriesController.cs     # 分类管理 (/api/categories)
│   ├── TopicsController.cs         # 主题管理 (/api/topics)
│   ├── PostsController.cs          # 帖子管理 (/api/posts)
│   └── TestController.cs           # 测试端点
│
├── Services/           # 💼 业务逻辑层
│   ├── IAuthService.cs / AuthService.cs           # 认证业务
│   ├── ICategoryService.cs / CategoryService.cs   # 分类业务
│   ├── ITopicService.cs / TopicService.cs         # 主题业务
│   ├── IPostService.cs / PostService.cs           # 帖子业务
│   └── ISignalRService.cs / SignalRService.cs     # 实时通信
│
├── Repositories/       # 🗄️ 数据访问层 (Dapper)
│   ├── IUserRepository.cs / UserRepository.cs     # 用户数据
│   ├── ICategoryRepository.cs / CategoryRepository.cs  # 分类数据
│   ├── ITopicRepository.cs / TopicRepository.cs       # 主题数据
│   ├── IPostRepository.cs / PostRepository.cs         # 帖子数据
│   └── IRefreshTokenRepository.cs / RefreshTokenRepository.cs
│
├── Models/             # 📋 数据模型
│   ├── Entities/       # 数据库实体类
│   │   ├── User.cs, Category.cs, Topic.cs, Post.cs
│   │   ├── Tag.cs, RefreshToken.cs
│   └── DTOs/          # API 数据传输对象
│       ├── AuthDTOs.cs, CategoryDTOs.cs
│       ├── TopicDTOs.cs, PostDTOs.cs
│       └── ApiResponse.cs, PaginationQuery.cs
│
├── Infrastructure/     # 🏗️ 基础设施层
│   ├── Database/       # 数据库工厂和连接
│   ├── Auth/          # JWT 认证服务
│   └── Email/         # 邮件服务 (MailKit)
│
├── Hubs/              # 🔄 SignalR 实时通信
│   └── TopicsHub.cs            # 主题房间广播
│
├── Middleware/        # 🛡️ 中间件
│   ├── ErrorHandlingMiddleware.cs    # 全局错误处理
│   └── RequestLoggingMiddleware.cs   # 请求日志
│
├── Migrations/        # 🗃️ 数据库迁移 (DbUp)
│   ├── 001_CreateUserTables.sql
│   ├── 002_CreateCategoriesAndTags.sql
│   └── 003_CreateTopicsAndPosts.sql
│
└── Extensions/        # ⚙️ 扩展配置
    ├── ServiceCollectionExtensions.cs  # DI 容器配置
    └── WebApplicationExtensions.cs     # 应用管道配置
```

### 前端结构（forum-frontend/src/）
```
src/
├── components/         # 🧩 UI 组件库
│   ├── ui/            # shadcn/ui 基础组件
│   │   ├── button.tsx, card.tsx, avatar.tsx
│   │   ├── dialog.tsx, dropdown-menu.tsx
│   │   └── form.tsx, input.tsx, loading-*.tsx
│   ├── auth/          # 认证相关组件
│   │   └── ProtectedRoute.tsx
│   ├── topics/        # 主题相关组件
│   │   └── TopicListCard.tsx
│   ├── filters/       # 筛选组件
│   │   ├── CategoryFilter.tsx, TagFilter.tsx
│   │   └── SortFilter.tsx
│   └── layout/        # 布局组件
│
├── pages/             # 📄 页面组件
│   ├── HomePage.tsx               # 主页 (/)
│   ├── auth/                      # 认证页面
│   │   ├── LoginPage.tsx          # 登录 (/login)
│   │   └── RegisterPage.tsx       # 注册 (/register)
│   ├── topics/                    # 主题页面
│   └── admin/                     # 管理后台
│
├── api/               # 🌐 API 客户端 (axios)
│   ├── auth.ts                    # 认证 API
│   ├── categories.ts, tags.ts     # 分类标签 API
│   └── topics.ts                  # 主题帖子 API
│
├── hooks/             # 🪝 自定义 React Hooks
│   ├── useAuth.ts                 # 认证状态管理
│   ├── useTopics.ts               # 主题数据管理
│   ├── useApiCall.ts              # API 调用封装
│   ├── useFormError.ts            # 表单错误处理
│   └── useIntersectionObserver.ts # 无限滚动
│
├── types/             # 📝 TypeScript 类型
│   ├── auth.ts                    # 认证相关类型
│   └── api.ts                     # API 响应类型
│
├── lib/               # 🔧 工具函数
│   ├── utils.ts                   # 通用工具
│   ├── error-utils.ts             # 错误处理工具
│   └── notification.ts            # 通知工具
│
├── styles/            # 🎨 样式文件
│   └── globals.css                # 全局样式 (Tailwind)
│
└── store/             # 状态管理 (预留)
```

### 关键文件位置速查

| 功能 | 后端位置 | 前端位置 |
|------|---------|---------|
| **用户认证** | `Controllers/AuthController.cs`<br>`Services/AuthService.cs` | `api/auth.ts`<br>`hooks/useAuth.ts`<br>`pages/auth/` |
| **主题帖子** | `Controllers/TopicsController.cs`<br>`Services/TopicService.cs` | `api/topics.ts`<br>`hooks/useTopics.ts`<br>`components/topics/` |
| **实时通信** | `Hubs/TopicsHub.cs`<br>`Services/SignalRService.cs` | `hooks/useTopics.ts` 中的 SignalR 连接 |
| **数据库模型** | `Models/Entities/` | `types/api.ts` 对应类型定义 |
| **API 响应格式** | `Models/DTOs/` | `types/api.ts` 响应类型 |
| **数据库迁移** | `Migrations/*.sql` | 无（后端管理） |
| **配置文件** | `appsettings.json`<br>`Extensions/` | `vite.config.ts` |

### 数据库设计
完整的数据库 DDL 和关系图请参见 [PRD 文档](doc/prd-discourse-style-forum.md) 第10节。

**核心表结构**：
- `users` → `user_roles` (角色权限)
- `categories` ↔ `topics` ↔ `posts` (内容层级)
- `tags` ↔ `topic_tags` (标签关联)
- `notifications`, `refresh_tokens`, `audit_logs` (辅助功能)

## 开发规范

### 核心原则
严格遵循项目的核心开发规范，详见 [编码规范文档](doc/coding_standards_and_principles.md)：
- **SOLID 原则**：单一职责、开闭、里氏替换、接口隔离、依赖倒置
- **KISS & YAGNI**：保持简单，避免过度设计
- **DRY & SoC**：避免重复，关注点分离

### 命名约定
- **后端 (C#)**：
  - 类/接口/方法：`PascalCase`（接口以 `I` 开头）
  - 私有字段：`_camelCase`（下划线前缀）
  - 异步方法：必须以 `Async` 结尾
  - 局部变量/参数：`camelCase`

- **前端 (TypeScript/React)**：
  - 组件：`PascalCase`（如 `TopicCard.tsx`）
  - Hooks/工具/API：`camelCase`（如 `useAuth.ts`）
  - 类型/接口：`PascalCase`
  - 常量：`UPPER_SNAKE_CASE`

### 关键模式
- **后端**：Repository 模式、依赖注入、事务管理
- **前端**：TanStack Query（服务端状态）、react-hook-form + zod（表单）
- **安全**：JWT 双令牌、HttpOnly cookies、参数化查询、输入验证
- **实时**：SignalR 房间机制、自动重连、状态同步

### 代码质量
- **异步编程**：所有 I/O 操作使用 async/await
- **错误处理**：统一异常处理、结构化日志
- **测试**：单元测试、集成测试、E2E 测试
- **安全**：永不记录敏感信息、防 XSS/SQL注入

## 重要配置与约定

### 数据库迁移
- 迁移脚本位于 `Forum.Api/Migrations/`，按数字前缀排序（001_, 002_...）
- 启动时通过 DbUp 自动执行，不可逆
- 新迁移必须在开发环境测试后才能提交
- 迁移失败会阻止应用启动

### 关键配置文件
- `Forum.Api/appsettings.json` - 后端配置（数据库、JWT、CORS、SMTP）
- `forum-frontend/vite.config.ts` - 前端配置（API 代理、别名）
- `docker-compose.yml` - 完整开发环境（数据库、Redis、应用）

### API 设计约定
- 遵循 RESTful 原则，详见 [PRD 第11节](doc/prd-discourse-style-forum.md)
- 统一响应格式，使用标准 HTTP 状态码
- 非 GET 请求需要 CSRF 令牌和 JWT 认证
- 乐观并发控制：PATCH 请求必须携带 `updatedAt` 字段

## 质量保证

### 测试策略
- **单元测试**：服务层逻辑，Repository 数据访问
- **集成测试**：API 端点，数据库交互
- **组件测试**：React 组件，用户交互
- **端到端**：关键业务流程（认证、发帖、实时通信）

### 性能指标
- API 响应时间 P95 < 200ms
- SignalR 实时延迟 P95 < 1s
- 搜索响应时间 < 500ms
- 邮件送达率 > 95%

### 安全要求
- 所有输入必须验证和清理
- Markdown 内容服务端消毒
- 敏感操作记录审计日志
- 定期安全扫描和依赖更新

## 开发工作流

### 功能开发流程
1. **API 优先**：设计 API 接口，更新类型定义
2. **后端实现**：Controller → Service → Repository 层次实现
3. **前端集成**：API 客户端 → 组件 → 页面集成
4. **测试验证**：单元测试 → 集成测试 → 手工验证
5. **文档更新**：API 文档、组件说明、变更日志

### 数据库变更流程
1. 设计变更：实体关系、索引策略、性能影响
2. 编写迁移：创建编号 SQL 脚本，包含回滚策略
3. 更新代码：实体类、Repository、DTO 类型
4. 测试验证：开发环境测试、性能验证
5. 代码评审：迁移脚本、相关代码变更

### 实时功能开发
1. **Hub 方法**：定义 SignalR 服务端方法
2. **事件定义**：客户端监听的事件类型
3. **前端集成**：连接管理、事件处理、状态同步
4. **测试验证**：多标签页测试、网络中断测试

### UI 组件开发
1. **shadcn/ui 优先**：使用 CLI 安装基础组件
2. **组合模式**：基于基础组件构建业务组件
3. **类型安全**：完整的 TypeScript 类型定义
4. **访问性**：遵循 WCAG 2.1 AA 标准

## 项目里程碑与文档

### 开发阶段
项目采用增量交付模式，分为 6 个里程碑（M0-M6）：
- **M0-M1**：基础架构和认证（已完成）
- **M2-M3**：内容管理和搜索（当前阶段）
- **M4-M6**：实时功能和优化

详细计划请参见：[实现工作流文档](doc/implementation-workflow.md)

### 相关文档
- [产品需求文档 (PRD)](doc/prd-discourse-style-forum.md) - 完整需求和技术规范
- [编码规范](doc/coding_standards_and_principles.md) - 开发原则和代码标准
- [实现工作流](doc/implementation-workflow.md) - 里程碑计划和验收标准
- [提交消息规范](doc/commit-messages-rules.md) - Git 提交格式要求
- [API 规范](doc/api-specification.md) - API 设计和文档标准

### 开发环境
- **后端开发**：[Forum.Api/DEVELOPMENT.md](Forum.Api/DEVELOPMENT.md)
- **前端开发**：[forum-frontend/README.md](forum-frontend/README.md)
- **Docker 环境**：使用 `docker-compose.yml` 一键启动完整环境