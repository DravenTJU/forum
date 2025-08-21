# 多阶段构建 - 构建阶段
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 复制项目文件并还原依赖
COPY Forum.Api.csproj .
RUN dotnet restore

# 复制源代码并构建
COPY . .

# 加载 .env 文件到环境变量（如果存在）
RUN if [ -f .env.docker ]; then \
        while IFS='=' read -r key value; do \
            if [ ! -z "$key" ] && [ "${key#\#}" = "$key" ]; then \
                export "$key"="$value"; \
            fi; \
        done < .env.docker; \
    fi
RUN dotnet build -c Release -o /app/build

# 发布阶段
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# 运行时阶段
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# 创建非root用户
RUN groupadd -r appuser && useradd -r -g appuser appuser

# 复制发布的文件
COPY --from=publish /app/publish .

# 创建日志目录并设置权限
RUN mkdir -p /app/logs && chown -R appuser:appuser /app

# 切换到非root用户
USER appuser

# 暴露端口
EXPOSE 80

# 设置环境变量
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://+:80

# 健康检查
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost/api/health || exit 1

# 启动应用
ENTRYPOINT ["dotnet", "Forum.Api.dll"]