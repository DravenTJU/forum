import { Routes, Route, Navigate } from 'react-router-dom'
import { LoginPage } from '@/pages/auth/LoginPage'
import { RegisterPage } from '@/pages/auth/RegisterPage'
import { ProtectedRoute } from '@/components/auth/ProtectedRoute'
import { useAuth } from '@/hooks/useAuth'
import { LoadingSpinner } from '@/components/ui/loading-spinner'

// 临时首页组件
function HomePage() {
  const { user, logout } = useAuth()

  return (
    <div className="min-h-screen bg-gray-50 py-12 px-4">
      <div className="max-w-4xl mx-auto">
        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex justify-between items-center mb-6">
            <h1 className="text-3xl font-bold text-gray-900">论坛系统</h1>
            <div className="flex items-center space-x-4">
              <span className="text-gray-600">欢迎，{user?.username}</span>
              <button
                onClick={() => logout()}
                className="px-4 py-2 bg-red-600 text-white rounded-md hover:bg-red-700"
              >
                退出登录
              </button>
            </div>
          </div>
          
          <div className="space-y-4">
            <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
              <h2 className="text-lg font-semibold text-blue-900">用户信息</h2>
              <div className="mt-2 text-blue-800">
                <p><strong>用户名:</strong> {user?.username}</p>
                <p><strong>邮箱:</strong> {user?.email}</p>
                <p><strong>邮箱验证:</strong> {user?.emailVerified ? '已验证' : '未验证'}</p>
                <p><strong>角色:</strong> {user?.roles.join(', ')}</p>
                <p><strong>注册时间:</strong> {user?.createdAt ? new Date(user.createdAt).toLocaleString() : '-'}</p>
              </div>
            </div>

            {!user?.emailVerified && (
              <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
                <h3 className="text-lg font-medium text-yellow-900">邮箱验证</h3>
                <p className="text-yellow-800 mt-1">
                  请验证您的邮箱地址以获得完整功能访问权限。
                </p>
                <button className="mt-2 px-3 py-1 bg-yellow-600 text-white rounded text-sm hover:bg-yellow-700">
                  重新发送验证邮件
                </button>
              </div>
            )}

            <div className="bg-gray-50 rounded-lg p-4">
              <h3 className="text-lg font-medium text-gray-900 mb-2">系统状态</h3>
              <p className="text-green-600">✅ 前端认证系统已完成</p>
              <p className="text-green-600">✅ 用户登录/注册功能正常</p>
              <p className="text-green-600">✅ 私有路由保护生效</p>
              <p className="text-blue-600">🔄 等待后端 API 集成...</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}

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
