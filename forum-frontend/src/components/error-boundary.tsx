import React, { Component, ErrorInfo, ReactNode } from 'react';
import { AlertTriangle, RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';

interface Props {
  children: ReactNode;
  fallback?: ReactNode;
  onError?: (error: Error, errorInfo: ErrorInfo) => void;
}

interface State {
  hasError: boolean;
  error?: Error;
}

/**
 * 错误边界组件
 * 捕获子组件中的 JavaScript 错误并显示降级 UI
 */
export class ErrorBoundary extends Component<Props, State> {
  public state: State = {
    hasError: false,
  };

  public static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  public componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error('ErrorBoundary caught an error:', error, errorInfo);
    
    // 调用外部错误处理器
    if (this.props.onError) {
      this.props.onError(error, errorInfo);
    }

    // 在生产环境中可以发送错误到监控服务
    if (import.meta.env.PROD) {
      // TODO: 发送错误到监控服务
      // reportError(error, errorInfo);
    }
  }

  private handleReload = () => {
    window.location.reload();
  };

  private handleRetry = () => {
    this.setState({ hasError: false, error: undefined });
  };

  public render() {
    if (this.state.hasError) {
      // 如果提供了自定义降级 UI，使用它
      if (this.props.fallback) {
        return this.props.fallback;
      }

      // 默认的错误显示 UI
      return (
        <div className="min-h-screen flex items-center justify-center bg-gray-50 p-4">
          <Card className="w-full max-w-md">
            <CardHeader className="text-center">
              <div className="mx-auto mb-4 h-12 w-12 text-red-500">
                <AlertTriangle className="h-full w-full" />
              </div>
              <CardTitle className="text-xl">出错了</CardTitle>
              <CardDescription>
                应用程序遇到了一个意外错误
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              {import.meta.env.DEV && this.state.error && (
                <div className="rounded-md bg-red-50 border border-red-200 p-3">
                  <div className="text-sm text-red-700 font-mono">
                    {this.state.error.message}
                  </div>
                  {this.state.error.stack && (
                    <details className="mt-2">
                      <summary className="text-sm text-red-600 cursor-pointer">
                        查看详细信息
                      </summary>
                      <pre className="mt-2 text-xs text-red-600 whitespace-pre-wrap">
                        {this.state.error.stack}
                      </pre>
                    </details>
                  )}
                </div>
              )}
              
              <div className="flex flex-col sm:flex-row gap-2">
                <Button 
                  onClick={this.handleRetry} 
                  variant="outline" 
                  className="flex-1"
                >
                  <RefreshCw className="mr-2 h-4 w-4" />
                  重试
                </Button>
                <Button 
                  onClick={this.handleReload} 
                  className="flex-1"
                >
                  刷新页面
                </Button>
              </div>

              <p className="text-center text-sm text-gray-500">
                如果问题持续存在，请联系技术支持
              </p>
            </CardContent>
          </Card>
        </div>
      );
    }

    return this.props.children;
  }
}

/**
 * API 错误边界组件
 * 专门处理 API 相关的错误
 */
interface ApiErrorBoundaryProps extends Props {
  title?: string;
  description?: string;
}

export function ApiErrorBoundary({ 
  children, 
  title = "加载失败", 
  description = "无法加载数据，请检查网络连接",
  ...props 
}: ApiErrorBoundaryProps) {
  const fallback = (
    <div className="flex flex-col items-center justify-center p-8 text-center">
      <AlertTriangle className="h-8 w-8 text-orange-500 mb-4" />
      <h3 className="text-lg font-semibold mb-2">{title}</h3>
      <p className="text-gray-600 mb-4">{description}</p>
      <Button onClick={() => window.location.reload()} variant="outline">
        <RefreshCw className="mr-2 h-4 w-4" />
        重新加载
      </Button>
    </div>
  );

  return (
    <ErrorBoundary {...props} fallback={fallback}>
      {children}
    </ErrorBoundary>
  );
}

/**
 * 错误边界 Hook
 * 用于函数组件中处理错误
 */
export function useErrorHandler() {
  const handleError = React.useCallback((error: Error, errorInfo?: any) => {
    console.error('Handled error:', error, errorInfo);
    
    // 在生产环境中发送错误到监控服务
    if (import.meta.env.PROD) {
      // TODO: 发送错误到监控服务
      // reportError(error, errorInfo);
    }
  }, []);

  return handleError;
}