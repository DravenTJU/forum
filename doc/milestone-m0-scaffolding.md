# M0: 项目脚手架搭建详细实现步骤

**时间估算**: 1周 (5个工作日)  
**优先级**: 最高 (阻塞后续开发)  
**负责人**: 全栈开发团队

## 📋 任务总览

- ✅ 前端 Vite + React + TypeScript 环境
- ✅ shadcn/ui 组件库与 Tailwind 集成
- ✅ 后端 ASP.NET Core 8 + MySQL + Dapper
- ✅ SignalR 实时通信基座
- ✅ 数据库设计与迁移系统
- ✅ 开发工具链配置

---

## 🎯 Day 1: 前端脚手架搭建

### 1.1 Vite + React + TypeScript 项目初始化

```bash
# 创建项目
npm create vite@latest forum-frontend -- --template react-ts
cd forum-frontend

# 安装基础依赖
npm install

# 安装路由和状态管理
npm install react-router-dom @tanstack/react-query
npm install @tanstack/react-query-devtools

# 安装表单处理
npm install react-hook-form @hookform/resolvers zod

# 安装实时通信
npm install @microsoft/signalr

# 安装工具库
npm install clsx tailwind-merge lucide-react sonner
npm install axios date-fns

# 开发依赖
npm install -D @types/node
```

### 1.2 TypeScript 配置

**`tsconfig.json`** - 路径别名配置
```json
{
  "compilerOptions": {
    "target": "ES2020",
    "lib": ["ES2020", "DOM", "DOM.Iterable"],
    "module": "ESNext",
    "skipLibCheck": true,
    "bundler": true,
    "allowImportingTsExtensions": true,
    "resolveJsonModule": true,
    "isolatedModules": true,
    "noEmit": true,
    "jsx": "react-jsx",
    "strict": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "noFallthroughCasesInSwitch": true,
    "baseUrl": ".",
    "paths": {
      "@/*": ["src/*"],
      "@/components/*": ["src/components/*"],
      "@/lib/*": ["src/lib/*"],
      "@/hooks/*": ["src/hooks/*"],
      "@/types/*": ["src/types/*"],
      "@/api/*": ["src/api/*"]
    }
  },
  "include": ["src"],
  "references": [{ "path": "./tsconfig.node.json" }]
}
```

### 1.3 Vite 配置

**`vite.config.ts`** - 代理与别名配置
```typescript
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { fileURLToPath, URL } from 'node:url'

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      "@": fileURLToPath(new URL("./src", import.meta.url)),
    },
  },
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:4000',
        changeOrigin: true,
        secure: false,
      },
      '/hubs': {
        target: 'http://localhost:4000',
        changeOrigin: true,
        ws: true, // WebSocket 代理
      },
    },
  },
})
```

### 1.4 项目目录结构

创建标准目录结构：

```
forum-frontend/
├── src/
│   ├── components/           # 可复用组件
│   │   ├── ui/              # shadcn/ui 组件
│   │   ├── layout/          # 布局组件
│   │   └── forms/           # 表单组件
│   ├── pages/               # 页面组件
│   │   ├── auth/            # 认证相关页面
│   │   ├── topics/          # 主题相关页面
│   │   └── admin/           # 管理页面
│   ├── hooks/               # 自定义 Hooks
│   ├── lib/                 # 工具函数
│   ├── api/                 # API 客户端
│   ├── types/               # TypeScript 类型定义
│   ├── store/               # 状态管理
│   └── styles/              # 样式文件
├── public/                  # 静态资源
└── docs/                   # 组件文档
```

```bash
# 创建目录结构
mkdir -p src/{components/{ui,layout,forms},pages/{auth,topics,admin},hooks,lib,api,types,store,styles}
```

---

## 🎨 Day 2: shadcn/ui + Tailwind 集成

### 2.1 Tailwind CSS 安装与配置

```bash
# 安装 Tailwind
npm install -D tailwindcss postcss autoprefixer
npx tailwindcss init -p
```

