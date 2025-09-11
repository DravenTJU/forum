import { MessageSquare, ArrowLeft } from 'lucide-react';
import { Button } from '@/components/ui/button';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Separator } from '@/components/ui/separator';

interface PostsHeaderProps {
  postsCount: number;
  sortBy: 'oldest' | 'newest';
  onSortChange: (sort: 'oldest' | 'newest') => void;
  isTopicLocked?: boolean;
  onReply?: () => void;
  onBackToTopics?: () => void;
}

const PostsHeader = ({ 
  postsCount, 
  sortBy, 
  onSortChange, 
  isTopicLocked = false, 
  onReply,
  onBackToTopics
}: PostsHeaderProps) => {
  const sortOptions = [
    { value: 'oldest', label: '时间顺序' },
    { value: 'newest', label: '最新优先' }
  ];

  return (
    <div className="flex items-center justify-between p-4 border-b bg-muted/20">
      <div className="flex items-center space-x-4">
        {/* 返回主题列表按钮 */}
        {onBackToTopics && (
          <Button variant="ghost" size="sm" onClick={onBackToTopics} className="gap-2">
            <ArrowLeft className="w-4 h-4" />
            返回列表
          </Button>
        )}
        
        <h2 className="text-base font-medium text-zinc-900">
          {postsCount} 条回复
        </h2>
      </div>

      <div className="flex items-center space-x-3">
        {/* 排序选择 */}
        <div className="flex items-center space-x-2">
          <span className="text-sm text-muted-foreground">排序:</span>
          <Select value={sortBy} onValueChange={onSortChange}>
            <SelectTrigger className="w-32">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {sortOptions.map((option) => (
                <SelectItem key={option.value} value={option.value}>
                  {option.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <Separator orientation="vertical" className="h-6" />

        {/* 回复按钮 */}
        {!isTopicLocked && (
          <Button onClick={onReply}>
            <MessageSquare className="w-4 h-4 mr-2" />
            回复
          </Button>
        )}
      </div>
    </div>
  );
};

export default PostsHeader;