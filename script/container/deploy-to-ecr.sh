#!/bin/bash

# AWS ECR 部署脚本 - Forum 项目
AWS_ACCOUNT_ID="528757821800"
AWS_REGION="ap-southeast-2"
AWS_PROFILE="528757821800_DevOpsPermissionSet"
ECR_REGISTRY="${AWS_ACCOUNT_ID}.dkr.ecr.${AWS_REGION}.amazonaws.com"

# 颜色输出
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}🚀 Forum 项目 ECR 部署脚本${NC}"
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "AWS 账户 ID: ${YELLOW}$AWS_ACCOUNT_ID${NC}"
echo -e "AWS 区域: ${YELLOW}$AWS_REGION${NC}"
echo -e "AWS Profile: ${YELLOW}$AWS_PROFILE${NC}"
echo -e "ECR 注册表: ${YELLOW}$ECR_REGISTRY${NC}"
echo

# 检查必要工具
echo -e "${BLUE}📋 检查必要工具...${NC}"
if ! command -v aws &> /dev/null; then
    echo -e "${RED}❌ AWS CLI 未安装，请先安装 AWS CLI${NC}"
    exit 1
fi

if ! command -v docker &> /dev/null; then
    echo -e "${RED}❌ Docker 未安装，请先安装 Docker${NC}"
    exit 1
fi

echo -e "${GREEN}✅ 工具检查完成${NC}"
echo

# 检查项目目录
echo -e "${BLUE}📁 检查项目目录...${NC}"
if [ ! -d "Forum.Api" ]; then
    echo -e "${RED}❌ Forum.Api 目录不存在${NC}"
    exit 1
fi

# if [ ! -d "forum-frontend" ]; then
#     echo -e "${RED}❌ forum-frontend 目录不存在${NC}"
#     exit 1
# fi

if [ ! -f "Forum.Api/Dockerfile" ]; then
    echo -e "${RED}❌ Forum.Api/Dockerfile 不存在${NC}"
    exit 1
fi

# if [ ! -f "forum-frontend/Dockerfile" ]; then
#     echo -e "${RED}❌ forum-frontend/Dockerfile 不存在${NC}"
#     exit 1
# fi

echo -e "${GREEN}✅ 项目目录检查完成${NC}"
echo

# ECR 登录
echo -e "${BLUE}🔐 登录到 Amazon ECR...${NC}"
aws ecr get-login-password --region $AWS_REGION --profile $AWS_PROFILE | docker login --username AWS --password-stdin $ECR_REGISTRY

if [ $? -ne 0 ]; then
    echo -e "${RED}❌ ECR 登录失败，请检查 AWS 凭证${NC}"
    exit 1
fi

echo -e "${GREEN}✅ ECR 登录成功${NC}"
echo

# 检查并创建 ECR 仓库
echo -e "${BLUE}📦 检查 ECR 仓库...${NC}"

# 检查 forum-api 仓库
if ! aws ecr describe-repositories --repository-names forum-api --region $AWS_REGION --profile $AWS_PROFILE &> /dev/null; then
    echo -e "${YELLOW}⚠️ forum-api 仓库不存在，正在创建...${NC}"
    aws ecr create-repository --repository-name forum-api --region $AWS_REGION --profile $AWS_PROFILE
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ forum-api 仓库创建成功${NC}"
    else
        echo -e "${RED}❌ forum-api 仓库创建失败${NC}"
        exit 1
    fi
else
    echo -e "${GREEN}✅ forum-api 仓库已存在${NC}"
fi

# 检查 forum-frontend 仓库 (已注释 - 仅构建后端)
# if ! aws ecr describe-repositories --repository-names forum-frontend --region $AWS_REGION --profile $AWS_PROFILE &> /dev/null; then
#     echo -e "${YELLOW}⚠️ forum-frontend 仓库不存在，正在创建...${NC}"
#     aws ecr create-repository --repository-name forum-frontend --region $AWS_REGION --profile $AWS_PROFILE
#     if [ $? -eq 0 ]; then
#         echo -e "${GREEN}✅ forum-frontend 仓库创建成功${NC}"
#     else
#         echo -e "${RED}❌ forum-frontend 仓库创建失败${NC}"
#         exit 1
#     fi
# else
#     echo -e "${GREEN}✅ forum-frontend 仓库已存在${NC}"
# fi

echo

# 构建并推送后端 API 镜像
echo -e "${BLUE}🏗️ 构建后端 API 镜像...${NC}"
docker build -t forum-api ./Forum.Api

if [ $? -ne 0 ]; then
    echo -e "${RED}❌ 后端镜像构建失败${NC}"
    exit 1
fi

echo -e "${GREEN}✅ 后端镜像构建成功${NC}"

echo -e "${BLUE}🏷️ 标记后端镜像...${NC}"
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
docker tag forum-api:latest $ECR_REGISTRY/forum-api:latest
docker tag forum-api:latest $ECR_REGISTRY/forum-api:$TIMESTAMP

echo -e "${BLUE}📤 推送后端镜像到 ECR...${NC}"
docker push $ECR_REGISTRY/forum-api:latest
docker push $ECR_REGISTRY/forum-api:$TIMESTAMP

if [ $? -ne 0 ]; then
    echo -e "${RED}❌ 后端镜像推送失败${NC}"
    exit 1
fi

echo -e "${GREEN}✅ 后端镜像推送成功${NC}"
echo

# 构建并推送前端镜像 (已注释 - 仅构建后端)
# echo -e "${BLUE}🏗️ 构建前端镜像...${NC}"
# docker build -t forum-frontend ./forum-frontend
# 
# if [ $? -ne 0 ]; then
#     echo -e "${RED}❌ 前端镜像构建失败${NC}"
#     exit 1
# fi
# 
# echo -e "${GREEN}✅ 前端镜像构建成功${NC}"
# 
# echo -e "${BLUE}🏷️ 标记前端镜像...${NC}"
# docker tag forum-frontend:latest $ECR_REGISTRY/forum-frontend:latest
# docker tag forum-frontend:latest $ECR_REGISTRY/forum-frontend:$TIMESTAMP
# 
# echo -e "${BLUE}📤 推送前端镜像到 ECR...${NC}"
# docker push $ECR_REGISTRY/forum-frontend:latest
# docker push $ECR_REGISTRY/forum-frontend:$TIMESTAMP
# 
# if [ $? -ne 0 ]; then
#     echo -e "${RED}❌ 前端镜像推送失败${NC}"
#     exit 1
# fi
# 
# echo -e "${GREEN}✅ 前端镜像推送成功${NC}"
# echo

# 显示结果摘要
echo -e "${GREEN}🎉 部署完成！${NC}"
echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${YELLOW}📋 推送结果摘要:${NC}"
echo -e "后端镜像: ${GREEN}$ECR_REGISTRY/forum-api:latest${NC}"
echo -e "版本标签: ${GREEN}$TIMESTAMP${NC}"
echo
echo -e "${YELLOW}🔗 ECR 仓库链接:${NC}"
echo -e "https://${AWS_REGION}.console.aws.amazon.com/ecr/repositories?region=${AWS_REGION}"
echo


# 清理本地镜像（可选）
read -p "是否清理本地构建的镜像？(y/N): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo -e "${BLUE}🧹 清理本地镜像...${NC}"
    docker rmi forum-api:latest 2>/dev/null || true
    echo -e "${GREEN}✅ 本地镜像清理完成${NC}"
fi

echo -e "${GREEN}✨ 脚本执行完成！${NC}"