**`tailwind.config.js`** - shadcn 兼容配置
```javascript
/** @type {import('tailwindcss').Config} */
module.exports = {
  darkMode: ["class"],
  content: [
    './pages/**/*.{ts,tsx}',
    './components/**/*.{ts,tsx}',
    './app/**/*.{ts,tsx}',
    './src/**/*.{ts,tsx}',
  ],
  prefix: "",
  theme: {
    container: {
      center: true,
      padding: "2rem",
      screens: {
        "2xl": "1400px",
      },
    },
    extend: {
      colors: {
        border: "hsl(var(--border))",
        input: "hsl(var(--input))",
        ring: "hsl(var(--ring))",
        background: "hsl(var(--background))",
        foreground: "hsl(var(--foreground))",
        primary: {
          DEFAULT: "hsl(var(--primary))",
          foreground: "hsl(var(--primary-foreground))",
        },
        secondary: {
          DEFAULT: "hsl(var(--secondary))",
          foreground: "hsl(var(--secondary-foreground))",
        },
        destructive: {
          DEFAULT: "hsl(var(--destructive))",
          foreground: "hsl(var(--destructive-foreground))",
        },
        muted: {
          DEFAULT: "hsl(var(--muted))",
          foreground: "hsl(var(--muted-foreground))",
        },
        accent: {
          DEFAULT: "hsl(var(--accent))",
          foreground: "hsl(var(--accent-foreground))",
        },
        popover: {
          DEFAULT: "hsl(var(--popover))",
          foreground: "hsl(var(--popover-foreground))",
        },
        card: {
          DEFAULT: "hsl(var(--card))",
          foreground: "hsl(var(--card-foreground))",
        },
      },
      borderRadius: {
        lg: "var(--radius)",
        md: "calc(var(--radius) - 2px)",
        sm: "calc(var(--radius) - 4px)",
      },
      keyframes: {
        "accordion-down": {
          from: { height: "0" },
          to: { height: "var(--radix-accordion-content-height)" },
        },
        "accordion-up": {
          from: { height: "var(--radix-accordion-content-height)" },
          to: { height: "0" },
        },
      },
      animation: {
        "accordion-down": "accordion-down 0.2s ease-out",
        "accordion-up": "accordion-up 0.2s ease-out",
      },
    },
  },
  plugins: [require("tailwindcss-animate")],
}
```

### 2.2 shadcn/ui 初始化

```bash
# 初始化 shadcn/ui
npx shadcn-ui@latest init

# 添加核心组件
npx shadcn@latest add button
npx shadcn@latest add card
npx shadcn@latest add avatar
npx shadcn@latest add badge
npx shadcn@latest add input
npx shadcn@latest add textarea
npx shadcn@latest add form
npx shadcn@latest add dropdown-menu
npx shadcn@latest add dialog
npx shadcn@latest add sheet
npx shadcn@latest add sonner
npx shadcn@latest add tabs
npx shadcn@latest add separator
npx shadcn@latest add scroll-area
npx shadcn@latest add skeleton
npx shadcn@latest add command
```

### 2.3 全局样式配置

**`src/styles/globals.css`**
```css
@tailwind base;
@tailwind components;
@tailwind utilities;

@layer base {
  :root {
    --background: 0 0% 100%;
    --foreground: 222.2 84% 4.9%;
    --card: 0 0% 100%;
    --card-foreground: 222.2 84% 4.9%;
    --popover: 0 0% 100%;
    --popover-foreground: 222.2 84% 4.9%;
    --primary: 221.2 83.2% 53.3%;
    --primary-foreground: 210 40% 98%;
    --secondary: 210 40% 96%;
    --secondary-foreground: 222.2 84% 4.9%;
    --muted: 210 40% 96%;
    --muted-foreground: 215.4 16.3% 46.9%;
    --accent: 210 40% 96%;
    --accent-foreground: 222.2 84% 4.9%;
    --destructive: 0 84.2% 60.2%;
    --destructive-foreground: 210 40% 98%;
    --border: 214.3 31.8% 91.4%;
    --input: 214.3 31.8% 91.4%;
    --ring: 221.2 83.2% 53.3%;
    --radius: 0.5rem;
  }

  .dark {
    --background: 222.2 84% 4.9%;
    --foreground: 210 40% 98%;
    --card: 222.2 84% 4.9%;
    --card-foreground: 210 40% 98%;
    --popover: 222.2 84% 4.9%;
    --popover-foreground: 210 40% 98%;
    --primary: 217.2 91.2% 59.8%;
    --primary-foreground: 222.2 84% 4.9%;
    --secondary: 217.2 32.6% 17.5%;
    --secondary-foreground: 210 40% 98%;
    --muted: 217.2 32.6% 17.5%;
    --muted-foreground: 215 20.2% 65.1%;
    --accent: 217.2 32.6% 17.5%;
    --accent-foreground: 210 40% 98%;
    --destructive: 0 62.8% 30.6%;
    --destructive-foreground: 210 40% 98%;
    --border: 217.2 32.6% 17.5%;
    --input: 217.2 32.6% 17.5%;
    --ring: 224.3 76.3% 94.1%;
  }
}

@layer base {
  * {
    @apply border-border;
  }
  body {
    @apply bg-background text-foreground;
  }
}

/* Discourse 风格自定义样式 */
.topic-timeline {
  /* 右侧时间轴样式 */
}

.composer-floating {
  /* 底部浮动编辑器样式 */
}

.post-card {
  /* 帖子卡片样式 */
}
```

