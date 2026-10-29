import { describe, it, expect, vi, beforeEach } from 'vitest';
import { statisticsApi } from '@/api/statisticsApi';
import { apiClient } from '@/api/client';

vi.mock('@/api/client', () => ({
  apiClient: {
    get: vi.fn(),
  },
}));

describe('statisticsApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('gọi đúng endpoint GET /api/v1/statistics/dashboard và trả về data', async () => {
    const mockDashboardData = {
      totalClients: 150,
      activeClients: 140,
      clientsByType: { Resident: 120, Guest: 30 },
      clientsWithFaceId: 135,
      faceIdSyncRatePercentage: 90.0,
      totalVehicles: 180,
      activeVehicles: 165,
      vehiclesByType: { Car: 60, Motorcycle: 120 },
      activeParkingSessions: 42,
      totalGates: 3,
      totalLanes: 8,
      activeLanes: 8,
    };

    vi.mocked(apiClient.get).mockResolvedValueOnce({
      data: {
        success: true,
        data: mockDashboardData,
        message: 'Lấy số liệu thống kê Dashboard thành công.',
      },
    });

    const result = await statisticsApi.getDashboardStatistics();

    expect(apiClient.get).toHaveBeenCalledWith('/v1/statistics/dashboard');
    expect(result).toEqual(mockDashboardData);
    expect(result.activeParkingSessions).toBe(42);
    expect(result.activeLanes).toBe(8);
  });

  it('gọi GET /v1/statistics/dashboard kèm params khi có query', async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce({
      data: {
        success: true,
        data: {},
      },
    });

    const params = { fromDate: '2026-10-28T00:00:00', toDate: '2026-10-28T23:59:59' };
    await statisticsApi.getDashboardStatistics(params);

    expect(apiClient.get).toHaveBeenCalledWith('/v1/statistics/dashboard', { params });
  });

  it('gọi đúng endpoint GET /v1/statistics/revenue kèm params', async () => {
    const mockRevenueData = {
      totalRevenue: 500000,
      paidSessionsCount: 10,
      completedSessionsCount: 15,
      averageFeePerSession: 50000,
      paidRatio: 66.7,
      timeline: [],
    };

    vi.mocked(apiClient.get).mockResolvedValueOnce({
      data: {
        success: true,
        data: mockRevenueData,
      },
    });

    const query = { fromDate: '2026-10-20T00:00:00', toDate: '2026-10-22T23:59:59' };
    const result = await statisticsApi.getRevenueReport(query);

    expect(apiClient.get).toHaveBeenCalledWith('/v1/statistics/revenue', { params: query });
    expect(result).toEqual(mockRevenueData);
  });

  it('gọi đúng endpoint GET /v1/excel/reports/revenue/export và đọc header x-export-truncated', async () => {
    const mockBlob = new Blob(['excel data']);
    vi.mocked(apiClient.get).mockResolvedValueOnce({
      data: mockBlob,
      headers: {
        'x-export-truncated': 'true',
      },
    });

    const query = { fromDate: '2026-10-01T00:00:00', toDate: '2026-10-31T23:59:59' };
    const result = await statisticsApi.exportRevenueReportExcel(query);

    expect(apiClient.get).toHaveBeenCalledWith(
      '/v1/excel/reports/revenue/export',
      {
        params: query,
        responseType: 'blob',
      }
    );
    expect(result.isTruncated).toBe(true);
  });
});
