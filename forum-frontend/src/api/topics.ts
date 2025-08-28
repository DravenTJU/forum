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
  try {
    console.log('🚀 开始请求主题列表:', { params, url: '/topics' });
    
    const response = await api.get<ApiResponse<Topic[]>>('/topics', { params });
    
    console.log('📥 API 响应:', {
      status: response.status,
      statusText: response.statusText,
      headers: response.headers,
      data: response.data
    });
    
    if (!response.data.success) {
      const errorMessage = response.data.error?.message || '获取主题列表失败';
      console.error('❌ API 业务逻辑错误:', {
        success: response.data.success,
        error: response.data.error,
        fullResponse: response.data
      });
      throw new Error(errorMessage);
    }
    
    const result = {
      topics: response.data.data || [],
      hasMore: response.data.meta?.hasNext || false,
      nextCursor: response.data.meta?.nextCursor
    };
    
    console.log('✅ 主题列表获取成功:', {
      topicCount: result.topics.length,
      hasMore: result.hasMore,
      nextCursor: result.nextCursor
    });
    
    return result;
  } catch (error) {
    console.error('💥 获取主题列表失败:', {
      error,
      errorMessage: error instanceof Error ? error.message : '未知错误',
      errorStack: error instanceof Error ? error.stack : undefined,
      params,
      timestamp: new Date().toISOString()
    });
    
    // 重新抛出错误，让上层处理
    throw error;
  }
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