### 2.4 工具函数配置

**`src/lib/utils.ts`**
```typescript
import { type ClassValue, clsx } from "clsx"
import { twMerge } from "tailwind-merge"

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

export function formatDate(date: Date | string): string {
  return new Intl.DateTimeFormat('zh-CN', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(date))
}

export function formatRelativeTime(date: Date | string): string {
  const now = new Date()
  const target = new Date(date)
  const diffInSeconds = Math.floor((now.getTime() - target.getTime()) / 1000)

  if (diffInSeconds < 60) return '刚刚'
  if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)}分钟前`
  if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)}小时前`
  if (diffInSeconds < 2592000) return `${Math.floor(diffInSeconds / 86400)}天前`
  
  return formatDate(date)
}
```

---

## 🔧 Day 3: 后端 ASP.NET Core 搭建

### 3.1 ASP.NET Core 8 项目创建

```bash
# 创建解决方案和项目
dotnet new sln -n Forum
dotnet new webapi -n Forum.Api
dotnet sln add Forum.Api
cd Forum.Api

# 安装核心 NuGet 包
dotnet add package MySqlConnector
dotnet add package Dapper
dotnet add package Microsoft.AspNetCore.SignalR
dotnet add package FluentValidation
dotnet add package FluentValidation.AspNetCore
dotnet add package MailKit
dotnet add package System.IdentityModel.Tokens.Jwt
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
dotnet add package DbUp-MySQL
```

### 3.2 项目结构设计

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

```bash
# 创建目录结构
mkdir -p {Controllers,Hubs,Models/{Entities,DTOs,Requests},Services,Repositories,Middleware,Infrastructure/{Database,Email,Auth},Migrations,Extensions}
```

### 3.3 核心配置文件

**`appsettings.json`**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=forum_db;Uid=forum_user;Pwd=forum_password;CharSet=utf8mb4;"
  },
  "JwtSettings": {
    "Secret": "your-super-secret-key-here-must-be-at-least-32-characters-long",
    "ExpirationInMinutes": 60,
    "RefreshExpirationInDays": 7,
    "Issuer": "ForumApi",
    "Audience": "ForumClient"
  },
  "EmailSettings": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "your-email@gmail.com",
    "SmtpPassword": "your-app-password",
    "FromEmail": "noreply@forum.com",
    "FromName": "Forum System"
  },
  "CorsSettings": {
    "AllowedOrigins": ["http://localhost:5173", "http://localhost:3000"]
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

**`appsettings.Development.json`**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=forum_dev_db;Uid=forum_user;Pwd=forum_password;CharSet=utf8mb4;"
  },
  "JwtSettings": {
    "Secret": "development-secret-key-32-chars-minimum"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "System": "Information",
      "Microsoft": "Information"
    }
  }
}
```

### 3.4 Program.cs 配置

**`Program.cs`** - 应用程序入口配置
```csharp
using Forum.Api.Extensions;
using Forum.Api.Hubs;
using Forum.Api.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog 配置
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// 添加服务
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 自定义服务注册
builder.Services.AddDatabaseServices(builder.Configuration);
builder.Services.AddAuthenticationServices(builder.Configuration);
builder.Services.AddBusinessServices();
builder.Services.AddEmailServices(builder.Configuration);
builder.Services.AddSignalR();

// CORS 配置
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>();
        policy.WithOrigins(allowedOrigins ?? new[] { "http://localhost:5173" })
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// 配置管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseCors("DefaultPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.MapControllers();
app.MapHub<TopicsHub>("/hubs/topics");

// 数据库迁移
await app.RunDatabaseMigrationsAsync();

