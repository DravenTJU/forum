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
    const apiResponse = response.data as ApiResponse<any>;
    
    // 如果响应中包含新的 CSRF Token，更新它
    const newCsrfToken = apiResponse.data?.csrfToken;
    if (newCsrfToken) {
      csrfToken = newCsrfToken;
    }
    
    // 检查 API 响应是否成功
    if (apiResponse.success) {
      // 将 data 字段提升到响应的根级别，保持向后兼容
      return { ...response, data: apiResponse.data };
    } else {
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
    if (error.response?.status === 401) {
      // 401 错误时清除 CSRF Token 并重定向到登录页
      csrfToken = null;
      window.location.href = '/login';
      return Promise.reject(error);
    }
    
    if (error.response?.status === 403) {
      const errorData = error.response.data;
      if (errorData?.error?.code === 'CSRF_TOKEN_INVALID' || errorData?.code === 'CSRF_TOKEN_INVALID') {
        // CSRF Token 无效时重新获取
        csrfToken = null;
        try {
          await getCsrfToken();
          // 重试原始请求
          return api(error.config);
        } catch (csrfError) {
          return Promise.reject(csrfError);
        }
      }
    }
    
    return Promise.reject(error);
  }
);

export const authApi = {
  register: (data: RegisterRequest) => 
    api.post<ApiResponse<AuthResponse>>('/auth/register', data),
    
  login: (data: LoginRequest) => 
    api.post<ApiResponse<AuthResponse>>('/auth/login', data),
    
  logout: () => 
    api.post<ApiResponse<{ message: string }>>('/auth/logout'),
    
  getCurrentUser: () => 
    api.get<ApiResponse<User>>('/auth/me'),
    
  sendEmailVerification: (email: string) => 
    api.post<ApiResponse<{ message: string }>>('/auth/verify-request', { token: email }),
    
  verifyEmail: (data: VerifyEmailRequest) => 
    api.post<ApiResponse<{ message: string }>>('/auth/verify', data),
    
  forgotPassword: (data: ForgotPasswordRequest) => 
    api.post<ApiResponse<{ message: string }>>('/auth/forgot', data),
    
  resetPassword: (data: ResetPasswordRequest) => 
    api.post<ApiResponse<{ message: string }>>('/auth/reset', data),
};

// 导出 CSRF Token 相关功能供其他模块使用
export { getCsrfToken };