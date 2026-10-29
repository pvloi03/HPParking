import {
  Users,
  Car,
  ShieldCheck,
  Receipt,
  Route,
  AlertTriangle,
} from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import type { DashboardStatisticsDto } from '@/types/statistics';
import type { KpiCardItem } from '@/types/dashboard';
import { KpiCard } from './KpiCard';

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

  const kpis: KpiCardItem[] = [
    {
      title: 'Nhân Sự',
      value: data.totalClients.toLocaleString(),
      subtext: `Đã kích hoạt: ${data.activeClients.toLocaleString()}`,
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
      title: 'Doanh Thu',
      value: `${(data.totalRevenue ?? 0).toLocaleString('vi-VN')} đ`,
      subtext: `Đã thu: ${(data.paidParkingSessionsCount ?? 0).toLocaleString('vi-VN')} lượt`,
      icon: Receipt,
      iconColor: 'text-emerald-600 dark:text-emerald-400',
      iconBg: 'bg-emerald-50 dark:bg-emerald-950/50',
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
      {kpis.map((kpi, idx) => (
        <KpiCard key={idx} item={kpi} />
      ))}
    </div>
  );
}
