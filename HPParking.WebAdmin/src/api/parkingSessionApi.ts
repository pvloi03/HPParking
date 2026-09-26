import axios from 'axios';
import { apiClient } from '@/api/client';
import type { ApiResponse } from '@/types/auth';
import type { PagedResult } from '@/types/masterData';
import type {
  ParkingSessionDetailDto,
  ParkingSessionDto,
  ParkingSessionFilterQuery,
} from '@/types/parkingSession';

/**
 * Trích xuất thông báo lỗi thân thiện khi gọi API phiên đỗ xe
 */
export function extractErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as ApiResponse<unknown> | undefined;

    if (data?.message) {
      if (data.message.includes('PARKING_SESSION_NOT_FOUND')) {
        return 'Không tìm thấy thông tin phiên đỗ xe trên hệ thống.';
      }
      return data.message;
    }

    if (data?.errors && data.errors.length > 0) {
      return data.errors.join('. ');
    }

    if (error.response?.status === 403) {
      return 'Bạn không có quyền thực hiện thao tác này (403 Forbidden).';
    }

    if (error.response?.status === 404) {
      return 'Không tìm thấy thông tin phiên đỗ xe.';
    }

    if (error.message) {
      return error.message;
    }
  }

  if (error instanceof Error) {
    return error.message;
  }

  return 'Đã xảy ra lỗi không xác định khi kết nối dịch vụ phiên đỗ xe.';
}

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

  /**
   * Xuất danh sách lịch sử phiên đỗ xe ra tệp Excel (.xlsx) theo bộ lọc
   */
  async exportParkingSessions(
    query?: ParkingSessionFilterQuery
  ): Promise<Blob> {
    const response = await apiClient.get<Blob>(
      '/v1/excel/parking-sessions/export',
      {
        params: query,
        responseType: 'blob',
      }
    );
    return response.data;
  },
};
