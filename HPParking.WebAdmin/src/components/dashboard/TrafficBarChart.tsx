import { Bar, BarChart, CartesianGrid, XAxis, YAxis } from 'recharts';
import { ArrowDownRight, ArrowUpRight, TrendingUp } from 'lucide-react';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import {
  ChartContainer,
  ChartLegend,
  ChartLegendContent,
  ChartTooltip,
  ChartTooltipContent,
  type ChartConfig,
} from '@/components/ui/chart';
import { Skeleton } from '@/components/ui/skeleton';
import type { DashboardFilterState } from '@/types/dashboard';
import type { ParkingSessionDto } from '@/types/parkingSession';

const chartConfig = {
  inCount: {
    label: 'Lượt Xe Vào',
    color: 'hsl(var(--chart-1))',
  },
  outCount: {
    label: 'Lượt Xe Ra',
    color: 'hsl(var(--chart-2))',
  },
} satisfies ChartConfig;

function formatDateDisplay(isoDateStr?: string): string {
  if (!isoDateStr) return '';
  const parts = isoDateStr.split('-');
  if (parts.length !== 3) return isoDateStr;
  return `${parts[2]}/${parts[1]}/${parts[0]}`;
}

function formatMonthDisplay(isoMonthStr?: string): string {
  if (!isoMonthStr) return '';
  const parts = isoMonthStr.split('-');
  if (parts.length !== 2) return isoMonthStr;
  return `${parts[1]}/${parts[0]}`;
}

export interface ChartDataItem {
  time: string;
  tooltipLabel?: string;
  inCount: number;
  outCount: number;
}

/**
 * Tổng hợp dữ liệu thực tế từ danh sách các phiên đỗ xe trong CSDL (Phương án 2)
 */
