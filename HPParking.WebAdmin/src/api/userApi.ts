import axios from 'axios';
import { apiClient } from './client';
import type { ApiResponse, PagedResult } from '@/types/masterData';
import type {
  UserDto,
  CreateUserRequest,
  UpdateUserRequest,
  ResetPasswordRequest,
  UserFilterQuery,
} from '@/types/user';

export function extractErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const data = error.response?.data as any;

    if (data?.errors) {
      if (Array.isArray(data.errors) && data.errors.length > 0) {
        return data.errors.join('. ');
      }
      if (typeof data.errors === 'object') {
        const errorList = Object.values(data.errors)
          .flat()
          .filter((msg): msg is string => typeof msg === 'string' && Boolean(msg));
        if (errorList.length > 0) {
          return errorList.join('. ');
        }
      }
    }

    if (data?.message) {
      if (data.message.includes('USER_DUPLICATE_USERNAME')) {
        return 'Tên đăng nhập này đã tồn tại trong hệ thống. Vui lòng chọn tên đăng nhập khác.';
      }
      if (data.message.includes('USER_NOT_FOUND')) {
        return 'Không tìm thấy tài khoản người dùng tương ứng.';
      }
      return data.message;
    }

    if (data?.title) {
      return data.title;
    }

    if (error.response?.status === 409) {
      return 'Dữ liệu bị trùng lặp hoặc vi phạm ràng buộc toàn vẹn.';
    }
    if (error.response?.status === 403) {
      return 'Bạn không có quyền thực hiện thao tác quản trị này (403 Forbidden).';
    }
    if (error.response?.status === 404) {
      return 'Không tìm thấy tài khoản người dùng (404 Not Found).';
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

export const usersApi = {
  getPaged: async (query?: UserFilterQuery): Promise<PagedResult<UserDto>> => {
    const response = await apiClient.get<ApiResponse<PagedResult<UserDto>>>(
      '/v1/users',
      { params: query }
    );
    return response.data.data;
  },

  getById: async (id: string): Promise<UserDto> => {
    const response = await apiClient.get<ApiResponse<UserDto>>(`/v1/users/${id}`);
    return response.data.data;
  },

  create: async (payload: CreateUserRequest): Promise<UserDto> => {
    const response = await apiClient.post<ApiResponse<UserDto>>('/v1/users', payload);
    return response.data.data;
  },

  update: async (id: string, payload: UpdateUserRequest): Promise<UserDto> => {
    const response = await apiClient.put<ApiResponse<UserDto>>(
      `/v1/users/${id}`,
      payload
    );
    return response.data.data;
  },

  delete: async (id: string, permanent = false): Promise<boolean> => {
    const response = await apiClient.delete<ApiResponse<boolean>>(
      `/v1/users/${id}`,
      { params: { permanent } }
    );
    return response.data.data;
  },

  restore: async (id: string): Promise<boolean> => {
    const response = await apiClient.post<ApiResponse<boolean>>(
      `/v1/users/${id}/restore`
    );
    return response.data.data;
  },

  resetPassword: async (id: string, payload: ResetPasswordRequest): Promise<boolean> => {
    const response = await apiClient.post<ApiResponse<boolean>>(
      `/v1/users/${id}/reset-password`,
      payload
    );
    return response.data.data;
  },

  toggleStatus: async (id: string): Promise<UserDto> => {
    const response = await apiClient.patch<ApiResponse<UserDto>>(
      `/v1/users/${id}/toggle-status`
    );
    return response.data.data;
  },
};
