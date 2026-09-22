import { apiClient } from '@/api/client';
import type { ApiResponse } from '@/types/auth';
import type {
  DashboardStatisticsDto,
  DistributionStatisticsDto,
  TrafficSummaryItemDto,
} from '@/types/statistics';

export const statisticsApi = {
  /**
   * Lấy toàn bộ chỉ số KPIs thời gian thực cho trang Dashboard
   */
  async getDashboardStatistics(): Promise<DashboardStatisticsDto> {
    const response = await apiClient.get<ApiResponse<DashboardStatisticsDto>>(
      '/api/v1/statistics/dashboard'
    );
    return response.data.data!;
  },

  /**
   * Lấy ma trận phân bổ khách hàng, phương tiện và hạ tầng theo đơn vị (Công ty/Phòng ban)
   */
  async getDistributionStatistics(
    companyId?: string
  ): Promise<DistributionStatisticsDto> {
    const response = await apiClient.get<ApiResponse<DistributionStatisticsDto>>(
      '/api/v1/statistics/distribution',
      {
        params: companyId ? { companyId } : undefined,
      }
    );
    return response.data.data!;
  },

  /**
   * Lấy báo cáo tổng hợp lưu lượng ra vào
   */
  async getTrafficSummary(params?: {
    fromDate?: string;
    toDate?: string;
    personId?: string;
  }): Promise<TrafficSummaryItemDto[]> {
    const response = await apiClient.get<ApiResponse<TrafficSummaryItemDto[]>>(
      '/api/v1/statistics/traffic-summary',
      { params }
    );
    return response.data.data!;
  },
};
