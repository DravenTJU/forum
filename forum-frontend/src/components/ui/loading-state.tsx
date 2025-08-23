import React from 'react';
import { Loader2 } from 'lucide-react';
import { cn } from '@/lib/utils';

interface LoadingSpinnerProps {
  size?: 'sm' | 'md' | 'lg';
  className?: string;
}

/**
 * 加载旋转器组件
 */
export function LoadingSpinner({ size = 'md', className }: LoadingSpinnerProps) {
  const sizeClasses = {
    sm: 'h-4 w-4',
    md: 'h-6 w-6', 
    lg: 'h-8 w-8',
  };

  return (
    <Loader2 className={cn('animate-spin', sizeClasses[size], className)} />
  );
}

interface LoadingStateProps {
  message?: string;
  size?: 'sm' | 'md' | 'lg';
  className?: string;
  fullScreen?: boolean;
}

/**
 * 加载状态组件
 */
export function LoadingState({ 
  message = '加载中...', 
  size = 'md', 
  className,
  fullScreen = false 
}: LoadingStateProps) {
  const containerClasses = fullScreen 
    ? 'fixed inset-0 flex items-center justify-center bg-white/80 backdrop-blur-sm z-50'
    : 'flex items-center justify-center p-8';

  return (
    <div className={cn(containerClasses, className)}>
      <div className="flex flex-col items-center gap-3">
        <LoadingSpinner size={size} />
        {message && (
          <p className="text-sm text-gray-600 animate-pulse">{message}</p>
        )}
      </div>
    </div>
  );
}

interface PageLoadingProps {
  message?: string;
}

/**
 * 页面级加载组件
 */
export function PageLoading({ message = '正在加载页面...' }: PageLoadingProps) {
  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50">
      <div className="text-center">
        <LoadingSpinner size="lg" className="mx-auto mb-4" />
        <h2 className="text-xl font-semibold mb-2">请稍候</h2>
        <p className="text-gray-600">{message}</p>
      </div>
    </div>
  );
}

interface InlineLoadingProps {
  message?: string;
  className?: string;
}

/**
 * 内联加载组件
 * 用于按钮或小区域的加载状态
 */
export function InlineLoading({ message, className }: InlineLoadingProps) {
  return (
    <div className={cn('flex items-center gap-2', className)}>
      <LoadingSpinner size="sm" />
      {message && <span className="text-sm">{message}</span>}
    </div>
  );
}

interface SectionLoadingProps {
  title?: string;
  description?: string;
  className?: string;
}

/**
 * 区域加载组件
 * 用于页面中特定区域的加载状态
 */
export function SectionLoading({ 
  title = '加载中', 
  description = '正在获取数据...', 
  className 
}: SectionLoadingProps) {
  return (
    <div className={cn('flex flex-col items-center justify-center p-12 text-center', className)}>
      <LoadingSpinner size="lg" className="mb-4" />
      <h3 className="text-lg font-semibold mb-2">{title}</h3>
      <p className="text-gray-600">{description}</p>
    </div>
  );
}

interface ButtonLoadingProps {
  loading?: boolean;
  children: React.ReactNode;
  loadingText?: string;
  className?: string;
}

/**
 * 按钮加载状态组件
 * 可以包装任何按钮组件
 */
export function ButtonLoading({ 
  loading = false, 
  children, 
  loadingText,
  className 
}: ButtonLoadingProps) {
  return (
    <div className={cn('relative', className)}>
      {loading && (
        <div className="absolute inset-0 flex items-center justify-center">
          <LoadingSpinner size="sm" />
          {loadingText && (
            <span className="ml-2 text-sm">{loadingText}</span>
          )}
        </div>
      )}
      <div className={loading ? 'opacity-0' : 'opacity-100'}>
        {children}
      </div>
    </div>
  );
}