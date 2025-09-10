import { useState } from 'react';
import { formatDistanceToNow } from 'date-fns';
import { zhCN } from 'date-fns/locale';
import { MessageSquare, Edit, Flag, MoreHorizontal, Pin, Lock, Eye } from 'lucide-react';

import { TopicDetail } from '@/types/api';
import { useAuth } from '@/hooks/useAuth';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';

interface TopicContentProps {
  topic: TopicDetail;
  onReply?: () => void;
}

const TopicContent = ({ topic, onReply }: TopicContentProps) => {
  const { user } = useAuth();
  // @ts-expect-error - 编辑功能开发中，暂时保留状态变量
  const [isEditing, setIsEditing] = useState(false);
  
  // 检查是否是作者
  const isAuthor = user?.id === topic.author.id;
  
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

  // 格式化数字显示
  const formatNumber = (num: number) => {
    if (num >= 1000) {
      return (num / 1000).toFixed(1) + 'K';
    }
    return num.toString();
  };
  
  // 处理编辑
  const handleEdit = () => {
    setIsEditing(true);
    // TODO: 实现编辑功能
  };
  
  // 处理举报
  const handleReport = () => {
    // TODO: 实现举报功能
  };
  
  // 渲染 Markdown 内容（临时用纯文本显示）
  const renderContent = (content: string) => {
    // TODO: 集成 Markdown 渲染器
    return (
      <div className="prose prose-sm max-w-none text-zinc-900 leading-relaxed">
        {content.split('\n').map((line, index) => (
          <p key={index} className="mb-3 last:mb-0">
            {line || '\u00A0'}
          </p>
        ))}
      </div>
    );
  };

  return (
    <div className="bg-white rounded-lg border border-zinc-200 mb-6">
      {/* 主题标题区 */}
      <div className="px-6 pt-6 pb-4 border-b border-zinc-100">
        {/* 主题标题和状态 */}
        <div className="flex items-start gap-3 mb-4">
          {topic.isPinned && (
            <Pin className="w-5 h-5 text-orange-500 mt-1 flex-shrink-0" />
          )}
          {topic.isLocked && (
            <Lock className="w-5 h-5 text-red-500 mt-1 flex-shrink-0" />
          )}
          <h1 className="text-xl font-bold text-zinc-900 leading-tight flex-1">
            {topic.title}
          </h1>
        </div>
        
        {/* 作者和发布信息 */}
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-full bg-zinc-200 flex items-center justify-center">
              {topic.author.avatarUrl ? (
                <img 
                  src={topic.author.avatarUrl} 
                  alt={topic.author.username}
                  className="w-10 h-10 rounded-full object-cover"
                />
              ) : (
                <span className="text-sm font-medium text-zinc-600">
                  {topic.author.username.charAt(0).toUpperCase()}
                </span>
              )}
            </div>
            <div>
              <div className="font-medium text-zinc-900">
                {topic.author.username}
              </div>
              <div className="text-sm text-zinc-500">
                发布于 {formatDate(topic.createdAt)}
                {topic.firstPost.isEdited && (
                  <span className="ml-2 text-zinc-400">(已编辑)</span>
                )}
              </div>
            </div>
          </div>
          
          {/* 操作菜单 */}
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="sm" className="h-8 w-8 p-0">
                <MoreHorizontal className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              {isAuthor && (
                <DropdownMenuItem onClick={handleEdit}>
                  <Edit className="mr-2 h-4 w-4" />
                  编辑主题
                </DropdownMenuItem>
              )}
              <DropdownMenuItem onClick={handleReport}>
                <Flag className="mr-2 h-4 w-4" />
                举报
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
        
        {/* 统计信息 */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-4 text-sm text-zinc-500">
            <div className="flex items-center gap-1">
              <MessageSquare className="w-4 h-4" />
              <span>{formatNumber(topic.replyCount)} 回复</span>
            </div>
            <div className="flex items-center gap-1">
              <Eye className="w-4 h-4" />
              <span>{formatNumber(topic.viewCount)} 浏览</span>
            </div>
          </div>
          
          {/* 分类和标签 */}
          <div className="flex items-center gap-3">
            <div className="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium bg-zinc-100 text-zinc-700">
              {topic.category.name}
            </div>
            {topic.tags.length > 0 && (
              <div className="flex flex-wrap gap-2">
                {topic.tags.map((tag) => (
                  <div 
                    key={tag.id}
                    className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium border"
                    style={{
                      backgroundColor: tag.color ? `${tag.color}20` : '#f4f4f5',
                      borderColor: tag.color || '#e4e4e7',
                      color: tag.color || '#52525b'
                    }}
                  >
                    {tag.name}
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>
      
      {/* 首帖内容 */}
      <div className="p-6">
        {topic.firstPost.contentMd ? (
          renderContent(topic.firstPost.contentMd)
        ) : (
          <div className="text-zinc-500 italic">
            此主题暂无内容
          </div>
        )}
        
        {/* 被提及的用户 */}
        {topic.firstPost.mentions && topic.firstPost.mentions.length > 0 && (
          <div className="mt-4 pt-4 border-t border-zinc-100">
            <div className="text-sm text-zinc-500 mb-2">
              提及了:
            </div>
            <div className="flex flex-wrap gap-2">
              {topic.firstPost.mentions.map((username) => (
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
      
      {/* 底部操作栏 */}
      <div className="flex items-center justify-between px-6 py-4 bg-zinc-50 rounded-b-lg border-t border-zinc-100">
        <div className="flex items-center gap-4">
          <span className="text-sm text-zinc-500">
            #1 楼 (主题)
          </span>
        </div>
        
        <div className="flex items-center gap-2">
          <Button 
            variant="outline" 
            size="sm"
            onClick={onReply}
            className="gap-2"
          >
            <MessageSquare className="h-4 w-4" />
            回复主题
          </Button>
        </div>
      </div>
    </div>
  );
};

export default TopicContent;