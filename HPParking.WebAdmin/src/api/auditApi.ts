import axios from 'axios';
import { apiClient } from './client';
import type { ApiResponse, PagedResult } from '@/types/masterData';
import type {
  AuditLogDto,
  AuditLogDetailDto,
  AuditLogFilterQuery,
} from '@/types/auditLog';

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
      return data.message;
    }

    if (data?.title) {
      return data.title;
    }

    if (error.response?.status === 403) {
      return 'Bạn không có quyền truy cập nhật ký kiểm toán (403 Forbidden).';
    }
    if (error.response?.status === 404) {
      return 'Không tìm thấy bản ghi nhật ký kiểm toán tương ứng.';
    }

    if (error.message) {
      return error.message;
    }
  }

  if (error instanceof Error) {
    return error.message;
  }

  return 'Đã xảy ra lỗi không xác định khi tải nhật ký kiểm toán.';
}

export const auditApi = {
  getPaged: async (
    query?: AuditLogFilterQuery
  ): Promise<PagedResult<AuditLogDto>> => {
    const response = await apiClient.get<ApiResponse<PagedResult<AuditLogDto>>>(
      '/v1/audit-logs',
      { params: query }
    );
    return response.data.data;
  },

  getById: async (id: string): Promise<AuditLogDetailDto> => {
    const response = await apiClient.get<ApiResponse<AuditLogDetailDto>>(
      `/v1/audit-logs/${id}`
    );
    return response.data.data;
  },
};
