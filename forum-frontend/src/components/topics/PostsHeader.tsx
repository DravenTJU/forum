import { MessageSquare } from 'lucide-react';
import { Button } from '@/components/ui/button';

interface PostsHeaderProps {
  postsCount: number;
  sortBy: 'oldest' | 'newest';
  onSortChange: (sort: 'oldest' | 'newest') => void;
  isTopicLocked?: boolean;
  onReply?: () => void;
}

const PostsHeader = ({ 
  postsCount, 
  sortBy, 
  onSortChange, 
  isTopicLocked = false, 
  onReply 
}: PostsHeaderProps) => {
  return (
    <div className="flex items-center justify-between p-4 border-b bg-muted/20">
      <div className="flex items-center gap-4">
        <h2 className="text-base font-medium text-zinc-900">
          {postsCount} 条回复
        </h2>
        
        {/* 排序选项 */}
        <div className="flex items-center gap-2">
          <span className="text-sm text-zinc-500">排序:</span>
          <div className="flex bg-zinc-100 rounded p-1">
            <button
              onClick={() => onSortChange('oldest')}
              className={`px-2 py-1 text-sm rounded ${
                sortBy === 'oldest'
                  ? 'bg-white text-zinc-900 shadow-sm'
                  : 'text-zinc-600 hover:text-zinc-900'
              }`}
            >
              时间顺序
            </button>
            <button
              onClick={() => onSortChange('newest')}
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
  );
};

export default PostsHeader;