import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { authApi } from '@/api/auth';
import { toast } from 'sonner';
import { extractErrorMessage } from '@/lib/error-utils';

export function useAuth() {
  const queryClient = useQueryClient();

  // 获取当前用户
  const { data: user, isLoading: isLoadingUser } = useQuery({
    queryKey: ['auth', 'user'],
    queryFn: async () => {
      try {
        const response = await authApi.getCurrentUser();
        return response.data;
      } catch (error) {
        return null;
      }
    },
    staleTime: 5 * 60 * 1000, // 5分钟
  });

  // 登录
  const loginMutation = useMutation({
    mutationFn: authApi.login,
    onSuccess: (response) => {
      // Cookie-based 认证，不需要手动存储 token
      queryClient.setQueryData(['auth', 'user'], response.data.user);
      toast.success('登录成功');
    },
    onError: (error: any) => {
      toast.error(extractErrorMessage(error, '登录失败'));
    },
  });

  // 注册
  const registerMutation = useMutation({
    mutationFn: authApi.register,
    onSuccess: (response) => {
      // Cookie-based 认证，不需要手动存储 token
      queryClient.setQueryData(['auth', 'user'], response.data.user);
      toast.success('注册成功');
    },
    onError: (error: any) => {
      toast.error(extractErrorMessage(error, '注册失败'));
    },
  });

  // 退出登录
  const logoutMutation = useMutation({
    mutationFn: authApi.logout,
    onSuccess: () => {
      // Cookie-based 认证，服务端会清除 Cookie
      queryClient.setQueryData(['auth', 'user'], null);
      queryClient.clear();
      toast.success('已退出登录');
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
      toast.success('验证邮件已发送');
    },
    onError: (error: any) => {
      toast.error(extractErrorMessage(error, '发送失败'));
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