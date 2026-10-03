import React from 'react';
import { Badge } from '@/components/ui/badge';
import { cn } from '@/lib/utils';

export interface ActiveStatusBadgeProps {
  isActive?: boolean | null;
  className?: string;
}

export const ActiveStatusBadge: React.FC<ActiveStatusBadgeProps> = ({
  isActive,
  className,
}) => {
  const active = Boolean(isActive);

  return (
    <Badge
      variant={active ? 'success' : 'secondary'}
      className={cn(
        'text-[11px] font-medium transition-colors shrink-0',
        active
          ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300 border border-emerald-300/60 dark:border-emerald-800 hover:bg-emerald-100'
          : 'bg-muted text-muted-foreground border border-border/60 hover:bg-muted',
        className
      )}
    >
      {active ? 'Đã kích hoạt' : 'Chưa kích hoạt'}
    </Badge>
  );
};
