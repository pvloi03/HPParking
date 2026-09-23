import axios from 'axios';
import { apiClient } from './client';
import type { ApiResponse, PagedResult } from '@/types/masterData';
import type {
  ClientDto,
  ClientFilterQuery,
  CreateClientRequest,
  UpdateClientRequest,
  SyncFaceIdResponse,
} from '@/types/client';

/**
 * Trích xuất thông báo lỗi thân thiện cho người dùng từ AxiosError,
 * đặc biệt ánh xạ các mã lỗi trùng lặp dữ liệu và sinh trắc học FaceID.
 */
export function extractErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as ApiResponse<unknown> | undefined;

    if (data?.message) {
      if (data.message.includes('CLIENT_PHONE_DUPLICATE')) {
        return 'Số điện thoại này đã được đăng ký cho một khách hàng khác trong hệ thống.';
      }
      if (data.message.includes('CLIENT_CODE_DUPLICATE')) {
        return 'Mã khách hàng / Số CCCD này đã tồn tại trên hệ thống.';
      }
      if (data.message.includes('CLIENT_HAS_VEHICLES')) {
        return 'Không thể xóa khách hàng vì vẫn còn phương tiện đang gán quyền sở hữu. Vui lòng hủy gán xe trước.';
      }
      if (data.message.includes('CLIENT_HAS_ACTIVE_SESSIONS')) {
        return 'Không thể xóa khách hàng vì xe của khách hàng đang nằm trong bãi đỗ.';
      }
      if (data.message.includes('CLIENT_AVATAR_INVALID')) {
        return 'Tệp ảnh không đúng định dạng (JPG, PNG, WebP) hoặc dung lượng vượt quá 5MB.';
      }
      if (data.message.includes('FACEID_SYNC_FAILED')) {
        return 'Đồng bộ khuôn mặt lên thiết bị FaceID thất bại. Vui lòng kiểm tra kết nối mạng của thiết bị ngoại vi.';
      }
      if (data.message.includes('PARENT_IS_DELETED')) {
        return 'Không thể khôi phục vì đơn vị trực thuộc (công ty/phòng ban) đang nằm trong thùng rác.';
      }
      return data.message;
    }

    if (data?.errors && data.errors.length > 0) {
      return data.errors.join('. ');
    }

    if (error.response?.status === 409) {
      return 'Dữ liệu khách hàng bị trùng lặp (Số điện thoại, CCCD hoặc Mã khách hàng) (409 Conflict).';
    }
    if (error.response?.status === 403) {
      return 'Bạn không có quyền thực hiện thao tác này (403 Forbidden).';
    }
    if (error.response?.status === 404) {
      return 'Không tìm thấy hồ sơ khách hàng tương ứng (404 Not Found).';
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

// =================== CLIENTS API ===================

export const clientApi = {
  getPaged: async (query?: ClientFilterQuery): Promise<PagedResult<ClientDto>> => {
    const response = await apiClient.get<ApiResponse<PagedResult<ClientDto>>>(
      '/v1/clients',
      { params: query }
    );
    return response.data.data;
  },

  getById: async (id: string): Promise<ClientDto> => {
    const response = await apiClient.get<ApiResponse<ClientDto>>(`/v1/clients/${id}`);
    return response.data.data;
  },

  create: async (payload: CreateClientRequest): Promise<ClientDto> => {
    const response = await apiClient.post<ApiResponse<ClientDto>>('/v1/clients', payload);
    return response.data.data;
  },

  update: async (id: string, payload: UpdateClientRequest): Promise<ClientDto> => {
    const response = await apiClient.put<ApiResponse<ClientDto>>(`/v1/clients/${id}`, payload);
    return response.data.data;
  },

  delete: async (id: string, hardDelete = false): Promise<boolean> => {
    const response = await apiClient.delete<ApiResponse<boolean>>(`/v1/clients/${id}`, {
      params: { hardDelete },
    });
    return response.data.data;
  },

  restore: async (id: string): Promise<ClientDto> => {
    const response = await apiClient.post<ApiResponse<ClientDto>>(`/v1/clients/${id}/restore`);
    return response.data.data;
  },

  uploadAvatar: async (id: string, file: File): Promise<string> => {
    const formData = new FormData();
    formData.append('file', file);
    const response = await apiClient.post<ApiResponse<string>>(
      `/v1/clients/${id}/avatar`,
      formData,
      {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      }
    );
    return response.data.data;
  },

  syncFaceId: async (id: string): Promise<SyncFaceIdResponse> => {
    const response = await apiClient.post<ApiResponse<SyncFaceIdResponse>>(
      `/v1/clients/${id}/sync-faceid`
    );
    return response.data.data;
  },
};
