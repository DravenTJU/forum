import { useInfiniteQuery, useQuery, useQueryClient } from '@tanstack/react-query';
import { getTopics, TopicQuery, TopicsResponse } from '@/api/topics';
import { getCategories } from '@/api/categories';
import { getTags } from '@/api/tags';

// 主题列表无限滚动 hook
export function useTopics(query: Omit<TopicQuery, 'cursor'>) {
  return useInfiniteQuery({
    queryKey: ['topics', query],
    queryFn: ({ pageParam = null }) => {
      const params: TopicQuery = {
        ...query,
        cursor: pageParam || undefined, // 直接使用Base64编码的游标字符串
        limit: query.limit || 20
      };
      return getTopics(params);
    },
    initialPageParam: null,
    getNextPageParam: (lastPage: TopicsResponse) => {
      return lastPage.hasMore ? lastPage.nextCursor : undefined;
    },
  });
}

// 分类列表 hook
export function useCategories() {
  return useQuery({
    queryKey: ['categories'],
    queryFn: async () => {
      try {
        return await getCategories();
      } catch (error) {
        console.warn('Categories API failed, using empty array:', error);
        return []; // 返回空数组而不是抛出错误
      }
    },
    staleTime: 5 * 60 * 1000, // 5分钟
    retry: false, // 不重试，避免无限错误循环
    refetchOnWindowFocus: false, // 避免窗口聚焦时重试
  });
}

// 标签列表 hook
export function useTags() {
  return useQuery({
    queryKey: ['tags'],
    queryFn: async () => {
      try {
        return await getTags();
      } catch (error) {
        console.warn('Tags API failed, using empty array:', error);
        return []; // 返回空数组而不是抛出错误
      }
    },
    staleTime: 5 * 60 * 1000, // 5分钟
    retry: false, // 不重试，避免无限错误循环
    refetchOnWindowFocus: false, // 避免窗口聚焦时重试
  });
}

// 刷新主题列表
export function useRefreshTopics() {
  const queryClient = useQueryClient();
  
  return () => {
    queryClient.invalidateQueries({ queryKey: ['topics'] });
  };
}