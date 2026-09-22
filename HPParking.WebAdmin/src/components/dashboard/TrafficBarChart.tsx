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

/**
 * Sinh dữ liệu trọn vẹn 24/7 (đủ 24 giờ từ 00h đến 23h) cho ngày đã chọn
 */
function generate24HoursData(dateStr?: string) {
  const data = [];
  const seed = dateStr ? dateStr.charCodeAt(dateStr.length - 1) : 5;

  for (let h = 0; h < 24; h++) {
    const hourLabel = `${String(h).padStart(2, '0')}h`;
    const nextHour = (h + 1) % 24;
    const fullTimeRange = `${String(h).padStart(2, '0')}:00 - ${String(nextHour).padStart(2, '0')}:00`;

    let baseIn = 6 + (h % 3);
    let baseOut = 4 + (h % 2);

    if (h >= 6 && h <= 8) {
      // Giờ cao điểm sáng vào ca làm việc
      baseIn = 75 + (h === 7 ? 68 : 28) + (seed % 10);
      baseOut = 12 + Math.floor(h * 2);
    } else if (h >= 9 && h <= 11) {
      baseIn = 35 + Math.floor((h % 3) * 12);
      baseOut = 28 + Math.floor((h % 3) * 10);
    } else if (h >= 12 && h <= 13) {
      baseIn = 32 + Math.floor((h % 2) * 8);
      baseOut = 36 + Math.floor((h % 2) * 10);
    } else if (h >= 14 && h <= 15) {
      baseIn = 38;
      baseOut = 32;
    } else if (h >= 16 && h <= 18) {
      // Giờ cao điểm chiều tan ca ra về
      baseIn = 22 + Math.floor((h % 3) * 6);
      baseOut = 85 + (h === 17 ? 78 : 32) + (seed % 10);
    } else if (h >= 19 && h <= 21) {
      baseIn = 16;
      baseOut = 35;
    } else if (h >= 22) {
      // Đêm muộn
      baseIn = 8;
      baseOut = 12;
    }

    data.push({
      time: hourLabel,
      tooltipLabel: `Khung giờ ${fullTimeRange}`,
      inCount: baseIn,
      outCount: baseOut,
    });
  }
  return data;
}

/**
 * Sinh dữ liệu trọn vẹn đủ tất cả các ngày trong tháng (từ ngày 01 đến ngày cuối tháng)
 */
function generateMonthDaysData(monthStr?: string) {
  const [yearStr, mStr] = (monthStr || '2026-09').split('-');
  const year = parseInt(yearStr, 10) || 2026;
  const month = parseInt(mStr, 10) || 9;
  const daysInMonth = new Date(year, month, 0).getDate();

  const data = [];
  for (let d = 1; d <= daysInMonth; d++) {
    const dayStr = String(d).padStart(2, '0');
    const dateObj = new Date(year, month - 1, d);
    const dayOfWeek = dateObj.getDay();

    const isWeekend = dayOfWeek === 0 || dayOfWeek === 6;
    const baseIn = isWeekend ? 210 + (d % 4) * 25 : 540 + ((d * 19) % 180);
    const baseOut = isWeekend ? 195 + (d % 4) * 20 : 520 + ((d * 13) % 170);

    data.push({
      time: `${dayStr}`,
      tooltipLabel: `Ngày ${dayStr}/${String(month).padStart(2, '0')}/${year}`,
      inCount: baseIn,
      outCount: baseOut,
    });
  }
  return data;
}

/**
 * Dữ liệu 12 tháng trọn vẹn trong năm
 */
const yearlyData = [
  { time: 'Thg 1', tooltipLabel: 'Tháng 01', inCount: 4520, outCount: 4410 },
  { time: 'Thg 2', tooltipLabel: 'Tháng 02', inCount: 3890, outCount: 3750 },
  { time: 'Thg 3', tooltipLabel: 'Tháng 03', inCount: 5120, outCount: 4980 },
  { time: 'Thg 4', tooltipLabel: 'Tháng 04', inCount: 4890, outCount: 4720 },
  { time: 'Thg 5', tooltipLabel: 'Tháng 05', inCount: 5340, outCount: 5180 },
  { time: 'Thg 6', tooltipLabel: 'Tháng 06', inCount: 5620, outCount: 5480 },
  { time: 'Thg 7', tooltipLabel: 'Tháng 07', inCount: 5410, outCount: 5290 },
  { time: 'Thg 8', tooltipLabel: 'Tháng 08', inCount: 5800, outCount: 5650 },
  { time: 'Thg 9', tooltipLabel: 'Tháng 09', inCount: 4950, outCount: 4810 },
  { time: 'Thg 10', tooltipLabel: 'Tháng 10', inCount: 5210, outCount: 5090 },
  { time: 'Thg 11', tooltipLabel: 'Tháng 11', inCount: 5100, outCount: 4950 },
  { time: 'Thg 12', tooltipLabel: 'Tháng 12', inCount: 5780, outCount: 5620 },
];