try
{
    Log.Information("Starting web application");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
```

---

## 🏗️ Day 4: 数据库设计与迁移

### 4.1 数据库迁移系统

**`Infrastructure/Database/DatabaseMigrator.cs`**
```csharp
using DbUp;
using MySqlConnector;
using System.Reflection;

namespace Forum.Api.Infrastructure.Database;

public static class DatabaseMigrator
{
    public static async Task RunMigrationsAsync(string connectionString)
    {
        // 确保数据库存在
        await EnsureDatabaseExistsAsync(connectionString);

        // 运行迁移
        var upgrader = DeployChanges.To
            .MySqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            throw new Exception($"Database migration failed: {result.Error}");
        }

        Console.WriteLine("Database migration completed successfully!");
    }

    private static async Task EnsureDatabaseExistsAsync(string connectionString)
    {
        var builder = new MySqlConnectionStringBuilder(connectionString);
        var databaseName = builder.Database;
        builder.Database = "";

        await using var connection = new MySqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE IF NOT EXISTS `{databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;";
        await command.ExecuteNonQueryAsync();
    }
}
```

### 4.2 核心数据库迁移脚本

**`Migrations/001_CreateUserTables.sql`**
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
  updated_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
  INDEX idx_users_email (email),
  INDEX idx_users_username (username),
  INDEX idx_users_status (status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE user_roles (
  user_id BIGINT UNSIGNED NOT NULL,
  role ENUM('user','mod','admin') NOT NULL,
  PRIMARY KEY (user_id, role),
  CONSTRAINT fk_user_roles_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Email verification tokens
CREATE TABLE email_verification_tokens (
  id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  user_id BIGINT UNSIGNED NOT NULL,
  token VARCHAR(100) NOT NULL UNIQUE,
  expires_at DATETIME(3) NOT NULL,
  used_at DATETIME(3) NULL,
  created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  CONSTRAINT fk_email_tokens_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
  INDEX idx_email_tokens_token (token),
  INDEX idx_email_tokens_expires (expires_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Refresh tokens
CREATE TABLE refresh_tokens (
  id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  user_id BIGINT UNSIGNED NOT NULL,
  token_hash BINARY(32) NOT NULL,
  expires_at DATETIME(3) NOT NULL,
  revoked_at DATETIME(3) NULL,
  ua VARCHAR(200) NULL,
  ip VARCHAR(45) NULL,
  created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  UNIQUE KEY uq_user_token_hash (user_id, token_hash),
  INDEX idx_rt_user (user_id),
  INDEX idx_rt_expires (expires_at),
  CONSTRAINT fk_rt_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

**`Migrations/002_CreateCategoriesAndTags.sql`**
```sql
-- Categories
CREATE TABLE categories (
  id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  name VARCHAR(100) NOT NULL,
  slug VARCHAR(100) NOT NULL UNIQUE,
  description TEXT,
  color VARCHAR(20) DEFAULT '#007acc',
  `order` INT NOT NULL DEFAULT 0,
  is_archived TINYINT(1) NOT NULL DEFAULT 0,
  created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  updated_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
  INDEX idx_categories_order (`order`, is_archived)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Category moderators
CREATE TABLE category_moderators (
  category_id BIGINT UNSIGNED NOT NULL,
  user_id BIGINT UNSIGNED NOT NULL,
  PRIMARY KEY (category_id, user_id),
  CONSTRAINT fk_cat_mod_cat FOREIGN KEY (category_id) REFERENCES categories(id) ON DELETE CASCADE,
  CONSTRAINT fk_cat_mod_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Tags
CREATE TABLE tags (
  id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  name VARCHAR(100) NOT NULL,
  slug VARCHAR(100) NOT NULL UNIQUE,
  description VARCHAR(255),
  color VARCHAR(20) DEFAULT '#6B7280',
  usage_count INT NOT NULL DEFAULT 0,
  created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  updated_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
  INDEX idx_tags_usage (usage_count DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Insert default categories
INSERT INTO categories (name, slug, description, color, `order`) VALUES
('通用讨论', 'general', '通用话题讨论区', '#007acc', 1),
('技术交流', 'tech', '技术相关讨论', '#10B981', 2),
('产品反馈', 'feedback', '产品使用反馈和建议', '#F59E0B', 3),
('公告通知', 'announcements', '官方公告和通知', '#EF4444', 0);

-- Insert default tags
INSERT INTO tags (name, slug, description, color) VALUES
('问题', 'question', '求助类问题', '#3B82F6'),
('讨论', 'discussion', '开放式讨论', '#8B5CF6'),
('分享', 'share', '经验分享', '#10B981'),
('建议', 'suggestion', '功能建议', '#F59E0B'),
('反馈', 'feedback', '问题反馈', '#EF4444');
```

**`Migrations/003_CreateTopicsAndPosts.sql`**
```sql
-- Topics
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
  INDEX idx_topic_author (author_id),
  INDEX idx_topic_pinned_last (is_pinned DESC, last_posted_at DESC, id DESC),
  FULLTEXT KEY ftx_topic_title (title)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Topic tags relationship
CREATE TABLE topic_tags (
  topic_id BIGINT UNSIGNED NOT NULL,
  tag_id BIGINT UNSIGNED NOT NULL,
  PRIMARY KEY (topic_id, tag_id),
  CONSTRAINT fk_tt_topic FOREIGN KEY (topic_id) REFERENCES topics(id) ON DELETE CASCADE,
  CONSTRAINT fk_tt_tag FOREIGN KEY (tag_id) REFERENCES tags(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Posts
CREATE TABLE posts (
  id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  topic_id BIGINT UNSIGNED NOT NULL,
  author_id BIGINT UNSIGNED NOT NULL,
  content_md MEDIUMTEXT NOT NULL,
  reply_to_post_id BIGINT UNSIGNED NULL,
  is_edited TINYINT(1) NOT NULL DEFAULT 0,
  is_deleted TINYINT(1) NOT NULL DEFAULT 0,
  deleted_at DATETIME(3) NULL,
  created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  updated_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
  CONSTRAINT fk_posts_topic FOREIGN KEY (topic_id) REFERENCES topics(id) ON DELETE CASCADE,
  CONSTRAINT fk_posts_author FOREIGN KEY (author_id) REFERENCES users(id),
  CONSTRAINT fk_posts_reply_to FOREIGN KEY (reply_to_post_id) REFERENCES posts(id),
  INDEX idx_posts_topic_created (topic_id, is_deleted, created_at, id),
  INDEX idx_posts_author (author_id),
  FULLTEXT KEY ftx_posts_content (content_md)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Post mentions
CREATE TABLE post_mentions (
  post_id BIGINT UNSIGNED NOT NULL,
  mentioned_user_id BIGINT UNSIGNED NOT NULL,
  PRIMARY KEY (post_id, mentioned_user_id),
  CONSTRAINT fk_pm_post FOREIGN KEY (post_id) REFERENCES posts(id) ON DELETE CASCADE,
  CONSTRAINT fk_pm_user FOREIGN KEY (mentioned_user_id) REFERENCES users(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

### 4.3 数据访问层基础

**`Infrastructure/Database/IDbConnectionFactory.cs`**
```csharp
using System.Data;

namespace Forum.Api.Infrastructure.Database;

public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateConnectionAsync();
}
```

**`Infrastructure/Database/MySqlConnectionFactory.cs`**
```csharp
using MySqlConnector;
using System.Data;

namespace Forum.Api.Infrastructure.Database;

public class MySqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public MySqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<IDbConnection> CreateConnectionAsync()
    {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }
}
```

---

## 🌐 Day 5: SignalR 基座与扩展方法

### 5.1 SignalR Hub 基础实现

**`Hubs/TopicsHub.cs`**
```csharp
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Forum.Api.Hubs;

[Authorize]
public class TopicsHub : Hub
{
    private readonly ILogger<TopicsHub> _logger;

    public TopicsHub(ILogger<TopicsHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinTopic(long topicId)
    {
        var groupName = GetTopicGroupName(topicId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        
        _logger.LogInformation("User {UserId} joined topic {TopicId}", 
            Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, topicId);
    }

    public async Task LeaveTopic(long topicId)
    {
        var groupName = GetTopicGroupName(topicId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        
        _logger.LogInformation("User {UserId} left topic {TopicId}", 
            Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, topicId);
    }

    public async Task Typing(long topicId, bool isTyping)
    {
        var groupName = GetTopicGroupName(topicId);
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

        await Clients.OthersInGroup(groupName).SendAsync("UserTyping", new
        {
            TopicId = topicId,
            UserId = userId,
            Username = username,
            IsTyping = isTyping,
            Timestamp = DateTime.UtcNow
        });
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("User {UserId} connected to SignalR", userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("User {UserId} disconnected from SignalR", userId);
        await base.OnDisconnectedAsync(exception);
    }

    private static string GetTopicGroupName(long topicId) => $"topic:{topicId}";
}
```

### 5.2 SignalR 服务接口

**`Services/ISignalRService.cs`**
```csharp
namespace Forum.Api.Services;

public interface ISignalRService
{
    Task NotifyPostCreatedAsync(long topicId, object postData);
    Task NotifyPostEditedAsync(long topicId, object postData);
    Task NotifyPostDeletedAsync(long topicId, long postId);
    Task NotifyTopicStatsUpdatedAsync(long topicId, object stats);
}
```

**`Services/SignalRService.cs`**
```csharp
using Microsoft.AspNetCore.SignalR;
using Forum.Api.Hubs;

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

    public async Task NotifyPostCreatedAsync(long topicId, object postData)
    {
        var groupName = $"topic:{topicId}";
        await _hubContext.Clients.Group(groupName).SendAsync("PostCreated", postData);
        _logger.LogDebug("Notified PostCreated for topic {TopicId}", topicId);
    }

    public async Task NotifyPostEditedAsync(long topicId, object postData)
    {
        var groupName = $"topic:{topicId}";
        await _hubContext.Clients.Group(groupName).SendAsync("PostEdited", postData);
        _logger.LogDebug("Notified PostEdited for topic {TopicId}", topicId);
    }

    public async Task NotifyPostDeletedAsync(long topicId, long postId)
    {
        var groupName = $"topic:{topicId}";
        await _hubContext.Clients.Group(groupName).SendAsync("PostDeleted", new { TopicId = topicId, PostId = postId });
        _logger.LogDebug("Notified PostDeleted for topic {TopicId}, post {PostId}", topicId, postId);
    }

    public async Task NotifyTopicStatsUpdatedAsync(long topicId, object stats)
    {
        var groupName = $"topic:{topicId}";
        await _hubContext.Clients.Group(groupName).SendAsync("TopicStats", stats);
        _logger.LogDebug("Notified TopicStats for topic {TopicId}", topicId);
    }
}
```

### 5.3 服务扩展方法

**`Extensions/ServiceCollectionExtensions.cs`**
```csharp
using Forum.Api.Infrastructure.Database;
using Forum.Api.Infrastructure.Auth;
using Forum.Api.Infrastructure.Email;
using Forum.Api.Services;
using Forum.Api.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Forum.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDbConnectionFactory, MySqlConnectionFactory>();
        
        // 注册仓储
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<ITopicRepository, TopicRepository>();
        services.AddScoped<IPostRepository, PostRepository>();
        
        return services;
    }

    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");

        services.Configure<JwtSettings>(jwtSettings);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ClockSkew = TimeSpan.Zero
                };

                // SignalR 支持
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }
                        
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordService, PasswordService>();

        return services;
    }

    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<ITopicService, TopicService>();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<ISignalRService, SignalRService>();

        return services;
    }

    public static IServiceCollection AddEmailServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }
}
```

**`Extensions/WebApplicationExtensions.cs`**
```csharp
using Forum.Api.Infrastructure.Database;

namespace Forum.Api.Extensions;

public static class WebApplicationExtensions
{
    public static async Task RunDatabaseMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Database connection string not found");
        }

        await DatabaseMigrator.RunMigrationsAsync(connectionString);
    }
}
```

---

## ✅ M0 验收清单

### 环境验证
- [ ] **前端开发服务器启动** (`npm run dev` → http://localhost:5173)
- [ ] **后端 API 服务启动** (`dotnet run` → http://localhost:4000)
- [ ] **Swagger 文档可访问** (http://localhost:4000/swagger)
- [ ] **数据库连接成功** (检查日志无连接错误)

### 功能验证
- [ ] **数据库表创建完成** (users, categories, topics, posts 等表存在)
- [ ] **SignalR 连接测试** (前端可连接 /hubs/topics)
- [ ] **shadcn 组件渲染** (Button, Card 等组件正常显示)
- [ ] **路径别名工作** (@/ 导入路径解析正确)

### 代码质量
- [ ] **ESLint/Prettier 配置生效** (代码格式化正常)
- [ ] **TypeScript 编译无错误** (npm run build 成功)
- [ ] **C# 编译无警告** (dotnet build 成功)
- [ ] **日志记录正常** (Serilog 输出到控制台和文件)

### 文档完整性
- [ ] **项目 README.md** (包含启动说明)
- [ ] **环境变量示例** (.env.example 文件)
- [ ] **数据库初始化脚本** (可重复执行)
- [ ] **开发指南文档** (代码规范、目录结构说明)

---

**预计完成时间**: 5 个工作日  
**关键阻塞点**: 数据库连接配置、SignalR 认证集成  
**下一步**: M1 用户认证系统开发