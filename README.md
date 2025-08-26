# Forum - Discourse 风格论坛系统

现代化的论坛应用，采用前后端分离架构，支持实时通信和丰富的内容管理功能。

## 项目特色

- **🚀 现代技术栈**: React + TypeScript + ASP.NET Core 8
- **⚡ 实时体验**: SignalR 驱动的即时消息和状态同步
- **🎨 优雅界面**: shadcn/ui + Tailwind CSS 构建的 Discourse 风格 UI
- **🔍 强大搜索**: MySQL 全文索引 + 分类标签组织
- **🔐 安全可靠**: JWT 双令牌 + 邮箱验证 + 权限管理
- **📱 响应式设计**: 移动优先，支持多设备访问

## 快速开始

### 环境要求

- **Node.js** 18+ (前端开发)
- **.NET 8.0 SDK** (后端开发)
- **MySQL 8.0+** (数据库)
- **Docker** (可选，容器化部署)

### 一键启动

```bash
# 使用 Docker Compose 启动完整环境
docker-compose up

# 访问地址
# 前端: http://localhost:3000
# 后端: http://localhost:4000
# 数据库管理: http://localhost:8080 (phpMyAdmin)
```

### 分步启动

```bash
# 1. 启动数据库
docker-compose up database

# 2. 启动后端 API
cd Forum.Api
dotnet run  # http://localhost:4000

# 3. 启动前端
cd forum-frontend  
npm run dev  # http://localhost:5173
```

## 项目结构

```
forum/
├── 📂 Forum.Api/          # 后端 ASP.NET Core API
├── 📂 forum-frontend/     # 前端 React 应用
├── 📂 doc/               # 项目文档和规范
├── 📂 database/          # 数据库初始化脚本
├── 🐳 docker-compose.yml # 容器化开发环境
├── 📋 CLAUDE.md          # AI 开发助手指南
└── 📖 README.md          # 项目概述 (本文件)
```

### 技术架构

| 层级 | 前端 | 后端 |
|------|------|------|
| **框架** | React 19 + TypeScript | ASP.NET Core 8 |
| **构建工具** | Vite 7 | .NET CLI |
| **UI/API** | shadcn/ui + Tailwind | RESTful API + SignalR |
| **状态管理** | TanStack Query + Hooks | Repository + Service |
| **数据层** | axios API 客户端 | Dapper + MySQL |
| **实时通信** | @microsoft/signalr | SignalR Hub |
| **表单验证** | react-hook-form + zod | FluentValidation |

## 核心功能

### 用户体验
- **📝 富文本编辑**: Markdown 编辑器 + 实时预览
- **💬 实时互动**: 即时回帖、编辑同步、输入状态指示
- **🔍 智能搜索**: 全文搜索 + 分类标签筛选
- **📱 响应式**: 完美适配桌面和移动设备
- **♿ 无障碍**: WCAG 2.1 AA 标准兼容

### 内容管理
- **📋 分类组织**: 多级分类 + 灵活标签系统
- **👥 权限管理**: 用户/版主/管理员分级权限
- **📧 邮件集成**: 注册验证 + 密码重置 + 通知提醒
- **🔒 安全防护**: XSS 防护 + SQL 注入防护 + CSRF 防护

### 技术特性
- **⚡ 高性能**: keyset 分页 + 智能缓存 + 连接池优化
- **🔄 实时同步**: 多用户实时协作，<1s 延迟
- **🛡️ 安全认证**: JWT 双令牌机制 + HttpOnly cookies
- **📊 可观测性**: 结构化日志 + 健康检查 + 性能监控

## 开发文档

### 快速上手
- **[前端开发指南](forum-frontend/README.md)** - React + TypeScript 开发规范
- **[后端开发指南](Forum.Api/README.md)** - ASP.NET Core + MySQL 开发规范
- **[AI 开发助手](CLAUDE.md)** - Claude Code 专用项目指南

### 项目规范
- **[产品需求文档](doc/prd-discourse-style-forum.md)** - 完整需求和技术规范
- **[编码规范](doc/coding_standards_and_principles.md)** - 开发原则和代码标准
- **[实现工作流](doc/implementation-workflow.md)** - 6 阶段开发计划
- **[API 规范](doc/api-specification.md)** - RESTful API 设计标准
- **[提交规范](doc/commit-messages-rules.md)** - Git 提交消息格式

## 开发里程碑

项目采用增量交付模式，当前进度：

- ✅ **M0: 项目脚手架** - 基础架构搭建完成
- ✅ **M1: 用户认证** - 注册登录和权限系统
- 🔄 **M2: 内容管理** - 主题帖子 CRUD + Markdown 支持
- 📋 **M3: 分类搜索** - 分类标签 + 全文搜索
- 📋 **M4: 实时功能** - SignalR 实时同步
- 📋 **M5: 通知管理** - 消息通知 + 基础管理
- 📋 **M6: 优化部署** - 性能优化 + 生产就绪

详细计划查看：[实现工作流文档](doc/implementation-workflow.md)

## 技术亮点

### 性能优化
- **数据库**: 智能索引设计 + keyset 分页避免深分页
- **缓存策略**: TanStack Query 前端缓存 + Redis 后端缓存
- **代码分割**: React.lazy 路由分割 + Vite 构建优化
- **实时优化**: SignalR 房间机制 + 自动重连

### 安全措施
- **认证授权**: JWT 双令牌 + 角色权限 + 邮箱验证
- **输入安全**: 全面输入验证 + Markdown 内容消毒
- **传输安全**: HTTPS 强制 + CORS 白名单 + 安全头
- **数据安全**: 参数化查询 + 密码哈希 + 审计日志

### 开发体验
- **类型安全**: 前后端完整 TypeScript 类型定义
- **开发工具**: ESLint + Prettier + 热重载 + Swagger
- **测试支持**: 单元测试 + 集成测试 + E2E 测试框架
- **容器化**: Docker 开发环境 + 生产部署支持

## 贡献指南

### 开发流程
1. Fork 项目并创建 feature 分支
2. 阅读相关开发文档和编码规范
3. 实现功能并编写测试
4. 提交符合规范的 commit 消息
5. 创建 Pull Request

### 代码规范
- 严格遵循 [编码规范文档](doc/coding_standards_and_principles.md)
- 前后端分别遵循对应的技术栈最佳实践
- 所有代码变更必须包含相应的测试
- 提交前运行代码质量检查

## 部署说明

### 开发环境
```bash
# 完整开发环境
docker-compose up

# 或者分步启动
docker-compose up database  # 仅数据库
# 然后手动启动前后端服务
```

### 生产环境
- **容器化部署**: 使用 Docker + Kubernetes
- **数据库**: MySQL 8.x 集群 + 读写分离
- **负载均衡**: Nginx + 多实例部署
- **缓存**: Redis 集群 + SignalR backplane
- **监控**: Prometheus + Grafana + ELK Stack

## 许可证

本项目采用 MIT 许可证。详见 LICENSE 文件。

## 技术支持

- **问题反馈**: 通过 GitHub Issues 提交
- **功能建议**: 通过 GitHub Discussions 讨论
- **开发文档**: 查看 `doc/` 目录下的详细文档
- **代码示例**: 参考项目中的实现代码