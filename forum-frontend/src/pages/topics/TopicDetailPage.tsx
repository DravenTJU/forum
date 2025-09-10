import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { toast } from 'sonner';

import SearchHeader from '@/components/layout/SearchHeader';
import CategorySidebar from '@/components/layout/CategorySidebar';
import TopicContent from '@/components/topics/TopicContent';
import PostsList from '@/components/topics/PostsList';
import { LoadingSpinner } from '@/components/ui/loading-spinner';
import { useAuth } from '@/hooks/useAuth';
import { useTopicDetail } from '@/hooks/useTopicDetail';
import { useCategories, useTags } from '@/hooks/useTopics';

export function TopicDetailPage() {
  const { topicId } = useParams<{ topicId: string; slug?: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  
  // 搜索相关状态
  const [searchQuery, setSearchQuery] = useState('');
  
  // 侧边栏筛选状态（与首页保持一致）
  const [selectedCategory, setSelectedCategory] = useState<string>('');
  const [selectedTags, setSelectedTags] = useState<string[]>([]);
  
  // 数据获取
  const { data: topic, isLoading, isError, error } = useTopicDetail(topicId!);
  const { data: categories = [] } = useCategories();
  const { data: tags = [] } = useTags();
  
  // 搜索事件处理
  const handleSearchChange = (query: string) => {
    setSearchQuery(query);
  };
  
  const handleSearchSubmit = () => {
    if (searchQuery.trim()) {
      toast.info(`搜索功能开发中: "${searchQuery}"`);
    }
  };
  
  // 侧边栏事件处理（与首页保持一致）
  const handleCategorySelect = (categoryId?: string) => {
    setSelectedCategory(categoryId || '');
    // 可以根据分类筛选跳转到首页
    if (categoryId) {
      navigate(`/?category=${categoryId}`);
    } else {
      navigate('/');
    }
  };
  
  const handleTagToggle = (tagSlug: string) => {
    setSelectedTags(prev => 
      prev.includes(tagSlug) 
        ? prev.filter(tag => tag !== tagSlug)
        : [...prev, tagSlug]
    );
    // 根据标签筛选跳转到首页
    navigate(`/?tag=${tagSlug}`);
  };
  
  const handleReply = () => {
    if (!user) {
      toast.error('请先登录后再回复');
      navigate('/login');
      return;
    }
    toast.info('回复功能开发中');
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

  // 加载状态
  if (isLoading) {
    return (
      <div className="flex flex-col h-screen">
        <SearchHeader
          searchQuery={searchQuery}
          onSearchChange={handleSearchChange}
          onSearchSubmit={handleSearchSubmit}
        />
        <div className="flex flex-1">
          <div className="hidden md:flex">
            {sidebarContent}
          </div>
          <main className="flex-1 flex items-center justify-center">
            <LoadingSpinner size="large" />
          </main>
        </div>
      </div>
    );
  }

  // 错误状态
  if (isError || !topic) {
    return (
      <div className="flex flex-col h-screen">
        <SearchHeader
          searchQuery={searchQuery}
          onSearchChange={handleSearchChange}
          onSearchSubmit={handleSearchSubmit}
        />
        <div className="flex flex-1">
          <div className="hidden md:flex">
            {sidebarContent}
          </div>
          <main className="flex-1 flex items-center justify-center">
            <div className="text-center py-12">
              <h2 className="text-xl font-semibold mb-2">主题不存在或已删除</h2>
              <p className="text-muted-foreground mb-4">
                {error?.message || '该主题可能已被删除或您没有访问权限'}
              </p>
              <button 
                onClick={() => navigate('/')}
                className="px-4 py-2 bg-primary text-primary-foreground rounded-md hover:bg-primary/90"
              >
                返回首页
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
      />
      
      {/* 主体布局 */}
      <div className="flex flex-1 overflow-hidden">
        {/* 左侧边栏 - 桌面端 */}
        <div className="hidden md:flex">
          {sidebarContent}
        </div>
        
        {/* 主内容区 */}
        <main className="flex-1 flex flex-col overflow-hidden">
          {/* 主内容容器 */}
          <div className="flex-1 overflow-auto p-6">
            {/* 主题标题和首帖合并区域 */}
            <TopicContent 
              topic={topic}
              onReply={handleReply}
            />
            
            {/* 回帖列表 */}
            <PostsList 
              topicId={topic.id}
              isTopicLocked={topic.isLocked}
              onReply={handleReply}
            />
          </div>
        </main>
      </div>
    </div>
  );
}