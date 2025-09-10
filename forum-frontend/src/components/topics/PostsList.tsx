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
}

const PostsList = ({ topicId, isTopicLocked = false, onReply }: PostsListProps) => {
  const [sortBy, setSortBy] = useState<'oldest' | 'newest'>('oldest');
  
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
  
  // 处理排序切换
  const handleSortChange = (newSort: 'oldest' | 'newest') => {
    setSortBy(newSort);
  };
  
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
      {/* 回帖列表头部 */}
      {allPosts.length > 0 && (
        <div className="bg-white rounded-lg border border-zinc-200 px-6 py-3 mb-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-4">
              <h2 className="text-base font-medium text-zinc-900">
                {allPosts.length} 条回复
              </h2>
              
              {/* 排序选项 */}
              <div className="flex items-center gap-2">
                <span className="text-sm text-zinc-500">排序:</span>
                <div className="flex bg-zinc-100 rounded p-1">
                  <button
                    onClick={() => handleSortChange('oldest')}
                    className={`px-2 py-1 text-sm rounded ${
                      sortBy === 'oldest'
                        ? 'bg-white text-zinc-900 shadow-sm'
                        : 'text-zinc-600 hover:text-zinc-900'
                    }`}
                  >
                    时间顺序
                  </button>
                  <button
                    onClick={() => handleSortChange('newest')}
                    className={`px-2 py-1 text-sm rounded ${
                      sortBy === 'newest'
                        ? 'bg-white text-zinc-900 shadow-sm'
                        : 'text-zinc-600 hover:text-zinc-900'
                    }`}
                  >
                    最新优先
                  </button>
                </div>
              </div>
            </div>
            
            {/* 操作按钮 */}
            <div className="flex items-center gap-2">
              {!isTopicLocked && (
                <Button onClick={onReply} size="sm" className="gap-2">
                  <MessageSquare className="w-4 h-4" />
                  回复
                </Button>
              )}
            </div>
          </div>
        </div>
      )}
      
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
              floorNumber={sortBy === 'oldest' ? index + 2 : allPosts.length - index + 1}
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