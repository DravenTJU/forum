import { toast } from 'sonner';
import type { ErrorCode } from '@/types/api';
import { getBusinessErrorMessage } from './error-utils';

/**
 * Notification type
 */
export type NotificationType = 'success' | 'error' | 'warning' | 'info';

/**
 * Notification options
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
 * Notification system
 * Enhanced notification system based on sonner
 */
export class NotificationSystem {
  /**
   * Show success message
   */
  static success(message: string, options?: NotificationOptions) {
    return toast.success(message, {
      duration: options?.duration || 4000,
      description: options?.description,
      action: options?.action,
    });
  }

  /**
   * Show error message
   */
  static error(message: string, options?: NotificationOptions) {
    return toast.error(message, {
      duration: options?.duration || 6000, // Error messages display longer
      description: options?.description,
      action: options?.action,
    });
  }

  /**
   * Show warning message
   */
  static warning(message: string, options?: NotificationOptions) {
    return toast.warning(message, {
      duration: options?.duration || 5000,
      description: options?.description,
      action: options?.action,
    });
  }

  /**
   * Show info message
   */
  static info(message: string, options?: NotificationOptions) {
    return toast.info(message, {
      duration: options?.duration || 4000,
      description: options?.description,
      action: options?.action,
    });
  }

  /**
   * Show loading message
   */
  static loading(message: string, promise?: Promise<any>) {
    if (promise) {
      return toast.promise(promise, {
        loading: message,
        success: 'Operation successful',
        error: 'Operation failed',
      });
    }
    
    return toast.loading(message);
  }

  /**
   * Show message corresponding to business error code
   */
  static businessError(code: ErrorCode, customMessage?: string) {
    const message = customMessage || getBusinessErrorMessage(code);
    const duration = ['UNAUTHORIZED', 'FORBIDDEN'].includes(code) ? 8000 : 6000;
    return this.error(message, {
      duration,
    });
  }

  /**
   * Show network error
   */
  static networkError() {
    return this.error('Network connection error, please check your connection and try again', {
      duration: 8000,
      action: {
        label: 'Retry',
        onClick: () => window.location.reload(),
      },
    });
  }

  /**
   * Show operation confirmation
   */
  static confirm(message: string, onConfirm: () => void, onCancel?: () => void) {
    return toast(message, {
      duration: Infinity,
      action: {
        label: 'Confirm',
        onClick: () => onConfirm(),
      },
      cancel: onCancel ? {
        label: 'Cancel',
        onClick: () => onCancel(),
      } : undefined,
    });
  }

  /**
   * Dismiss all notifications
   */
  static dismiss() {
    toast.dismiss();
  }

  /**
   * Dismiss specific notification
   */
  static dismissById(id: string | number) {
    toast.dismiss(id);
  }
}

/**
 * Shortcut notification methods
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
 * Common notification messages
 */
export const Messages = {
  // General operations
  SAVE_SUCCESS: 'Save successful',
  SAVE_FAILED: 'Save failed',
  DELETE_SUCCESS: 'Delete successful', 
  DELETE_FAILED: 'Delete failed',
  UPDATE_SUCCESS: 'Update successful',
  UPDATE_FAILED: 'Update failed',
  COPY_SUCCESS: 'Copy successful',
  
  // Authentication related
  LOGIN_SUCCESS: 'Login successful',
  LOGIN_FAILED: 'Login failed',
  LOGOUT_SUCCESS: 'Logged out successfully',
  REGISTER_SUCCESS: 'Registration successful',
  REGISTER_FAILED: 'Registration failed',
  PASSWORD_RESET_SENT: 'Password reset email sent',
  EMAIL_VERIFICATION_SENT: 'Verification email sent',
  
  // Network and system
  NETWORK_ERROR: 'Network connection error',
  SERVER_ERROR: 'Server error, please try again later',
  UNAUTHORIZED: 'Login expired, please log in again',
  FORBIDDEN: 'Insufficient permissions',
  NOT_FOUND: 'Requested resource not found',
  
  // Form validation
  REQUIRED_FIELD: 'This field is required',
  INVALID_EMAIL: 'Please enter a valid email address',
  PASSWORD_TOO_SHORT: 'Password must be at least 8 characters',
  PASSWORDS_NOT_MATCH: 'Passwords do not match',
  
  // File operations
  FILE_UPLOAD_SUCCESS: 'File upload successful',
  FILE_UPLOAD_FAILED: 'File upload failed',
  FILE_TOO_LARGE: 'File size exceeds limit',
  INVALID_FILE_TYPE: 'Unsupported file type',
} as const;

/**
 * Operation result notification
 */
export function notifyResult<T>(
  operation: string,
  result: { success: boolean; data?: T; error?: any }
) {
  if (result.success) {
    notify.success(`${operation} successful`);
  } else {
    notify.error(`${operation} failed: ${result.error?.message || 'Unknown error'}`);
  }
  
  return result;
}