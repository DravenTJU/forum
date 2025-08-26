# Forum 前端 - React + TypeScript + Vite

Discourse 风格论坛的前端应用，基于现代 Web 技术栈构建。

## 技术栈

- **框架**: React 19 + TypeScript
- **构建工具**: Vite 7
- **UI 框架**: shadcn/ui + Radix UI
- **样式**: Tailwind CSS 4
- **状态管理**: TanStack Query (服务端状态) + React Hooks (本地状态)
- **表单**: react-hook-form + zod 验证
- **路由**: React Router v7
- **实时通信**: @microsoft/signalr
- **图标**: Lucide React

## 开发命令

```bash
# 安装依赖
npm install

# 启动开发服务器
npm run dev              # http://localhost:5173

# 构建相关
npm run build           # TypeScript 编译 + Vite 生产构建
npm run preview         # 预览生产构建

# 代码质量
npm run lint            # ESLint 代码检查
```

## 项目结构

```
src/
├── components/         # 🧩 UI 组件库
│   ├── ui/            # shadcn/ui 基础组件
│   │   ├── button.tsx, card.tsx, dialog.tsx
│   │   ├── form.tsx, input.tsx, loading-*.tsx
│   │   └── ...更多 UI 组件
│   ├── auth/          # 认证相关组件
│   ├── topics/        # 主题相关组件
│   ├── filters/       # 筛选组件
│   └── layout/        # 布局组件
│
├── pages/             # 📄 页面组件
│   ├── HomePage.tsx           # 主页 (/)
│   ├── auth/                  # 认证页面
│   │   ├── LoginPage.tsx      # 登录 (/login)
│   │   └── RegisterPage.tsx   # 注册 (/register)
│   ├── topics/                # 主题页面
│   └── admin/                 # 管理后台
│
├── api/               # 🌐 API 客户端层
│   ├── auth.ts                # 认证 API
│   ├── categories.ts          # 分类 API
│   ├── tags.ts                # 标签 API
│   └── topics.ts              # 主题帖子 API
│
├── hooks/             # 🪝 自定义 React Hooks
│   ├── useAuth.ts             # 认证状态管理
│   ├── useTopics.ts           # 主题数据管理
│   ├── useApiCall.ts          # API 调用封装
│   ├── useFormError.ts        # 表单错误处理
│   └── useIntersectionObserver.ts  # 无限滚动
│
├── types/             # 📝 TypeScript 类型定义
│   ├── auth.ts                # 认证相关类型
│   └── api.ts                 # API 响应类型
│
├── lib/               # 🔧 工具函数
│   ├── utils.ts               # 通用工具 (cn, 日期格式化等)
│   ├── error-utils.ts         # 错误处理工具
│   └── notification.ts        # 通知工具
│
├── styles/            # 🎨 样式文件
│   └── globals.css            # 全局样式
│
└── store/             # 状态管理 (预留扩展)
```

## 开发规范

### 组件开发
- **命名**: 组件文件使用 `PascalCase.tsx`
- **类型**: 为所有组件 props 定义 TypeScript 接口
- **导出**: 使用默认导出，配合命名导出类型
```tsx
interface ButtonProps {
  variant?: 'default' | 'ghost'
  children: React.ReactNode
}

export default function Button({ variant = 'default', children }: ButtonProps) {
  return <button className={cn('base-styles', variants[variant])}>{children}</button>
}
```

### Hook 开发
- **命名**: 以 `use` 开头，使用 `camelCase`
- **返回值**: 使用对象返回多个值，便于解构
- **类型**: 明确定义返回值类型
```tsx
export function useAuth(): {
  user: User | null
  login: (credentials: LoginRequest) => Promise<void>
  logout: () => void
  isLoading: boolean
} {
  // 实现...
}
```

### API 客户端
- **文件组织**: 按业务模块分离（auth.ts, topics.ts）
- **错误处理**: 统一错误处理和类型定义
- **类型安全**: 请求和响应都有完整类型定义
```tsx
export async function loginUser(credentials: LoginRequest): Promise<ApiResponse<LoginResponse>> {
  const response = await api.post<LoginResponse>('/auth/login', credentials)
  return response.data
}
```

### 状态管理策略
- **服务端状态**: TanStack Query 管理 API 数据、缓存、同步
- **本地状态**: React useState/useReducer 管理组件状态
- **表单状态**: react-hook-form 管理表单和验证
- **全局状态**: 仅在必要时使用 Context API

