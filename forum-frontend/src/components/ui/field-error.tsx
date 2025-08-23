import { AlertCircle } from 'lucide-react';
import { cn } from '@/lib/utils';

interface FieldErrorProps {
  error?: string | string[];
  className?: string;
}

/**
 * 字段错误显示组件
 * 支持单个错误消息或错误消息数组
 */
export function FieldError({ error, className }: FieldErrorProps) {
  if (!error || (Array.isArray(error) && error.length === 0)) {
    return null;
  }

  const errors = Array.isArray(error) ? error : [error];

  return (
    <div className={cn('flex items-start gap-1.5 mt-1', className)}>
      <AlertCircle className="h-4 w-4 text-red-500 shrink-0 mt-0.5" />
      <div className="flex-1 text-sm text-red-600 space-y-1">
        {errors.map((err, index) => (
          <div key={index}>{err}</div>
        ))}
      </div>
    </div>
  );
}

/**
 * 表单级错误显示组件
 * 用于显示整个表单的错误消息
 */
interface FormErrorProps {
  error?: string;
  errors?: Record<string, string[]>;
  className?: string;
}

export function FormError({ error, errors, className }: FormErrorProps) {
  const hasError = error || (errors && Object.keys(errors).length > 0);

  if (!hasError) {
    return null;
  }

  return (
    <div className={cn(
      'rounded-md bg-red-50 border border-red-200 p-3 space-y-2',
      className
    )}>
      {error && (
        <div className="flex items-center gap-2 text-red-800">
          <AlertCircle className="h-4 w-4" />
          <span className="text-sm font-medium">{error}</span>
        </div>
      )}
      
      {errors && Object.keys(errors).length > 0 && (
        <div className="space-y-1">
          {Object.entries(errors).map(([field, fieldErrors]) => (
            <div key={field} className="text-sm text-red-700">
              <span className="font-medium capitalize">{field}:</span>{' '}
              {fieldErrors.join(', ')}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

/**
 * 成功消息显示组件
 */
interface SuccessMessageProps {
  message?: string;
  className?: string;
}

export function SuccessMessage({ message, className }: SuccessMessageProps) {
  if (!message) {
    return null;
  }

  return (
    <div className={cn(
      'rounded-md bg-green-50 border border-green-200 p-3',
      className
    )}>
      <div className="flex items-center gap-2 text-green-800">
        <AlertCircle className="h-4 w-4" />
        <span className="text-sm font-medium">{message}</span>
      </div>
    </div>
  );
}