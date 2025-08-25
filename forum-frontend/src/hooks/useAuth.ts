import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { authApi } from '@/api/auth';
import { notify, Messages } from '@/lib/notification';
import { getSmartErrorMessage } from '@/lib/error-utils';

export function useAuth() {
  const queryClient = useQueryClient();

  // 获取当前用户 - 仅在可能有有效session时查询
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
    staleTime: 5 * 60 * 1000, // 5分钟
    retry: false, // 不重试，避免重复401请求
    refetchOnWindowFocus: false, // 防止页面焦点变化时重新获取
  });

  // 登录
  const loginMutation = useMutation({
    mutationFn: authApi.login,
    onSuccess: (response) => {
      // Cookie-based 认证，不需要手动存储 token
      // 响应拦截器已经解包了 ApiResponse，所以 response.data 就是 AuthResponse
      queryClient.setQueryData(['auth', 'user'], response.data.user);
      notify.success(Messages.LOGIN_SUCCESS);
    },
    onError: (error: any) => {
      notify.error(getSmartErrorMessage(error, Messages.LOGIN_FAILED));
    },
  });

  // 注册
  const registerMutation = useMutation({
    mutationFn: authApi.register,
    onSuccess: (response) => {
      // Cookie-based 认证，不需要手动存储 token
      queryClient.setQueryData(['auth', 'user'], response.data.user);
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