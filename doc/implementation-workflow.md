# Discourse 风格论坛 - 系统化实现工作流

基于 PRD 分析生成的完整系统化实现工作流。

## 🏗️ 技术架构总览

**前端**: Vite + React + TypeScript + shadcn/ui + Tailwind
**后端**: ASP.NET Core 8 + MySQL + Dapper + SignalR
**实时通信**: SignalR Hub (主题房间)
**核心特性**: Discourse 风格 UI、实时回帖、分类标签、全文搜索

## 📋 实现工作流 (6个里程碑)

### M0: 项目脚手架搭建 (Week 1)

#### 前端脚手架
- [ ] **Vite + React + TypeScript 项目初始化**
  - 配置 `vite.config.ts` 别名映射 (`@/*`)
  - 设置代理到后端 API (`/api` → `localhost:4000`)
  - WebSocket 代理配置 (`/hubs/*` → SignalR)

- [ ] **shadcn/ui 组件库集成**
  - 安装 Tailwind CSS + `clsx` + `tailwind-merge`
  - 初始化 shadcn 组件 (`Button`, `Card`, `Avatar`, `Badge`)
  - 配置 Lucide React 图标库

- [ ] **开发工具配置**
  - ESLint + Prettier 代码规范
  - 路径别名 TypeScript 配置
  - 基础 UI 组件库设置

#### 后端脚手架
- [ ] **ASP.NET Core 8 项目创建**
  - Web API 项目模板
  - MySQL 连接配置 (`MySqlConnector`)
  - Dapper ORM 集成

- [ ] **SignalR 实时通信基座**
  - `TopicsHub` 基础结构
  - 连接管理与房间机制
  - 前端 `@microsoft/signalr` 客户端

- [ ] **数据库设计与迁移**
  - MySQL 8.x 数据库创建 (utf8mb4)
  - 核心表结构 DDL (users, topics, posts, categories)
  - DbUp 迁移工具集成

**完成标准**: 前后端可启动，SignalR 连接成功，数据库表创建完成

---

### M1: 用户认证系统 (Week 2)

#### 认证后端 API
- [ ] **JWT 认证架构**
  - Access Token (短期) + Refresh Token (长期)
  - HttpOnly + Secure Cookie 配置
  - Token 刷新机制

- [ ] **用户注册/登录流程**
  ```csharp
  POST /api/auth/register  // {username, email, password}
  POST /api/auth/login     // {email, password}
  POST /api/auth/refresh   // Token 刷新
  GET  /api/auth/me        // 当前用户信息
  ```

- [ ] **邮箱验证系统**
  - MailKit SMTP 发送验证邮件
  - 邮箱验证 Token 机制
  - 未验证用户发帖限制

#### 认证前端 UI
- [ ] **shadcn 表单组件**
  - `react-hook-form` + `zod` 表单验证
  - 注册/登录页面 (`/register`, `/login`)
  - 密码重置流程 (`/reset-password`)

- [ ] **认证状态管理**
  - TanStack Query 认证状态
  - 私有路由保护
  - 用户菜单与退出登录

**完成标准**: 用户可注册、验证邮箱、登录，认证状态持久化

---

### M2: 主题帖子 CRUD + Markdown (Week 3-4)

#### 内容管理后端
- [ ] **主题 API 设计**
  ```csharp
  GET  /api/topics?cursor=&categoryId=&tag=     // 主题列表 (Keyset 分页)
  POST /api/topics                              // 创建主题
  GET  /api/topics/:id                          // 主题详情
  PATCH /api/topics/:id                         // 编辑主题 (带乐观锁)
  DELETE /api/topics/:id                        // 删除主题
  ```

- [ ] **帖子 API 设计**
  ```csharp
  GET  /api/topics/:topicId/posts?cursor=       // 帖子列表
  POST /api/topics/:topicId/posts               // 发帖 (事务更新统计)
  PATCH /api/posts/:id                          // 编辑帖子 (10分钟限时)
  DELETE /api/posts/:id                         // 删除帖子
  ```

- [ ] **Markdown 处理与安全**
  - 服务端 Markdown 渲染
  - HTML 内容消毒 (防 XSS)
  - @ 提及解析与存储

#### Discourse 风格前端
- [ ] **主题列表页面** (`/`)
  - 分类徽章 + 标签 Badge 显示
  - 头像栈 (最近参与者)
  - 回帖数、浏览量、最后活跃时间
  - 无限滚动 + Keyset 分页

- [ ] **主题详情页面** (`/t/:topicId/:slug`)
  - 右侧垂直时间轴 (滚动位置指示)
  - 楼层卡片布局
  - 锚点 URL 更新 (`#post-123`)
  - 引用块渲染

