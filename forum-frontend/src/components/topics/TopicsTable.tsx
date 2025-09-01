import { useState } from 'react';
import { formatDistanceToNow } from 'date-fns';
import { zhCN } from 'date-fns/locale';
import { ChevronUp, ChevronDown, MessageSquare, Eye, Pin, Lock, ChevronsLeft, ChevronLeft, ChevronRight, ChevronsRight } from 'lucide-react';

import { Checkbox } from '@/components/ui/checkbox';
import { Topic } from '@/types/api';

interface TopicsTableProps {
  topics: Topic[];
  selectedTopics: string[];
  onTopicSelect: (topicId: string, selected: boolean) => void;
  onSelectAll: (selected: boolean) => void;
  onSort: (field: 'title' | 'author' | 'lastReply' | 'replies') => void;
  sortField?: 'title' | 'author' | 'lastReply' | 'replies' | null;
  sortDirection?: 'asc' | 'desc';
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
  sortField,
  sortDirection = 'asc',
  rowsPerPage,
  onRowsPerPageChange,
  totalPages,
  currentPage,
  onPageChange,
  totalRows,
  isLoading = false
}: TopicsTableProps) => {
  const handleSort = (field: 'title' | 'author' | 'lastReply' | 'replies') => {
    onSort(field);
  };

  const getSortIcon = (field: string) => {
    if (sortField !== field) {
      return <ChevronUp className="w-4 h-4 inline ml-2 opacity-30" />;
    }
    return sortDirection === 'asc' 
      ? <ChevronUp className="w-4 h-4 inline ml-2" />
      : <ChevronDown className="w-4 h-4 inline ml-2" />;
  };

  const formatDate = (dateString: string) => {
    try {
      const date = new Date(dateString);
      return date.toLocaleDateString('zh-CN', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit'
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
    <div className="content-stretch flex flex-col items-start justify-between relative size-full">
      {/* Table */}
      <div className="bg-white content-stretch flex flex-col items-start justify-start relative shrink-0 w-full border border-zinc-200 rounded-lg">
        {/* Header Row */}
        <div className="content-stretch flex h-12 items-center justify-start relative shrink-0 w-full border-b border-zinc-200">
          {/* Checkbox Column */}
          <div className="box-border content-stretch flex h-12 items-center justify-start px-4 py-0 relative shrink-0">
            <Checkbox
              checked={allSelected}
              onCheckedChange={(checked) => onSelectAll(!!checked)}
              className="size-4"
            />
          </div>
          
          {/* Category Column */}
          <div className="box-border content-stretch flex gap-2 h-12 items-center justify-start px-4 py-0 relative shrink-0">
            <div className="flex flex-col font-normal justify-center leading-[0] relative shrink-0 text-[14px] text-nowrap text-zinc-500">
              <p className="leading-[20px] whitespace-pre">Category</p>
            </div>
          </div>
          
          {/* Title Column */}
          <div 
            className={`basis-0 box-border content-stretch flex gap-2 grow h-12 items-center justify-start min-h-px min-w-px px-4 py-0 relative shrink-0 cursor-pointer hover:bg-zinc-50 ${sortField === 'title' ? 'bg-zinc-50' : ''}`}
            onClick={() => handleSort('title')}
          >
            <div className={`flex flex-col font-normal justify-center leading-[0] relative shrink-0 text-[14px] text-nowrap ${sortField === 'title' ? 'text-zinc-900 font-medium' : 'text-zinc-500'}`}>
              <p className="leading-[20px] whitespace-pre">Title</p>
            </div>
            {getSortIcon('title')}
          </div>
          
          {/* Author Column */}
          <div 
            className={`box-border content-stretch flex gap-2 h-12 items-center justify-start px-4 py-0 relative shrink-0 w-[118px] cursor-pointer hover:bg-zinc-50 ${sortField === 'author' ? 'bg-zinc-50' : ''}`}
            onClick={() => handleSort('author')}
          >
            <div className={`flex flex-col font-normal justify-center leading-[0] relative shrink-0 text-[14px] text-nowrap ${sortField === 'author' ? 'text-zinc-900 font-medium' : 'text-zinc-500'}`}>
              <p className="leading-[20px] whitespace-pre">Author</p>
            </div>
            {getSortIcon('author')}
          </div>
          
          {/* Last Reply Column */}
          <div 
            className={`box-border content-stretch flex gap-2 h-12 items-center justify-center px-4 py-0 relative shrink-0 w-[118px] cursor-pointer hover:bg-zinc-50 ${sortField === 'lastReply' ? 'bg-zinc-50' : ''}`}
            onClick={() => handleSort('lastReply')}
          >
            <div className={`flex flex-col font-normal justify-center leading-[0] relative shrink-0 text-[14px] text-nowrap text-center ${sortField === 'lastReply' ? 'text-zinc-900 font-medium' : 'text-zinc-500'}`}>
              <p className="leading-[20px] whitespace-pre">Last reply</p>
            </div>
            {getSortIcon('lastReply')}
          </div>
          
          {/* Reply/View Column */}
          <div 
            className={`box-border content-stretch flex gap-2 h-12 items-center justify-center px-4 py-0 relative shrink-0 w-[127px] cursor-pointer hover:bg-zinc-50 ${sortField === 'replies' ? 'bg-zinc-50' : ''}`}
            onClick={() => handleSort('replies')}
          >
            <div className={`flex flex-col font-normal justify-center leading-[0] relative shrink-0 text-[14px] text-nowrap ${sortField === 'replies' ? 'text-zinc-900 font-medium' : 'text-zinc-500'}`}>
              <p className="leading-[20px] whitespace-pre">Reply/View</p>
            </div>
            {getSortIcon('replies')}
          </div>
        </div>
        
        {/* Table Body */}
        <div className="content-stretch flex flex-col items-start justify-center relative shrink-0 w-full">
          {isLoading ? (
            <div className="box-border content-stretch flex items-center justify-center pl-0 pr-5 py-8 relative shrink-0 w-full border-b border-zinc-200">
              <div className="flex items-center justify-center space-x-2">
                <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-primary"></div>
                <span className="text-sm text-zinc-500">加载中...</span>
              </div>
            </div>
          ) : topics.length === 0 ? (
            <div className="box-border content-stretch flex items-center justify-center pl-0 pr-5 py-8 relative shrink-0 w-full border-b border-zinc-200">
              <span className="text-sm text-zinc-500">暂无主题</span>
            </div>
          ) : (
            topics.map((topic) => (
              <div 
                key={topic.id} 
                className="box-border content-stretch flex items-center justify-start pl-0 pr-5 py-0 relative shrink-0 w-full border-b border-zinc-200 hover:bg-zinc-50"
              >
                {/* Checkbox */}
                <div className="box-border content-stretch flex items-center justify-start p-[16px] relative shrink-0">
                  <Checkbox
                    checked={selectedTopics.includes(topic.id)}
                    onCheckedChange={(checked) => onTopicSelect(topic.id, !!checked)}
                    className="size-4"
                  />
                </div>
                
                {/* Category */}
                <div className="box-border content-stretch flex items-center justify-center p-[16px] relative shrink-0 w-[87px]">
                  <div className="flex flex-col font-normal justify-center leading-[0] relative shrink-0 text-[14px] text-nowrap text-zinc-950">
                    <p className="leading-[20px] whitespace-pre">{topic.category.name}</p>
                  </div>
                </div>
                
                {/* Title */}
                <div className="basis-0 box-border content-stretch flex flex-col gap-2 items-start justify-center p-[16px] relative grow shrink-0">
                  <div className="content-stretch flex gap-2 items-center justify-start relative shrink-0 w-full">
                    {topic.isPinned && (
                      <Pin className="relative shrink-0 size-4 text-orange-500" />
                    )}
                    {topic.isLocked && (
                      <Lock className="relative shrink-0 size-4 text-red-500" />
                    )}
                    <div className="basis-0 flex flex-col font-medium grow justify-center leading-[0] min-h-px min-w-px relative shrink-0 text-[14px] text-zinc-950 hover:text-blue-600 cursor-pointer">
                      <p className="leading-[20px]">{topic.title}</p>
                    </div>
                  </div>
                  
                  {/* Tags */}
                  {topic.tags.length > 0 && (
                    <div className="content-start flex flex-wrap gap-2 items-start justify-start relative shrink-0 w-full">
                      {topic.tags.map((tag) => (
                        <div 
                          key={tag.id}
                          className="box-border content-stretch flex items-center justify-center px-2.5 py-0.5 relative rounded-[9999px] shrink-0 border border-zinc-200"
                          style={{
                            backgroundColor: tag.color ? `${tag.color}20` : undefined,
                            borderColor: tag.color || undefined,
                          }}
                        >
                          <div 
                            className="flex flex-col font-semibold justify-center leading-[0] relative shrink-0 text-[12px] text-center text-nowrap"
                            style={{ color: tag.color || '#09090b' }}
                          >
                            <p className="leading-[20px] whitespace-pre">{tag.name}</p>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                  
                  {/* Description/Preview */}
                  <div className="flex flex-col font-normal justify-center leading-[0] relative shrink-0 text-[#8e8e8e] text-[12px] w-full">
                    <p className="leading-[20px] line-clamp-2">
                      Available from version 6000.3.0a, we are introducing the UnifiedRayTracing API, a new library that enables ray tracing workloads on GPUs without dedicated hardware acceleration...Read more
                    </p>
                  </div>
                </div>
                
                {/* Author */}
                <div className="box-border content-stretch flex flex-col items-center justify-center leading-[0] p-[16px] relative shrink-0 text-nowrap w-[118px]">
                  <div className="flex flex-col font-medium justify-center relative shrink-0 text-[14px] text-zinc-950">
                    <p className="leading-[20px] text-nowrap whitespace-pre">{topic.author.username}</p>
                  </div>
                  <div className="flex flex-col font-normal justify-center relative shrink-0 text-[#8e8e8e] text-[12px]">
                    <p className="leading-[20px] text-nowrap whitespace-pre">{formatDate(topic.createdAt)}</p>
                  </div>
                </div>
                
                {/* Last Reply */}
                <div className="box-border content-stretch flex flex-col items-center justify-center leading-[0] p-[16px] relative shrink-0 text-nowrap w-[118px]">
                  {topic.lastPostedAt && topic.lastPoster ? (
                    <>
                      <div className="flex flex-col font-medium justify-center relative shrink-0 text-[14px] text-zinc-950">
                        <p className="leading-[20px] text-nowrap whitespace-pre">{topic.lastPoster.username}</p>
                      </div>
                      <div className="flex flex-col font-normal justify-center relative shrink-0 text-[#8e8e8e] text-[12px]">
                        <p className="leading-[20px] text-nowrap whitespace-pre">{formatDate(topic.lastPostedAt)}</p>
                      </div>
                    </>
                  ) : (
                    <div className="flex flex-col font-normal justify-center relative shrink-0 text-[#8e8e8e] text-[12px]">
                      <p className="leading-[20px] text-nowrap whitespace-pre">无回复</p>
                    </div>
                  )}
                </div>
                
                {/* Reply/View Stats */}
                <div className="box-border content-stretch flex flex-col items-center justify-center pl-8 pr-4 py-4 relative shrink-0 w-[127px]">
                  <div className="content-stretch flex gap-1 items-center justify-start relative shrink-0">
                    <MessageSquare className="relative shrink-0 size-4 text-zinc-500" />
                    <div className="flex flex-col font-normal justify-center leading-[0] relative shrink-0 text-[14px] text-nowrap text-right text-zinc-950">
                      <p className="leading-[20px] whitespace-pre">{formatNumber(topic.replyCount)}</p>
                    </div>
                  </div>
                  <div className="content-stretch flex gap-1 items-center justify-start relative shrink-0">
                    <Eye className="relative shrink-0 size-4 text-zinc-500" />
                    <div className="flex flex-col font-normal justify-center leading-[0] relative shrink-0 text-[14px] text-nowrap text-right text-zinc-950">
                      <p className="leading-[20px] whitespace-pre">{formatNumber(topic.viewCount)}</p>
                    </div>
                  </div>
                </div>
              </div>
            ))
          )}
        </div>
      </div>
      
      {/* Bottom Pagination */}
      <div className="box-border content-stretch flex items-center justify-between p-[16px] relative shrink-0 w-full">
        <div className="flex flex-col font-normal justify-center leading-[0] relative shrink-0 text-[14px] text-center text-nowrap text-zinc-500">
          <p className="leading-[20px] whitespace-pre">{selectedTopics.length} of {totalRows} row(s) selected.</p>
        </div>
        <div className="content-stretch flex gap-6 items-center justify-start relative shrink-0">
          {/* Rows per page */}
          <div className="content-stretch flex gap-2 items-center justify-start relative shrink-0">
            <div className="flex flex-col font-normal justify-center leading-[0] relative shrink-0 text-[14px] text-center text-nowrap text-zinc-950">
              <p className="leading-[20px] whitespace-pre">Rows per page</p>
            </div>
            <div className="content-stretch flex flex-col gap-2 items-start justify-start relative shrink-0 w-[70px]">
              <select 
                className="bg-white box-border content-stretch flex h-10 items-center justify-center px-3 py-2 relative rounded-[6px] shrink-0 w-full border border-zinc-200 text-[14px] text-zinc-900"
                value={rowsPerPage}
                onChange={(e) => onRowsPerPageChange(parseInt(e.target.value))}
              >
                <option value={10}>10</option>
                <option value={25}>25</option>
                <option value={50}>50</option>
                <option value={100}>100</option>
              </select>
            </div>
          </div>
          
          {/* Page info */}
          <div className="flex flex-col font-normal justify-center leading-[0] relative shrink-0 text-[14px] text-center text-nowrap text-zinc-950">
            <p className="leading-[20px] whitespace-pre">Page {currentPage} of {totalPages}</p>
          </div>
          
          {/* Navigation buttons */}
          <div className="content-stretch flex gap-2 items-center justify-start relative shrink-0">
            <button
              className={`bg-white box-border content-stretch flex items-center justify-center px-4 py-2 relative rounded-[6px] shrink-0 size-10 border border-zinc-200 shadow-[0px_1px_3px_0px_rgba(0,0,0,0.05)] ${currentPage === 1 ? 'opacity-50 cursor-not-allowed' : 'hover:bg-zinc-50'}`}
              onClick={() => onPageChange(1)}
              disabled={currentPage === 1}
            >
              <ChevronsLeft className="size-6 text-zinc-700" />
            </button>
            <button
              className={`bg-white box-border content-stretch flex items-center justify-center px-4 py-2 relative rounded-[6px] shrink-0 size-10 border border-zinc-200 shadow-[0px_1px_3px_0px_rgba(0,0,0,0.05)] ${currentPage === 1 ? 'opacity-50 cursor-not-allowed' : 'hover:bg-zinc-50'}`}
              onClick={() => onPageChange(currentPage - 1)}
              disabled={currentPage === 1}
            >
              <ChevronLeft className="size-6 text-zinc-700" />
            </button>
            <button
              className={`bg-white box-border content-stretch flex items-center justify-center px-4 py-2 relative rounded-[6px] shrink-0 size-10 border border-zinc-200 shadow-[0px_1px_3px_0px_rgba(0,0,0,0.05)] ${currentPage === totalPages ? 'opacity-50 cursor-not-allowed' : 'hover:bg-zinc-50'}`}
              onClick={() => onPageChange(currentPage + 1)}
              disabled={currentPage === totalPages}
            >
              <ChevronRight className="size-6 text-zinc-700" />
            </button>
            <button
              className={`bg-white box-border content-stretch flex items-center justify-center px-4 py-2 relative rounded-[6px] shrink-0 size-10 border border-zinc-200 shadow-[0px_1px_3px_0px_rgba(0,0,0,0.05)] ${currentPage === totalPages ? 'opacity-50 cursor-not-allowed' : 'hover:bg-zinc-50'}`}
              onClick={() => onPageChange(totalPages)}
              disabled={currentPage === totalPages}
            >
              <ChevronsRight className="size-6 text-zinc-700" />
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default TopicsTable;