export function aggregateSessionsToChart(
  sessions: ParkingSessionDto[] = [],
  filter: DashboardFilterState
): {
  chartData: ChartDataItem[];
  periodDescription: string;
  maxBarSize: number;
} {
  if (filter.type === 'day') {
    // 24 giờ trọn vẹn từ 00h đến 23h
    const slots: ChartDataItem[] = Array.from({ length: 24 }, (_, h) => {
      const hourLabel = `${String(h).padStart(2, '0')}h`;
      const nextHour = (h + 1) % 24;
      return {
        time: hourLabel,
        tooltipLabel: `Khung giờ ${String(h).padStart(2, '0')}:00 - ${String(nextHour).padStart(2, '0')}:00`,
        inCount: 0,
        outCount: 0,
      };
    });

    for (const session of sessions) {
      if (session.inTime) {
        const inDate = new Date(session.inTime);
        const inDateStr = inDate.toLocaleDateString('en-CA');
        if (inDateStr === filter.date) {
          const h = inDate.getHours();
          if (h >= 0 && h < 24) {
            slots[h].inCount += 1;
          }
        }
      }
      if (session.outTime) {
        const outDate = new Date(session.outTime);
        const outDateStr = outDate.toLocaleDateString('en-CA');
        if (outDateStr === filter.date) {
          const h = outDate.getHours();
          if (h >= 0 && h < 24) {
            slots[h].outCount += 1;
          }
        }
      }
    }

    return {
      chartData: slots,
      periodDescription: `Lưu lượng xe 24/7 theo từng giờ ngày ${formatDateDisplay(filter.date)} (24 giờ)`,
      maxBarSize: 16,
    };
  }

  if (filter.type === 'month') {
    const [yearStr, mStr] = (filter.month || '2026-09').split('-');
    const year = parseInt(yearStr, 10) || 2026;
    const month = parseInt(mStr, 10) || 9;
    const daysInMonth = new Date(year, month, 0).getDate();

    const slots: ChartDataItem[] = Array.from({ length: daysInMonth }, (_, i) => {
      const day = i + 1;
      const dayStr = String(day).padStart(2, '0');
      return {
        time: dayStr,
        tooltipLabel: `Ngày ${dayStr}/${String(month).padStart(2, '0')}/${year}`,
        inCount: 0,
        outCount: 0,
      };
    });

    for (const session of sessions) {
      if (session.inTime) {
        const inDate = new Date(session.inTime);
        if (inDate.getFullYear() === year && inDate.getMonth() + 1 === month) {
          const dayIndex = inDate.getDate() - 1;
          if (dayIndex >= 0 && dayIndex < daysInMonth) {
            slots[dayIndex].inCount += 1;
          }
        }
      }
      if (session.outTime) {
        const outDate = new Date(session.outTime);
        if (outDate.getFullYear() === year && outDate.getMonth() + 1 === month) {
          const dayIndex = outDate.getDate() - 1;
          if (dayIndex >= 0 && dayIndex < daysInMonth) {
            slots[dayIndex].outCount += 1;
          }
        }
      }
    }

    return {
      chartData: slots,
      periodDescription: `Lưu lượng xe đầy đủ ${slots.length} ngày trong tháng ${formatMonthDisplay(filter.month)}`,
      maxBarSize: 14,
    };
  }

  if (filter.type === 'year') {
    const year = parseInt(filter.year, 10) || 2026;
    const slots: ChartDataItem[] = Array.from({ length: 12 }, (_, i) => ({
      time: `Thg ${i + 1}`,
      tooltipLabel: `Tháng ${String(i + 1).padStart(2, '0')}/${year}`,
      inCount: 0,
      outCount: 0,
    }));

    for (const session of sessions) {
      if (session.inTime) {
        const inDate = new Date(session.inTime);
        if (inDate.getFullYear() === year) {
          const m = inDate.getMonth();
          if (m >= 0 && m < 12) {
            slots[m].inCount += 1;
          }
        }
      }
      if (session.outTime) {
        const outDate = new Date(session.outTime);
        if (outDate.getFullYear() === year) {
          const m = outDate.getMonth();
          if (m >= 0 && m < 12) {
            slots[m].outCount += 1;
          }
        }
      }
    }

    return {
      chartData: slots,
      periodDescription: `Lưu lượng xe qua 12 tháng năm ${filter.year}`,
      maxBarSize: 28,
    };
  }

  // Chế độ Tùy chọn (Custom range)
  const startDate = new Date(filter.customFrom);
  const endDate = new Date(filter.customTo);
  const diffTime = Math.abs(endDate.getTime() - startDate.getTime());
  const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24)) + 1;

  if (diffDays <= 1) {
    return aggregateSessionsToChart(sessions, {
      ...filter,
      type: 'day',
      date: filter.customFrom,
    });
  }

  const slots: (ChartDataItem & { dateStr: string })[] = [];
  const curr = new Date(startDate);
  while (curr <= endDate) {
    const d = String(curr.getDate()).padStart(2, '0');
    const m = String(curr.getMonth() + 1).padStart(2, '0');
    const y = curr.getFullYear();
    const dateStr = curr.toLocaleDateString('en-CA');
    slots.push({
      time: `${d}/${m}`,
      tooltipLabel: `Ngày ${d}/${m}/${y}`,
      dateStr,
      inCount: 0,
      outCount: 0,
    });
    curr.setDate(curr.getDate() + 1);
  }

  for (const session of sessions) {
    if (session.inTime) {
      const inDateStr = new Date(session.inTime).toLocaleDateString('en-CA');
      const found = slots.find((s) => s.dateStr === inDateStr);
      if (found) found.inCount += 1;
    }
    if (session.outTime) {
      const outDateStr = new Date(session.outTime).toLocaleDateString('en-CA');
      const found = slots.find((s) => s.dateStr === outDateStr);
      if (found) found.outCount += 1;
    }
  }

  return {
    chartData: slots,
    periodDescription: `Lưu lượng xe từ ${formatDateDisplay(filter.customFrom)} đến ${formatDateDisplay(filter.customTo)} (${slots.length} ngày)`,
    maxBarSize: slots.length > 20 ? 14 : 20,
  };
}

interface TrafficBarChartProps {
  isLoading?: boolean;
  filter?: DashboardFilterState;
  sessions?: ParkingSessionDto[];
}

const defaultFilter: DashboardFilterState = {
  type: 'day',
  date: '2026-09-22',
  month: '2026-09',
  year: '2026',
  customFrom: '2026-09-16',
  customTo: '2026-09-22',
};

