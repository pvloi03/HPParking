import { Calendar } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';

export type DatePreset = 'today' | 'last7days' | 'thisMonth' | 'custom';

export interface RevenueFilterBarProps {
  preset: DatePreset;
  fromDate: string;
  toDate: string;
  onPresetChange: (preset: DatePreset) => void;
  onFromDateChange: (val: string) => void;
  onToDateChange: (val: string) => void;
}

export function RevenueFilterBar({
  preset,
  fromDate,
  toDate,
  onPresetChange,
  onFromDateChange,
  onToDateChange,
}: RevenueFilterBarProps) {
  return (
    <Card className="shadow-xs border-border/80">
      <CardContent className="p-4 flex flex-col md:flex-row items-stretch md:items-center justify-between gap-4">
        {/* Quick presets */}
        <div className="flex flex-wrap items-center gap-1.5">
          {([
            { id: 'today', label: 'Hôm nay' },
            { id: 'last7days', label: '7 ngày qua' },
            { id: 'thisMonth', label: 'Tháng này' },
          ] as const).map(({ id, label }) => (
            <Button
              key={id}
              type="button"
              variant={preset === id ? 'default' : 'outline'}
              size="sm"
              onClick={() => onPresetChange(id)}
              className="h-8 text-xs font-medium px-3"
            >
              {label}
            </Button>
          ))}
        </div>

        {/* Date range picker inputs */}
        <div className="flex flex-wrap items-center gap-2 text-xs">
          <div className="flex items-center gap-1.5">
            <Calendar className="h-3.5 w-3.5 text-muted-foreground" />
            <span className="text-muted-foreground font-medium">Từ:</span>
            <Input
              type="date"
              value={fromDate}
              max={toDate}
              onChange={(e) => onFromDateChange(e.target.value)}
              className="h-8 w-36 text-xs px-2"
              aria-label="Từ ngày"
            />
          </div>
          <div className="flex items-center gap-1.5">
            <span className="text-muted-foreground font-medium">Đến:</span>
            <Input
              type="date"
              value={toDate}
              min={fromDate}
              onChange={(e) => onToDateChange(e.target.value)}
              className="h-8 w-36 text-xs px-2"
              aria-label="Đến ngày"
            />
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
