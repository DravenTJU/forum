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
  
  // 获取当前页的主题数据
  const allTopics = topicsData?.pages.flatMap(page => page.topics) ?? [];
  const totalTopics = allTopics.length;
  const totalPages = Math.ceil(totalTopics / rowsPerPage);
  const startIndex = (currentPage - 1) * rowsPerPage;
  const currentTopics = allTopics.slice(startIndex, startIndex + rowsPerPage);
  
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
    // TODO: 实现表格排序
    toast.info(`表格排序功能开发中: ${field}`);
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