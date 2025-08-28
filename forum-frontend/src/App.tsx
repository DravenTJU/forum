import { Routes, Route, Navigate } from 'react-router-dom'
import { LoginPage } from '@/pages/auth/LoginPage'
import { RegisterPage } from '@/pages/auth/RegisterPage'
import { HomePage } from '@/pages/HomePage'
import { ProtectedRoute } from '@/components/auth/ProtectedRoute'
import { useAuth } from '@/hooks/useAuth'
import { LoadingSpinner } from '@/components/ui/loading-spinner'

function App() {
  const { isLoadingUser } = useAuth()
  const isDev = import.meta.env.DEV
  
  // 只在需要认证的页面显示加载状态
  const currentPath = window.location.pathname
  const isAuthPage = ['/login', '/register'].includes(currentPath)
  
  if (isLoadingUser && !isAuthPage) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <LoadingSpinner size="large" />
      </div>
    )
  }

  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      
      {/* 开发环境：直接访问首页 */}
      {isDev ? (
        <Route path="/" element={<HomePage />} />
      ) : (
        /* 生产环境：受保护的路由 */
        <Route 
          path="/" 
          element={
            <ProtectedRoute>
              <HomePage />
            </ProtectedRoute>
          } 
        />
      )}
      
      {/* 默认重定向到首页 */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default App
