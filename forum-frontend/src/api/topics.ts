import { api } from './auth';
import { ApiResponse, Topic, TopicDetail, Post, PaginationQuery } from '@/types/api';

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

// 获取主题详情（包含首帖）
export const getTopicDetail = async (id: string): Promise<TopicDetail> => {
  try {
    console.log('🚀 开始请求主题详情:', { topicId: id });
    
    const response = await api.get<ApiResponse<TopicDetail>>(`/topics/${id}`);
    
    console.log('📥 主题详情 API 响应:', {
      status: response.status,
      data: response.data
    });
    
    if (!response.data.success) {
      const errorMessage = response.data.error?.message || '获取主题详情失败';
      console.error('❌ 主题详情 API 业务逻辑错误:', response.data.error);
      throw new Error(errorMessage);
    }
    
    const result = response.data.data!;
    console.log('✅ 主题详情获取成功:', {
      topicId: result.id,
      title: result.title,
      hasFirstPost: !!result.firstPost
    });
    
    return result;
  } catch (error) {
    console.error('💥 获取主题详情失败:', {
      error,
      topicId: id,
      timestamp: new Date().toISOString()
    });
    throw error;
  }
};

// 保持向后兼容的简单主题获取方法
export const getTopic = async (id: string): Promise<Topic> => {
  const response = await api.get<ApiResponse<Topic>>(`/topics/${id}`);
  
  if (!response.data.success) {
    throw new Error(response.data.error?.message || '获取主题详情失败');
  }
  
  return response.data.data!;
};

// 回帖查询参数
export interface PostQuery extends PaginationQuery {
  sort?: 'oldest' | 'newest';
}

// 回帖列表响应
export interface PostsResponse {
  posts: Post[];
  hasMore: boolean;
  nextCursor?: string;
}

// 获取主题回帖列表
export const getTopicPosts = async (topicId: string, params: PostQuery = {}): Promise<PostsResponse> => {
  try {
    console.log('🚀 开始请求回帖列表:', { topicId, params });
    
    const response = await api.get<ApiResponse<Post[]>>(`/topics/${topicId}/posts`, { params });
    
    console.log('📥 回帖列表 API 响应:', {
      status: response.status,
      data: response.data
    });
    
    if (!response.data.success) {
      const errorMessage = response.data.error?.message || '获取回帖列表失败';
      console.error('❌ 回帖列表 API 业务逻辑错误:', response.data.error);
      throw new Error(errorMessage);
    }
    
    const result = {
      posts: response.data.data || [],
      hasMore: response.data.meta?.hasNext || false,
      nextCursor: response.data.meta?.nextCursor
    };
    
    console.log('✅ 回帖列表获取成功:', {
      topicId,
      postCount: result.posts.length,
      hasMore: result.hasMore
    });
    
    return result;
  } catch (error) {
    console.error('💥 获取回帖列表失败:', {
      error,
      topicId,
      params,
      timestamp: new Date().toISOString()
    });
    throw error;
  }
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

// 创建回帖
export interface CreatePostRequest {
  contentMd: string;
  replyToPostId?: string;
}

export const createPost = async (topicId: string, data: CreatePostRequest): Promise<Post> => {
  try {
    console.log('🚀 开始创建回帖:', { topicId, data });
    
    const response = await api.post<ApiResponse<Post>>(`/topics/${topicId}/posts`, data);
    
    if (!response.data.success) {
      const errorMessage = response.data.error?.message || '创建回帖失败';
      console.error('❌ 创建回帖失败:', response.data.error);
      throw new Error(errorMessage);
    }
    
    const result = response.data.data!;
    console.log('✅ 回帖创建成功:', { postId: result.id });
    
    return result;
  } catch (error) {
    console.error('💥 创建回帖失败:', {
      error,
      topicId,
      data,
      timestamp: new Date().toISOString()
    });
    throw error;
  }
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