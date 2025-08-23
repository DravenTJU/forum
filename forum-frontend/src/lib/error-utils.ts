import type { ApiError } from '@/types/api';

/**
 * 从错误响应中提取用户友好的错误消息
 */
export function extractErrorMessage(error: any, defaultMessage: string): string {
  const errorData = error.response?.data as ApiError | undefined;
  
  if (!errorData) {
    return defaultMessage;
  }
  
  // 优先返回主错误消息
  if (errorData.message) {
    return errorData.message;
  }
  
  // 处理字段级验证错误
  if (errorData.details) {
    const fieldErrors = Object.entries(errorData.details)
      .map(([field, messages]) => `${field}: ${messages.join(', ')}`)
      .join('; ');
    return fieldErrors || defaultMessage;
  }
  
  return defaultMessage;
}

/**
 * 获取字段级验证错误，用于表单显示
 */
export function getFieldErrors(error: any): Record<string, string[]> {
  const errorData = error.response?.data as ApiError | undefined;
  return errorData?.details || {};
}