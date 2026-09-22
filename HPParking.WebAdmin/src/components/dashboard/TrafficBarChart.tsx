import { useState } from 'react';
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

// Dữ liệu mẫu theo khung giờ hôm nay
const hourlyData = [
  { time: '06:00', inCount: 42, outCount: 12 },
  { time: '08:00', inCount: 156, outCount: 38 },
  { time: '10:00', inCount: 88, outCount: 65 },
  { time: '12:00', inCount: 64, outCount: 72 },
  { time: '14:00', inCount: 75, outCount: 58 },
  { time: '16:00', inCount: 92, outCount: 142 },
  { time: '18:00', inCount: 52, outCount: 168 },
  { time: '20:00', inCount: 28, outCount: 64 },
];

// Dữ liệu theo 7 ngày qua
const weeklyData = [
  { time: 'Thứ 2', inCount: 580, outCount: 540 },
  { time: 'Thứ 3', inCount: 620, outCount: 595 },
  { time: 'Thứ 4', inCount: 645, outCount: 610 },
  { time: 'Thứ 5', inCount: 590, outCount: 575 },
  { time: 'Thứ 6', inCount: 710, outCount: 690 },
  { time: 'Thứ 7', inCount: 420, outCount: 395 },
  { time: 'Chủ Nhật', inCount: 310, outCount: 290 },
];

interface TrafficBarChartProps {
  isLoading?: boolean;
}

export function TrafficBarChart({ isLoading }: TrafficBarChartProps) {
  const [range, setRange] = useState<'today' | 'week'>('today');

  const chartData = range === 'today' ? hourlyData : weeklyData;

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
            Lưu lượng xe quét thẻ và nhận diện biển số qua cổng theo thời gian
          </CardDescription>
        </div>

        <div className="flex items-center gap-2">
          {/* Quick Period Selector */}
          <div className="flex items-center rounded-lg border border-border bg-muted/30 p-0.5 text-xs">
            <button
              type="button"
              onClick={() => setRange('today')}
              className={`px-2.5 py-1 rounded-md font-medium transition-colors cursor-pointer text-xs ${range === 'today'
                ? 'bg-card text-foreground shadow-2xs font-semibold'
                : 'text-muted-foreground hover:text-foreground'
                }`}
            >
              Hôm Nay
            </button>
            <button
              type="button"
              onClick={() => setRange('week')}
              className={`px-2.5 py-1 rounded-md font-medium transition-colors cursor-pointer text-xs ${range === 'week'
                ? 'bg-card text-foreground shadow-2xs font-semibold'
                : 'text-muted-foreground hover:text-foreground'
                }`}
            >
              7 Ngày Qua
            </button>
          </div>

          {/* Quick KPI pills */}
          <div className="hidden md:flex items-center gap-2 text-xs">
            <span className="flex items-center gap-1 px-2.5 py-1 rounded-md bg-blue-50 dark:bg-blue-950/40 text-blue-700 dark:text-blue-300 font-medium border border-blue-200/60 dark:border-blue-900/40">
              <ArrowDownRight className="h-3.5 w-3.5 text-blue-600" />
              Vào: <strong>{totalIn.toLocaleString()}</strong>
            </span>
            <span className="flex items-center gap-1 px-2.5 py-1 rounded-md bg-emerald-50 dark:bg-emerald-950/40 text-emerald-700 dark:text-emerald-300 font-medium border border-emerald-200/60 dark:border-emerald-900/40">
              <ArrowUpRight className="h-3.5 w-3.5 text-emerald-600" />
              Ra: <strong>{totalOut.toLocaleString()}</strong>
            </span>
          </div>
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