/**
 * Xử lý khoảng ngày tùy chọn
 */
function getCustomRangeData(from?: string, to?: string) {
  if (!from || !to) return generateMonthDaysData();

  const startDate = new Date(from);
  const endDate = new Date(to);
  if (isNaN(startDate.getTime()) || isNaN(endDate.getTime())) {
    return generateMonthDaysData();
  }

  const diffTime = Math.abs(endDate.getTime() - startDate.getTime());
  const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24)) + 1;

  if (diffDays <= 1) {
    return generate24HoursData(from);
  }

  if (diffDays <= 31) {
    const data = [];
    const curr = new Date(startDate);
    while (curr <= endDate) {
      const day = String(curr.getDate()).padStart(2, '0');
      const month = String(curr.getMonth() + 1).padStart(2, '0');
      const dayOfWeek = curr.getDay();
      const isWeekend = dayOfWeek === 0 || dayOfWeek === 6;

      data.push({
        time: `${day}/${month}`,
        tooltipLabel: `Ngày ${day}/${month}/${curr.getFullYear()}`,
        inCount: isWeekend ? 210 + (curr.getDate() % 4) * 20 : 540 + ((curr.getDate() * 17) % 170),
        outCount: isWeekend ? 195 + (curr.getDate() % 4) * 20 : 520 + ((curr.getDate() * 13) % 160),
      });
      curr.setDate(curr.getDate() + 1);
    }
    return data;
  }

  const stepDays = Math.max(1, Math.floor(diffDays / 8));
  const data = [];
  const curr = new Date(startDate);
  while (curr <= endDate) {
    const next = new Date(curr);
    next.setDate(next.getDate() + stepDays - 1);
    const actualNext = next > endDate ? endDate : next;

    const startStr = `${String(curr.getDate()).padStart(2, '0')}/${String(curr.getMonth() + 1).padStart(2, '0')}`;
    const endStr = `${String(actualNext.getDate()).padStart(2, '0')}/${String(actualNext.getMonth() + 1).padStart(2, '0')}`;
    const labelRange = startStr === endStr ? startStr : `${startStr}-${endStr}`;

    data.push({
      time: labelRange,
      tooltipLabel: `Giai đoạn ${labelRange}`,
      inCount: (stepDays * 480) + Math.floor(Math.random() * 80),
      outCount: (stepDays * 460) + Math.floor(Math.random() * 80),
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
  let chartData = generate24HoursData(filter.date);
  let periodDescription = `Lưu lượng xe 24/7 theo từng giờ ngày ${formatDateDisplay(filter.date)} (24 giờ)`;
  let maxBarSize = 16;

  if (filter.type === 'day') {
    chartData = generate24HoursData(filter.date);
    periodDescription = `Lưu lượng xe 24/7 theo từng giờ ngày ${formatDateDisplay(filter.date)} (24 giờ)`;
    maxBarSize = 16;
  } else if (filter.type === 'month') {
    chartData = generateMonthDaysData(filter.month);
    periodDescription = `Lưu lượng xe đầy đủ ${chartData.length} ngày trong tháng ${formatMonthDisplay(filter.month)}`;
    maxBarSize = 14;
  } else if (filter.type === 'year') {
    chartData = yearlyData;
    periodDescription = `Lưu lượng xe qua 12 tháng năm ${filter.year}`;
    maxBarSize = 28;
  } else if (filter.type === 'custom') {
    chartData = getCustomRangeData(filter.customFrom, filter.customTo);
    periodDescription = `Lưu lượng xe từ ${formatDateDisplay(filter.customFrom)} đến ${formatDateDisplay(filter.customTo)} (${chartData.length} mốc)`;
    maxBarSize = chartData.length > 20 ? 14 : 20;
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
