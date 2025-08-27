import axios from 'axios';
import type { 
  AuthResponse, 
  LoginRequest, 
  RegisterRequest, 
  User, 
  ForgotPasswordRequest,
  ResetPasswordRequest,
  VerifyEmailRequest 
} from '@/types/auth';
import type { ApiResponse } from '@/types/api';

// CSRF Token 管理
let csrfToken: string | null = null;

const getCsrfToken = async (): Promise<string> => {
  if (!csrfToken) {
    try {
      // 从登录响应或专门的端点获取 CSRF Token
      // 这里假设有一个专门的端点，具体根据后端实现调整
      const response = await axios.get<ApiResponse<{ csrfToken: string }>>('/api/v1/auth/csrf-token', { 
        withCredentials: true 
      });
      
      if (response.data.success && response.data.data) {
        csrfToken = response.data.data.csrfToken;
      } else {
        throw new Error(response.data.error?.message || 'Failed to get CSRF token');
      }
    } catch (error) {
      console.error('Failed to get CSRF token:', error);
      throw error;
    }
  }
  return csrfToken!; // 使用非空断言，因为我们已经确保它不为 null
};

const api = axios.create({
  baseURL: '/api/v1',
  withCredentials: true, // 包含 Cookie
});

// 请求拦截器 - 添加 CSRF Token
api.interceptors.request.use(async (config) => {
  // 对非 GET 请求添加 CSRF Token
  if (config.method && !['get', 'head', 'options'].includes(config.method.toLowerCase())) {
    try {
      const token = await getCsrfToken();
      config.headers['X-CSRF-Token'] = token;
    } catch (error) {
      console.error('Failed to add CSRF token:', error);
      // 继续请求，让服务端处理缺少 CSRF token 的情况
    }
  }
  return config;
});

// 响应拦截器 - 处理 ApiResponse 格式和认证错误
api.interceptors.response.use(
  (response) => {
    console.log('📦 API 响应拦截器 - 成功:', {
      url: response.config.url,
      method: response.config.method,
      status: response.status,
      statusText: response.statusText,
      dataType: typeof response.data
    });

    const apiResponse = response.data as ApiResponse<any>;
    
    // 如果响应中包含新的 CSRF Token，更新它
    const newCsrfToken = apiResponse.data?.csrfToken;
    if (newCsrfToken) {
      csrfToken = newCsrfToken;
      console.log('🔑 CSRF Token 已更新');
    }
    
    // 检查 API 响应是否成功
    if (apiResponse.success) {
      console.log('✅ API 响应成功:', {
        url: response.config.url,
        dataLength: apiResponse.data ? (Array.isArray(apiResponse.data) ? apiResponse.data.length : 'object') : 'null'
      });
      // 保持完整的 ApiResponse 结构，不要提升 data 字段
      return response;
    } else {
      console.error('❌ API 业务逻辑失败:', {
        url: response.config.url,
        method: response.config.method,
        success: apiResponse.success,
        error: apiResponse.error
      });
      // API 级别的错误，抛出包含错误信息的异常
      const error = new Error(apiResponse.error?.message || 'API Error');
      (error as any).response = {
        ...response,
        data: apiResponse.error
      };
      throw error;
    }
  },
  async (error) => {
    console.error('💥 API 响应拦截器 - 错误:', {
      url: error.config?.url,
      method: error.config?.method,
      status: error.response?.status,
      statusText: error.response?.statusText,
      message: error.message,
      response: error.response?.data,
      isNetworkError: !error.response,
      timestamp: new Date().toISOString()
    });

    if (error.response?.status === 401) {
      console.warn('🔐 401 未授权，清除 CSRF Token');
      // 401 错误时清除 CSRF Token
      csrfToken = null;
      
      // 只有在不在登录/注册页面时才重定向，防止无限循环
      const currentPath = window.location.pathname;
      if (currentPath !== '/login' && currentPath !== '/register') {
        console.log('🔀 重定向到登录页面');
        window.location.href = '/login';
      }
      
      return Promise.reject(error);
    }
    
    if (error.response?.status === 403) {
      const errorData = error.response.data;
      if (errorData?.error?.code === 'CSRF_TOKEN_INVALID' || errorData?.code === 'CSRF_TOKEN_INVALID') {
        console.warn('🔑 CSRF Token 无效，尝试重新获取');
        // CSRF Token 无效时重新获取
        csrfToken = null;
        try {
          await getCsrfToken();
          console.log('🔄 重试原始请求');
          // 重试原始请求
          return api(error.config);
        } catch (csrfError) {
          console.error('❌ CSRF Token 重新获取失败:', csrfError);
          return Promise.reject(csrfError);
        }
      }
    }

    // 网络错误特殊处理
    if (!error.response) {
      console.error('🌐 网络错误或服务器无响应:', {
        message: error.message,
        code: error.code,
        config: {
          url: error.config?.url,
          method: error.config?.method,
          baseURL: error.config?.baseURL
        }
      });
    }
    
    return Promise.reject(error);
  }
);

// 由于响应拦截器会将 ApiResponse<T> 转换为 T，所以这里的返回类型应该是解包后的类型
export const authApi = {
  register: (data: RegisterRequest) => 
    api.post<AuthResponse>('/auth/register', data),
    
  login: (data: LoginRequest) => 
    api.post<AuthResponse>('/auth/login', data),
    
  logout: () => 
    api.post<{ message: string }>('/auth/logout'),
    
  getCurrentUser: () => 
    api.get<User>('/auth/me'),
    
  sendEmailVerification: (email: string) => 
    api.post<{ message: string }>('/auth/verify-request', { token: email }),
    
  verifyEmail: (data: VerifyEmailRequest) => 
    api.post<{ message: string }>('/auth/verify', data),
    
  forgotPassword: (data: ForgotPasswordRequest) => 
    api.post<{ message: string }>('/auth/forgot', data),
    
  resetPassword: (data: ResetPasswordRequest) => 
    api.post<{ message: string }>('/auth/reset', data),
};

// 导出 API 实例和 CSRF Token 相关功能供其他模块使用
export { api, getCsrfToken };