export function TrafficBarChart({
  isLoading,
  filter = defaultFilter,
  sessions = [],
}: TrafficBarChartProps) {
  const { chartData, periodDescription, maxBarSize } = aggregateSessionsToChart(
    sessions,
    filter
  );

  const totalIn = chartData.reduce((acc, cur) => acc + cur.inCount, 0);
  const totalOut = chartData.reduce((acc, cur) => acc + cur.outCount, 0);

  return (
    <Card className="border-border/80 shadow-xs">
      <CardHeader className="pb-3 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <div>
          <CardTitle className="text-sm font-bold text-foreground flex items-center gap-2">
            <TrendingUp className="h-4 w-4 text-blue-600 dark:text-blue-400" />
            Thống Kê Số Lượt Xe Ra Vào
          </CardTitle>
          <CardDescription className="text-xs text-muted-foreground mt-0.5">
            {periodDescription}
          </CardDescription>
        </div>

        {/* Quick KPI pills - tính tổng thời gian thực từ Database */}
        <div className="flex items-center gap-2 text-xs">
          <span className="flex items-center gap-1 px-2.5 py-1 rounded-md bg-blue-50 dark:bg-blue-950/40 text-blue-700 dark:text-blue-300 font-medium border border-blue-200/60 dark:border-blue-900/40">
            <ArrowDownRight className="h-3.5 w-3.5 text-blue-600" />
            Vào: <strong>{totalIn.toLocaleString()}</strong>
          </span>
          <span className="flex items-center gap-1 px-2.5 py-1 rounded-md bg-emerald-50 dark:bg-emerald-950/40 text-emerald-700 dark:text-emerald-300 font-medium border border-emerald-200/60 dark:border-emerald-900/40">
            <ArrowUpRight className="h-3.5 w-3.5 text-emerald-600" />
            Ra: <strong>{totalOut.toLocaleString()}</strong>
          </span>
        </div>
      </CardHeader>

      <CardContent className="pt-2 px-2 sm:px-6">
        {isLoading ? (
          <div className="h-[280px] w-full flex items-end gap-3 p-4">
            {Array.from({ length: 8 }).map((_, i) => (
              <div key={i} className="flex-1 flex gap-1 items-end h-full">
                <Skeleton
                  className="w-1/2 rounded-t-md"
                  style={{ height: `${Math.max(20, (i * 13) % 100)}%` }}
                />
                <Skeleton
                  className="w-1/2 rounded-t-md"
                  style={{ height: `${Math.max(15, (i * 17) % 90)}%` }}
                />
              </div>
            ))}
          </div>
        ) : (
          <div className="w-full overflow-x-auto pb-1">
            <ChartContainer
              config={chartConfig}
              className="h-[290px] min-w-[560px] sm:min-w-full w-full"
            >
              <BarChart
                data={chartData}
                margin={{ top: 10, right: 16, left: 8, bottom: 0 }}
              >
                <CartesianGrid
                  strokeDasharray="3 3"
                  vertical={false}
                  className="stroke-border/40"
                />
                <XAxis
                  dataKey="time"
                  tickLine={false}
                  tickMargin={10}
                  axisLine={false}
                  interval={0}
                  className="text-[10px] fill-muted-foreground font-medium"
                />
                <YAxis
                  width={48}
                  tickLine={false}
                  axisLine={false}
                  tickMargin={6}
                  className="text-[11px] fill-muted-foreground"
                />
                <ChartTooltip
                  cursor={{ fill: 'hsl(var(--muted) / 0.3)' }}
                  content={
                    <ChartTooltipContent
                      indicator="dot"
                      labelFormatter={(_, payload) => {
                        if (
                          payload &&
                          payload.length > 0 &&
                          payload[0].payload &&
                          payload[0].payload.tooltipLabel
                        ) {
                          return payload[0].payload.tooltipLabel;
                        }
                        return _;
                      }}
                    />
                  }
                />
                <ChartLegend content={<ChartLegendContent />} />
                <Bar
                  dataKey="inCount"
                  fill="var(--color-inCount)"
                  radius={[3, 3, 0, 0]}
                  maxBarSize={maxBarSize}
                />
                <Bar
                  dataKey="outCount"
                  fill="var(--color-outCount)"
                  radius={[3, 3, 0, 0]}
                  maxBarSize={maxBarSize}
                />
              </BarChart>
            </ChartContainer>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
