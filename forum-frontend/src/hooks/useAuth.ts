import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { authApi } from '@/api/auth';
import { toast } from 'sonner';

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
      localStorage.setItem('accessToken', response.data.accessToken);
      queryClient.setQueryData(['auth', 'user'], response.data.user);
      toast.success('登录成功');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || '登录失败');
    },
  });

  // 注册
  const registerMutation = useMutation({
    mutationFn: authApi.register,
    onSuccess: (response) => {
      localStorage.setItem('accessToken', response.data.accessToken);
      queryClient.setQueryData(['auth', 'user'], response.data.user);
      toast.success('注册成功');
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || '注册失败');
    },
  });

  // 退出登录
  const logoutMutation = useMutation({
    mutationFn: authApi.logout,
    onSuccess: () => {
      localStorage.removeItem('accessToken');
      queryClient.setQueryData(['auth', 'user'], null);
      queryClient.clear();
      toast.success('已退出登录');
    },
    onError: () => {
      // 即使请求失败也清除本地状态
      localStorage.removeItem('accessToken');
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
      toast.error(error.response?.data?.message || '发送失败');
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