import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { fileURLToPath, URL } from 'node:url'
import { reactClickToComponent } from "vite-plugin-react-click-to-component"
import { visualizer } from 'rollup-plugin-visualizer'

export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
    reactClickToComponent(),
    visualizer({
      filename: 'dist/stats.html',
      open: false,
      gzipSize: true,
      brotliSize: true,
    })
  ],
  resolve: {
    alias: {
      "@": fileURLToPath(new URL("./src", import.meta.url)),
    },
  },
  build: {
    // 启用 CSS 代码分割
    cssCodeSplit: true,
    // 生成 source map 用于调试（生产环境可设为 false）
    sourcemap: false,
    // 压缩配置
    minify: 'esbuild',
    // 移除console和debugger
    terserOptions: {
      compress: {
        drop_console: true,
        drop_debugger: true,
      },
    },
    rollupOptions: {
      // 外部化处理的依赖
      external: [],
      output: {
        // 智能代码分割
        manualChunks: (id) => {
          // node_modules 中的模块
          if (id.includes('node_modules')) {
            // React 核心
            if (id.includes('react') || id.includes('react-dom')) {
              return 'react-vendor'
            }
            
            // React Router
            if (id.includes('react-router-dom')) {
              return 'router'
            }
            
            // Radix UI 组件
            if (id.includes('@radix-ui')) {
              return 'ui-vendor'
            }
            
            // TanStack Query
            if (id.includes('@tanstack/react-query')) {
              return 'query'
            }
            
            // 表单相关
            if (id.includes('react-hook-form') || id.includes('@hookform')) {
              return 'form'
            }
            
            // 工具库
            if (id.includes('clsx') || 
                id.includes('class-variance-authority') || 
                id.includes('tailwind-merge') ||
                id.includes('date-fns') ||
                id.includes('zod')) {
              return 'utils'
            }
            
            // 网络请求
            if (id.includes('axios')) {
              return 'network'
            }
            
            // 图标和主题
            if (id.includes('lucide-react') || 
                id.includes('next-themes') || 
                id.includes('sonner')) {
              return 'icons-theme'
            }
            
            // 其他 node_modules 包
            return 'vendor'
          }
          
          // 项目文件分块
          if (id.includes('/pages/')) {
            return 'pages'
          }
          
          if (id.includes('/components/')) {
            return 'components'
          }
          
          if (id.includes('/hooks/') || id.includes('/api/') || id.includes('/lib/')) {
            return 'shared'
          }
        },
      },
    },
    // 增加代码块大小警告阈值到 600KB
    chunkSizeWarningLimit: 600,
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
