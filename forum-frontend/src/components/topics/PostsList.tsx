import { useState } from 'react';
import { MessageSquare } from 'lucide-react';

import PostCard from './PostCard';
import { LoadingSpinner } from '@/components/ui/loading-spinner';
import { Button } from '@/components/ui/button';
import { useTopicPosts } from '@/hooks/useTopicDetail';

interface PostsListProps {
  topicId: string;
  isTopicLocked?: boolean;
  onReply?: () => void;
  sortBy?: 'oldest' | 'newest';
}

const PostsList = ({ 
  topicId, 
  isTopicLocked = false, 
  onReply,
  sortBy: externalSortBy
}: PostsListProps) => {
  const [internalSortBy] = useState<'oldest' | 'newest'>('oldest');
  
  // 使用外部传入的排序状态或内部状态
  const sortBy = externalSortBy ?? internalSortBy;
  
  // 获取回帖数据
  const {
    data,
    isLoading,
    isError,
    error,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage
  } = useTopicPosts(topicId);
  
  // 获取所有回帖
  const allPosts = data?.pages.flatMap((page: any) => page.posts) ?? [];
  
  // 排序处理
  const sortedPosts = [...allPosts].sort((a, b) => {
    const aTime = new Date(a.createdAt).getTime();
    const bTime = new Date(b.createdAt).getTime();
    return sortBy === 'oldest' ? aTime - bTime : bTime - aTime;
  });
  
  // 处理排序切换（已移至组件参数处理）
  
  // 加载更多
  const handleLoadMore = () => {
    if (hasNextPage && !isFetchingNextPage) {
      fetchNextPage();
    }
  };

  // 如果没有回帖
  if (!isLoading && allPosts.length === 0) {
    return (
      <div className="bg-white rounded-lg border border-zinc-200 p-8 text-center">
        <MessageSquare className="w-12 h-12 text-zinc-300 mx-auto mb-4" />
        <h3 className="text-lg font-medium text-zinc-600 mb-2">
          暂无回帖
        </h3>
        <p className="text-zinc-500 mb-4">
          {isTopicLocked ? '此主题已锁定，无法回复' : '成为第一个回复的人吧！'}
        </p>
        {!isTopicLocked && (
          <Button onClick={onReply} className="gap-2">
            <MessageSquare className="w-4 h-4" />
            发表回复
          </Button>
        )}
      </div>
    );
  }

  return (
    <div className="space-y-4">
      
      {/* 加载状态 */}
      {isLoading && (
        <div className="bg-white rounded-lg border border-zinc-200 p-8 text-center">
          <LoadingSpinner size="large" />
          <p className="text-zinc-500 mt-4">加载回帖中...</p>
        </div>
      )}
      
      {/* 错误状态 */}
      {isError && (
        <div className="bg-white rounded-lg border border-zinc-200 p-8 text-center">
          <h3 className="text-lg font-medium text-red-600 mb-2">
            加载失败
          </h3>
          <p className="text-zinc-500 mb-4">
            {error?.message || '无法加载回帖列表，请稍后重试'}
          </p>
          <Button 
            variant="outline" 
            onClick={() => window.location.reload()}
          >
            重新加载
          </Button>
        </div>
      )}
      
      {/* 回帖列表 */}
      {!isLoading && !isError && sortedPosts.length > 0 && (
        <div className="space-y-4">
          {sortedPosts.map((post, index) => (
            <PostCard
              key={post.id}
              post={post}
              floorNumber={sortBy === 'oldest' ? index + 1 : allPosts.length - index}
              onReply={onReply}
            />
          ))}
          
          {/* 加载更多按钮 */}
          {hasNextPage && (
            <div className="text-center py-4">
              <Button
                variant="outline"
                onClick={handleLoadMore}
                disabled={isFetchingNextPage}
                className="gap-2"
              >
                {isFetchingNextPage ? (
                  <>
                    <LoadingSpinner size="small" />
                    加载中...
                  </>
                ) : (
                  '加载更多回帖'
                )}
              </Button>
            </div>
          )}
          
          {/* 底部回复区域 */}
          {!isTopicLocked && (
            <div className="bg-white rounded-lg border border-zinc-200 p-6 text-center">
              <MessageSquare className="w-8 h-8 text-zinc-300 mx-auto mb-3" />
              <p className="text-zinc-500 mb-4">
                想要参与讨论？
              </p>
              <Button onClick={onReply} size="lg" className="gap-2">
                <MessageSquare className="w-4 h-4" />
                发表回复
              </Button>
            </div>
          )}
        </div>
      )}
    </div>
  );
};

export default PostsList;