### 样式规范
- **Tailwind 优先**: 使用 Tailwind 类名进行样式设计
- **shadcn/ui**: 优先使用 shadcn/ui 组件，必要时扩展
- **条件样式**: 使用 `cn()` 工具函数合并类名
- **响应式**: 移动优先设计，使用 Tailwind 响应式前缀

## shadcn/ui 组件管理

### 安装新组件
```bash
npx shadcn@latest add [component-name]
```

### 现有组件
- 基础组件: button, card, dialog, form, input, label
- 布局组件: scroll-area, separator, sheet, tabs
- 反馈组件: loading-spinner, skeleton, sonner (toast)
- 交互组件: avatar, badge, command, dropdown-menu

### 自定义组件
在 `components/` 下按功能模块组织，基于 shadcn/ui 组件构建业务组件。

## 实时通信

### SignalR 集成
```tsx
// 连接管理
const connection = new HubConnectionBuilder()
  .withUrl('/hubs/topics')
  .withAutomaticReconnect()
  .build()

// 事件监听
connection.on('PostCreated', (post) => {
  // 处理新帖子
})

connection.on('PostEdited', (post) => {
  // 处理帖子编辑
})
```

### 实时状态管理
- 在 `useTopics` hook 中集成 SignalR 连接
- 自动处理连接状态、重连、错误恢复
- 与 TanStack Query 缓存同步更新

## API 集成

### 认证流程
- JWT 双令牌机制 (Access + Refresh)
- HttpOnly cookies 存储
- 自动令牌刷新
- 路由守卫保护

### 数据获取模式
```tsx
// 使用 TanStack Query
const { data: topics, isLoading, error } = useQuery({
  queryKey: ['topics', { categoryId, page }],
  queryFn: () => fetchTopics({ categoryId, page }),
  staleTime: 5 * 60 * 1000  // 5 分钟缓存
})
```

### 表单提交
```tsx
// 使用 react-hook-form + zod
const form = useForm<LoginFormData>({
  resolver: zodResolver(loginSchema),
  defaultValues: { email: '', password: '' }
})

const { mutate: login, isPending } = useMutation({
  mutationFn: loginUser,
  onSuccess: () => router.push('/'),
  onError: (error) => toast.error(error.message)
})
```

## 构建和部署

### 开发环境
- Vite 开发服务器，支持 HMR 和快速刷新
- API 请求代理到后端 (`http://localhost:4000`)
- 实时通信 WebSocket 代理

### 生产构建
```bash
npm run build
```
- TypeScript 类型检查
- Vite 打包优化
- 资源压缩和分割
- 输出到 `dist/` 目录

### 环境配置
- `vite.config.ts` - Vite 配置，包含 API 代理设置
- `tsconfig.json` - TypeScript 配置
- `tailwind.config.js` - Tailwind CSS 配置
- `components.json` - shadcn/ui 配置

## 开发工作流

### 新功能开发
1. **API 类型定义**: 在 `types/api.ts` 定义请求/响应类型
2. **API 客户端**: 在 `api/` 目录实现 API 调用函数
3. **Hooks 封装**: 在 `hooks/` 目录封装数据获取和状态逻辑
4. **组件开发**: 在 `components/` 创建 UI 组件
5. **页面集成**: 在 `pages/` 组装完整页面

### 组件开发流程
1. **shadcn/ui 基础**: 优先使用现有组件
2. **业务封装**: 基于基础组件创建业务组件
3. **类型定义**: 完整的 props 类型定义
4. **测试验证**: 在不同场景下测试组件

### 调试技巧
- **React DevTools**: 组件状态调试
- **TanStack Query DevTools**: API 状态和缓存调试  
- **浏览器调试**: Network 面板查看 API 请求
- **Console 日志**: 开发环境的详细日志

## 性能优化

### 代码分割
- React.lazy() 和 Suspense 实现路由级代码分割
- 组件级懒加载优化首屏加载

### 缓存策略
- TanStack Query 智能缓存 API 响应
- 静态资源浏览器缓存
- Service Worker 缓存 (可选)

### 图片优化
- 使用适当格式 (WebP, AVIF)
- 懒加载和占位符
- 响应式图片

## 相关文档

- [项目整体架构](../CLAUDE.md)
- [后端 API 文档](../Forum.Api/README.md)
- [编码规范](../doc/coding_standards_and_principles.md)
- [实现工作流](../doc/implementation-workflow.md)