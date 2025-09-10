import { useState } from 'react';
import { formatDistanceToNow } from 'date-fns';
import { zhCN } from 'date-fns/locale';
import { Edit, Flag, MoreHorizontal, Quote, ChevronUp, Link } from 'lucide-react';

import { Post } from '@/types/api';
import { useAuth } from '@/hooks/useAuth';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';

interface PostCardProps {
  post: Post;
  floorNumber: number;
  onReply?: () => void;
}

const PostCard = ({ post, floorNumber, onReply }: PostCardProps) => {
  const { user } = useAuth();
  const [isCollapsed, setIsCollapsed] = useState(true);
  
  // 检查是否是作者
  const isAuthor = user?.id === post.author.id;
  
  // 格式化时间
  const formatDate = (dateString: string) => {
    try {
      const date = new Date(dateString);
      return formatDistanceToNow(date, { 
        locale: zhCN, 
        addSuffix: true 
      });
    } catch {
      return dateString;
    }
  };
  
  // 处理编辑
  const handleEdit = () => {
    // TODO: 实现编辑功能
  };
  
  // 处理举报
  const handleReport = () => {
    // TODO: 实现举报功能
  };
  
  // 处理引用回复
  const handleQuoteReply = () => {
    // TODO: 实现引用回复功能
    onReply?.();
  };
  
  // 处理楼层链接复制
  const handleCopyFloorLink = () => {
    const url = `${window.location.href}#post-${floorNumber}`;
    navigator.clipboard.writeText(url);
    // TODO: 显示复制成功提示
  };
  
  // 切换展开/收起
  const handleToggleCollapse = () => {
    setIsCollapsed(!isCollapsed);
  };
  
  // 渲染 Markdown 内容（临时用纯文本显示）
  const renderContent = (content: string) => {
    const lines = content.split('\n');
    const maxPreviewLines = 3;
    
    if (isCollapsed && lines.length > maxPreviewLines) {
      const previewContent = lines.slice(0, maxPreviewLines).join('\n');
      return (
        <div className="prose prose-sm max-w-none text-zinc-900 leading-relaxed">
          {previewContent.split('\n').map((line, index) => (
            <p key={index} className="mb-2 last:mb-0">
              {line || '\u00A0'}
            </p>
          ))}
          <button
            onClick={handleToggleCollapse}
            className="text-blue-600 hover:text-blue-700 text-sm font-medium mt-2"
          >
            显示全部...
          </button>
        </div>
      );
    }
    
    return (
      <div className="prose prose-sm max-w-none text-zinc-900 leading-relaxed">
        {lines.map((line, index) => (
          <p key={index} className="mb-3 last:mb-0">
            {line || '\u00A0'}
          </p>
        ))}
        {lines.length > maxPreviewLines && !isCollapsed && (
          <button
            onClick={handleToggleCollapse}
            className="text-blue-600 hover:text-blue-700 text-sm font-medium mt-2 flex items-center gap-1"
          >
            <ChevronUp className="w-4 h-4" />
            收起
          </button>
        )}
      </div>
    );
  };

  return (
    <div 
      id={`post-${floorNumber}`}
      className="bg-white rounded-lg border border-zinc-200 hover:shadow-sm transition-all"
    >
      {/* 帖子头部 */}
      <div className="flex items-start justify-between px-4 py-3 border-b border-zinc-100">
        <div className="flex items-start gap-3 flex-1">
          {/* 作者头像 */}
          <div className="w-8 h-8 rounded-full bg-zinc-200 flex items-center justify-center flex-shrink-0">
            {post.author.avatarUrl ? (
              <img 
                src={post.author.avatarUrl} 
                alt={post.author.username}
                className="w-8 h-8 rounded-full object-cover"
              />
            ) : (
              <span className="text-xs font-medium text-zinc-600">
                {post.author.username.charAt(0).toUpperCase()}
              </span>
            )}
          </div>
          
          {/* 作者信息和楼层 */}
          <div className="flex-1 min-w-0">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <h4 className="font-medium text-zinc-900 text-sm truncate">
                  {post.author.username}
                </h4>
                <span className="text-xs px-1.5 py-0.5 bg-zinc-100 text-zinc-600 rounded flex-shrink-0">
                  #{floorNumber}
                </span>
              </div>
              <div className="text-xs text-zinc-500">
                {formatDate(post.createdAt)}
                {post.isEdited && (
                  <span className="ml-1 text-zinc-400">(已编辑)</span>
                )}
              </div>
            </div>
          </div>
        </div>
        
        {/* 操作菜单 */}
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="sm" className="h-7 w-7 p-0 text-zinc-400 hover:text-zinc-600">
              <MoreHorizontal className="h-4 w-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onClick={handleQuoteReply}>
              <Quote className="mr-2 h-4 w-4" />
              引用回复
            </DropdownMenuItem>
            <DropdownMenuItem onClick={handleCopyFloorLink}>
              <Link className="mr-2 h-4 w-4" />
              复制链接
            </DropdownMenuItem>
            {isAuthor && (
              <DropdownMenuItem onClick={handleEdit}>
                <Edit className="mr-2 h-4 w-4" />
                编辑
              </DropdownMenuItem>
            )}
            <DropdownMenuItem onClick={handleReport}>
              <Flag className="mr-2 h-4 w-4" />
              举报
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
      
      {/* 引用的帖子 */}
      {post.replyToPost && (
        <div className="mx-4 mt-4 p-3 bg-zinc-50 border-l-4 border-zinc-300 rounded-r">
          <div className="text-sm">
            <span className="font-medium text-zinc-700">
              @{post.replyToPost.author}:
            </span>
            <span className="text-zinc-600 ml-2">
              {post.replyToPost.excerpt}
            </span>
          </div>
        </div>
      )}
      
      {/* 帖子内容 */}
      <div className="p-4">
        {post.contentMd ? (
          renderContent(post.contentMd)
        ) : (
          <div className="text-zinc-500 italic">
            此回帖暂无内容
          </div>
        )}
        
        {/* 被提及的用户 */}
        {post.mentions && post.mentions.length > 0 && (
          <div className="mt-4 pt-4 border-t border-zinc-100">
            <div className="text-sm text-zinc-500 mb-2">
              提及了:
            </div>
            <div className="flex flex-wrap gap-2">
              {post.mentions.map((username) => (
                <span 
                  key={username}
                  className="text-sm px-2 py-1 bg-zinc-100 text-zinc-700 rounded"
                >
                  @{username}
                </span>
              ))}
            </div>
          </div>
        )}
      </div>
      
      {/* 底部操作栏 - 简化版 */}
      <div className="flex items-center justify-end px-4 py-2 border-t border-zinc-100 bg-zinc-50 rounded-b-lg">
        <div className="flex items-center gap-2">
          <Button 
            variant="ghost" 
            size="sm"
            onClick={handleQuoteReply}
            className="h-7 text-xs text-zinc-600 hover:text-zinc-900"
          >
            引用
          </Button>
          
          <Button 
            variant="ghost" 
            size="sm"
            onClick={onReply}
            className="h-7 text-xs text-blue-600 hover:text-blue-700"
          >
            回复
          </Button>
        </div>
      </div>
    </div>
  );
};

export default PostCard;