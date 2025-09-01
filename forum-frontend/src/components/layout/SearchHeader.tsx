
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { useAuth } from '@/hooks/useAuth';

interface SearchHeaderProps {
  searchQuery: string;
  onSearchChange: (query: string) => void;
  onSearchSubmit: () => void;
}

const SearchHeader = ({
  searchQuery,
  onSearchChange,
  onSearchSubmit
}: SearchHeaderProps) => {
  const { user, logout } = useAuth();

  const handleSearchKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      onSearchSubmit();
    }
  };

  return (
    <div className="box-border content-stretch flex items-center justify-between px-4 py-0 relative w-full h-14 bg-white border-b border-zinc-200">
      {/* Left side - Logo and title */}
      <div className="content-stretch flex gap-3 items-center justify-start relative shrink-0">
        <div className="relative shrink-0 size-[18px]">
          <div className="w-[18px] h-[18px] bg-zinc-900 rounded-sm flex items-center justify-center">
            <span className="text-white text-xs font-bold">F</span>
          </div>
        </div>
        <div className="flex flex-col font-medium justify-center leading-[0] relative shrink-0 text-[14px] text-nowrap text-zinc-950">
          <p className="leading-[20px] whitespace-pre">Forum</p>
        </div>
      </div>

      {/* Right side - Search and avatar */}
      <div className="content-stretch flex gap-3 items-center justify-start relative shrink-0">
        {/* Search input */}
        <div className="bg-white box-border content-stretch flex h-9 items-center justify-start px-4 py-2 relative rounded-[6px] shrink-0 w-[280px] border border-zinc-200 shadow-[0px_1px_3px_0px_rgba(0,0,0,0.05)]">
          <input
            type="text"
            placeholder="Search"
            value={searchQuery}
            onChange={(e) => onSearchChange(e.target.value)}
            onKeyDown={handleSearchKeyPress}
            className="basis-0 flex flex-col font-normal grow justify-center leading-[0] min-h-px min-w-px relative shrink-0 text-[14px] text-zinc-500 bg-transparent border-none outline-none placeholder:text-zinc-500"
          />
        </div>

        {/* User avatar */}
        {user ? (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <button className="box-border content-stretch flex items-center justify-center px-4 py-2 relative rounded-[9999px] shrink-0 size-8 hover:bg-zinc-50 transition-colors">
                <div className="content-stretch flex items-center justify-center overflow-hidden relative rounded-[9999px] shrink-0 size-6">
                  <Avatar className="size-6">
                    <AvatarImage src={user?.avatarUrl} alt={user?.username || 'User'} />
                    <AvatarFallback className="text-xs">
                      {user?.username?.charAt(0).toUpperCase() || 'U'}
                    </AvatarFallback>
                  </Avatar>
                </div>
              </button>
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
                Profile
              </DropdownMenuItem>
              <DropdownMenuItem>
                Themes
              </DropdownMenuItem>
              <DropdownMenuItem>
                Bookmarks
              </DropdownMenuItem>
              <DropdownMenuItem>
                Notifications
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem>
                Settings
              </DropdownMenuItem>
              <DropdownMenuItem
                onClick={() => logout()}
                className="text-red-600 focus:text-red-600"
              >
                Logout
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        ) : (
          <div className="content-stretch flex items-center justify-center overflow-hidden relative rounded-[9999px] shrink-0 size-6 bg-zinc-200">
            <span className="text-xs text-zinc-600">?</span>
          </div>
        )}
      </div>
    </div>
  );
};

export default SearchHeader;