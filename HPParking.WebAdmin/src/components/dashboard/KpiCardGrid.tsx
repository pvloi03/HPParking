import {
  Users,
  Car,
  ShieldCheck,
  Building2,
  Route,
  AlertTriangle,
} from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import type { DashboardStatisticsDto } from '@/types/statistics';

interface KpiCardGridProps {
  data?: DashboardStatisticsDto;
  isLoading?: boolean;
}

export function KpiCardGrid({ data, isLoading }: KpiCardGridProps) {
  if (isLoading || !data) {
    return (
      <div className="grid gap-4 grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-5">
        {Array.from({ length: 5 }).map((_, i) => (
          <Card key={i} className="shadow-xs border-border/80">
            <CardContent className="p-4 space-y-3">
              <div className="flex items-center justify-between">
                <Skeleton className="h-3.5 w-24" />
                <Skeleton className="h-8 w-8 rounded-xl" />
              </div>
              <Skeleton className="h-7 w-16" />
              <Skeleton className="h-3 w-32" />
            </CardContent>
          </Card>
        ))}
      </div>
    );
  }

  const laneActivePercentage =
    data.totalLanes > 0
      ? Math.round((data.activeLanes / data.totalLanes) * 100)
      : 100;

  const isHighCapacity = data.activeParkingSessions >= 180;

  const kpis = [
    {
      title: 'Khách Hàng',
      value: data.totalClients.toLocaleString(),
      subtext: `Đang hoạt động: ${data.activeClients.toLocaleString()}`,
      icon: Users,
      iconColor: 'text-blue-600 dark:text-blue-400',
      iconBg: 'bg-blue-50 dark:bg-blue-950/50',
    },
    {
      title: 'Phương Tiện',
      value: data.totalVehicles.toLocaleString(),
      subtext: `Kích hoạt: ${data.activeVehicles.toLocaleString()} xe`,
      icon: Car,
      iconColor: 'text-emerald-600 dark:text-emerald-400',
      iconBg: 'bg-emerald-50 dark:bg-emerald-950/50',
    },
    {
      title: 'Xe Đang Đỗ',
      value: data.activeParkingSessions.toLocaleString(),
      subtext: isHighCapacity
        ? 'Cảnh báo: Dung lượng sắp đầy'
        : 'Theo dõi thời gian thực',
      icon: isHighCapacity ? AlertTriangle : ShieldCheck,
      iconColor: isHighCapacity
        ? 'text-red-600 dark:text-red-400'
        : 'text-amber-600 dark:text-amber-400',
      iconBg: isHighCapacity
        ? 'bg-red-50 dark:bg-red-950/50'
        : 'bg-amber-50 dark:bg-amber-950/50',
      isWarning: isHighCapacity,
    },
    {
      title: 'Cổng Kiểm Soát',
      value: data.totalGates.toLocaleString(),
      subtext: 'Trạm điều khiển trung tâm',
      icon: Building2,
      iconColor: 'text-indigo-600 dark:text-indigo-400',
      iconBg: 'bg-indigo-50 dark:bg-indigo-950/50',
    },
    {
      title: 'Làn Vận Hành',
      value: `${data.activeLanes} / ${data.totalLanes}`,
      subtext: `Tỷ lệ trực tuyến: ${laneActivePercentage}%`,
      icon: Route,
      iconColor: 'text-purple-600 dark:text-purple-400',
      iconBg: 'bg-purple-50 dark:bg-purple-950/50',
    },
  ];

  return (
    <div className="grid gap-4 grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-5">
      {kpis.map((kpi, idx) => {
        const Icon = kpi.icon;
        return (
          <Card
            key={idx}
            className={`transition-all duration-200 hover:shadow-md border-border/80 ${
              kpi.isWarning ? 'border-red-300 dark:border-red-900/60 bg-red-50/20' : ''
            }`}
          >
            <CardContent className="p-4">
              <div className="flex items-center justify-between">
                <span className="text-xs font-semibold text-muted-foreground truncate">
                  {kpi.title}
                </span>
                <div
                  className={`h-8 w-8 rounded-xl ${kpi.iconBg} ${kpi.iconColor} flex items-center justify-center shrink-0 shadow-2xs`}
                >
                  <Icon className="h-4 w-4" />
                </div>
              </div>
              <div className="mt-2.5">
                <div className="text-xl font-bold tracking-tight text-foreground">
                  {kpi.value}
                </div>
                <p
                  className={`text-[11px] mt-0.5 truncate ${
                    kpi.isWarning
                      ? 'text-red-600 dark:text-red-400 font-medium'
                      : 'text-muted-foreground'
                  }`}
                >
                  {kpi.subtext}
                </p>
              </div>
            </CardContent>
          </Card>
        );
      })}
    </div>
  );
}
