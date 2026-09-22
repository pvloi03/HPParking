import axios from 'axios';
import { apiClient } from './client';
import type { ApiResponse, PagedResult } from '@/types/masterData';
import type {
  VehicleDto,
  VehicleFilterQuery,
  CreateVehicleRequest,
  UpdateVehicleRequest,
} from '@/types/vehicle';

/**
 * Trích xuất thông báo lỗi thân thiện cho người dùng từ AxiosError,
 * đặc biệt ánh xạ các mã lỗi kiểm tra trùng lặp biển số và ràng buộc chủ xe.
 */
export function extractErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as ApiResponse<unknown> | undefined;

    if (data?.message) {
      if (data.message.includes('VEHICLE_PLATE_DUPLICATE') || data.message.includes('đã được đăng ký')) {
        return 'Biển số xe này đã được đăng ký và đang hoạt động trong hệ thống.';
      }
      if (data.message.includes('CLIENT_NOT_FOUND')) {
        return 'Không tìm thấy hồ sơ khách hàng để gán quyền sở hữu xe.';
      }
      if (data.message.includes('VEHICLE_NOT_FOUND')) {
        return 'Không tìm thấy thông tin phương tiện tương ứng.';
      }
      if (data.message.includes('VEHICLE_HAS_ACTIVE_SESSION')) {
        return 'Không thể xóa phương tiện vì xe đang gửi trong bãi đỗ.';
      }
      if (data.message.includes('PARENT_IS_DELETED')) {
        return 'Không thể khôi phục vì khách hàng chủ sở hữu đang nằm trong thùng rác hoặc đã bị xóa.';
      }
      return data.message;
    }

    if (data?.errors && data.errors.length > 0) {
      return data.errors.join('. ');
    }

    if (error.response?.status === 409) {
      return 'Biển số xe bị trùng lặp với một phương tiện khác đang hoạt động (409 Conflict).';
    }
    if (error.response?.status === 403) {
      return 'Bạn không có quyền thực hiện thao tác này (403 Forbidden).';
    }
    if (error.response?.status === 404) {
      return 'Không tìm thấy phương tiện hoặc chủ xe tương ứng (404 Not Found).';
    }

    if (error.message) {
      return error.message;
    }
  }

  if (error instanceof Error) {
    return error.message;
  }

  return 'Đã xảy ra lỗi không xác định. Vui lòng thử lại sau.';
}

// =================== VEHICLES API ===================

export const vehicleApi = {
  getPaged: async (query?: VehicleFilterQuery): Promise<PagedResult<VehicleDto>> => {
    const response = await apiClient.get<ApiResponse<PagedResult<VehicleDto>>>(
      '/v1/vehicles',
      { params: query }
    );
    return response.data.data;
  },

  getById: async (id: string): Promise<VehicleDto> => {
    const response = await apiClient.get<ApiResponse<VehicleDto>>(`/v1/vehicles/${id}`);
    return response.data.data;
  },

  getByClientId: async (clientId: string): Promise<VehicleDto[]> => {
    const response = await apiClient.get<ApiResponse<VehicleDto[]>>(
      `/v1/vehicles/client/${clientId}`
    );
    return response.data.data;
  },

  create: async (payload: CreateVehicleRequest): Promise<VehicleDto> => {
    const response = await apiClient.post<ApiResponse<VehicleDto>>('/v1/vehicles', payload);
    return response.data.data;
  },

  update: async (id: string, payload: UpdateVehicleRequest): Promise<VehicleDto> => {
    const response = await apiClient.put<ApiResponse<VehicleDto>>(`/v1/vehicles/${id}`, payload);
    return response.data.data;
  },

  delete: async (id: string, hardDelete = false): Promise<boolean> => {
    const response = await apiClient.delete<ApiResponse<boolean>>(`/v1/vehicles/${id}`, {
      params: { hardDelete },
    });
    return response.data.data;
  },

  restore: async (id: string): Promise<VehicleDto> => {
    const response = await apiClient.post<ApiResponse<VehicleDto>>(`/v1/vehicles/${id}/restore`);
    return response.data.data;
  },
};
