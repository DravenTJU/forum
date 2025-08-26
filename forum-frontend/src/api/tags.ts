import { api } from './auth';
import { ApiResponse, Tag } from '@/types/api';

// 获取标签列表
export const getTags = async (): Promise<Tag[]> => {
  const response = await api.get<ApiResponse<Tag[]>>('/tags');
  
  if (!response.data.success) {
    throw new Error(response.data.error?.message || '获取标签列表失败');
  }
  
  return response.data.data!;
};

// 根据slug获取标签
export const getTagsBySlug = async (slugs: string[]): Promise<Tag[]> => {
  const params = new URLSearchParams();
  slugs.forEach(slug => params.append('slugs', slug));
  
  const response = await api.get<ApiResponse<Tag[]>>(`/tags/by-slug?${params.toString()}`);
  
  if (!response.data.success) {
    throw new Error(response.data.error?.message || '获取标签失败');
  }
  
  return response.data.data!;
};

// Re-export the configured api instance
export { api };