import { useState, useCallback } from 'react';
import { getFieldErrors, getSmartErrorMessage } from '@/lib/error-utils';

interface FormErrorState {
  message?: string;
  fieldErrors?: Record<string, string[]>;
}

/**
 * 表单错误处理 Hook
 * 提供统一的表单错误状态管理和处理
 */
export function useFormError() {
  const [errorState, setErrorState] = useState<FormErrorState>({});

  // 清除所有错误
  const clearErrors = useCallback(() => {
    setErrorState({});
  }, []);

  // 设置通用错误消息
  const setError = useCallback((message: string) => {
    setErrorState({ message, fieldErrors: {} });
  }, []);

  // 从 API 错误响应中设置错误
  const setErrorFromResponse = useCallback((error: any, defaultMessage: string = '操作失败') => {
    const fieldErrors = getFieldErrors(error);
    const message = getSmartErrorMessage(error, defaultMessage);

    setErrorState({
      message: Object.keys(fieldErrors).length === 0 ? message : undefined,
      fieldErrors: Object.keys(fieldErrors).length > 0 ? fieldErrors : undefined,
    });
  }, []);

  // 设置字段级错误
  const setFieldError = useCallback((field: string, messages: string[]) => {
    setErrorState(prev => ({
      ...prev,
      fieldErrors: {
        ...prev.fieldErrors,
        [field]: messages,
      },
    }));
  }, []);

  // 清除特定字段的错误
  const clearFieldError = useCallback((field: string) => {
    setErrorState(prev => {
      if (!prev.fieldErrors) return prev;
      
      const { [field]: _, ...remainingErrors } = prev.fieldErrors;
      return {
        ...prev,
        fieldErrors: Object.keys(remainingErrors).length > 0 ? remainingErrors : undefined,
      };
    });
  }, []);

  // 获取特定字段的错误
  const getFieldError = useCallback((field: string): string[] => {
    return errorState.fieldErrors?.[field] || [];
  }, [errorState.fieldErrors]);

  // 检查是否有错误
  const hasError = !!(errorState.message || (errorState.fieldErrors && Object.keys(errorState.fieldErrors).length > 0));

  // 检查特定字段是否有错误
  const hasFieldError = useCallback((field: string): boolean => {
    return !!(errorState.fieldErrors?.[field]?.length);
  }, [errorState.fieldErrors]);

  return {
    // 状态
    errorState,
    hasError,
    
    // 方法
    clearErrors,
    setError,
    setErrorFromResponse,
    setFieldError,
    clearFieldError,
    getFieldError,
    hasFieldError,
    
    // 便捷属性
    message: errorState.message,
    fieldErrors: errorState.fieldErrors,
  };
}

/**
 * 创建表单字段错误处理器
 * 与 react-hook-form 集成使用
 */
export function createFieldErrorHandler<T extends Record<string, any>>(
  setError: (name: keyof T, error: { message: string }) => void,
  clearErrors: () => void
) {
  return {
    // 从 API 响应设置字段错误
    setFieldErrorsFromResponse: (error: any) => {
      const fieldErrors = getFieldErrors(error);
      
      // 清除现有错误
      clearErrors();
      
      // 设置新的字段错误
      Object.entries(fieldErrors).forEach(([field, messages]) => {
        if (field in ({} as T)) {
          setError(field as keyof T, { message: messages.join(', ') });
        }
      });
    },
    
    // 清除特定字段错误
    clearFieldError: (field: keyof T) => {
      setError(field, { message: '' });
    },
  };
}