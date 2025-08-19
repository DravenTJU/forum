# 项目核心开发规范与原则

本文档定义了项目在前后端开发中必须共同遵守的核心规范与软件工程原则。它是保证代码质量、可维护性、可扩展性和团队协作效率的基石。

---

## 1. 核心设计原则

### KISS (Keep It Simple, Stupid) & YAGNI (You Ain't Gonna Need It)
-   **规范**: 始终选择最简单、最直接的解决方案。除非有明确、可预见的需求，否则不要添加当前不需要的功能或过度设计的抽象层。
-   **目的**: 避免不必要的复杂性，降低维护成本，加快开发速度。

### DRY (Don't Repeat Yourself)
-   **规范**: 避免重复代码。通过创建可复用的函数、组件、服务或类来封装通用逻辑。
-   **目的**: 提高代码复用性，减少错误，使修改逻辑时只需改动一处。

### SoC (Separation of Concerns) & SSOT (Single Source of Truth)
-   **规范**: 严格分离不同职责的模块。例如，UI、业务逻辑、数据访问应清晰分离。每个数据或状态都应有唯一的、权威的来源。
-   **目的**: 提高模块化，降低耦合度。保证数据一致性，避免因数据副本导致的问题。

---

## 2. SOLID 原则

所有面向对象和组件的设计都应遵循 SOLID 原则。

-   **S - 单一职责原则 (Single Responsibility Principle)**
    -   每个类、模块或函数只负责一项功能。
-   **O - 开闭原则 (Open/Closed Principle)**
    -   对扩展开放，对修改关闭。通过扩展（如继承、实现接口）来增加新功能，而非修改现有代码。
-   **L - 里氏替换原则 (Liskov Substitution Principle)**
    -   子类必须能够替换其父类，而程序行为不发生改变。
-   **I - 接口隔离原则 (Interface Segregation Principle)**
    -   使用多个专门的接口，而不是一个庞大、通用的接口。
-   **D - 依赖倒置原则 (Dependency Inversion Principle)**
    -   高层模块不应依赖于低层模块，两者都应依赖于抽象（接口）。这是整洁架构的核心。

---

## 3. 开发实践与质量

### 一致性与最小惊讶原则 (Consistency & Principle of Least Astonishment)
-   **规范**: 代码风格、命名约定、目录结构和架构模式应在整个项目中保持一致。代码的行为应符合开发者的普遍预期。
-   **目的**: 提高代码的可读性、可预测性和可维护性。

### API 优先与安全性 (API-First & Security)
-   **规范**: 在功能开发前，优先设计和约定好 API 接口。所有代码编写都必须考虑安全性，对所有外部输入（API 请求、用户输入）进行严格的验证和清理，遵循默认不信任原则。
-   **目的**: 确保前后端协作顺畅，从源头构建安全的系统。

### 可测试性 (Testability)
-   **规范**: 代码必须易于测试。避免静态耦合，使用依赖注入，编写纯函数，并确保业务逻辑与外部依赖解耦。
-   **目的**: 支持单元测试和集成测试的自动化，保障代码质量和重构安全。

### 渐进增强与性能优化 (Progressive Enhancement & Performance)
-   **规范**: **前端**: 优先保证核心功能的可用性，然后逐步为现代浏览器增加体验增强功能。**后端**: 关注关键路径的性能，如数据库查询、高频 API 调用，避免不必要的计算和 I/O 操作。
-   **目的**: 提升用户体验和系统整体效率。

---

## 4. 代码风格与命名约定

### A. 后端 (C# / ASP.NET Core)

-   **代码风格**: 
    -   遵循 **Microsoft C# 官方编码约定**。
    -   使用项目根目录下的 `.editorconfig` 文件来统一和强制代码风格。
    -   在提交代码前，推荐使用 `dotnet format` 命令自动格式化代码。
-   **命名约定**:
    -   **类、接口、枚举、公共方法、公共属性**: `PascalCase` (帕斯卡命名法)。
    -   **接口**: 必须以 `I` 作为前缀，例如 `IUserRepository`。
    -   **私有字段 (private fields)**: 必须以 `_` 作为前缀，例如 `_connectionString`。
    -   **局部变量、方法参数**: `camelCase` (驼峰命名法)。
    -   **常量 (const)**: `PascalCase`。
    -   **异步方法**: 必须以 `Async` 作为后缀，例如 `GetUserByIdAsync`。

### B. 前端 (React / TypeScript)

-   **代码风格**:
    -   使用 **Prettier** 进行代码的自动格式化，确保风格统一。
    -   使用 **ESLint** 进行代码质量检查，并遵循项目配置的规则。
-   **命名约定**:
    -   **组件**: `PascalCase`，文件名使用 `.tsx` 后缀，例如 `TorrentCard.tsx`。
    -   **非组件文件 (Hooks, Utils, API, Slices)**: `camelCase`，文件名使用 `.ts` 后缀，例如 `useAuth.ts`, `apiClient.ts`。
    -   **变量、函数**: `camelCase`。
    -   **常量**: `UPPER_SNAKE_CASE` (大写蛇形命名法)，例如 `const API_BASE_URL = '...'`。
    -   **类型、接口**: `PascalCase`，例如 `type UserProfile = { ... }`。