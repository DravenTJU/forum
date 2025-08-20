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

const api = axios.create({
  baseURL: '/api',
  withCredentials: true, // 包含 Cookie
});

// 请求拦截器 - 添加 Access Token
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// 响应拦截器 - 自动刷新 Token
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;
    
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;
      
      try {
        const response = await api.post<AuthResponse>('/auth/refresh');
        const { accessToken } = response.data;
        
        localStorage.setItem('accessToken', accessToken);
        originalRequest.headers.Authorization = `Bearer ${accessToken}`;
        
        return api(originalRequest);
      } catch (refreshError) {
        // 刷新失败，清除本地状态
        localStorage.removeItem('accessToken');
        window.location.href = '/login';
        return Promise.reject(refreshError);
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
    
  refreshToken: () => 
    api.post<AuthResponse>('/auth/refresh'),
    
  sendEmailVerification: (email: string) => 
    api.post('/auth/verify-request', { token: email }),
    
  verifyEmail: (data: VerifyEmailRequest) => 
    api.post('/auth/verify', data),
    
  forgotPassword: (data: ForgotPasswordRequest) => 
    api.post('/auth/forgot', data),
    
  resetPassword: (data: ResetPasswordRequest) => 
    api.post('/auth/reset', data),
};