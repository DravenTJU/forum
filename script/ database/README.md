# 数据库测试数据脚本

此目录包含论坛系统的数据库测试数据生成和管理脚本。

## 📁 脚本文件

### 1. `insert_test_data.sql`
**功能**: 生成完整的论坛测试数据

**包含数据**:
- **12个测试用户** - 不同角色和活跃度
  - 1个管理员 (admin)
  - 1个版主 (moderator) 
  - 8个普通用户（活跃用户、新手、资深开发者等）
  - 2个特殊状态用户（未验证、暂停）

- **15个测试主题** - 涵盖不同分类和状态
  - 置顶主题（欢迎指南、技术讨论）
  - 热门技术话题（React 18、ASP.NET Core、Docker等）
  - 新手求助帖
  - 锁定主题（功能建议）

- **45个帖子** - 真实的讨论内容
  - 详细的技术分享内容
  - 用户互动回复
  - @提及和引用关系
  - Markdown格式的代码示例

- **完整的关联数据**
  - 用户角色权限分配
  - 主题标签关联
  - 帖子提及关系
  - 分类版主权限

### 2. `cleanup_test_data.sql`
**功能**: 清理所有测试数据，重置为初始状态

**保留数据**:
- 系统默认分类（通用讨论、技术交流、产品反馈、公告通知）
- 系统默认标签（问题、讨论、分享、建议、反馈）

**清理数据**:
- 所有用户账户和相关数据
- 所有主题和帖子
- 所有用户创建的关联关系

## 🚀 使用方法

### 前提条件
1. 确保数据库已运行相关迁移脚本（001_*.sql, 002_*.sql, 003_*.sql）
2. 具备数据库写入权限
3. **仅在开发/测试环境使用**

### 生成测试数据
```bash
# 方式1: 通过 MySQL 命令行
mysql -h localhost -u your_username -p your_database < script/insert_test_data.sql

# 方式2: 通过 MySQL Workbench 或其他客户端工具
# 打开 insert_test_data.sql 文件并执行

# 方式3: 通过 Docker (如果使用 docker-compose)
docker-compose exec mysql mysql -u forum_user -p forum_db < /docker-entrypoint-initdb.d/insert_test_data.sql
```

### 清理测试数据
```bash
# ⚠️ 警告：这会删除所有用户数据！
mysql -h localhost -u your_username -p your_database < script/cleanup_test_data.sql
```

## 🧪 测试场景覆盖

### 用户类型测试
- **管理员权限**: 全功能权限测试
- **版主权限**: 内容管理权限测试  
- **普通用户**: 基本功能测试
- **新手用户**: 注册流程和初始体验
- **不活跃用户**: 长期未登录场景
- **受限用户**: 暂停账户状态测试

### 内容类型测试
- **置顶主题**: 优先级显示测试
- **锁定主题**: 只读模式测试
- **热门讨论**: 多回帖互动测试
- **技术分享**: 长内容和代码展示测试
- **求助帖子**: 问答模式测试

### 交互功能测试
- **@提及功能**: 用户通知系统测试
- **回复引用**: 层级回复显示测试
- **标签筛选**: 内容分类和搜索测试
- **分类浏览**: 内容组织测试

### 搜索功能测试  
- **全文搜索**: 通过大量内容测试搜索功能
- **标签搜索**: 标签系统有效性验证
- **用户搜索**: 用户查找功能测试

## 📊 数据统计

执行 `insert_test_data.sql` 后将包含：

| 数据类型 | 数量 | 说明 |
|---------|------|------|
| 用户 | 12 | 包含各种角色和状态 |
| 主题 | 15 | 不同分类和状态的主题 |  
| 帖子 | 45+ | 包含首帖和回帖 |
| 用户角色 | 15 | 角色权限分配 |
| 主题标签 | 20+ | 主题标签关联 |
| 帖子提及 | 25+ | @提及关系 |

## 🔒 安全注意事项

1. **仅限开发/测试环境**：绝对不要在生产环境执行这些脚本
2. **数据备份**：执行前请备份重要数据
3. **权限控制**：确保脚本执行权限受到适当控制
4. **密码安全**：所有测试用户使用相同的哈希密码（测试用）

## 🔑 测试账户信息

**默认密码**: 所有测试账户密码均为 `123123123`

**主要测试账户**:
- `admin@forum.example.com` - 管理员账户
- `mod@forum.example.com` - 版主账户  
- `jane@example.com` - 活跃开发者
- `david@example.com` - 新手用户

## 🛠️ 自定义修改

### 修改用户数据
在 `insert_test_data.sql` 的用户插入部分修改：
```sql
INSERT INTO users (username, email, password_hash, ...) VALUES
('your_username', 'your_email@example.com', 'password_hash', ...);
```

### 修改内容数据
调整主题和帖子内容以适应你的测试需求：
```sql
INSERT INTO topics (title, slug, author_id, category_id, ...) VALUES
('Your Topic Title', 'your-topic-slug', author_id, category_id, ...);
```

### 修改关联关系
根据需要调整用户角色、主题标签等关联：
```sql
INSERT INTO user_roles (user_id, role) VALUES (user_id, 'role_name');
INSERT INTO topic_tags (topic_id, tag_id) VALUES (topic_id, tag_id);
```

## 🐛 故障排除

### 常见错误

1. **外键约束错误**
   ```
   Error: Cannot add or update a child row: a foreign key constraint fails
   ```
   **解决方案**: 确保数据库迁移已正确执行，相关表已存在

2. **唯一约束错误**  
   ```
   Error: Duplicate entry for key 'username' or 'email'
   ```
   **解决方案**: 先执行清理脚本，或修改重复的用户名/邮箱

3. **权限错误**
   ```
   Error: Access denied for user
   ```
   **解决方案**: 确保数据库用户具有足够的权限

### 验证数据完整性
```sql
-- 检查数据统计
SELECT 'Users' as type, COUNT(*) as count FROM users
UNION ALL SELECT 'Topics', COUNT(*) FROM topics  
UNION ALL SELECT 'Posts', COUNT(*) FROM posts;

-- 检查关联关系
SELECT 'Orphaned Posts' as issue, COUNT(*) as count 
FROM posts p LEFT JOIN topics t ON p.topic_id = t.id 
WHERE t.id IS NULL;
```

## 📝 更新日志

- **v1.0** (2025-08-25)
  - 初始版本
  - 包含完整的论坛测试数据
  - 支持所有核心功能测试场景