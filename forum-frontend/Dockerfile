# 多阶段构建 - 构建阶段
FROM node:20-alpine AS build
WORKDIR /app

# 复制 package.json 和 yarn.lock（如果存在）
COPY package*.json ./
COPY yarn.lock* ./

# 安装依赖
RUN if [ -f yarn.lock ]; then yarn install --frozen-lockfile; else npm ci; fi

# 复制源代码
COPY . .

# 构建应用
RUN if [ -f yarn.lock ]; then yarn build; else npm run build; fi

# 生产阶段 - 使用 Nginx 提供静态文件
FROM nginx:alpine AS runtime

# 安装 curl 用于健康检查
RUN apk add --no-cache curl

# 复制构建结果
COPY --from=build /app/dist /usr/share/nginx/html

# 复制自定义 Nginx 配置
COPY nginx.conf /etc/nginx/nginx.conf

# 创建非 root 用户
RUN adduser -D -S -G www-data appuser

# 设置权限
RUN chown -R appuser:www-data /usr/share/nginx/html \
    && chown -R appuser:www-data /var/cache/nginx \
    && chown -R appuser:www-data /var/log/nginx \
    && chown -R appuser:www-data /etc/nginx/conf.d \
    && touch /var/run/nginx.pid \
    && chown appuser:www-data /var/run/nginx.pid

# 切换到非 root 用户
USER appuser

# 暴露端口
EXPOSE 80

# 健康检查
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost/ || exit 1

# 启动 Nginx
CMD ["nginx", "-g", "daemon off;"]