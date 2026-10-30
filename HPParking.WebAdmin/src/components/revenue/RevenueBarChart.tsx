import { useMemo } from 'react';
import {
  Bar,
  BarChart,
  CartesianGrid,
  XAxis,
  YAxis,
} from 'recharts';
import { Receipt } from 'lucide-react';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import {
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
  type ChartConfig,
} from '@/components/ui/chart';
import type { RevenueDailyItemDto } from '@/types/statistics';
import { formatDateDisplay, formatDayMonth } from '@/utils/dateUtils';

const chartConfig = {
  totalRevenue: {
    label: 'Doanh Thu (VNĐ)',
    color: 'hsl(var(--chart-1))',
  },
} satisfies ChartConfig;

export interface RevenueBarChartProps {
  timeline?: RevenueDailyItemDto[];
  totalRevenue?: number;
  fromDate: string;
  toDate: string;
  isLoading?: boolean;
}

export function RevenueBarChart({
  timeline,
  totalRevenue = 0,
  fromDate,
  toDate,
  isLoading,
}: RevenueBarChartProps) {
  const chartData = useMemo(() => {
    if (!timeline) return [];
    return timeline.map((item) => {
      const displayDate = formatDayMonth(item.date);
      const tooltipDate = formatDateDisplay(item.date);
      return {
        date: item.date,
        displayDate,
        tooltipDate,
        totalRevenue: item.revenue,
        paidSessionsCount: item.paidCount,
      };
    });
  }, [timeline]);

  return (
    <Card className="shadow-xs border-border/80">
      <CardHeader className="pb-2">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
          <div>
            <CardTitle className="text-base font-semibold">
              Biểu Đồ Doanh Thu Theo Ngày
            </CardTitle>
            <CardDescription className="text-xs text-muted-foreground mt-0.5">
              Biểu đồ cột theo chuỗi ngày liên tục trong khoảng thời gian từ {formatDateDisplay(fromDate)} đến {formatDateDisplay(toDate)}
            </CardDescription>
          </div>
          <div className="flex items-center gap-2 text-xs">
            <span className="flex items-center gap-1.5 px-2.5 py-1 rounded-md bg-emerald-50 dark:bg-emerald-950/40 text-emerald-700 dark:text-emerald-300 font-medium border border-emerald-200/60 dark:border-emerald-900/40">
              <Receipt className="h-3.5 w-3.5 text-emerald-600" />
              Tổng thu: <strong>{totalRevenue.toLocaleString('vi-VN')} đ</strong>
            </span>
          </div>
        </div>
      </CardHeader>

      <CardContent className="pt-2 px-2 sm:px-6">
        {isLoading ? (
          <div className="h-[280px] w-full flex items-end gap-3 p-4">
            {Array.from({ length: 7 }).map((_, i) => (
              <div key={i} className="flex-1 flex flex-col justify-end items-center h-full gap-2">
                <Skeleton
                  className="w-full rounded-t-md"
                  style={{ height: `${Math.max(20, ((i + 1) * 23) % 95)}%` }}
                />
                <Skeleton className="h-3 w-8" />
              </div>
            ))}
          </div>
        ) : chartData.length === 0 ? (
          <div className="h-[280px] w-full flex items-center justify-center text-muted-foreground text-sm">
            Không có dữ liệu trong khoảng thời gian đã chọn
          </div>
        ) : (
          <div className="w-full overflow-x-auto pb-1">
            <ChartContainer
              config={chartConfig}
              className="h-[290px] min-w-[500px] sm:min-w-full w-full"
            >
              <BarChart
                data={chartData}
                margin={{ top: 12, right: 16, left: 16, bottom: 4 }}
              >
                <CartesianGrid
                  strokeDasharray="3 3"
                  vertical={false}
                  className="stroke-border/40"
                />
                <XAxis
                  dataKey="displayDate"
                  tickLine={false}
                  axisLine={false}
                  tickMargin={8}
                  className="text-[11px] fill-muted-foreground"
                />
                <YAxis
                  width={70}
                  tickLine={false}
                  axisLine={false}
                  tickMargin={6}
                  tickFormatter={(v) => {
                    if (v >= 1_000_000) return `${(v / 1_000_000).toFixed(1)}M`;
                    if (v >= 1_000) return `${(v / 1_000).toFixed(0)}k`;
                    return `${v}`;
                  }}
                  className="text-[11px] fill-muted-foreground"
                />
                <ChartTooltip
                  cursor={{ fill: 'hsl(var(--muted) / 0.3)' }}
                  content={
                    <ChartTooltipContent
                      indicator="dot"
                      labelFormatter={(_, payload) => {
                        if (payload && payload.length > 0 && payload[0]?.payload) {
                          const p = payload[0].payload;
                          return `Ngày ${p.tooltipDate} (${p.paidSessionsCount} lượt thu)`;
                        }
                        return _;
                      }}
                      formatter={(value) => [
                        `${Number(value).toLocaleString('vi-VN')} đ`,
                        'Doanh thu',
                      ]}
                    />
                  }
                />
                <Bar
                  dataKey="totalRevenue"
                  fill="hsl(var(--chart-1))"
                  radius={[4, 4, 0, 0]}
                  maxBarSize={48}
                />
              </BarChart>
            </ChartContainer>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
