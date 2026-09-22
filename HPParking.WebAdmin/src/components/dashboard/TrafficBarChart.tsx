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

const hourlyData = [
  { time: '00:00', inCount: 12, outCount: 6 },
  { time: '02:00', inCount: 8, outCount: 4 },
  { time: '04:00', inCount: 15, outCount: 9 },
  { time: '06:00', inCount: 68, outCount: 22 },
  { time: '08:00', inCount: 165, outCount: 48 },
  { time: '10:00', inCount: 94, outCount: 72 },
  { time: '12:00', inCount: 78, outCount: 86 },
  { time: '14:00', inCount: 82, outCount: 64 },
  { time: '16:00', inCount: 104, outCount: 158 },
  { time: '18:00', inCount: 62, outCount: 184 },
  { time: '20:00', inCount: 35, outCount: 76 },
  { time: '22:00', inCount: 18, outCount: 32 },
];

const monthlyData = [
  { time: '01 - 04', inCount: 640, outCount: 590 },
  { time: '05 - 08', inCount: 760, outCount: 720 },
  { time: '09 - 12', inCount: 690, outCount: 670 },
  { time: '13 - 16', inCount: 810, outCount: 780 },
  { time: '17 - 20', inCount: 850, outCount: 830 },
  { time: '21 - 24', inCount: 780, outCount: 760 },
  { time: '25 - 28', inCount: 730, outCount: 710 },
  { time: '29 - 31', inCount: 560, outCount: 540 },
];

const yearlyData = [
  { time: 'Thg 1', inCount: 4520, outCount: 4410 },
  { time: 'Thg 2', inCount: 3890, outCount: 3750 },
  { time: 'Thg 3', inCount: 5120, outCount: 4980 },
  { time: 'Thg 4', inCount: 4890, outCount: 4720 },
  { time: 'Thg 5', inCount: 5340, outCount: 5180 },
  { time: 'Thg 6', inCount: 5620, outCount: 5480 },
  { time: 'Thg 7', inCount: 5410, outCount: 5290 },
  { time: 'Thg 8', inCount: 5800, outCount: 5650 },
  { time: 'Thg 9', inCount: 4950, outCount: 4810 },
  { time: 'Thg 10', inCount: 5210, outCount: 5090 },
  { time: 'Thg 11', inCount: 5100, outCount: 4950 },
  { time: 'Thg 12', inCount: 5780, outCount: 5620 },
];

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

function getCustomRangeData(from?: string, to?: string) {
  if (!from || !to) return monthlyData;

  const startDate = new Date(from);
  const endDate = new Date(to);
  const diffTime = Math.abs(endDate.getTime() - startDate.getTime());
  const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24)) + 1;

  if (diffDays <= 1) {
    return hourlyData;
  }

  if (diffDays <= 10) {
    const data = [];
    const curr = new Date(startDate);
    while (curr <= endDate) {
      const day = String(curr.getDate()).padStart(2, '0');
      const month = String(curr.getMonth() + 1).padStart(2, '0');
      data.push({
        time: `${day}/${month}`,
        inCount: 380 + Math.floor(Math.sin(curr.getDate()) * 150 + 120),
        outCount: 360 + Math.floor(Math.cos(curr.getDate()) * 140 + 110),
      });
      curr.setDate(curr.getDate() + 1);
    }
    return data;
  }

  // Chia thành 6-7 mốc khoảng
  const stepDays = Math.max(1, Math.floor(diffDays / 6));
  const data = [];
  const curr = new Date(startDate);
  while (curr <= endDate) {
    const next = new Date(curr);
    next.setDate(next.getDate() + stepDays - 1);
    const actualNext = next > endDate ? endDate : next;
    
    const startStr = `${String(curr.getDate()).padStart(2, '0')}/${String(curr.getMonth() + 1).padStart(2, '0')}`;
    const endStr = `${String(actualNext.getDate()).padStart(2, '0')}/${String(actualNext.getMonth() + 1).padStart(2, '0')}`;
    
    data.push({
      time: startStr === endStr ? startStr : `${startStr}-${endStr}`,
      inCount: (stepDays * 450) + Math.floor(Math.random() * 100),
      outCount: (stepDays * 430) + Math.floor(Math.random() * 100),
    });

    curr.setDate(curr.getDate() + stepDays);
  }
  return data;
}

interface TrafficBarChartProps {
  isLoading?: boolean;
  filter?: DashboardFilterState;
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
}: TrafficBarChartProps) {
  let chartData = hourlyData;
  let periodDescription = `Lưu lượng xe theo các khung giờ ngày ${formatDateDisplay(filter.date)}`;

  if (filter.type === 'day') {
    chartData = hourlyData;
    periodDescription = `Lưu lượng xe theo các khung giờ ngày ${formatDateDisplay(filter.date)}`;
  } else if (filter.type === 'month') {
    chartData = monthlyData;
    periodDescription = `Lưu lượng xe theo các giai đoạn trong tháng ${formatMonthDisplay(filter.month)}`;
  } else if (filter.type === 'year') {
    chartData = yearlyData;
    periodDescription = `Lưu lượng xe qua 12 tháng năm ${filter.year}`;
  } else if (filter.type === 'custom') {
    chartData = getCustomRangeData(filter.customFrom, filter.customTo);
    periodDescription = `Lưu lượng xe từ ${formatDateDisplay(filter.customFrom)} đến ${formatDateDisplay(filter.customTo)}`;
  }

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

        {/* Quick KPI pills - đồng bộ theo bộ lọc dùng chung */}
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
          <ChartContainer config={chartConfig} className="h-[290px] w-full">
            <BarChart
              data={chartData}
              margin={{ top: 10, right: 10, left: -20, bottom: 0 }}
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
                className="text-[11px] fill-muted-foreground font-medium"
              />
              <YAxis
                tickLine={false}
                axisLine={false}
                tickMargin={8}
                className="text-[11px] fill-muted-foreground"
              />
              <ChartTooltip
                cursor={{ fill: 'hsl(var(--muted) / 0.3)' }}
                content={<ChartTooltipContent indicator="dot" />}
              />
              <ChartLegend content={<ChartLegendContent />} />
              <Bar
                dataKey="inCount"
                fill="var(--color-inCount)"
                radius={[4, 4, 0, 0]}
                maxBarSize={32}
              />
              <Bar
                dataKey="outCount"
                fill="var(--color-outCount)"
                radius={[4, 4, 0, 0]}
                maxBarSize={32}
              />
            </BarChart>
          </ChartContainer>
        )}
      </CardContent>
    </Card>
  );
}
