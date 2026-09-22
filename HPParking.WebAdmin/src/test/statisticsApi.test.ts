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

  it('gọi đúng endpoint GET /v1/statistics/distribution kèm query companyId', async () => {
    const mockDistributionData = {
      totalFilteredClients: 100,
      totalFilteredVehicles: 120,
      totalFilteredGates: 2,
      totalFilteredLanes: 4,
      items: [
        {
          companyId: 'comp-1',
          companyName: 'Công ty Cổ phần HPParking',
          departmentId: 'dept-1',
          departmentName: 'Phòng Kỹ Thuật',
          clientCount: 40,
          vehicleCount: 50,
          gateCount: 2,
          laneCount: 4,
        },
      ],
    };

    vi.mocked(apiClient.get).mockResolvedValueOnce({
      data: {
        success: true,
        data: mockDistributionData,
        message: 'Thành công.',
      },
    });

    const result = await statisticsApi.getDistributionStatistics('comp-1');

    expect(apiClient.get).toHaveBeenCalledWith(
      '/v1/statistics/distribution',
      {
        params: { companyId: 'comp-1' },
      }
    );
    expect(result.items?.[0].companyName).toBe('Công ty Cổ phần HPParking');
  });
});
