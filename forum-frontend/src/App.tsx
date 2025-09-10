import { Routes, Route, Navigate } from 'react-router-dom'
import { Suspense, lazy } from 'react'
import { ProtectedRoute } from '@/components/auth/ProtectedRoute'
import { useAuth } from '@/hooks/useAuth'
import { LoadingSpinner } from '@/components/ui/loading-spinner'

// 懒加载页面组件
const LoginPage = lazy(() => import('@/pages/auth/LoginPage').then(m => ({ default: m.LoginPage })))
const RegisterPage = lazy(() => import('@/pages/auth/RegisterPage').then(m => ({ default: m.RegisterPage })))
const HomePage = lazy(() => import('@/pages/HomePage').then(m => ({ default: m.HomePage })))
const TopicDetailPage = lazy(() => import('@/pages/topics/TopicDetailPage').then(m => ({ default: m.TopicDetailPage })))

// 页面加载器组件
const PageLoader = () => (
  <div className="min-h-screen flex items-center justify-center">
    <LoadingSpinner size="large" />
  </div>
)

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
    <Suspense fallback={<PageLoader />}>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        
        {/* 开发环境：直接访问首页 */}
        {isDev ? (
          <>
            <Route path="/" element={<HomePage />} />
            <Route path="/t/:topicId/:slug?" element={<TopicDetailPage />} />
          </>
        ) : (
          /* 生产环境：受保护的路由 */
          <>
            <Route 
              path="/" 
              element={
                <ProtectedRoute>
                  <HomePage />
                </ProtectedRoute>
              } 
            />
            <Route 
              path="/t/:topicId/:slug?" 
              element={
                <ProtectedRoute>
                  <TopicDetailPage />
                </ProtectedRoute>
              } 
            />
          </>
        )}
        
        {/* 默认重定向到首页 */}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </Suspense>
  )
}

export default App
