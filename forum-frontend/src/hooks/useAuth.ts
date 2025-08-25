import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { authApi } from '@/api/auth';
import { notify, Messages } from '@/lib/notification';
import { getSmartErrorMessage } from '@/lib/error-utils';

export function useAuth() {
  const queryClient = useQueryClient();

  // 检查当前路径是否需要认证状态
  const shouldFetchUser = () => {
    const currentPath = window.location.pathname;
    // 在登录和注册页面不需要获取用户信息
    return !['/login', '/register'].includes(currentPath);
  };

  // 获取当前用户 - 仅在非登录页面查询
  const { data: user, isLoading: isLoadingUser } = useQuery({
    queryKey: ['auth', 'user'],
    queryFn: async () => {
      try {
        const response = await authApi.getCurrentUser();
        return response.data;
      } catch (error) {
        // 401 错误不需要额外处理，响应拦截器已经处理
        return null;
      }
    },
    enabled: shouldFetchUser(), // 只在需要时查询
    staleTime: 5 * 60 * 1000, // 5分钟
    retry: false, // 不重试，避免重复401请求
    refetchOnWindowFocus: false, // 防止页面焦点变化时重新获取
  });

  // 登录
  const loginMutation = useMutation({
    mutationFn: authApi.login,
    onSuccess: () => {
      // JWT Token存储在HttpOnly Cookie中，符合API规范
      // 登录成功后刷新用户信息
      queryClient.invalidateQueries({ queryKey: ['auth', 'user'] });
      notify.success(Messages.LOGIN_SUCCESS);
    },
    onError: (error: any) => {
      notify.error(getSmartErrorMessage(error, Messages.LOGIN_FAILED));
    },
  });

  // 注册
  const registerMutation = useMutation({
    mutationFn: authApi.register,
    onSuccess: () => {
      // 注册成功，等待用户验证邮箱后登录
      notify.success(Messages.REGISTER_SUCCESS);
    },
    onError: (error: any) => {
      notify.error(getSmartErrorMessage(error, Messages.REGISTER_FAILED));
    },
  });

  // 退出登录
  const logoutMutation = useMutation({
    mutationFn: authApi.logout,
    onSuccess: () => {
      // Cookie-based 认证，服务端会清除 Cookie
      queryClient.setQueryData(['auth', 'user'], null);
      queryClient.clear();
      notify.success(Messages.LOGOUT_SUCCESS);
    },
    onError: () => {
      // 即使请求失败也清除本地状态
      queryClient.setQueryData(['auth', 'user'], null);
      queryClient.clear();
    },
  });

  // 发送邮箱验证
  const sendVerificationMutation = useMutation({
    mutationFn: (email: string) => authApi.sendEmailVerification(email),
    onSuccess: () => {
      notify.success(Messages.EMAIL_VERIFICATION_SENT);
    },
    onError: (error: any) => {
      notify.error(getSmartErrorMessage(error, '发送失败'));
    },
  });

  return {
    user,
    isAuthenticated: !!user,
    isLoadingUser,
    login: loginMutation.mutate,
    register: registerMutation.mutate,
    logout: logoutMutation.mutate,
    sendVerification: sendVerificationMutation.mutate,
    isLoggingIn: loginMutation.isPending,
    isRegistering: registerMutation.isPending,
  };
}