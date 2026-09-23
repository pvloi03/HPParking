import { apiClient } from '@/api/client';
import type { ApiResponse } from '@/types/auth';
import type { PagedResult } from '@/types/masterData';
import type {
  ParkingSessionDetailDto,
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
    >('/v1/parking-sessions', {
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
    return (result.items as ParkingSessionDto[]) ?? [];
  },

  /**
   * Lấy chi tiết phiên đỗ xe theo Id kèm 4 ảnh bằng chứng và thời lượng đỗ xe (ParkingSessionDetailDto)
   */
  async getSessionById(id: string): Promise<ParkingSessionDetailDto> {
    const response = await apiClient.get<ApiResponse<ParkingSessionDetailDto>>(
      `/v1/parking-sessions/${id}`
    );
    return response.data.data!;
  },
};
