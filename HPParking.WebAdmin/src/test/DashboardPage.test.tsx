import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { DashboardPage } from '@/pages/DashboardPage';
import { statisticsApi } from '@/api/statisticsApi';
import { parkingSessionApi } from '@/api/parkingSessionApi';
import type { DashboardStatisticsDto } from '@/types/statistics';

vi.mock('@/api/statisticsApi', () => ({
  statisticsApi: {
    getDashboardStatistics: vi.fn(),
  },
}));

vi.mock('@/api/parkingSessionApi', () => ({
  parkingSessionApi: {
    getParkingSessions: vi.fn(),
  },
}));

const mockStatistics: DashboardStatisticsDto = {
  totalClients: 100,
  activeClients: 95,
  clientsByType: {},
  clientsWithFaceId: 80,
  faceIdSyncRatePercentage: 84.2,
  totalVehicles: 150,
  activeVehicles: 140,
  vehiclesByType: {},
  activeParkingSessions: 25,
  totalGates: 4,
  totalLanes: 8,
  activeLanes: 8,
  totalRevenue: 5000000,
  paidParkingSessionsCount: 50,
};

function renderWithClient(ui: React.ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
      },
    },
  });
  return render(
    <QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>
  );
}

describe('DashboardPage Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(statisticsApi.getDashboardStatistics).mockResolvedValue(mockStatistics);
    vi.mocked(parkingSessionApi.getParkingSessions).mockResolvedValue({
      items: [],
      pagination: {
        pageIndex: 1,
        pageSize: 100,
        totalCount: 0,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    });
  });

  it('gọi getDashboardStatistics với định dạng ngày chuẩn khi bấm tab Tháng lần đầu', async () => {
    renderWithClient(<DashboardPage />);

    // Chờ tải xong lần đầu (chế độ Ngày)
    await waitFor(() => {
      expect(statisticsApi.getDashboardStatistics).toHaveBeenCalled();
    });

    // Bấm chuyển sang tab Tháng
    const monthTab = screen.getByRole('button', { name: 'Tháng' });
    fireEvent.click(monthTab);

    // Kiểm tra getDashboardStatistics được gọi với dateRange hợp lệ dạng YYYY-MM-01T00:00:00 (không bị -01-01)
    await waitFor(() => {
      const calls = vi.mocked(statisticsApi.getDashboardStatistics).mock.calls;
      const lastCallArg = calls[calls.length - 1][0];
      expect(lastCallArg).toBeDefined();
      expect(lastCallArg?.fromDate).toMatch(/^\d{4}-\d{2}-01T00:00:00$/);
      expect(lastCallArg?.toDate).toMatch(/^\d{4}-\d{2}-\d{2}T23:59:59$/);
      // Đảm bảo không bị lỗi -01-01 lặp
      expect(lastCallArg?.fromDate).not.toContain('-01-01T');
    });
  });
});
