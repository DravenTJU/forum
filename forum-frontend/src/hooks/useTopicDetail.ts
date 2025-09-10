import { useQuery, useInfiniteQuery, useQueryClient } from '@tanstack/react-query';
import { getTopicDetail, getTopicPosts, PostQuery } from '@/api/topics';

// 主题详情数据获取 hook
export function useTopicDetail(topicId: string) {
  return useQuery({
    queryKey: ['topic-detail', topicId],
    queryFn: () => getTopicDetail(topicId),
    enabled: !!topicId, // 只有当 topicId 存在时才执行查询
    staleTime: 2 * 60 * 1000, // 2分钟内数据被认为是新鲜的
    retry: (failureCount, error) => {
      // 如果是404错误（主题不存在），不要重试
      if (error && typeof error === 'object' && 'status' in error && error.status === 404) {
        return false;
      }
      // 其他错误最多重试2次
      return failureCount < 2;
    },
  });
}

// 主题回帖列表无限滚动 hook
export function useTopicPosts(topicId: string, query: Omit<PostQuery, 'cursor'> = {}) {
  return useInfiniteQuery({
    queryKey: ['topic-posts', topicId, query],
    queryFn: ({ pageParam }: { pageParam: string | null }) => {
      const params: PostQuery = {
        ...query,
        cursor: pageParam || undefined,
        limit: query.limit || 20
      };
      return getTopicPosts(topicId, params);
    },
    initialPageParam: null as string | null,
    getNextPageParam: (lastPage) => {
      return lastPage.hasMore ? lastPage.nextCursor : undefined;
    },
    enabled: !!topicId, // 只有当 topicId 存在时才执行查询
    staleTime: 1 * 60 * 1000, // 1分钟内数据被认为是新鲜的
    retry: (failureCount, error) => {
      // 如果是404错误（主题不存在），不要重试
      if (error && typeof error === 'object' && 'status' in error && error.status === 404) {
        return false;
      }
      // 其他错误最多重试2次
      return failureCount < 2;
    },
  });
}

// 刷新主题详情和回帖
export function useRefreshTopicDetail() {
  const queryClient = useQueryClient();
  
  return (topicId: string) => {
    // 刷新主题详情
    queryClient.invalidateQueries({ queryKey: ['topic-detail', topicId] });
    // 刷新该主题的回帖列表
    queryClient.invalidateQueries({ queryKey: ['topic-posts', topicId] });
    // 也刷新主题列表，因为统计数据可能变化
    queryClient.invalidateQueries({ queryKey: ['topics'] });
  };
}

// 预加载主题详情（用于提升用户体验）
export function usePrefetchTopicDetail() {
  const queryClient = useQueryClient();
  
  return (topicId: string) => {
    queryClient.prefetchQuery({
      queryKey: ['topic-detail', topicId],
      queryFn: () => getTopicDetail(topicId),
      staleTime: 2 * 60 * 1000,
    });
  };
}

// 获取缓存中的主题详情（用于快速访问）
export function useCachedTopicDetail(topicId: string) {
  const queryClient = useQueryClient();
  
  return () => {
    return queryClient.getQueryData(['topic-detail', topicId]);
  };
}