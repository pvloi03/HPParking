import React from 'react';
import { Phone, Mail } from 'lucide-react';
import { cn } from '@/lib/utils';

export interface ContactInfoCellProps {
  phoneNumber?: string | null;
  email?: string | null;
  className?: string;
  emptyFallback?: React.ReactNode;
}

export const ContactInfoCell: React.FC<ContactInfoCellProps> = ({
  phoneNumber,
  email,
  className,
  emptyFallback = <span className="text-muted-foreground">—</span>,
}) => {
  const cleanPhone = phoneNumber?.trim();
  const cleanEmail = email?.trim();

  if (!cleanPhone && !cleanEmail) {
    return <>{emptyFallback}</>;
  }

  return (
    <div className={cn('flex flex-col gap-1 text-xs', className)}>
      {cleanPhone ? (
        <div className="flex items-center gap-1.5 text-foreground">
          <Phone className="h-3.5 w-3.5 text-muted-foreground shrink-0" />
          <span className="font-mono text-xs">{cleanPhone}</span>
        </div>
      ) : null}
      {cleanEmail ? (
        <div className="flex items-center gap-1.5 text-muted-foreground">
          <Mail className="h-3.5 w-3.5 text-muted-foreground shrink-0" />
          <span className="truncate max-w-[200px] text-xs" title={cleanEmail}>
            {cleanEmail}
          </span>
        </div>
      ) : null}
    </div>
  );
};
