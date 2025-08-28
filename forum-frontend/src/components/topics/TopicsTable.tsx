import { useState } from 'react';
import { formatDistanceToNow } from 'date-fns';
import { zhCN } from 'date-fns/locale';
import { ChevronUp, ChevronDown, MessageSquare, Eye, Clock } from 'lucide-react';

import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Checkbox } from '@/components/ui/checkbox';
import { Topic } from '@/types/api';

interface TopicsTableProps {
  topics: Topic[];
  selectedTopics: string[];
  onTopicSelect: (topicId: string, selected: boolean) => void;
  onSelectAll: (selected: boolean) => void;
  onSort: (field: 'title' | 'author' | 'lastReply' | 'replies') => void;
  rowsPerPage: number;
  onRowsPerPageChange: (rows: number) => void;
  totalPages: number;
  currentPage: number;
  onPageChange: (page: number) => void;
  totalRows: number;
  isLoading?: boolean;
}

const TopicsTable = ({
  topics,
  selectedTopics,
  onTopicSelect,
  onSelectAll,
  onSort,
  rowsPerPage,
  onRowsPerPageChange,
  totalPages,
  currentPage,
  onPageChange,
  totalRows,
  isLoading = false
}: TopicsTableProps) => {
  const [sortConfig, setSortConfig] = useState<{
    field: string;
    direction: 'asc' | 'desc';
  } | null>(null);

  const handleSort = (field: 'title' | 'author' | 'lastReply' | 'replies') => {
    const direction = 
      sortConfig?.field === field && sortConfig.direction === 'asc' 
        ? 'desc' 
        : 'asc';
    
    setSortConfig({ field, direction });
    onSort(field);
  };

  const getSortIcon = (field: string) => {
    if (sortConfig?.field !== field) return null;
    return sortConfig.direction === 'asc' 
      ? <ChevronUp className="w-4 h-4 inline ml-1" />
      : <ChevronDown className="w-4 h-4 inline ml-1" />;
  };

  const formatDate = (dateString: string) => {
    try {
      return formatDistanceToNow(new Date(dateString), {
        addSuffix: true,
        locale: zhCN,
      });
    } catch {
      return dateString;
    }
  };

  const formatNumber = (num: number) => {
    if (num >= 1000) {
      return (num / 1000).toFixed(1) + 'K';
    }
    return num.toString();
  };

  const allSelected = topics.length > 0 && selectedTopics.length === topics.length;

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div className="text-sm text-muted-foreground">
          {totalRows > 0 && (
            <>
              显示 {Math.min((currentPage - 1) * rowsPerPage + 1, totalRows)} 到{' '}
              {Math.min(currentPage * rowsPerPage, totalRows)} 条，共 {totalRows} 条记录
            </>
          )}
        </div>
        <div className="flex items-center space-x-2">
          <span className="text-sm text-muted-foreground">每页显示:</span>
          <Select
            value={rowsPerPage.toString()}
            onValueChange={(value) => onRowsPerPageChange(parseInt(value))}
          >
            <SelectTrigger className="w-20">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="10">10</SelectItem>
              <SelectItem value="25">25</SelectItem>
              <SelectItem value="50">50</SelectItem>
              <SelectItem value="100">100</SelectItem>
            </SelectContent>
          </Select>
        </div>
      </div>

      <div className="border rounded-lg">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="w-12">
                <Checkbox
                  checked={allSelected}
                  onCheckedChange={(checked) => onSelectAll(!!checked)}
                />
              </TableHead>
              <TableHead>分类</TableHead>
              <TableHead 
                className="cursor-pointer hover:bg-muted/50"
                onClick={() => handleSort('title')}
              >
                标题 {getSortIcon('title')}
              </TableHead>
              <TableHead 
                className="cursor-pointer hover:bg-muted/50"
                onClick={() => handleSort('author')}
              >
                作者 {getSortIcon('author')}
              </TableHead>
              <TableHead 
                className="cursor-pointer hover:bg-muted/50"
                onClick={() => handleSort('lastReply')}
              >
                最后回复 {getSortIcon('lastReply')}
              </TableHead>
              <TableHead 
                className="cursor-pointer hover:bg-muted/50"
                onClick={() => handleSort('replies')}
              >
                回复/查看 {getSortIcon('replies')}
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableRow>
                <TableCell colSpan={6} className="text-center py-8">
                  <div className="flex items-center justify-center space-x-2">
                    <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-primary"></div>
                    <span>加载中...</span>
                  </div>
                </TableCell>
              </TableRow>
            ) : topics.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} className="text-center py-8 text-muted-foreground">
                  暂无主题
                </TableCell>
              </TableRow>
            ) : (
              topics.map((topic) => (
                <TableRow key={topic.id} className="hover:bg-muted/50">
                  <TableCell>
                    <Checkbox
                      checked={selectedTopics.includes(topic.id)}
                      onCheckedChange={(checked) => onTopicSelect(topic.id, !!checked)}
                    />
                  </TableCell>
                  
                  <TableCell>
                    <Badge 
                      variant="secondary" 
                      className="font-medium"
                    >
                      {topic.category.name}
                    </Badge>
                  </TableCell>
                  
                  <TableCell className="max-w-md">
                    <div className="space-y-1">
                      <div className="flex items-start space-x-2">
                        {topic.isPinned && (
                          <Badge variant="outline" className="text-xs">置顶</Badge>
                        )}
                        {topic.isLocked && (
                          <Badge variant="outline" className="text-xs">锁定</Badge>
                        )}
                        <h3 className="font-medium leading-none hover:text-primary cursor-pointer">
                          {topic.title}
                        </h3>
                      </div>
                      {topic.tags.length > 0 && (
                        <div className="flex flex-wrap gap-1">
                          {topic.tags.map((tag) => (
                            <Badge
                              key={tag.id}
                              variant="outline"
                              className="text-xs"
                              style={{
                                backgroundColor: tag.color ? `${tag.color}20` : undefined,
                                borderColor: tag.color || undefined,
                                color: tag.color || undefined,
                              }}
                            >
                              {tag.name}
                            </Badge>
                          ))}
                        </div>
                      )}
                    </div>
                  </TableCell>
                  
                  <TableCell>
                    <div className="flex items-center space-x-2">
                      <Avatar className="w-6 h-6">
                        <AvatarImage src={topic.author.avatarUrl} />
                        <AvatarFallback className="text-xs">
                          {topic.author.username.charAt(0).toUpperCase()}
                        </AvatarFallback>
                      </Avatar>
                      <span className="text-sm font-medium">{topic.author.username}</span>
                    </div>
                    <div className="text-xs text-muted-foreground mt-1">
                      <Clock className="w-3 h-3 inline mr-1" />
                      {formatDate(topic.createdAt)}
                    </div>
                  </TableCell>
                  
                  <TableCell>
                    {topic.lastPostedAt && topic.lastPoster ? (
                      <div className="space-y-1">
                        <div className="flex items-center space-x-2">
                          <Avatar className="w-5 h-5">
                            <AvatarFallback className="text-xs">
                              {topic.lastPoster.username.charAt(0).toUpperCase()}
                            </AvatarFallback>
                          </Avatar>
                          <span className="text-sm">{topic.lastPoster.username}</span>
                        </div>
                        <div className="text-xs text-muted-foreground">
                          {formatDate(topic.lastPostedAt)}
                        </div>
                      </div>
                    ) : (
                      <span className="text-sm text-muted-foreground">无回复</span>
                    )}
                  </TableCell>
                  
                  <TableCell>
                    <div className="flex items-center space-x-4 text-sm">
                      <div className="flex items-center space-x-1">
                        <MessageSquare className="w-4 h-4 text-muted-foreground" />
                        <span className="font-medium">{formatNumber(topic.replyCount)}</span>
                      </div>
                      <div className="flex items-center space-x-1">
                        <Eye className="w-4 h-4 text-muted-foreground" />
                        <span>{formatNumber(topic.viewCount)}</span>
                      </div>
                    </div>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {/* 分页控制 */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between">
          <div className="text-sm text-muted-foreground">
            第 {currentPage} 页，共 {totalPages} 页
          </div>
          <div className="flex items-center space-x-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => onPageChange(1)}
              disabled={currentPage === 1}
            >
              首页
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => onPageChange(currentPage - 1)}
              disabled={currentPage === 1}
            >
              上一页
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => onPageChange(currentPage + 1)}
              disabled={currentPage === totalPages}
            >
              下一页
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => onPageChange(totalPages)}
              disabled={currentPage === totalPages}
            >
              末页
            </Button>
          </div>
        </div>
      )}
    </div>
  );
};

export default TopicsTable;