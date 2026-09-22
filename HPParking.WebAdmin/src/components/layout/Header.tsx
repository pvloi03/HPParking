import { ModeToggle } from '@/components/layout/ModeToggle';

interface HeaderProps {
  title?: string;
  subtitle?: string;
}

export function Header({
  title = 'Bảng Điều Khiển',
  subtitle = 'Trung tâm giám sát & vận hành bãi đỗ xe HPParking',
}: HeaderProps) {
  return (
    <header className="h-14 border-b border-border bg-card/90 backdrop-blur-md px-6 flex items-center justify-between sticky top-0 z-40">
      <div className="flex items-center gap-3">
        <div className="hidden sm:flex h-8 px-2 py-0.5 bg-white rounded-md border border-border shadow-xs items-center justify-center">
          <img
            src="/logo.png"
            alt="Hoàng Phát Technology Era"
            className="h-6 w-auto object-contain"
          />
        </div>
        <div className="hidden sm:block h-5 w-px bg-border" />
        <div>
          <h2 className="text-sm font-bold text-foreground tracking-tight">
            {title}
          </h2>
          {subtitle && (
            <p className="text-[11px] text-muted-foreground mt-0.5">
              {subtitle}
            </p>
          )}
        </div>
      </div>

      <div className="flex items-center gap-3">
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
