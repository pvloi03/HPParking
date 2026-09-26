import { useQuery } from '@tanstack/react-query';
import { statisticsApi } from '@/api/statisticsApi';
import { parkingSessionApi } from '@/api/parkingSessionApi';
import type { DashboardFilterState } from '@/types/dashboard';

export const DASHBOARD_QUERY_KEYS = {
  kpis: ['statistics', 'dashboard'] as const,
  recentSessions: (pageSize = 5) =>
    ['parking-sessions', 'recent', pageSize] as const,
  trafficSessions: (filter: DashboardFilterState) =>
    ['parking-sessions', 'traffic-chart', filter.type, filter.date, filter.month, filter.year, filter.customFrom, filter.customTo] as const,
};

/**
 * Hook truy vấn 6 chỉ số KPIs thời gian thực cho trang Dashboard
 */
export function useDashboardKPIs() {
  return useQuery({
    queryKey: DASHBOARD_QUERY_KEYS.kpis,
    queryFn: () => statisticsApi.getDashboardStatistics(),
    staleTime: 60 * 1000,
    refetchOnWindowFocus: true,
  });
}

/**
 * Hook truy vấn danh sách các phiên đỗ xe vào/ra gần nhất
 */
export function useRecentSessions(pageSize = 5) {
  return useQuery({
    queryKey: DASHBOARD_QUERY_KEYS.recentSessions(pageSize),
    queryFn: () => parkingSessionApi.getRecentSessions(pageSize),
    staleTime: 30 * 1000,
    refetchOnWindowFocus: true,
  });
}

/**
 * Hook truy vấn danh sách phiên đỗ xe thực tế để tổng hợp lưu lượng theo bộ lọc
 */
export function useTrafficSessions(filter: DashboardFilterState) {
  let toDate: string | undefined;

  if (filter.type === 'day') {
    toDate = `${filter.date}T23:59:59`;
  } else if (filter.type === 'month') {
    const [yearStr, mStr] = (filter.month || '2026-09').split('-');
    const year = parseInt(yearStr, 10) || 2026;
    const month = parseInt(mStr, 10) || 9;
    const daysInMonth = new Date(year, month, 0).getDate();
    toDate = `${filter.month}-${String(daysInMonth).padStart(2, '0')}T23:59:59`;
  } else if (filter.type === 'year') {
    toDate = `${filter.year}-12-31T23:59:59`;
  } else if (filter.type === 'custom') {
    toDate = `${filter.customTo}T23:59:59`;
  }

  return useQuery({
    queryKey: DASHBOARD_QUERY_KEYS.trafficSessions(filter),
    queryFn: async () => {
      const res = await parkingSessionApi.getParkingSessions({
        pageIndex: 1,
        pageSize: 100,
        toDate,
      });
      return res.items ?? [];
    },
    staleTime: 30 * 1000,
    refetchOnWindowFocus: true,
  });
}

