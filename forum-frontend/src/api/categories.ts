import { api } from './auth';
import { ApiResponse, Category } from '@/types/api';

// 获取分类列表
export const getCategories = async (): Promise<Category[]> => {
  const response = await api.get<ApiResponse<Category[]>>('/categories');
  
  if (!response.data.success) {
    throw new Error(response.data.error?.message || '获取分类列表失败');
  }
  
  return response.data.data!;
};

// 获取分类详情
export const getCategory = async (id: string): Promise<Category> => {
  const response = await api.get<ApiResponse<Category>>(`/categories/${id}`);
  
  if (!response.data.success) {
    throw new Error(response.data.error?.message || '获取分类详情失败');
  }
  
  return response.data.data!;
};

// Re-export the configured api instance
export { api };