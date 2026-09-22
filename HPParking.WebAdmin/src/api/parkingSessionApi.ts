import { apiClient } from '@/api/client';
import type { ApiResponse } from '@/types/auth';
import type {
  PagedResult,
  ParkingSessionDto,
  ParkingSessionFilterQuery,
} from '@/types/parkingSession';

export const parkingSessionApi = {
  /**
   * Tra cứu danh sách phiên đỗ xe có phân trang và bộ lọc
   */
  async getParkingSessions(
    query?: ParkingSessionFilterQuery
  ): Promise<PagedResult<ParkingSessionDto>> {
    const response = await apiClient.get<
      ApiResponse<PagedResult<ParkingSessionDto>>
    >('/api/v1/parking-sessions', {
      params: query,
    });
    return response.data.data!;
  },

  /**
   * Lấy danh sách các phiên đỗ xe vào/ra gần nhất phục vụ Dashboard
   */
  async getRecentSessions(pageSize = 5): Promise<ParkingSessionDto[]> {
    const result = await this.getParkingSessions({
      pageIndex: 1,
      pageSize,
    });
    return result.items ?? [];
  },

  /**
   * Lấy chi tiết phiên đỗ xe theo Id kèm ảnh bằng chứng
   */
  async getSessionById(id: string): Promise<ParkingSessionDto> {
    const response = await apiClient.get<ApiResponse<ParkingSessionDto>>(
      `/api/v1/parking-sessions/${id}`
    );
    return response.data.data!;
  },
};
