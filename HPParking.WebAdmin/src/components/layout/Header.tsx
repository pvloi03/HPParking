import { Menu } from 'lucide-react';
import { ModeToggle } from '@/components/layout/ModeToggle';
import { useUiStore } from '@/stores/uiStore';

interface HeaderProps {
  title?: string;
  subtitle?: string;
}

export function Header({
  title = 'Bảng Điều Khiển',
  subtitle = 'Trung tâm giám sát & vận hành bãi đỗ xe HPParking',
}: HeaderProps) {
  const toggleMobileDrawer = useUiStore((s) => s.toggleMobileDrawer);

  return (
    <header className="h-16 border-b border-border bg-card/90 backdrop-blur-md px-4 sm:px-6 flex items-center justify-between sticky top-0 z-40">
      <div className="flex items-center gap-3 min-w-0">
        {/* Mobile Hamburger Button */}
        <button
          type="button"
          onClick={toggleMobileDrawer}
          className="lg:hidden p-2 -ml-2 rounded-lg text-muted-foreground hover:text-foreground hover:bg-muted/80 transition-colors cursor-pointer min-h-[44px] min-w-[44px] flex items-center justify-center shrink-0"
          aria-label="Mở menu điều hướng"
          title="Mở menu"
        >
          <Menu className="h-5 w-5" />
        </button>

        <div className="min-w-0">
          <h2 className="text-sm font-bold text-foreground tracking-tight truncate">
            {title}
          </h2>
          {subtitle && (
            <p className="text-xs text-muted-foreground mt-0.5 truncate hidden sm:block">
              {subtitle}
            </p>
          )}
        </div>
      </div>

      <div className="flex items-center gap-3 shrink-0">
        {/* Realtime Live Indicator */}
        <div className="hidden sm:flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11px] font-medium bg-emerald-50 dark:bg-emerald-950/40 text-emerald-700 dark:text-emerald-400 border border-emerald-200 dark:border-emerald-800/50">
          <span className="relative flex h-1.5 w-1.5">
            <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75" />
            <span className="relative inline-flex rounded-full h-1.5 w-1.5 bg-emerald-500" />
          </span>
          <span>Trực Tuyến</span>
        </div>

        {/* Theme Toggle */}
        <ModeToggle />
      </div>
    </header>
  );
}
