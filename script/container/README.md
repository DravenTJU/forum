# ECR 部署脚本说明文档

## 概述

`deploy-to-ecr.sh` 是一个自动化脚本，用于将 Forum 项目的后端 API Docker 镜像构建并推送到 AWS ECR (Elastic Container Registry)。

## 功能特性

- 🔍 自动检查必要工具（AWS CLI、Docker）
- 📁 验证项目目录结构
- 🔐 自动登录 AWS ECR
- 📦 自动创建 ECR 仓库（如果不存在）
- 🏗️ 构建 Docker 镜像
- 📤 推送镜像到 ECR（包含 latest 和时间戳标签）
- 🧹 可选的本地镜像清理

## 前置要求

### 1. 必要工具

确保已安装以下工具：

```bash
# AWS CLI
brew install awscli

# Docker
# 通过 Docker Desktop 或其他方式安装
```

### 2. AWS 凭证配置

#### 方法一：AWS Profile 配置（推荐）

1. 创建或编辑 AWS credentials 文件：
```bash
mkdir -p ~/.aws
```

2. 在 `~/.aws/credentials` 中添加您的凭证：
```ini
[528757821800_DevOpsPermissionSet]
aws_access_key_id=YOUR_ACCESS_KEY_ID
aws_secret_access_key=YOUR_SECRET_ACCESS_KEY
aws_session_token=YOUR_SESSION_TOKEN  # 如果使用临时凭证
```

3. 在 `~/.aws/config` 中配置区域：
```ini
[profile 528757821800_DevOpsPermissionSet]
region=ap-southeast-2
output=json
```

#### 方法二：环境变量

```bash
export AWS_ACCESS_KEY_ID="YOUR_ACCESS_KEY_ID"
export AWS_SECRET_ACCESS_KEY="YOUR_SECRET_ACCESS_KEY"
export AWS_SESSION_TOKEN="YOUR_SESSION_TOKEN"  # 如果使用临时凭证
export AWS_DEFAULT_REGION="ap-southeast-2"
```

### 3. 权限要求

确保您的 AWS 凭证具有以下权限：
- `ecr:GetAuthorizationToken`
- `ecr:BatchCheckLayerAvailability`
- `ecr:GetDownloadUrlForLayer`
- `ecr:BatchGetImage`
- `ecr:DescribeRepositories`
- `ecr:CreateRepository`
- `ecr:InitiateLayerUpload`
- `ecr:UploadLayerPart`
- `ecr:CompleteLayerUpload`
- `ecr:PutImage`

## 脚本配置

### 默认配置

脚本默认配置如下：

```bash
AWS_ACCOUNT_ID="528757821800"
AWS_REGION="ap-southeast-2"
AWS_PROFILE="528757821800_DevOpsPermissionSet"
```

### 自定义配置

如需修改配置，编辑脚本顶部的变量：

```bash
# 修改账户 ID
AWS_ACCOUNT_ID="YOUR_ACCOUNT_ID"

# 修改区域
AWS_REGION="YOUR_PREFERRED_REGION"

# 修改 Profile 名称
AWS_PROFILE="YOUR_PROFILE_NAME"
```

## 使用方法

### 1. 基本用法

在项目根目录（包含 `Forum.Api` 文件夹）执行：

```bash
# 给脚本添加执行权限（首次使用）
chmod +x script/container/deploy-to-ecr.sh

# 运行脚本
./script/container/deploy-to-ecr.sh
```

### 2. 脚本执行流程

脚本会按以下顺序执行：

1. **环境检查**
   - 检查 AWS CLI 和 Docker 是否已安装
   - 验证项目目录结构

2. **AWS 认证**
   - 使用配置的 Profile 登录 ECR

3. **仓库管理**
   - 检查 `forum-api` ECR 仓库是否存在
   - 如不存在则自动创建

4. **镜像构建**
   - 构建后端 API Docker 镜像

5. **镜像推送**
   - 推送 `latest` 标签
   - 推送时间戳标签（格式：`YYYYMMDD-HHMMSS`）

6. **结果展示**
   - 显示推送结果摘要
   - 提供 ECR 控制台链接

### 3. 示例输出

