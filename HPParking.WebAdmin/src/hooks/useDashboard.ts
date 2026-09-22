import { useQuery } from '@tanstack/react-query';
import { statisticsApi } from '@/api/statisticsApi';
import { parkingSessionApi } from '@/api/parkingSessionApi';

export const DASHBOARD_QUERY_KEYS = {
  kpis: ['statistics', 'dashboard'] as const,
  recentSessions: (pageSize = 5) =>
    ['parking-sessions', 'recent', pageSize] as const,
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
