import { api } from './auth';
import { ApiResponse, Topic, PaginationQuery } from '@/types/api';

// 主题查询参数
export interface TopicQuery extends PaginationQuery {
  categoryId?: string;
  tagSlugs?: string[];
  sort?: 'latest' | 'hot' | 'top' | 'new';
}

// 分页游标类型 - 符合API规范的Keyset分页
export interface PaginationCursor {
  lastPostedAt?: string;
  id?: string;
}

// 主题列表响应
export interface TopicsResponse {
  topics: Topic[];
  hasMore: boolean;
  nextCursor?: string; // Base64编码的游标字符串
}

// 获取主题列表
export const getTopics = async (params: TopicQuery): Promise<TopicsResponse> => {
  const response = await api.get<ApiResponse<Topic[]>>('/topics', { params });
  
  if (!response.data.success) {
    throw new Error(response.data.error?.message || '获取主题列表失败');
  }
  
  // 返回符合API规范的响应格式
  return {
    topics: response.data.data || [],
    hasMore: response.data.meta?.hasNext || false,
    nextCursor: response.data.meta?.nextCursor
  };
};

// 获取主题详情
export const getTopic = async (id: string): Promise<Topic> => {
  const response = await api.get<ApiResponse<Topic>>(`/topics/${id}`);
  
  if (!response.data.success) {
    throw new Error(response.data.error?.message || '获取主题详情失败');
  }
  
  return response.data.data!;
};

// 创建主题
export interface CreateTopicRequest {
  title: string;
  contentMd: string;
  categoryId: string;
  tagSlugs?: string[];
}

export const createTopic = async (data: CreateTopicRequest): Promise<Topic> => {
  const response = await api.post<ApiResponse<Topic>>('/topics', data);
  
  if (!response.data.success) {
    throw new Error(response.data.error?.message || '创建主题失败');
  }
  
  return response.data.data!;
};

// 分页游标编码/解码工具函数
export const encodePaginationCursor = (cursor: PaginationCursor): string => {
  return btoa(JSON.stringify(cursor));
};

export const decodePaginationCursor = (cursorString: string): PaginationCursor => {
  try {
    return JSON.parse(atob(cursorString));
  } catch {
    console.warn('Invalid pagination cursor:', cursorString);
    return {};
  }
};

// Re-export the configured api instance
export { api };