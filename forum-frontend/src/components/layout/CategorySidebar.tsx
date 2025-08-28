import { useState } from 'react';
import { 
  Clock, 
  TrendingUp, 
  Bookmark, 
  ChevronDown, 
  ChevronRight,
  Hash,
  Folder,
  MessageSquare
} from 'lucide-react';

import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Separator } from '@/components/ui/separator';
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from '@/components/ui/collapsible';
import { cn } from '@/lib/utils';
import { Category, Tag } from '@/types/api';

interface CategorySidebarProps {
  categories: Category[];
  tags: Tag[];
  selectedCategory?: string;
  selectedTags: string[];
  onCategorySelect: (categoryId?: string) => void;
  onTagToggle: (tagSlug: string) => void;
  className?: string;
}

interface NavigationItem {
  key: string;
  label: string;
  icon: React.ReactNode;
  count?: number;
  isActive?: boolean;
  onClick: () => void;
}

const CategorySidebar = ({
  categories,
  tags,
  selectedCategory,
  selectedTags,
  onCategorySelect,
  onTagToggle,
  className
}: CategorySidebarProps) => {
  const [isCategoriesOpen, setIsCategoriesOpen] = useState(true);
  const [isTagsOpen, setIsTagsOpen] = useState(true);

  // 导航菜单项
  const navigationItems: NavigationItem[] = [
    {
      key: 'latest',
      label: 'Latest Topics',
      icon: <Clock className="w-4 h-4" />,
      count: 128,
      isActive: !selectedCategory,
      onClick: () => onCategorySelect(undefined)
    },
    {
      key: 'top',
      label: 'Top Topics',
      icon: <TrendingUp className="w-4 h-4" />,
      count: 17,
      isActive: false,
      onClick: () => {
        // TODO: 实现热门主题筛选
      }
    },
    {
      key: 'bookmarks',
      label: 'Bookmarks',
      icon: <Bookmark className="w-4 h-4" />,
      count: 5,
      isActive: false,
      onClick: () => {
        // TODO: 实现书签功能
      }
    }
  ];

  return (
    <div className={cn("w-64 border-r bg-muted/10", className)}>
      <ScrollArea className="h-full px-3 py-4">
        <div className="space-y-4">
          {/* 导航菜单 */}
          <div className="space-y-1">
            {navigationItems.map((item) => (
              <Button
                key={item.key}
                variant={item.isActive ? "secondary" : "ghost"}
                className={cn(
                  "w-full justify-start",
                  item.isActive && "bg-secondary text-secondary-foreground"
                )}
                onClick={item.onClick}
              >
                <div className="flex items-center justify-between w-full">
                  <div className="flex items-center space-x-2">
                    {item.icon}
                    <span>{item.label}</span>
                  </div>
                  {item.count !== undefined && (
                    <Badge variant="secondary" className="text-xs">
                      {item.count}
                    </Badge>
                  )}
                </div>
              </Button>
            ))}
          </div>

          <Separator />

          {/* 分类部分 */}
          <Collapsible open={isCategoriesOpen} onOpenChange={setIsCategoriesOpen}>
            <CollapsibleTrigger className="flex items-center justify-between w-full p-2 text-sm font-semibold text-left hover:bg-muted rounded-md">
              <div className="flex items-center space-x-2">
                <Folder className="w-4 h-4" />
                <span>Categories</span>
              </div>
              {isCategoriesOpen ? (
                <ChevronDown className="w-4 h-4" />
              ) : (
                <ChevronRight className="w-4 h-4" />
              )}
            </CollapsibleTrigger>
            <CollapsibleContent className="space-y-1 mt-2">
              {categories.length === 0 ? (
                <div className="px-2 py-4 text-sm text-muted-foreground text-center">
                  暂无分类
                </div>
              ) : (
                categories.map((category) => (
                  <Button
                    key={category.id}
                    variant={selectedCategory === category.id ? "secondary" : "ghost"}
                    className={cn(
                      "w-full justify-start pl-6",
                      selectedCategory === category.id && "bg-secondary text-secondary-foreground"
                    )}
                    onClick={() => onCategorySelect(category.id)}
                  >
                    <div className="flex items-center justify-between w-full">
                      <div className="flex items-center space-x-2">
                        <MessageSquare className="w-3 h-3" />
                        <span className="text-sm">{category.name}</span>
                      </div>
                      {category.topicCount !== undefined && (
                        <Badge variant="outline" className="text-xs">
                          {category.topicCount}
                        </Badge>
                      )}
                    </div>
                  </Button>
                ))
              )}
            </CollapsibleContent>
          </Collapsible>

          <Separator />

          {/* 标签部分 */}
          <Collapsible open={isTagsOpen} onOpenChange={setIsTagsOpen}>
            <CollapsibleTrigger className="flex items-center justify-between w-full p-2 text-sm font-semibold text-left hover:bg-muted rounded-md">
              <div className="flex items-center space-x-2">
                <Hash className="w-4 h-4" />
                <span>Tags</span>
              </div>
              {isTagsOpen ? (
                <ChevronDown className="w-4 h-4" />
              ) : (
                <ChevronRight className="w-4 h-4" />
              )}
            </CollapsibleTrigger>
            <CollapsibleContent className="space-y-1 mt-2">
              {tags.length === 0 ? (
                <div className="px-2 py-4 text-sm text-muted-foreground text-center">
                  暂无标签
                </div>
              ) : (
                tags.slice(0, 20).map((tag) => ( // 限制显示数量，避免过长
                  <Button
                    key={tag.id}
                    variant={selectedTags.includes(tag.slug) ? "secondary" : "ghost"}
                    className={cn(
                      "w-full justify-start pl-6",
                      selectedTags.includes(tag.slug) && "bg-secondary text-secondary-foreground"
                    )}
                    onClick={() => onTagToggle(tag.slug)}
                  >
                    <div className="flex items-center justify-between w-full">
                      <div className="flex items-center space-x-2">
                        <div 
                          className="w-2 h-2 rounded-full"
                          style={{ 
                            backgroundColor: tag.color || '#6b7280' 
                          }}
                        />
                        <span className="text-sm truncate">{tag.name}</span>
                      </div>
                      {tag.topicCount !== undefined && (
                        <Badge variant="outline" className="text-xs">
                          {tag.topicCount}
                        </Badge>
                      )}
                    </div>
                  </Button>
                ))
              )}
              
              {tags.length > 20 && (
                <Button
                  variant="ghost"
                  className="w-full justify-start pl-6 text-sm text-muted-foreground"
                  onClick={() => {
                    // TODO: 实现查看更多标签
                  }}
                >
                  查看更多 ({tags.length - 20}) ...
                </Button>
              )}
            </CollapsibleContent>
          </Collapsible>
        </div>
      </ScrollArea>
    </div>
  );
};

export default CategorySidebar;