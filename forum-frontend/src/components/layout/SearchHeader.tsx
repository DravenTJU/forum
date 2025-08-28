import { useState } from 'react';
import { Search, Menu, Plus } from 'lucide-react';

import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { Sheet, SheetContent, SheetTrigger } from '@/components/ui/sheet';
import { useAuth } from '@/hooks/useAuth';

interface SearchHeaderProps {
  searchQuery: string;
  onSearchChange: (query: string) => void;
  onSearchSubmit: () => void;
  onNewTopicClick: () => void;
  sidebarContent?: React.ReactNode;
}

const SearchHeader = ({
  searchQuery,
  onSearchChange,
  onSearchSubmit,
  onNewTopicClick,
  sidebarContent
}: SearchHeaderProps) => {
  const { user, logout } = useAuth();
  const [isSearchFocused, setIsSearchFocused] = useState(false);

  const handleSearchKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      onSearchSubmit();
    }
  };

  return (
    <header className="sticky top-0 z-50 w-full border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
      <div className="container flex h-14 items-center px-4">
        {/* 移动端菜单按钮 */}
        <div className="md:hidden">
          <Sheet>
            <SheetTrigger asChild>
              <Button variant="ghost" size="sm">
                <Menu className="h-5 w-5" />
                <span className="sr-only">打开菜单</span>
              </Button>
            </SheetTrigger>
            <SheetContent side="left" className="w-64 p-0">
              {sidebarContent}
            </SheetContent>
          </Sheet>
        </div>

        {/* Logo/标题 */}
        <div className="flex items-center space-x-2">
          <div className="flex items-center space-x-2 font-semibold">
            <div className="w-6 h-6 bg-primary rounded-sm flex items-center justify-center">
              <span className="text-primary-foreground text-sm font-bold">F</span>
            </div>
            <span className="hidden sm:inline-block">Forum</span>
          </div>
        </div>

        {/* 搜索框 */}
        <div className="flex-1 max-w-md mx-4">
          <div className={`relative ${isSearchFocused ? 'ring-2 ring-ring ring-offset-2' : ''} rounded-md transition-all`}>
            <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
            <Input
              placeholder="搜索主题..."
              value={searchQuery}
              onChange={(e) => onSearchChange(e.target.value)}
              onKeyPress={handleSearchKeyPress}
              onFocus={() => setIsSearchFocused(true)}
              onBlur={() => setIsSearchFocused(false)}
              className="pl-8 border-0 shadow-sm focus-visible:ring-0"
            />
          </div>
        </div>

        {/* 右侧操作区 */}
        <div className="flex items-center space-x-2">
          {/* 发帖按钮 */}
          <Button
            onClick={onNewTopicClick}
            size="sm"
            className="hidden sm:flex"
          >
            <Plus className="h-4 w-4 mr-1" />
            Post
          </Button>
          
          {/* 移动端发帖按钮 */}
          <Button
            onClick={onNewTopicClick}
            size="sm"
            className="sm:hidden"
          >
            <Plus className="h-4 w-4" />
            <span className="sr-only">发布主题</span>
          </Button>

          {/* 用户菜单 */}
          {user ? (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" className="relative h-8 w-8 rounded-full">
                  <Avatar className="h-8 w-8">
                    <AvatarImage src={user?.avatarUrl} alt={user?.username || 'User'} />
                    <AvatarFallback>
                      {user?.username?.charAt(0).toUpperCase() || 'U'}
                    </AvatarFallback>
                  </Avatar>
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent className="w-56" align="end" forceMount>
                <DropdownMenuLabel className="font-normal">
                  <div className="flex flex-col space-y-1">
                    <p className="text-sm font-medium leading-none">
                      {user?.username || 'User'}
                    </p>
                    <p className="text-xs leading-none text-muted-foreground">
                      {user?.email || ''}
                    </p>
                  </div>
                </DropdownMenuLabel>
                <DropdownMenuSeparator />
                <DropdownMenuItem>
                  个人主页
                </DropdownMenuItem>
                <DropdownMenuItem>
                  我的主题
                </DropdownMenuItem>
                <DropdownMenuItem>
                  书签
                </DropdownMenuItem>
                <DropdownMenuItem>
                  通知
                </DropdownMenuItem>
                <DropdownMenuSeparator />
                <DropdownMenuItem>
                  设置
                </DropdownMenuItem>
                <DropdownMenuItem
                  onClick={() => logout()}
                  className="text-red-600 focus:text-red-600"
                >
                  退出登录
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          ) : (
            <div className="flex items-center space-x-2">
              <Button variant="ghost" size="sm">
                登录
              </Button>
              <Button size="sm">
                注册
              </Button>
            </div>
          )}
        </div>
      </div>
    </header>
  );
};

export default SearchHeader;