- [ ] **底部浮动编辑器 (Composer)**
  - 抽屉式编辑区域
  - Markdown 编辑/预览切换
  - 实时字数统计
  - 离开前保存提醒

**完成标准**: 用户可创建主题、发帖，Markdown 正确渲染，编辑器体验流畅

---

### M3: 分类标签 + 搜索 (Week 5)

#### 内容组织后端
- [ ] **分类系统 API**
  ```csharp
  GET  /api/categories                    // 分类列表
  POST /api/categories                    // 创建分类 (Admin)
  PATCH /api/categories/:id               // 编辑分类
  ```

- [ ] **标签系统 API**
  ```csharp
  GET  /api/tags                          // 标签列表  
  POST /api/tags                          // 创建标签 (Mod+)
  ```

- [ ] **MySQL 全文搜索**
  ```sql
  ALTER TABLE topics ADD FULLTEXT ftx_topic_title (title);
  ALTER TABLE posts ADD FULLTEXT ftx_posts_content (content_md);
  ```
  - `MATCH ... AGAINST` 查询实现
  - 主题标题 + 帖子内容组合搜索
  - 按相关度排序

#### 内容发现前端
- [ ] **分类/标签筛选**
  - 分类页面 (`/c/:categorySlug`)
  - 标签筛选 (`/?tag=typescript`)
  - 多维度组合筛选

- [ ] **搜索功能**
  - 顶部搜索框
  - 搜索结果页面 (`/search?q=...`)
  - 快速跳转到楼层锚点

- [ ] **发布界面改进**
  - 分类选择器
  - 标签输入/选择
  - 主题创建表单 (`/new`)

**完成标准**: 分类标签体系完整，搜索功能可用，内容可有效组织和发现

---

### M4: SignalR 实时功能 (Week 6)

#### 实时通信后端
- [ ] **SignalR Hub 完整实现**
  ```csharp
  // TopicsHub 方法
  JoinTopic(topicId: long)              // 加入主题房间
  LeaveTopic(topicId: long)             // 离开房间
  Typing(topicId: long, isTyping: bool) // 输入状态
  ```

- [ ] **实时事件广播**
  ```csharp
  // 服务端 → 客户端事件
  PostCreated(payload)                  // 新帖发布
  PostEdited(payload)                   // 帖子编辑  
  PostDeleted(payload)                  // 帖子删除
  TopicStats(payload)                   // 统计更新
  UserTyping(payload)                   // 输入指示
  ```

- [ ] **事务与实时同步**
  - 发帖事务内触发 SignalR 广播
  - 乐观并发控制 (updatedAt 校验)
  - 消息幂等性保证

#### 实时体验前端
- [ ] **SignalR 客户端集成**
  ```typescript
  // @microsoft/signalr 连接管理
  connection.invoke("JoinTopic", topicId);
  connection.on("PostCreated", handleNewPost);
  connection.on("PostEdited", handleEditPost);
  ```

- [ ] **实时 UI 更新**
  - 新帖实时插入 (≤1s 延迟)
  - 编辑内容同步更新
  - 删除状态标记
  - "正在输入" 指示器

- [ ] **连接管理优化**
  - 自动重连机制
  - 连接状态指示
  - 离线/在线状态处理

**完成标准**: 多用户同时在主题页，任一用户操作在 1 秒内同步到其他用户

---

### M5: 通知系统 + 基础管理 (Week 7)

#### 通知后端
- [ ] **通知系统 API**
  ```csharp
  GET  /api/notifications?unreadOnly=true  // 通知列表
  POST /api/notifications/:id/read         // 标记已读
  ```

- [ ] **@ 提及通知**
  - 帖子中 @ 用户解析
  - `post_mentions` 表记录
  - 实时通知推送

- [ ] **管理功能 API**
  ```csharp
  POST /api/topics/:id/pin                 // 置顶主题
  POST /api/topics/:id/lock                // 锁定主题
  POST /api/users/:id/ban                  // 封禁用户 (Admin)
  ```

#### 通知与管理前端
- [ ] **通知 UI**
  - 右上角铃铛图标
  - 未读数 Badge 提示
  - 通知下拉菜单
  - 点击跳转到对应楼层

- [ ] **版主管理界面**
  - 主题管理按钮 (置顶/锁定/移动)
  - 管理员后台 (`/admin`)
  - 用户封禁界面

- [ ] **权限控制**
  - 基于角色的 UI 显示/隐藏
  - 被锁定主题禁止回帖
  - 编辑时间窗口限制 (10分钟)

**完成标准**: @ 提及产生通知并可跳转，版主管理功能正常，权限控制生效

---

