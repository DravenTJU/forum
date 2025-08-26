/**
 * API 配置和导出文件
 * 
 * 这个文件作为所有 API 功能的统一入口，确保：
 * 1. 所有 API 调用都使用配置好 CSRF Token 的客户端
 * 2. 统一的错误处理和响应格式
 * 3. 类型安全和一致性
 */

// 导出配置好的 API 客户端和认证相关功能
export { api, authApi, getCsrfToken } from './auth';

// 导出分类相关 API
export * from './categories';

// 导出标签相关 API  
export * from './tags';

// 导出主题相关 API
export * from './topics';

// 导出所有类型定义
export * from '@/types/api';
export * from '@/types/auth';