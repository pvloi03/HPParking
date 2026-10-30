import { Receipt, TrendingUp, Percent, CreditCard } from 'lucide-react';
import type { RevenueReportDto, KpiCardItem } from '@/types/statistics';
import { KpiCard } from '@/components/dashboard/KpiCard';

export interface RevenueKpiGridProps {
  data?: RevenueReportDto;
  isLoading?: boolean;
}

/**
 * Grid 4 thẻ KPI chỉ số doanh thu (Tổng thu, Lượt thu, Doanh thu TB, Tỷ lệ thu)
 */
export function RevenueKpiGrid({ data, isLoading }: RevenueKpiGridProps) {
  const kpis: KpiCardItem[] = [
    {
      title: 'Tổng Doanh Thu',
      value: `${(data?.totalRevenue ?? 0).toLocaleString('vi-VN')} đ`,
      subtext: 'Doanh thu thực thu trong kỳ',
      icon: Receipt,
      iconColor: 'text-emerald-600 dark:text-emerald-400',
      iconBg: 'bg-emerald-50 dark:bg-emerald-950/50',
    },
    {
      title: 'Lượt Thu Phí',
      value: `${(data?.paidSessionsCount ?? 0).toLocaleString('vi-VN')} lượt`,
      subtext: 'Phiên hoàn thành đã trả phí',
      icon: CreditCard,
      iconColor: 'text-blue-600 dark:text-blue-400',
      iconBg: 'bg-blue-50 dark:bg-blue-950/50',
    },
    {
      title: 'Doanh Thu TB / Lượt',
      value: `${Math.round(data?.averageFeePerSession ?? 0).toLocaleString('vi-VN')} đ/lượt`,
      subtext: 'Bình quân trên lượt thu phí',
      icon: TrendingUp,
      iconColor: 'text-amber-600 dark:text-amber-400',
      iconBg: 'bg-amber-50 dark:bg-amber-950/50',
    },
    {
      title: 'Tỷ Lệ Thu Phí',
      value: `${(data?.paidRatio ?? 0).toFixed(1)}%`,
      subtext: `${(data?.paidSessionsCount ?? 0).toLocaleString('vi-VN')} / ${(data?.completedSessionsCount ?? 0).toLocaleString('vi-VN')} lượt hoàn thành`,
      icon: Percent,
      iconColor: 'text-purple-600 dark:text-purple-400',
      iconBg: 'bg-purple-50 dark:bg-purple-950/50',
    },
  ];

  return (
    <div className="grid gap-4 grid-cols-1 sm:grid-cols-2 lg:grid-cols-4">
      {kpis.map((kpi, idx) => (
        <KpiCard key={idx} item={kpi} isLoading={isLoading} />
      ))}
    </div>
  );
}
