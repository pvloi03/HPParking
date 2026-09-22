import React, { useState } from 'react';
import {
  LayoutDashboard,
  History,
  Building2,
  Building,
  Briefcase,
  Users,
  Car,
  DoorOpen,
  Route,
  Cpu,
  ShieldAlert,
  UserCog,
  Trash2,
  ChevronLeft,
  ChevronRight,
  ChevronDown,
  LogOut,
  X,
} from 'lucide-react';
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip';
import { ConfirmDialog } from '@/components/common/ConfirmDialog';
import { useUiStore } from '@/stores/uiStore';
import { useAuthStore } from '@/stores/authStore';
import { authApi } from '@/api/authApi';
import { useNavigate, useLocation } from 'react-router-dom';
import { cn } from '@/lib/utils';

interface NavSubItem {
  title: string;
  href: string;
  icon: React.ComponentType<{ className?: string }>;
}

interface NavGroup {
  title: string;
  icon: React.ComponentType<{ className?: string }>;
  children: NavSubItem[];
}

interface NavSingleItem {
  title: string;
  href: string;
  icon: React.ComponentType<{ className?: string }>;
}

type MenuItem =
  | { type: 'single'; item: NavSingleItem }
  | { type: 'group'; group: NavGroup };

const menuConfig: MenuItem[] = [
  {
    type: 'single',
    item: {
      title: 'Dashboard',
      href: '/dashboard',
      icon: LayoutDashboard,
    },
  },
  {
    type: 'single',
    item: {
      title: 'Lịch sử xe ra vào',
      href: '/parking-sessions',
      icon: History,
    },
  },
  {
    type: 'group',
    group: {
      title: 'Cơ cấu tổ chức',
      icon: Building2,
      children: [
        { title: 'Công ty', href: '/companies', icon: Building },
        { title: 'Phòng ban', href: '/departments', icon: Building2 },
        { title: 'Nhà thầu', href: '/contractors', icon: Briefcase },
      ],
    },
  },
  {
    type: 'group',
    group: {
      title: 'Khách hàng & Xe',
      icon: Users,
      children: [
        { title: 'Khách hàng', href: '/clients', icon: Users },
        { title: 'Phương tiện', href: '/vehicles', icon: Car },
      ],
    },
  },
  {
    type: 'group',
    group: {
      title: 'Hạ tầng bãi xe',
      icon: Route,
      children: [
        { title: 'Cổng bãi xe', href: '/gates', icon: DoorOpen },
        { title: 'Làn xe', href: '/lanes', icon: Route },
        { title: 'Thiết bị', href: '/devices', icon: Cpu },
      ],
    },
  },
  {
    type: 'group',
    group: {
      title: 'Sổ cái & Kiểm toán',
      icon: ShieldAlert,
      children: [
        { title: 'Nhật ký kiểm toán', href: '/audit-logs', icon: ShieldAlert },
        { title: 'Người dùng', href: '/users', icon: UserCog },
        { title: 'Thùng rác hệ thống', href: '/recycle-bin', icon: Trash2 },
      ],
    },
  },
];

interface SidebarProps {
  isMobile?: boolean;
  onClose?: () => void;
}

