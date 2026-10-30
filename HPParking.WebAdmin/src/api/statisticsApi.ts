import { apiClient } from '@/api/client';
import type { ApiResponse } from '@/types/auth';
import type { ExportBlob } from '@/types/excel';
import type {
  DashboardStatisticsDto,
  RevenueReportDto,
  RevenueStatisticsQuery,
  TrafficSummaryFilterQuery,
  TrafficSummaryItemDto,
} from '@/types/statistics';

export const statisticsApi = {
  /**
   * Lấy toàn bộ chỉ số KPIs thời gian thực cho trang Dashboard (hỗ trợ lọc thời gian OutTime)
   */
  async getDashboardStatistics(
    params?: RevenueStatisticsQuery
  ): Promise<DashboardStatisticsDto> {
    const response = await (params
      ? apiClient.get<ApiResponse<DashboardStatisticsDto>>('/v1/statistics/dashboard', { params })
      : apiClient.get<ApiResponse<DashboardStatisticsDto>>('/v1/statistics/dashboard'));
    return response.data.data!;
  },

  /**
   * Lấy báo cáo thống kê doanh thu và lượt xe theo từng ngày (Timeline Zero-Fill)
   */
  async getRevenueReport(
    query?: RevenueStatisticsQuery
  ): Promise<RevenueReportDto> {
    const response = await (query
      ? apiClient.get<ApiResponse<RevenueReportDto>>('/v1/statistics/revenue', { params: query })
      : apiClient.get<ApiResponse<RevenueReportDto>>('/v1/statistics/revenue'));
    return response.data.data!;
  },

  /**
   * Xuất báo cáo doanh thu 2 sheet ra tệp Excel (.xlsx)
   */
  async exportRevenueReportExcel(
    query?: RevenueStatisticsQuery
  ): Promise<ExportBlob> {
    const response = await apiClient.get<Blob>(
      '/v1/excel/reports/revenue/export',
      {
        ...(query ? { params: query } : {}),
        responseType: 'blob',
      }
    );
    const isTruncated =
      response.headers?.['x-export-truncated'] === 'true' ||
      response.headers?.['X-Export-Truncated'] === 'true';
    const blob = response.data as ExportBlob;
    blob.isTruncated = isTruncated;
    return blob;
  },

  /**
   * Lấy báo cáo tổng hợp lưu lượng ra vào (TrafficSummaryFilterQuery)
   */
  async getTrafficSummary(
    params?: TrafficSummaryFilterQuery
  ): Promise<TrafficSummaryItemDto[]> {
    const response = await apiClient.get<ApiResponse<TrafficSummaryItemDto[]>>(
      '/v1/statistics/traffic-summary',
      { params }
    );
    return response.data.data!;
  },
};
