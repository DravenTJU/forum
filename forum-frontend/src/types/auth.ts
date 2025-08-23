export interface User {
  id: string; // 修正为 string 类型
  username: string;
  email: string;
  emailVerified: boolean;
  avatarUrl?: string;
  bio?: string;
  status: 'active' | 'suspended'; // 新增状态字段
  roles: Array<'user' | 'mod' | 'admin'>; // 限定角色类型
  lastSeenAt?: string; // 新增最后在线时间
  createdAt: string;
}

export interface UserProfile extends User {
  topicCount: number;
  postCount: number;
}

export interface AuthResponse {
  user: User;
  csrfToken: string; // 根据 API 规范，登录响应包含 CSRF Token
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
}

export interface VerifyEmailRequest {
  token: string;
}

// ApiError 已在 api.ts 中定义，这里移除重复定义