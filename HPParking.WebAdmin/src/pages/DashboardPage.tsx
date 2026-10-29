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
import {
  getLocalToday,
  getLocalPastDate,
  getLocalMonthStart,
  getLocalMonthEnd,
} from '@/utils/dateUtils';

export function DashboardPage() {
  const [filter, setFilter] = useState<DashboardFilterState>(() => {
    const today = getLocalToday();
    const weekAgo = getLocalPastDate(6);
    return {
      type: 'day',
      date: today,
      month: getLocalMonthStart(),
      year: today.slice(0, 4),
      customFrom: weekAgo,
      customTo: today,
    };
  });

  const dateRange = (() => {
    if (filter.type === 'day') {
      return {
        fromDate: `${filter.date}T00:00:00`,
        toDate: `${filter.date}T23:59:59`,
      };
    }
    if (filter.type === 'month') {
      const [yearStr, mStr] = (filter.month || '').split('-');
      const year = Number(yearStr);
      const month = Number(mStr);
      if (!Number.isInteger(year) || !Number.isInteger(month)) {
        // Tháng bộ lọc không hợp lệ: fallback về tháng hiện tại (local)
        return {
          fromDate: `${getLocalMonthStart()}T00:00:00`,
          toDate: `${getLocalMonthEnd()}T23:59:59`,
        };
      }
      const daysInMonth = new Date(year, month, 0).getDate();
      return {
        fromDate: `${filter.month}-01T00:00:00`,
        toDate: `${filter.month}-${String(daysInMonth).padStart(2, '0')}T23:59:59`,
      };
    }
    if (filter.type === 'year') {
      return {
        fromDate: `${filter.year}-01-01T00:00:00`,
        toDate: `${filter.year}-12-31T23:59:59`,
      };
    }
    if (filter.type === 'custom') {
      return {
        fromDate: `${filter.customFrom}T00:00:00`,
        toDate: `${filter.customTo}T23:59:59`,
      };
    }
    return undefined;
  })();

  const {
    data: kpiData,
    isLoading: isKpiLoading,
    isRefetching: isKpiRefetching,
    refetch: refetchKpis,
    error: kpiError,
  } = useDashboardKPIs(dateRange);

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
