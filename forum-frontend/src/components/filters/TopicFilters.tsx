import { useState } from 'react';
import { Filter, X, Plus } from 'lucide-react';

import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  DropdownMenuCheckboxItem,
} from '@/components/ui/dropdown-menu';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Separator } from '@/components/ui/separator';
import { Category, Tag } from '@/types/api';

interface TopicFiltersProps {
  categories: Category[];
  tags: Tag[];
  selectedCategory?: string;
  selectedTags: string[];
  sortBy: string;
  onCategoryChange: (categoryId?: string) => void;
  onTagToggle: (tagSlug: string) => void;
  onSortChange: (sort: string) => void;
  onClearFilters: () => void;
  onNewTopicClick: () => void;
}

const TopicFilters = ({
  categories,
  tags,
  selectedCategory,
  selectedTags,
  sortBy,
  onCategoryChange,
  onTagToggle,
  onSortChange,
  onClearFilters,
  onNewTopicClick
}: TopicFiltersProps) => {
  const [isFilterOpen, setIsFilterOpen] = useState(false);

  const selectedCategoryName = categories.find(c => c.id === selectedCategory)?.name;
  const selectedTagNames = tags.filter(tag => selectedTags.includes(tag.slug));

  const hasActiveFilters = selectedCategory || selectedTags.length > 0;
  const filterCount = (selectedCategory ? 1 : 0) + selectedTags.length;

  const handleRemoveTag = (tagSlug: string) => {
    onTagToggle(tagSlug);
  };

  const handleRemoveCategory = () => {
    onCategoryChange(undefined);
  };

  const sortOptions = [
    { value: 'latest', label: 'Latest Reply' },
    { value: 'hot', label: 'Hot Topics' },
    { value: 'new', label: 'Newest' },
    { value: 'top', label: 'Most Views' }
  ];

  return (
    <div className="flex items-center justify-between p-4 border-b bg-muted/20">
      <div className="flex items-center space-x-4">
        {/* Filter按钮 */}
        <DropdownMenu open={isFilterOpen} onOpenChange={setIsFilterOpen}>
          <DropdownMenuTrigger asChild>
            <Button variant="outline" className="relative">
              <Filter className="w-4 h-4 mr-2" />
              Filter
              {filterCount > 0 && (
                <Badge 
                  variant="secondary" 
                  className="absolute -top-2 -right-2 w-5 h-5 p-0 flex items-center justify-center text-xs"
                >
                  {filterCount}
                </Badge>
              )}
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent className="w-80" align="start">
            <DropdownMenuLabel>Filter Options</DropdownMenuLabel>
            <DropdownMenuSeparator />
            
            {/* 分类筛选 */}
            <div className="p-2">
              <div className="text-sm font-medium mb-2">Category</div>
              <div className="space-y-1">
                <DropdownMenuCheckboxItem
                  checked={!selectedCategory}
                  onCheckedChange={() => onCategoryChange(undefined)}
                >
                  All Categories
                </DropdownMenuCheckboxItem>
                {categories.map((category) => (
                  <DropdownMenuCheckboxItem
                    key={category.id}
                    checked={selectedCategory === category.id}
                    onCheckedChange={() => 
                      onCategoryChange(selectedCategory === category.id ? undefined : category.id)
                    }
                  >
                    {category.name} ({category.topicCount || 0})
                  </DropdownMenuCheckboxItem>
                ))}
              </div>
            </div>
            
            <DropdownMenuSeparator />
            
            {/* 标签筛选 */}
            <div className="p-2">
              <div className="text-sm font-medium mb-2">Tags</div>
              <div className="space-y-1 max-h-40 overflow-y-auto">
                {tags.slice(0, 15).map((tag) => (
                  <DropdownMenuCheckboxItem
                    key={tag.id}
                    checked={selectedTags.includes(tag.slug)}
                    onCheckedChange={() => onTagToggle(tag.slug)}
                  >
                    <div className="flex items-center space-x-2">
                      <div 
                        className="w-2 h-2 rounded-full"
                        style={{ backgroundColor: tag.color || '#6b7280' }}
                      />
                      <span>{tag.name}</span>
                      <span className="text-xs text-muted-foreground">
                        ({tag.topicCount || 0})
                      </span>
                    </div>
                  </DropdownMenuCheckboxItem>
                ))}
                {tags.length > 15 && (
                  <div className="text-xs text-muted-foreground p-2">
                    {tags.length - 15} more tags...
                  </div>
                )}
              </div>
            </div>
            
            {hasActiveFilters && (
              <>
                <DropdownMenuSeparator />
                <DropdownMenuItem
                  onClick={onClearFilters}
                  className="text-red-600 focus:text-red-600"
                >
                  Clear All Filters
                </DropdownMenuItem>
              </>
            )}
          </DropdownMenuContent>
        </DropdownMenu>

        {/* 活动筛选器显示 */}
        {hasActiveFilters && (
          <div className="flex items-center space-x-2">
            {selectedCategoryName && (
              <Badge variant="secondary" className="flex items-center space-x-1">
                <span>{selectedCategoryName}</span>
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-auto p-0 hover:bg-transparent"
                  onClick={handleRemoveCategory}
                >
                  <X className="w-3 h-3" />
                </Button>
              </Badge>
            )}
            
            {selectedTagNames.map((tag) => (
              <Badge 
                key={tag.id} 
                variant="outline" 
                className="flex items-center space-x-1"
                style={{
                  backgroundColor: tag.color ? `${tag.color}20` : undefined,
                  borderColor: tag.color || undefined,
                  color: tag.color || undefined,
                }}
              >
                <span>{tag.name}</span>
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-auto p-0 hover:bg-transparent"
                  onClick={() => handleRemoveTag(tag.slug)}
                >
                  <X className="w-3 h-3" />
                </Button>
              </Badge>
            ))}
            
            {filterCount > 0 && (
              <Button
                variant="ghost"
                size="sm"
                onClick={onClearFilters}
                className="text-muted-foreground hover:text-foreground"
              >
                Clear Filters ({filterCount})
              </Button>
            )}
          </div>
        )}
      </div>

      <div className="flex items-center space-x-3">
        {/* 排序选择 */}
        <div className="flex items-center space-x-2">
          <span className="text-sm text-muted-foreground">Order by:</span>
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

        {/* 发帖按钮 */}
        <Button onClick={onNewTopicClick}>
          <Plus className="w-4 h-4 mr-2" />
          Post
        </Button>
      </div>
    </div>
  );
};

export default TopicFilters;