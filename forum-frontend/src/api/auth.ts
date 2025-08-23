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

// CSRF Token 管理
let csrfToken: string | null = null;

const getCsrfToken = async (): Promise<string> => {
  if (!csrfToken) {
    try {
      // 从登录响应或专门的端点获取 CSRF Token
      // 这里假设有一个专门的端点，具体根据后端实现调整
      const response = await axios.get('/api/v1/auth/csrf-token', { 
        withCredentials: true 
      });
      csrfToken = response.data.csrfToken;
    } catch (error) {
      console.error('Failed to get CSRF token:', error);
      throw error;
    }
  }
  return csrfToken;
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

// 响应拦截器 - 处理认证错误
api.interceptors.response.use(
  (response) => {
    // 如果响应中包含新的 CSRF Token，更新它
    const newCsrfToken = response.data?.csrfToken;
    if (newCsrfToken) {
      csrfToken = newCsrfToken;
    }
    return response;
  },
  async (error) => {
    if (error.response?.status === 401) {
      // 401 错误时清除 CSRF Token 并重定向到登录页
      csrfToken = null;
      window.location.href = '/login';
      return Promise.reject(error);
    }
    
    if (error.response?.status === 403 && error.response?.data?.error?.code === 'CSRF_TOKEN_INVALID') {
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
    
    return Promise.reject(error);
  }
);

export const authApi = {
  register: (data: RegisterRequest) => 
    api.post<AuthResponse>('/auth/register', data),
    
  login: (data: LoginRequest) => 
    api.post<AuthResponse>('/auth/login', data),
    
  logout: () => 
    api.post('/auth/logout'),
    
  getCurrentUser: () => 
    api.get<User>('/auth/me'),
    
  sendEmailVerification: (email: string) => 
    api.post('/auth/verify-request', { token: email }),
    
  verifyEmail: (data: VerifyEmailRequest) => 
    api.post('/auth/verify', data),
    
  forgotPassword: (data: ForgotPasswordRequest) => 
    api.post('/auth/forgot', data),
    
  resetPassword: (data: ResetPasswordRequest) => 
    api.post('/auth/reset', data),
};

// 导出 CSRF Token 相关功能供其他模块使用
export { getCsrfToken };