### M6: 优化与生产就绪 (Week 8)

#### 性能优化
- [ ] **数据库优化**
  ```sql
  -- 关键索引
  INDEX idx_topic_cat_last (category_id, is_deleted, last_posted_at DESC, id DESC)
  INDEX idx_posts_topic_created (topic_id, created_at, id)
  ```

- [ ] **缓存策略**
  - 热门主题列表缓存 (Redis/内存)
  - Markdown 渲染结果缓存
  - 分类/标签列表缓存

- [ ] **API 性能监控**
  - P95 响应时间 < 200ms
  - SignalR 消息延迟 P95 < 1s
  - 数据库连接池优化

#### 生产部署
- [ ] **安全加固**
  - CORS 白名单配置
  - Rate Limiting 限流
  - CSRF 双提交验证
  - 安全响应头设置

- [ ] **监控与日志**
  - Serilog 结构化日志
  - ASP.NET Core 指标收集
  - 错误追踪与告警

- [ ] **可用性改进**
  - WCAG AA 无障碍标准
  - SEO meta 标签优化
  - 移动端响应式适配

**完成标准**: 系统性能达标，安全措施完备，可用性良好，生产环境就绪

---

## 🚀 技术风险与对策

| 风险点 | 影响 | 对策 |
|-------|------|------|
| **MySQL FULLTEXT 中文分词效果** | 搜索体验差 | 后续接入 ElasticSearch/Meilisearch |
| **SignalR 扩展性** | 多实例部署困难 | 使用 Redis Backplane |
| **Markdown XSS 风险** | 安全漏洞 | 强制后端消毒 + 白名单渲染 |
| **并发编辑冲突** | 数据不一致 | 乐观并发控制 (updatedAt 校验) |

## 📊 关键质量指标

- **实时延迟**: P95 ≤ 1s (SignalR 端到端)
- **API 性能**: P95 < 200ms 
- **搜索响应**: < 500ms
- **邮件送达率**: > 95%
- **代码覆盖率**: > 80% (单元测试)

## 🔧 开发环境配置

### 前端环境配置
```bash
# 项目初始化
npm create vite@latest forum-frontend -- --template react-ts
cd forum-frontend
npm install

# shadcn/ui 安装
npx shadcn-ui@latest init
npx shadcn-ui@latest add button card avatar badge

# 依赖安装
npm install @tanstack/react-query react-hook-form zod
npm install @microsoft/signalr lucide-react clsx tailwind-merge
```

### 后端环境配置
```bash
# 项目创建
dotnet new webapi -n ForumApi
cd ForumApi

# NuGet 包安装
dotnet add package MySqlConnector
dotnet add package Dapper
dotnet add package MailKit
dotnet add package FluentValidation
dotnet add package Serilog.AspNetCore
```

### 数据库初始化
```sql
-- 创建数据库
CREATE DATABASE forum_db CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;

-- 创建用户
CREATE USER 'forum_user'@'localhost' IDENTIFIED BY 'forum_password';
GRANT ALL PRIVILEGES ON forum_db.* TO 'forum_user'@'localhost';
FLUSH PRIVILEGES;
```

## 📝 验收测试清单

### M0 验收
- [ ] 前端开发服务器启动 (`npm run dev`)
- [ ] 后端 API 服务启动 (`dotnet run`)
- [ ] SignalR 连接测试页面可用
- [ ] 数据库连接成功，表结构创建完成

### M1 验收
- [ ] 用户注册流程完整
- [ ] 邮箱验证链接有效
- [ ] 登录状态持久化
- [ ] JWT Token 刷新机制正常

### M2 验收
- [ ] 主题创建与列表显示
- [ ] Markdown 渲染无 XSS 风险
- [ ] 帖子编辑 10 分钟限时生效
- [ ] 引用功能正常工作

### M3 验收
- [ ] 分类筛选功能正常
- [ ] 标签系统完整
- [ ] 搜索结果准确且响应快速
- [ ] 组合筛选逻辑正确

### M4 验收
- [ ] 多浏览器标签页实时同步
- [ ] 输入状态指示器工作
- [ ] 离线重连机制有效
- [ ] 实时延迟符合指标 (P95 ≤ 1s)

### M5 验收
- [ ] @ 提及通知及时推送
- [ ] 版主权限正确控制
- [ ] 管理界面功能完整
- [ ] 权限边界测试通过

### M6 验收
- [ ] 性能指标达标
- [ ] 安全扫描通过
- [ ] 无障碍性测试合格
- [ ] 生产环境部署成功

---

**文档版本**: v1.0  
**最后更新**: 2025-08-19  
**负责人**: 开发团队  
**审核人**: 架构师 + 产品经理