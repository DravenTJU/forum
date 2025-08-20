# Forum API - Discourse风格论坛后端

基于 ASP.NET Core 8 + MySQL + Dapper 的现代化论坛后端系统。

## 🚀 快速开始

### 环境要求

- .NET 8.0 SDK
- MySQL 8.0+
- Node.js 18+ (用于前端开发)

### 后端启动

```bash
cd Forum.Api
dotnet restore
dotnet run
```

API 服务将在 http://localhost:4000 启动

- Swagger 文档: http://localhost:4000/swagger
- 健康检查: http://localhost:4000/api/health

### 前端启动

```bash
cd forum-frontend
npm install
npm run dev
```

前端将在 http://localhost:5173 启动

## 📁 项目结构

```
Forum.Api/
├── Controllers/          # API 控制器
├── Hubs/                # SignalR Hubs
├── Models/              # 数据模型
│   ├── Entities/        # 数据库实体
│   ├── DTOs/           # 数据传输对象
│   └── Requests/       # 请求模型
├── Services/           # 业务服务
├── Repositories/       # 数据访问层
├── Middleware/         # 中间件
├── Infrastructure/     # 基础设施
│   ├── Database/       # 数据库相关
│   ├── Email/          # 邮件服务
│   └── Auth/           # 认证相关
├── Migrations/         # 数据库迁移
└── Extensions/         # 扩展方法
```

## 🛠️ 开发指南

### 数据库配置

1. 创建 MySQL 数据库
2. 修改 `appsettings.json` 中的连接字符串
3. 应用启动时会自动运行数据库迁移

### API 认证

使用 JWT Bearer 认证：

```bash
# 用户注册
curl -X POST http://localhost:4000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"test","email":"test@example.com","password":"password123"}'

# 用户登录
curl -X POST http://localhost:4000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"password123"}'
```

### SignalR 实时通信

WebSocket 连接地址: `ws://localhost:4000/hubs/topics`

需要在连接时传递 JWT token 进行认证。

## 🔧 配置说明

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=forum_db;..."
  },
  "JwtSettings": {
    "Secret": "your-secret-key",
    "ExpirationInMinutes": 60
  }
}
```

## 📝 开发规范

- 遵循 Clean Architecture 原则
- 使用 Repository 模式进行数据访问
- 所有 API 返回统一格式的 JSON 响应
- 使用 Serilog 进行结构化日志记录

## 🧪 测试

```bash
# 运行单元测试
dotnet test

# 检查代码质量
dotnet build --verbosity normal
```

## 📚 相关文档

- [M0 项目脚手架](doc/milestone-m0-scaffolding.md)
- [M1 用户认证系统](doc/milestone-m1-authentication.md)
- [编码规范](doc/coding_standards_and_principles.md)
- [产品需求文档](doc/prd-discourse-style-forum.md)