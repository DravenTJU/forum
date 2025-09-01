import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import SearchHeader from '@/components/layout/SearchHeader';
import CategorySidebar from '@/components/layout/CategorySidebar';
import TopicFilters from '@/components/filters/TopicFilters';
import TopicsTable from '@/components/topics/TopicsTable';
import { useAuth } from '@/hooks/useAuth';
import { useTopics, useCategories, useTags } from '@/hooks/useTopics';

export function HomePage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  
  // 筛选和搜索状态
  const [selectedCategory, setSelectedCategory] = useState<string>('');
  const [selectedTags, setSelectedTags] = useState<string[]>([]);
  const [sortBy, setSortBy] = useState('latest');
  const [searchQuery, setSearchQuery] = useState('');
  
  // 表格状态
  const [selectedTopics, setSelectedTopics] = useState<string[]>([]);
  const [currentPage, setCurrentPage] = useState(1);
  const [rowsPerPage, setRowsPerPage] = useState(20);
  const [tableSortField, setTableSortField] = useState<'title' | 'author' | 'lastReply' | 'replies' | null>(null);
  const [tableSortDirection, setTableSortDirection] = useState<'asc' | 'desc'>('asc');

  // 构建查询参数
  const queryParams = {
    categoryId: selectedCategory || undefined,
    tagSlugs: selectedTags.length > 0 ? selectedTags : undefined,
    sort: sortBy as 'latest' | 'hot' | 'top' | 'new',
    limit: rowsPerPage,
  };
  
  // 数据查询
  const { data: topicsData, isLoading: topicsLoading, isError: topicsError } = useTopics(queryParams);
  const { data: categories = [], isLoading: categoriesLoading } = useCategories();
  const { data: tags = [], isLoading: tagsLoading } = useTags();
  
  // 获取并排序主题数据
  const allTopics = topicsData?.pages.flatMap(page => page.topics) ?? [];
  
  // 应用表格排序
  const sortedTopics = tableSortField ? [...allTopics].sort((a, b) => {
    let aValue, bValue;
    
    switch (tableSortField) {
      case 'title':
        aValue = a.title.toLowerCase();
        bValue = b.title.toLowerCase();
        break;
      case 'author':
        aValue = a.author.username.toLowerCase();
        bValue = b.author.username.toLowerCase();
        break;
      case 'lastReply':
        aValue = a.lastPostedAt ? new Date(a.lastPostedAt).getTime() : new Date(a.createdAt).getTime();
        bValue = b.lastPostedAt ? new Date(b.lastPostedAt).getTime() : new Date(b.createdAt).getTime();
        break;
      case 'replies':
        // 主要按回复数排序，如果相同则按浏览数排序
        aValue = a.replyCount * 1000000 + a.viewCount;
        bValue = b.replyCount * 1000000 + b.viewCount;
        break;
      default:
        return 0;
    }
    
    // 确保比较逻辑正确
    const comparison = aValue < bValue ? -1 : aValue > bValue ? 1 : 0;
    return tableSortDirection === 'asc' ? comparison : -comparison;
  }) : allTopics;
  
  const totalTopics = sortedTopics.length;
  const totalPages = Math.ceil(totalTopics / rowsPerPage);
  const startIndex = (currentPage - 1) * rowsPerPage;
  const currentTopics = sortedTopics.slice(startIndex, startIndex + rowsPerPage);
  
  const isLoading = topicsLoading || categoriesLoading || tagsLoading;

  // 事件处理函数
  const handleCategorySelect = (categoryId?: string) => {
    setSelectedCategory(categoryId || '');
    setCurrentPage(1); // 重置到第一页
  };
  
  const handleTagToggle = (tagSlug: string) => {
    setSelectedTags(prev => 
      prev.includes(tagSlug) 
        ? prev.filter(tag => tag !== tagSlug)
        : [...prev, tagSlug]
    );
    setCurrentPage(1); // 重置到第一页
  };
  
  const handleSortChange = (sort: string) => {
    setSortBy(sort);
    setCurrentPage(1); // 重置到第一页
  };
  
  const handleClearFilters = () => {
    setSelectedCategory('');
    setSelectedTags([]);
    setSortBy('latest');
    setSearchQuery('');
    setCurrentPage(1);
  };
  
  const handleSearchChange = (query: string) => {
    setSearchQuery(query);
  };
  
  const handleSearchSubmit = () => {
    if (searchQuery.trim()) {
      // TODO: 实现搜索功能
      toast.info(`搜索功能开发中: "${searchQuery}"`);
    }
  };
  
  const handleNewTopic = () => {
    if (!user) {
      toast.error('请先登录后再发布主题');
      navigate('/login');
      return;
    }
    // TODO: 导航到发布主题页面
    toast.info('发布主题功能开发中');
  };
  
  // 表格选择处理
  const handleTopicSelect = (topicId: string, selected: boolean) => {
    if (selected) {
      setSelectedTopics(prev => [...prev, topicId]);
    } else {
      setSelectedTopics(prev => prev.filter(id => id !== topicId));
    }
  };
  
  const handleSelectAll = (selected: boolean) => {
    if (selected) {
      setSelectedTopics(currentTopics.map(topic => topic.id));
    } else {
      setSelectedTopics([]);
    }
  };
  
  const handleTableSort = (field: 'title' | 'author' | 'lastReply' | 'replies') => {
    if (tableSortField === field) {
      // 如果点击的是当前排序字段，切换排序方向
      setTableSortDirection(prev => prev === 'asc' ? 'desc' : 'asc');
    } else {
      // 如果点击的是新字段，设置为升序排序
      setTableSortField(field);
      setTableSortDirection('asc');
    }
    setCurrentPage(1); // 重置到第一页
  };
  
  const handlePageChange = (page: number) => {
    setCurrentPage(page);
  };
  
  const handleRowsPerPageChange = (rows: number) => {
    setRowsPerPage(rows);
    setCurrentPage(1); // 重置到第一页
  };
  
  // 侧边栏内容
  const sidebarContent = (
    <CategorySidebar
      categories={categories}
      tags={tags}
      selectedCategory={selectedCategory}
      selectedTags={selectedTags}
      onCategorySelect={handleCategorySelect}
      onTagToggle={handleTagToggle}
    />
  );

  if (topicsError) {
    return (
      <div className="flex flex-col h-screen">
        <SearchHeader
          searchQuery={searchQuery}
          onSearchChange={handleSearchChange}
          onSearchSubmit={handleSearchSubmit}
          onNewTopicClick={handleNewTopic}
          sidebarContent={sidebarContent}
        />
        <div className="flex flex-1">
          <div className="hidden md:flex">
            {sidebarContent}
          </div>
          <main className="flex-1 p-6">
            <div className="text-center py-12">
              <h2 className="text-xl font-semibold mb-2">数据加载失败</h2>
              <p className="text-muted-foreground mb-4">请检查网络连接或稍后重试</p>
              <button 
                onClick={() => window.location.reload()}
                className="px-4 py-2 bg-primary text-primary-foreground rounded-md hover:bg-primary/90"
              >
                重新加载
              </button>
            </div>
          </main>
        </div>
      </div>
    );
  }

  return (
    <div className="flex flex-col h-screen">
      {/* 顶部导航 */}
      <SearchHeader
        searchQuery={searchQuery}
        onSearchChange={handleSearchChange}
        onSearchSubmit={handleSearchSubmit}
        onNewTopicClick={handleNewTopic}
        sidebarContent={sidebarContent}
      />
      
      {/* 主体布局 */}
      <div className="flex flex-1 overflow-hidden">
        {/* 左侧边栏 - 桌面端 */}
        <div className="hidden md:flex">
          {sidebarContent}
        </div>
        
        {/* 主内容区 */}
        <main className="flex-1 flex flex-col overflow-hidden">
          {/* 筛选器 */}
          <TopicFilters
            categories={categories}
            tags={tags}
            selectedCategory={selectedCategory}
            selectedTags={selectedTags}
            sortBy={sortBy}
            onCategoryChange={handleCategorySelect}
            onTagToggle={handleTagToggle}
            onSortChange={handleSortChange}
            onClearFilters={handleClearFilters}
            onNewTopicClick={handleNewTopic}
          />
          
          {/* 主题表格容器 */}
          <div className="flex-1 overflow-auto p-6">
            <TopicsTable
              topics={currentTopics}
              selectedTopics={selectedTopics}
              onTopicSelect={handleTopicSelect}
              onSelectAll={handleSelectAll}
              onSort={handleTableSort}
              sortField={tableSortField}
              sortDirection={tableSortDirection}
              rowsPerPage={rowsPerPage}
              onRowsPerPageChange={handleRowsPerPageChange}
              totalPages={totalPages}
              currentPage={currentPage}
              onPageChange={handlePageChange}
              totalRows={totalTopics}
              isLoading={isLoading}
            />
          </div>
        </main>
      </div>
    </div>
  );
}