export function Sidebar({ isMobile = false, onClose }: SidebarProps = {}) {
  const { isSidebarCollapsed, toggleSidebar } = useUiStore();
  const user = useAuthStore((s) => s.user);
  const clearAuth = useAuthStore((s) => s.clearAuth);
  const [openGroups, setOpenGroups] = useState<Record<string, boolean>>({
    'Cơ cấu tổ chức': true,
    'Khách hàng & Xe': true,
    'Hạ tầng bãi xe': true,
    'Sổ cái & Kiểm toán': true,
  });
  const navigate = useNavigate();
  const location = useLocation();
  const activePath = location.pathname;
  const [showLogoutDialog, setShowLogoutDialog] = useState(false);

  const effectiveCollapsed = isMobile ? false : isSidebarCollapsed;

  const handleLogout = async () => {
    try {
      await authApi.logout();
    } catch {
      // ignore network or session errors during logout
    } finally {
      clearAuth();
      window.location.href = '/login';
    }
  };

  const userInitials = user?.fullName
    ? user.fullName
      .trim()
      .split(/\s+/)
      .map((n) => n[0])
      .slice(-2)
      .join('')
      .toUpperCase()
    : 'AD';
  const userDisplayName = user?.fullName || 'Quản trị viên';
  const userRoleLabel = `${user?.role || 'Admin'} • HPParking`;

  const toggleGroup = (title: string) => {
    setOpenGroups((prev) => ({
      ...prev,
      [title]: !prev[title],
    }));
  };

  const handleNavigate = (href: string) => {
    navigate(href);
    if (isMobile) {
      onClose?.();
    }
  };

  return (
    <TooltipProvider delayDuration={0}>
      <aside
        className={cn(
          isMobile
            ? 'flex flex-col h-full w-full bg-card select-none'
            : 'relative hidden lg:flex flex-col border-r border-border bg-card transition-all duration-250 ease-in-out select-none shrink-0 z-30',
          !isMobile && (effectiveCollapsed ? 'w-[68px]' : 'w-60')
        )}
      >
        {/* Brand Header */}
        <div
          className={cn(
            'flex items-center h-16 border-b border-border shrink-0 transition-all duration-200',
            effectiveCollapsed ? 'justify-center px-1.5' : 'justify-between px-3'
          )}
        >
          {isMobile ? (
            <>
              <div className="flex items-center min-w-0 flex-1 overflow-hidden pr-2">
                <img
                  src="/logo.png"
                  alt="HPParking Logo"
                  className="h-12 max-h-[50px] w-auto max-w-[185px] object-contain shrink-0"
                />
              </div>
              <button
                type="button"
                onClick={onClose}
                className="p-2 rounded-lg text-muted-foreground hover:text-foreground hover:bg-muted/80 transition-colors cursor-pointer min-h-[44px] min-w-[44px] flex items-center justify-center shrink-0"
                aria-label="Đóng thanh bên"
                title="Đóng menu"
              >
                <X className="h-5 w-5" />
              </button>
            </>
          ) : !effectiveCollapsed ? (
            <>
              <div className="flex items-center min-w-0 flex-1 overflow-hidden pr-1">
                <img
                  src="/logo.png"
                  alt="HPParking Logo"
                  className="h-12 max-h-[50px] w-auto max-w-[185px] object-contain shrink-0"
                />
              </div>
              <button
                type="button"
                onClick={toggleSidebar}
                className="p-1.5 rounded-md text-muted-foreground hover:text-foreground hover:bg-muted/80 transition-colors cursor-pointer shrink-0"
                aria-label="Thu gọn thanh bên"
                title="Thu gọn sidebar"
              >
                <ChevronLeft className="h-4 w-4" />
              </button>
            </>
          ) : (
            <button
              type="button"
              onClick={toggleSidebar}
              className="h-11 w-[56px] rounded-lg hover:bg-muted/60 flex items-center justify-center transition-all cursor-pointer group p-1"
              aria-label="Mở rộng thanh bên"
              title="Mở rộng sidebar"
            >
              <img
                src="/logo.png"
                alt="HPParking Logo"
                className="w-full h-auto max-h-9 object-contain group-hover:hidden"
              />
              <ChevronRight className="h-5 w-5 text-foreground hidden group-hover:block" />
            </button>
          )}
        </div>

        {/* Section title for test accessibility */}
        {!effectiveCollapsed && (
          <div className="sr-only">
            <span>Tổng quan</span>
          </div>
        )}

        {/* Navigation list */}
        <nav className="flex-1 overflow-y-auto py-3 px-2 space-y-1">
          {menuConfig.map((menu, idx) => {
            if (menu.type === 'single') {
              const { item } = menu;
              const isActive = activePath === item.href;
              const Icon = item.icon;

              const singleBtn = (
                <button
                  key={item.href}
                  type="button"
                  onClick={() => handleNavigate(item.href)}
                  className={cn(
                    'w-full flex items-center gap-2.5 rounded-lg text-[13px] font-medium transition-all duration-150 cursor-pointer',
                    effectiveCollapsed ? 'justify-center p-2.5' : 'px-3 py-2',
                    isActive
                      ? 'bg-blue-600 text-white shadow-xs shadow-blue-500/30'
                      : 'text-muted-foreground hover:text-foreground hover:bg-muted/70'
                  )}
                >
                  <Icon
                    className={cn(
                      'h-4 w-4 shrink-0',
                      isActive ? 'text-white' : 'text-muted-foreground'
                    )}
                  />
                  {!effectiveCollapsed && (
                    <span className="truncate flex-1 text-left">{item.title}</span>
                  )}
                </button>
              );

              if (effectiveCollapsed) {
                return (
                  <Tooltip key={item.href}>
                    <TooltipTrigger asChild>{singleBtn}</TooltipTrigger>
                    <TooltipContent side="right" sideOffset={10}>
                      {item.title}
                    </TooltipContent>
                  </Tooltip>
                );
              }

              return singleBtn;
            }

            // Group item
            const { group } = menu;
            const isOpen = openGroups[group.title] ?? true;
            const GroupIcon = group.icon;
            const isGroupActive = group.children.some((c) => c.href === activePath);

            return (
              <div key={`group-${idx}`} className="pt-1">
                <button
                  type="button"
                  onClick={() => toggleGroup(group.title)}
                  className={cn(
                    'w-full flex items-center gap-2.5 rounded-lg text-[13px] font-medium transition-colors cursor-pointer',
                    effectiveCollapsed
                      ? 'justify-center p-2.5'
                      : 'px-3 py-2 justify-between',
                    isGroupActive
                      ? 'text-blue-600 dark:text-blue-400 font-semibold'
                      : 'text-muted-foreground hover:text-foreground hover:bg-muted/60'
                  )}
                >
                  <div className="flex items-center gap-2.5 min-w-0">
                    <GroupIcon
                      className={cn(
                        'h-4 w-4 shrink-0',
                        isGroupActive
                          ? 'text-blue-600 dark:text-blue-400'
                          : 'text-muted-foreground'
                      )}
                    />
                    {!effectiveCollapsed && (
                      <span className="truncate">{group.title}</span>
                    )}
                  </div>
                  {!effectiveCollapsed && (
                    <ChevronDown
                      className={cn(
                        'h-3.5 w-3.5 text-muted-foreground/80 transition-transform duration-200 shrink-0',
                        isOpen && 'rotate-180 text-blue-500'
                      )}
                    />
                  )}
                </button>

                {!effectiveCollapsed && isOpen && (
                  <div className="ml-3 pl-3 border-l border-border mt-0.5 mb-1 space-y-0.5">
                    {group.children.map((child) => {
                      const isChildActive = activePath === child.href;
                      const ChildIcon = child.icon;

                      return (
                        <button
                          key={child.href}
                          type="button"
                          onClick={() => handleNavigate(child.href)}
                          className={cn(
                            'w-full flex items-center gap-2 px-2.5 py-1.5 rounded-md text-[12px] font-medium transition-colors cursor-pointer text-left',
                            isChildActive
                              ? 'text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-950/40 font-semibold'
                              : 'text-muted-foreground hover:text-foreground hover:bg-muted/60'
                          )}
                        >
                          <ChildIcon
                            className={cn(
                              'h-3.5 w-3.5 shrink-0',
                              isChildActive
                                ? 'text-blue-500'
                                : 'text-muted-foreground'
                            )}
                          />
                          <span className="truncate">{child.title}</span>
                        </button>
                      );
                    })}
                  </div>
                )}
              </div>
            );
          })}
        </nav>

        {/* User Profile Section at bottom */}
        <div className="border-t border-border p-3 shrink-0 bg-muted/20">
          <div
            className={cn(
              'flex items-center gap-2.5',
              effectiveCollapsed && 'flex-col justify-center'
            )}
          >
            <div className="h-7 w-7 rounded-full bg-gradient-to-br from-blue-600 to-indigo-500 text-white text-[11px] font-bold flex items-center justify-center shrink-0 shadow-xs">
              {userInitials}
            </div>
            {!effectiveCollapsed && (
              <div className="min-w-0 flex-1">
                <p className="text-[12px] font-semibold text-foreground truncate leading-tight">
                  {userDisplayName}
                </p>
                <p className="text-[10px] text-muted-foreground leading-tight truncate">
                  {userRoleLabel}
                </p>
              </div>
            )}
            <button
              type="button"
              onClick={() => setShowLogoutDialog(true)}
              title="Đăng xuất"
              className="p-1.5 rounded-md text-muted-foreground hover:text-destructive hover:bg-destructive/10 transition-colors cursor-pointer shrink-0"
              aria-label="Đăng xuất"
            >
              <LogOut className="h-3.5 w-3.5" />
            </button>
          </div>
        </div>

        {/* Logout Confirmation Dialog */}
        <ConfirmDialog
          open={showLogoutDialog}
          onOpenChange={setShowLogoutDialog}
          title="Xác Nhận Đăng Xuất"
          description="Bạn có chắc chắn muốn đăng xuất khỏi hệ thống quản trị HPParking không?"
          confirmText="Đăng Xuất"
          cancelText="Hủy Bỏ"
          variant="destructive"
          icon={<LogOut className="h-5 w-5 text-destructive" />}
          confirmIcon={<LogOut className="h-3.5 w-3.5" />}
          onConfirm={() => {
            setShowLogoutDialog(false);
            void handleLogout();
          }}
        />
      </aside>
    </TooltipProvider>
  );
}
