import type { LucideIcon } from 'lucide-react';
import type { KpiCardItem } from '@/types/dashboard';
import { Card, CardContent } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';

export interface KpiCardProps {
  item: KpiCardItem;
  isLoading?: boolean;
  className?: string;
}

/**
 * Thẻ hiển thị chỉ số KPI chuẩn hóa dùng chung cho Dashboard và các trang Báo cáo
 */
export function KpiCard({ item, isLoading, className }: KpiCardProps) {
  const Icon: LucideIcon = item.icon;

  return (
    <Card
      className={cn(
        'transition-all duration-200 hover:shadow-md border-border/80 shadow-xs',
        item.isWarning && 'border-red-300 dark:border-red-900/60 bg-red-50/20',
        className
      )}
    >
      <CardContent className="p-4 space-y-3">
        <div className="flex items-center justify-between">
          <span className="text-xs font-semibold text-muted-foreground truncate">
            {item.title}
          </span>
          <div
            className={cn(
              'h-8 w-8 rounded-xl flex items-center justify-center shrink-0 shadow-2xs',
              item.iconBg,
              item.iconColor
            )}
          >
            <Icon className="h-4 w-4" />
          </div>
        </div>

        {isLoading ? (
          <div className="space-y-1">
            <Skeleton className="h-7 w-28" />
            <Skeleton className="h-3.5 w-36" />
          </div>
        ) : (
          <div>
            <div className="text-xl sm:text-2xl font-bold tracking-tight text-foreground truncate">
              {item.value}
            </div>
            <p
              className={cn(
                'text-[11px] sm:text-xs mt-0.5 truncate',
                item.isWarning
                  ? 'text-red-600 dark:text-red-400 font-medium'
                  : 'text-muted-foreground'
              )}
            >
              {item.subtext}
            </p>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
