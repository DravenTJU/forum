import type { ApiError, ErrorCode } from '@/types/api';

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
 * 从ASP.NET Core标准错误格式中提取错误消息
 */
export function extractAspNetCoreErrorMessage(error: any, defaultMessage: string): string {
  const errorData = error.response?.data;
  
  if (!errorData) {
    return defaultMessage;
  }
  
  // 处理ASP.NET Core标准格式: { title, errors: { field: [messages] } }
  if (errorData.errors && typeof errorData.errors === 'object') {
    const fieldErrors = Object.entries(errorData.errors)
      .map(([field, messages]) => {
        const messageArray = Array.isArray(messages) ? messages : [messages];
        return `${field}: ${messageArray.join(', ')}`;
      })
      .join('; ');
    return fieldErrors || errorData.title || defaultMessage;
  }
  
  // 处理简单错误格式
  if (errorData.title) {
    return errorData.title;
  }
  
  return defaultMessage;
}

/**
 * 获取字段级验证错误，用于表单显示
 */
export function getFieldErrors(error: any): Record<string, string[]> {
  if (!error || !error.response) {
    return {};
  }
  
  const errorData = error.response.data as ApiError | undefined;
  return errorData?.details || {};
}

/**
 * 从ASP.NET Core格式中获取字段级验证错误
 */
export function getAspNetCoreFieldErrors(error: any): Record<string, string[]> {
  if (!error || !error.response) {
    return {};
  }
  
  const errorData = error.response.data;
  
  if (!errorData?.errors || typeof errorData.errors !== 'object') {
    return {};
  }
  
  // 转换ASP.NET Core格式到统一格式
  const fieldErrors: Record<string, string[]> = {};
  Object.entries(errorData.errors).forEach(([field, messages]) => {
    fieldErrors[field] = Array.isArray(messages) ? messages : [messages];
  });
  
  return fieldErrors;
}

/**
 * 获取错误码
 */
export function getErrorCode(error: any): ErrorCode | null {
  const errorData = error.response?.data as ApiError | undefined;
  return errorData?.code as ErrorCode | null;
}

/**
 * 检查是否为特定错误码
 */
export function isErrorCode(error: any, code: ErrorCode): boolean {
  return getErrorCode(error) === code;
}

/**
 * 根据业务错误码获取用户友好消息
 */
export function getBusinessErrorMessage(code: ErrorCode): string {
  const errorMessages: Record<ErrorCode, string> = {
    'VALIDATION_FAILED': '输入信息有误，请检查后重试',
    'UNAUTHORIZED': '请先登录后再进行此操作',
    'FORBIDDEN': '您没有权限执行此操作',
    'NOT_FOUND': '请求的资源不存在',
    'CONFLICT': '操作冲突，请刷新页面后重试',
    'TOO_MANY_REQUESTS': '操作过于频繁，请稍后再试',
    'INTERNAL_ERROR': '服务暂时不可用，请稍后重试',
    'CSRF_TOKEN_INVALID': '页面已过期，请刷新后重试',
    'TOPIC_LOCKED': '主题已被锁定，无法进行操作',
    'INSUFFICIENT_PERMISSIONS': '权限不足，无法执行此操作'
  };
  
  return errorMessages[code] || '操作失败，请重试';
}

/**
 * 智能错误处理 - 结合业务错误码和原始消息
 */
export function getSmartErrorMessage(error: any, defaultMessage: string): string {
  const errorCode = getErrorCode(error);
  
  // 如果有业务错误码，优先使用业务友好消息
  if (errorCode) {
    const businessMessage = getBusinessErrorMessage(errorCode);
    
    // 对于验证失败，还是显示具体的字段错误
    if (errorCode === 'VALIDATION_FAILED') {
      const originalMessage = extractErrorMessage(error, defaultMessage);
      return originalMessage !== defaultMessage ? originalMessage : businessMessage;
    }
    
    return businessMessage;
  }
  
  // 尝试ASP.NET Core格式
  const aspNetCoreMessage = extractAspNetCoreErrorMessage(error, '');
  if (aspNetCoreMessage && aspNetCoreMessage !== defaultMessage) {
    return aspNetCoreMessage;
  }
  
  // 否则使用原始消息提取
  return extractErrorMessage(error, defaultMessage);
}

/**
 * 统一获取字段错误 - 支持多种错误格式
 */
export function getUnifiedFieldErrors(error: any): Record<string, string[]> {
  // 先尝试标准API格式
  const standardErrors = getFieldErrors(error);
  if (Object.keys(standardErrors).length > 0) {
    return standardErrors;
  }
  
  // 再尝试ASP.NET Core格式
  return getAspNetCoreFieldErrors(error);
}

/**
 * 检查错误是否需要重新登录
 */
export function shouldRedirectToLogin(error: any): boolean {
  const errorCode = getErrorCode(error);
  return errorCode === 'UNAUTHORIZED' || error.response?.status === 401;
}

/**
 * 检查错误是否为网络错误
 */
export function isNetworkError(error: any): boolean {
  return !error.response && (error.code === 'NETWORK_ERROR' || error.message === 'Network Error');
}