```
🚀 Forum 项目 ECR 部署脚本
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
AWS 账户 ID: 528757821800
AWS 区域: ap-southeast-2
AWS Profile: 528757821800_DevOpsPermissionSet
ECR 注册表: 528757821800.dkr.ecr.ap-southeast-2.amazonaws.com

📋 检查必要工具...
✅ 工具检查完成

📁 检查项目目录...
✅ 项目目录检查完成

🔐 登录到 Amazon ECR...
✅ ECR 登录成功

📦 检查 ECR 仓库...
✅ forum-api 仓库已存在

🏗️ 构建后端 API 镜像...
✅ 后端镜像构建成功

🏷️ 标记后端镜像...
📤 推送后端镜像到 ECR...
✅ 后端镜像推送成功

🎉 部署完成！
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📋 推送结果摘要:
后端镜像: 528757821800.dkr.ecr.ap-southeast-2.amazonaws.com/forum-api:latest
版本标签: 20240101-123456

🔗 ECR 仓库链接:
https://ap-southeast-2.console.aws.amazon.com/ecr/repositories?region=ap-southeast-2

📝 下一步:
1. 在 AWS ECS 中创建后端 API 任务定义
2. 配置 ECS 服务和负载均衡器
3. 设置 RDS 和 ElastiCache
4. 配置环境变量和 Secrets Manager
5. 构建并推送前端镜像（如需要）

是否清理本地构建的镜像？(y/N): n
✨ 脚本执行完成！
```

## 故障排除

### 常见问题

#### 1. AWS CLI 未安装
```
❌ AWS CLI 未安装，请先安装 AWS CLI
```
**解决方案：** 安装 AWS CLI
```bash
brew install awscli
```

#### 2. Docker 未安装或未启动
```
❌ Docker 未安装，请先安装 Docker
```
**解决方案：** 安装并启动 Docker Desktop

#### 3. ECR 登录失败
```
❌ ECR 登录失败，请检查 AWS 凭证
```
**解决方案：**
- 检查 AWS 凭证配置是否正确
- 确认凭证未过期（临时凭证通常 1-12 小时过期）
- 验证权限设置

#### 4. 项目目录不存在
```
❌ Forum.Api 目录不存在
```
**解决方案：** 确保在项目根目录执行脚本

#### 5. Docker 构建失败
**解决方案：**
- 检查 `Forum.Api/Dockerfile` 是否存在
- 确认 Docker 服务正在运行
- 查看详细错误信息并修复

### 验证 AWS 凭证

可以手动验证 AWS 凭证：

```bash
# 验证凭证是否有效
aws sts get-caller-identity --profile 528757821800_DevOpsPermissionSet

# 检查 ECR 访问权限
aws ecr describe-repositories --region ap-southeast-2 --profile 528757821800_DevOpsPermissionSet
```

## 扩展使用

### 启用前端镜像构建

如需同时构建前端镜像，取消脚本中相关注释：

1. 取消第 46-49 行的注释（前端目录检查）
2. 取消第 56-59 行的注释（前端 Dockerfile 检查）
3. 取消第 94-105 行的注释（前端 ECR 仓库创建）
4. 取消第 137-162 行的注释（前端镜像构建和推送）

### 自定义镜像标签

修改脚本中的标签逻辑：

```bash
# 当前：使用时间戳
TIMESTAMP=$(date +%Y%m%d-%H%M%S)

# 自定义：使用 Git 提交 SHA
GIT_SHA=$(git rev-parse --short HEAD)
docker tag forum-api:latest $ECR_REGISTRY/forum-api:$GIT_SHA
```

## 安全注意事项

1. **凭证安全**
   - 不要在脚本中硬编码凭证
   - 定期轮换 AWS 访问密钥
   - 使用临时凭证（如 STS）

2. **权限最小化**
   - 只授予必要的 ECR 权限
   - 使用 IAM 角色而非用户凭证（生产环境）

3. **网络安全**
   - 确保 Docker 镜像不包含敏感信息
   - 使用私有 ECR 仓库

## 维护和更新

### 脚本更新

定期检查和更新脚本：
- AWS CLI 版本兼容性
- Docker 最佳实践
- 安全配置更新

### 监控和日志

建议在 CI/CD 环境中：
- 记录脚本执行日志
- 监控镜像推送状态
- 设置失败告警

## 联系支持

如遇到问题，请：
1. 检查本文档的故障排除部分
2. 查看项目的 CLAUDE.md 文档
3. 联系开发团队或提交 Issue

---

**版本：** 1.0  
**最后更新：** 2024年  
**维护者：** Forum 开发团队