import { useState } from 'react';
import { AlertCircle } from 'lucide-react';
import { KpiCardGrid } from '@/components/dashboard/KpiCardGrid';
import { TrafficBarChart } from '@/components/dashboard/TrafficBarChart';
import { DashboardFilterBar } from '@/components/dashboard/DashboardFilterBar';
import {
  useDashboardKPIs,
  useTrafficSessions,
} from '@/hooks/useDashboard';
import type { DashboardFilterState } from '@/types/dashboard';

export function DashboardPage() {
  const [filter, setFilter] = useState<DashboardFilterState>(() => {
    const today = new Date().toISOString().split('T')[0];
    const month = today.slice(0, 7);
    const year = today.slice(0, 4);
    const weekAgo = new Date(Date.now() - 6 * 86400000).toISOString().split('T')[0];
    return {
      type: 'day',
      date: today,
      month,
      year,
      customFrom: weekAgo,
      customTo: today,
    };
  });

  const {
    data: kpiData,
    isLoading: isKpiLoading,
    isRefetching: isKpiRefetching,
    refetch: refetchKpis,
    error: kpiError,
  } = useDashboardKPIs();

  const {
    data: trafficSessions,
    isLoading: isTrafficLoading,
    isRefetching: isTrafficRefetching,
    refetch: refetchTraffic,
  } = useTrafficSessions(filter);

  const isRefreshing = isKpiRefetching || isTrafficRefetching;

  const handleRefresh = async () => {
    await Promise.all([refetchKpis(), refetchTraffic()]);
  };

  return (
    <div className="space-y-6">
      {/* Top Control Bar: Title & Unified Filter Bar */}
      <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-4">
        <div>
          <h1 className="text-xl font-bold tracking-tight text-foreground">
            Tổng Quan Hoạt Động Bãi Xe
          </h1>
          <p className="text-xs text-muted-foreground mt-0.5">
            Dữ liệu giám sát tự động cập nhật thời gian thực từ API HPParking
          </p>
        </div>

        {/* Thanh bộ lọc dùng chung: Ngày, Tháng, Năm, Tùy chọn & Nút làm mới */}
        <DashboardFilterBar
          filter={filter}
          onFilterChange={setFilter}
          onRefresh={handleRefresh}
          isRefreshing={isRefreshing}
        />
      </div>

      {kpiError && (
        <div className="p-3.5 rounded-xl bg-amber-50 dark:bg-amber-950/40 border border-amber-200 dark:border-amber-800/60 text-amber-800 dark:text-amber-300 text-xs flex items-center gap-2.5">
          <AlertCircle className="h-4 w-4 shrink-0" />
          <span>
            Không thể kết nối đến dịch vụ thống kê. Đang hiển thị bộ nhớ đệm
            hoặc vui lòng kiểm tra kết nối API.
          </span>
        </div>
      )}

      {/* 5 Primary KPI Cards */}
      <KpiCardGrid data={kpiData} isLoading={isKpiLoading} />

      {/* Traffic Flow Bar Chart (shadcn/ui + Recharts) - Đồng bộ theo bộ lọc dùng chung và dữ liệu thật */}
      <TrafficBarChart
        isLoading={isTrafficLoading || isKpiLoading || isRefreshing}
        filter={filter}
        sessions={trafficSessions ?? []}
      />
    </div>
  );
}

export default DashboardPage;
