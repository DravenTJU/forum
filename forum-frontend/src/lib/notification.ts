import { toast } from 'sonner';
import type { ErrorCode } from '@/types/api';
import { getBusinessErrorMessage } from './error-utils';

/**
 * 通知类型
 */
export type NotificationType = 'success' | 'error' | 'warning' | 'info';

/**
 * 通知选项
 */
interface NotificationOptions {
  duration?: number;
  action?: {
    label: string;
    onClick: () => void;
  };
  description?: string;
}

/**
 * 通知系统
 * 基于 sonner 的增强通知系统
 */
export class NotificationSystem {
  /**
   * 显示成功消息
   */
  static success(message: string, options?: NotificationOptions) {
    return toast.success(message, {
      duration: options?.duration || 4000,
      description: options?.description,
      action: options?.action,
    });
  }

  /**
   * 显示错误消息
   */
  static error(message: string, options?: NotificationOptions) {
    return toast.error(message, {
      duration: options?.duration || 6000, // 错误消息显示更长时间
      description: options?.description,
      action: options?.action,
    });
  }

  /**
   * 显示警告消息
   */
  static warning(message: string, options?: NotificationOptions) {
    return toast.warning(message, {
      duration: options?.duration || 5000,
      description: options?.description,
      action: options?.action,
    });
  }

  /**
   * 显示信息消息
   */
  static info(message: string, options?: NotificationOptions) {
    return toast.info(message, {
      duration: options?.duration || 4000,
      description: options?.description,
      action: options?.action,
    });
  }

  /**
   * 显示加载消息
   */
  static loading(message: string, promise?: Promise<any>) {
    if (promise) {
      return toast.promise(promise, {
        loading: message,
        success: '操作成功',
        error: '操作失败',
      });
    }
    
    return toast.loading(message);
  }

  /**
   * 显示业务错误码对应的消息
   */
  static businessError(code: ErrorCode, customMessage?: string) {
    const message = customMessage || getBusinessErrorMessage(code);
    const duration = ['UNAUTHORIZED', 'FORBIDDEN'].includes(code) ? 8000 : 6000;
    return this.error(message, {
      duration,
    });
  }

  /**
   * 显示网络错误
   */
  static networkError() {
    return this.error('网络连接异常，请检查网络后重试', {
      duration: 8000,
      action: {
        label: '重试',
        onClick: () => window.location.reload(),
      },
    });
  }

  /**
   * 显示操作确认
   */
  static confirm(message: string, onConfirm: () => void, onCancel?: () => void) {
    return toast(message, {
      duration: Infinity,
      action: {
        label: '确认',
        onClick: () => onConfirm(),
      },
      cancel: onCancel ? {
        label: '取消',
        onClick: () => onCancel(),
      } : undefined,
    });
  }

  /**
   * 关闭所有通知
   */
  static dismiss() {
    toast.dismiss();
  }

  /**
   * 关闭特定通知
   */
  static dismissById(id: string | number) {
    toast.dismiss(id);
  }
}

/**
 * 快捷通知方法
 */
export const notify = {
  success: NotificationSystem.success,
  error: NotificationSystem.error,
  warning: NotificationSystem.warning,
  info: NotificationSystem.info,
  loading: NotificationSystem.loading,
  businessError: NotificationSystem.businessError,
  networkError: NotificationSystem.networkError,
  confirm: NotificationSystem.confirm,
  dismiss: NotificationSystem.dismiss,
};

/**
 * 常用通知消息
 */
export const Messages = {
  // 通用操作
  SAVE_SUCCESS: '保存成功',
  SAVE_FAILED: '保存失败',
  DELETE_SUCCESS: '删除成功', 
  DELETE_FAILED: '删除失败',
  UPDATE_SUCCESS: '更新成功',
  UPDATE_FAILED: '更新失败',
  COPY_SUCCESS: '复制成功',
  
  // 认证相关
  LOGIN_SUCCESS: '登录成功',
  LOGIN_FAILED: '登录失败',
  LOGOUT_SUCCESS: '已退出登录',
  REGISTER_SUCCESS: '注册成功',
  REGISTER_FAILED: '注册失败',
  PASSWORD_RESET_SENT: '密码重置邮件已发送',
  EMAIL_VERIFICATION_SENT: '验证邮件已发送',
  
  // 网络和系统
  NETWORK_ERROR: '网络连接异常',
  SERVER_ERROR: '服务器错误，请稍后重试',
  UNAUTHORIZED: '登录已过期，请重新登录',
  FORBIDDEN: '权限不足',
  NOT_FOUND: '请求的资源不存在',
  
  // 表单验证
  REQUIRED_FIELD: '此字段为必填项',
  INVALID_EMAIL: '请输入有效的邮箱地址',
  PASSWORD_TOO_SHORT: '密码长度不能少于8位',
  PASSWORDS_NOT_MATCH: '两次输入的密码不一致',
  
  // 文件操作
  FILE_UPLOAD_SUCCESS: '文件上传成功',
  FILE_UPLOAD_FAILED: '文件上传失败',
  FILE_TOO_LARGE: '文件大小超出限制',
  INVALID_FILE_TYPE: '不支持的文件类型',
} as const;

/**
 * 操作结果通知
 */
export function notifyResult<T>(
  operation: string,
  result: { success: boolean; data?: T; error?: any }
) {
  if (result.success) {
    notify.success(`${operation}成功`);
  } else {
    notify.error(`${operation}失败: ${result.error?.message || '未知错误'}`);
  }
  
  return result;
}