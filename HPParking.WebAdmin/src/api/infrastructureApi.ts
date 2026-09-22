import axios from 'axios';
import { apiClient } from './client';
import type { ApiResponse, PagedResult } from '@/types/masterData';
import type {
  GateDto,
  GateFilterQuery,
  CreateGateRequest,
  UpdateGateRequest,
  LaneDto,
  LaneDetailDto,
  LaneFilterQuery,
  CreateLaneRequest,
  UpdateLaneRequest,
  DeviceDto,
  DeviceFilterQuery,
  CreateDeviceRequest,
  UpdateDeviceRequest,
} from '@/types/infrastructure';

/**
 * Trích xuất thông báo lỗi thân thiện cho người dùng từ AxiosError,
 * đặc biệt ánh xạ các mã lỗi Toàn Vẹn Tham Chiếu Hạ Tầng (ADR 0030, ADR 0034)
 * và Bảo Vệ Trạng Thái Hoạt Động (Active State Protection).
 */
export function extractErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as ApiResponse<unknown> | undefined;

    if (data?.message) {
      if (data.message.includes('GATE_HAS_LANES')) {
        return 'Không thể xóa cổng vì vẫn còn làn xe trực thuộc. Vui lòng chuyển hoặc xóa làn xe trước.';
      }
      if (data.message.includes('DEVICE_IN_USE_BY_LANE')) {
        return 'Không thể xóa thiết bị vì đang được liên kết cấu hình trong một hoặc nhiều làn xe.';
      }
      if (data.message.includes('INFRA_ACTIVE_DEPENDENCY_EXISTS')) {
        return 'Không thể ngừng hoạt động vì vẫn còn các thành phần phụ thuộc (làn xe hoặc thiết bị) đang trong trạng thái hoạt động.';
      }
      if (data.message.includes('DEVICE_IS_DELETED')) {
        return 'Không thể liên kết thiết bị vì thiết bị đã chọn nằm trong thùng rác hoặc đã bị xóa.';
      }
      if (data.message.includes('GATE_IS_DELETED')) {
        return 'Không thể liên kết cổng vì cổng đã chọn nằm trong thùng rác hoặc đã bị xóa.';
      }
      if (data.message.includes('PARENT_IS_DELETED')) {
        return 'Không thể khôi phục vì phần tử cha đang nằm trong thùng rác hoặc đã bị xóa. Vui lòng khôi phục phần tử cha trước.';
      }
      return data.message;
    }

    if (data?.errors && data.errors.length > 0) {
      return data.errors.join('. ');
    }

    if (error.response?.status === 409) {
      return 'Dữ liệu bị trùng lặp hoặc vi phạm ràng buộc toàn vẹn dữ liệu hạ tầng (409 Conflict).';
    }
    if (error.response?.status === 403) {
      return 'Bạn không có quyền thực hiện thao tác này (403 Forbidden).';
    }
    if (error.response?.status === 404) {
      return 'Không tìm thấy bản ghi tương ứng (404 Not Found).';
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

// =================== GATES API ===================

export const gatesApi = {
  getPaged: async (query?: GateFilterQuery): Promise<PagedResult<GateDto>> => {
    const response = await apiClient.get<ApiResponse<PagedResult<GateDto>>>(
      '/v1/gates',
      { params: query }
    );
    return response.data.data;
  },

  getById: async (id: string): Promise<GateDto> => {
    const response = await apiClient.get<ApiResponse<GateDto>>(`/v1/gates/${id}`);
    return response.data.data;
  },

  create: async (payload: CreateGateRequest): Promise<GateDto> => {
    const response = await apiClient.post<ApiResponse<GateDto>>('/v1/gates', payload);
    return response.data.data;
  },

  update: async (id: string, payload: UpdateGateRequest): Promise<GateDto> => {
    const response = await apiClient.put<ApiResponse<GateDto>>(`/v1/gates/${id}`, payload);
    return response.data.data;
  },

  delete: async (id: string, hardDelete = false): Promise<boolean> => {
    const response = await apiClient.delete<ApiResponse<boolean>>(`/v1/gates/${id}`, {
      params: { hardDelete },
    });
    return response.data.data;
  },

  restore: async (id: string): Promise<GateDto> => {
    const response = await apiClient.post<ApiResponse<GateDto>>(`/v1/gates/${id}/restore`);
    return response.data.data;
  },
};

// =================== LANES API ===================

export const lanesApi = {
  getPaged: async (query?: LaneFilterQuery): Promise<PagedResult<LaneDto>> => {
    const response = await apiClient.get<ApiResponse<PagedResult<LaneDto>>>(
      '/v1/lanes',
      { params: query }
    );
    return response.data.data;
  },

  getById: async (id: string): Promise<LaneDto> => {
    const response = await apiClient.get<ApiResponse<LaneDto>>(`/v1/lanes/${id}`);
    return response.data.data;
  },

  getDetail: async (id: string): Promise<LaneDetailDto> => {
    const response = await apiClient.get<ApiResponse<LaneDetailDto>>(`/v1/lanes/${id}/detail`);
    return response.data.data;
  },

  create: async (payload: CreateLaneRequest): Promise<LaneDto> => {
    const response = await apiClient.post<ApiResponse<LaneDto>>('/v1/lanes', payload);
    return response.data.data;
  },

  update: async (id: string, payload: UpdateLaneRequest): Promise<LaneDto> => {
    const response = await apiClient.put<ApiResponse<LaneDto>>(`/v1/lanes/${id}`, payload);
    return response.data.data;
  },

  delete: async (id: string, hardDelete = false): Promise<boolean> => {
    const response = await apiClient.delete<ApiResponse<boolean>>(`/v1/lanes/${id}`, {
      params: { hardDelete },
    });
    return response.data.data;
  },

  restore: async (id: string): Promise<LaneDto> => {
    const response = await apiClient.post<ApiResponse<LaneDto>>(`/v1/lanes/${id}/restore`);
    return response.data.data;
  },
};

// =================== DEVICES API ===================

export const devicesApi = {
  getPaged: async (query?: DeviceFilterQuery): Promise<PagedResult<DeviceDto>> => {
    const response = await apiClient.get<ApiResponse<PagedResult<DeviceDto>>>(
      '/v1/devices',
      { params: query }
    );
    return response.data.data;
  },

  getById: async (id: string): Promise<DeviceDto> => {
    const response = await apiClient.get<ApiResponse<DeviceDto>>(`/v1/devices/${id}`);
    return response.data.data;
  },

  create: async (payload: CreateDeviceRequest): Promise<DeviceDto> => {
    const response = await apiClient.post<ApiResponse<DeviceDto>>('/v1/devices', payload);
    return response.data.data;
  },

  update: async (id: string, payload: UpdateDeviceRequest): Promise<DeviceDto> => {
    const response = await apiClient.put<ApiResponse<DeviceDto>>(`/v1/devices/${id}`, payload);
    return response.data.data;
  },

  delete: async (id: string, hardDelete = false): Promise<boolean> => {
    const response = await apiClient.delete<ApiResponse<boolean>>(`/v1/devices/${id}`, {
      params: { hardDelete },
    });
    return response.data.data;
  },

  restore: async (id: string): Promise<DeviceDto> => {
    const response = await apiClient.post<ApiResponse<DeviceDto>>(`/v1/devices/${id}/restore`);
    return response.data.data;
  },
};
