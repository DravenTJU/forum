import React from 'react';
import { useMutation, useQuery, UseQueryOptions, UseMutationOptions } from '@tanstack/react-query';
import { toast } from 'sonner';
import { getSmartErrorMessage, isNetworkError, shouldRedirectToLogin } from '@/lib/error-utils';

// API 调用的通用选项
interface ApiCallOptions {
  showSuccessToast?: boolean;
  successMessage?: string;
  showErrorToast?: boolean;
  errorMessage?: string;
  onError?: (error: any) => void;
  onSuccess?: (data: any) => void;
}

/**
 * 通用 API 查询 Hook
 */
export function useApiQuery<TData = any>(
  queryKey: string[],
  queryFn: () => Promise<TData>,
  options?: Omit<UseQueryOptions<TData>, 'queryKey' | 'queryFn'> & ApiCallOptions
) {
  const { showErrorToast = true, errorMessage = '获取数据失败', onError, onSuccess, ...queryOptions } = options || {};

  const handleError = (error: any) => {
    // 网络错误处理
    if (isNetworkError(error)) {
      if (showErrorToast) {
        toast.error('网络连接异常，请检查网络后重试');
      }
      return;
    }

    // 需要重新登录
    if (shouldRedirectToLogin(error)) {
      if (showErrorToast) {
        toast.error('登录已过期，请重新登录');
      }
      // 延迟跳转，让用户看到提示
      setTimeout(() => {
        window.location.href = '/login';
      }, 1500);
      return;
    }

    // 显示错误提示
    if (showErrorToast) {
      const message = getSmartErrorMessage(error, errorMessage);
      toast.error(message);
    }

    // 执行自定义错误处理
    if (onError) {
      onError(error);
    }
  };

  const result = useQuery({
    queryKey,
    queryFn,
    ...queryOptions,
  });

  // 处理错误
  React.useEffect(() => {
    if (result.error) {
      handleError(result.error);
    }
  }, [result.error]);

  // 处理成功
  React.useEffect(() => {
    if (result.data && onSuccess) {
      onSuccess(result.data);
    }
  }, [result.data, onSuccess]);

  return result;
}

/**
 * 通用 API 变更 Hook
 */
export function useApiMutation<TData = any, TVariables = any>(
  mutationFn: (variables: TVariables) => Promise<TData>,
  options?: Omit<UseMutationOptions<TData, any, TVariables>, 'mutationFn'> & ApiCallOptions
) {
  const {
    showSuccessToast = false,
    successMessage = '操作成功',
    showErrorToast = true,
    errorMessage = '操作失败',
    onError,
    onSuccess,
    ...mutationOptions
  } = options || {};

  const handleSuccess = React.useCallback((data: TData) => {
    // 显示成功提示
    if (showSuccessToast) {
      toast.success(successMessage);
    }

    // 执行自定义成功处理
    if (onSuccess) {
      onSuccess(data);
    }
  }, [showSuccessToast, successMessage, onSuccess]);

  const handleError = React.useCallback((error: any) => {
    // 网络错误处理
    if (isNetworkError(error)) {
      if (showErrorToast) {
        toast.error('网络连接异常，请检查网络后重试');
      }
      return;
    }

    // 需要重新登录
    if (shouldRedirectToLogin(error)) {
      if (showErrorToast) {
        toast.error('登录已过期，请重新登录');
      }
      // 延迟跳转，让用户看到提示
      setTimeout(() => {
        window.location.href = '/login';
      }, 1500);
      return;
    }

    // 显示错误提示
    if (showErrorToast) {
      const message = getSmartErrorMessage(error, errorMessage);
      toast.error(message);
    }

    // 执行自定义错误处理
    if (onError) {
      onError(error);
    }
  }, [showErrorToast, errorMessage, onError]);

  const mutation = useMutation({
    mutationFn,
    ...mutationOptions,
  });

  // 处理成功和错误
  React.useEffect(() => {
    if (mutation.isSuccess && mutation.data) {
      handleSuccess(mutation.data);
    }
  }, [mutation.isSuccess, mutation.data, handleSuccess]);

  React.useEffect(() => {
    if (mutation.error) {
      handleError(mutation.error);
    }
  }, [mutation.error, handleError]);

  return mutation;
}

/**
 * 创建优化的 API 调用 Hook
 */
export function createApiHook<TData = any, TVariables = any>(
  mutationFn: (variables: TVariables) => Promise<TData>,
  defaultOptions?: ApiCallOptions
) {
  return (options?: UseMutationOptions<TData, any, TVariables> & ApiCallOptions) => {
    const mergedOptions = { ...defaultOptions, ...options };
    return useApiMutation(mutationFn, mergedOptions);
  };
}

/**
 * 分页查询 Hook
 */
export function usePaginatedQuery<TData = any>(
  queryKey: string[],
  queryFn: (cursor?: string, limit?: number) => Promise<{ data: TData[]; meta: { hasNext?: boolean; nextCursor?: string } }>,
  options?: ApiCallOptions & {
    limit?: number;
  }
) {
  const { limit = 20, ...restOptions } = options || {};

  return useApiQuery(
    queryKey,
    () => queryFn(undefined, limit),
    {
      ...restOptions,
      // 设置合理的缓存时间
      staleTime: 5 * 60 * 1000, // 5分钟
      // 在 React Query v5 中使用 placeholderData 替代 keepPreviousData
      placeholderData: (previousData) => previousData,
    }
  );
}

/**
 * 无限滚动查询 Hook
 */
export function useInfiniteApiQuery<TData = any>(
  queryKey: string[],
  queryFn: (cursor?: string, limit?: number) => Promise<{ data: TData[]; meta: { hasNext?: boolean; nextCursor?: string } }>,
  options?: ApiCallOptions & {
    limit?: number;
  }
) {
  const { limit = 20, showErrorToast = true, errorMessage = '获取数据失败', onError } = options || {};

  return useApiQuery(
    [...queryKey, 'infinite'],
    () => queryFn(undefined, limit),
    {
      showErrorToast,
      errorMessage,
      onError,
    }
  );
}