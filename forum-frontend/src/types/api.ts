// API 通用类型定义，符合后端 API 规范

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: {
    code: string;
    message: string;
    details?: Record<string, string[]>; // 字段级错误
  };
  meta?: {
    total?: number;
    hasNext?: boolean;
    nextCursor?: string;
  };
}

export interface PaginationQuery {
  cursor?: string;  // Keyset 分页游标
  limit?: number;   // 每页数量，默认 20，最大 100
}

export interface Category {
  id: string;
  name: string;
  slug: string;
  description?: string;
  order: number;
  isArchived: boolean;
  topicCount?: number;
}

export interface Tag {
  id: string;
  name: string;
  slug: string;
  description?: string;
  color?: string;
  topicCount?: number;
}

export interface Topic {
  id: string;
  title: string;
  slug: string;
  author: {
    id: string;
    username: string;
    avatarUrl?: string;
  };
  category: {
    id: string;
    name: string;
    slug: string;
  };
  tags: Tag[];
  isPinned: boolean;
  isLocked: boolean;
  isDeleted: boolean;
  replyCount: number;
  viewCount: number;
  lastPostedAt?: string;
  lastPoster?: {
    id: string;
    username: string;
  };
  createdAt: string;
  updatedAt: string;
}

export interface TopicDetail extends Topic {
  firstPost: Post;
}

export interface Post {
  id: string;
  topicId: string;
  author: {
    id: string;
    username: string;
    avatarUrl?: string;
  };
  contentMd: string;
  contentHtml?: string; // 服务端渲染的 HTML（可选缓存）
  replyToPost?: {
    id: string;
    author: string;
    excerpt: string;
  };
  mentions: string[]; // 被 @ 的用户名列表
  isEdited: boolean;
  isDeleted: boolean;
  createdAt: string;
  updatedAt: string;
}

// 搜索相关类型
export interface SearchResult {
  type: 'topic' | 'post';
  topicId: string;
  postId?: string;
  title?: string;
  snippet?: string;
  score: number;
  lastPostedAt: string;
}

// 通知相关类型
export interface Notification {
  id: string;
  type: 'mention' | 'reply' | 'like';
  topicId: string;
  postId?: string;
  byUser: {
    id: string;
    username: string;
  };
  snippet: string;
  readAt?: string;
  createdAt: string;
}

// 错误类型
export interface ApiError {
  code: string;
  message: string;
  details?: Record<string, string[]>;
}

// HTTP 状态码对应的错误类型
export type ErrorCode = 
  | 'VALIDATION_FAILED'
  | 'UNAUTHORIZED'
  | 'FORBIDDEN'
  | 'NOT_FOUND'
  | 'CONFLICT'
  | 'TOO_MANY_REQUESTS'
  | 'INTERNAL_ERROR'
  | 'CSRF_TOKEN_INVALID'
  | 'TOPIC_LOCKED'
  | 'INSUFFICIENT_PERMISSIONS';