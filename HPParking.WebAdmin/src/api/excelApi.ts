import axios from 'axios';
import { apiClient } from './client';
import type { ApiResponse } from '@/types/masterData';
import type {
  ExcelEntity,
  ExcelImportResultDto,
  DuplicateMode,
} from '@/types/excel';

/**
 * Trích xuất thông báo lỗi thân thiện khi xử lý tệp Excel
 */
export function extractErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as ApiResponse<unknown> | undefined;

    if (data?.message) {
      return data.message;
    }

    if (data?.errors && data.errors.length > 0) {
      return data.errors.join('. ');
    }

    if (error.response?.status === 400) {
      return 'Tệp Excel không đúng định dạng hoặc dữ liệu cột không hợp lệ.';
    }
    if (error.response?.status === 403) {
      return 'Bạn không có quyền thực hiện thao tác nhập/xuất tệp này (403 Forbidden).';
    }

    if (error.message) {
      return error.message;
    }
  }

  if (error instanceof Error) {
    return error.message;
  }

  return 'Đã xảy ra lỗi không xác định khi xử lý tệp Excel.';
}

export const excelApi = {
  /**
   * Tải tệp mẫu Excel chuẩn hóa (.xlsx)
   */
  downloadTemplate: async (entity: ExcelEntity): Promise<Blob> => {
    const response = await apiClient.get<Blob>(`/v1/excel/${entity}/template`, {
      responseType: 'blob',
    });
    return response.data;
  },

  /**
   * Nhập dữ liệu hàng loạt từ tệp Excel
   */
  importData: async (
    entity: ExcelEntity,
    file: File,
    dryRun = false,
    duplicateMode: DuplicateMode = 1
  ): Promise<ExcelImportResultDto> => {
    const formData = new FormData();
    formData.append('file', file);

    const response = await apiClient.post<ApiResponse<ExcelImportResultDto>>(
      `/v1/excel/${entity}/import`,
      formData,
      {
        params: {
          dryRun,
          duplicateMode,
        },
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      }
    );
    return response.data.data;
  },

  /**
   * Xuất danh sách dữ liệu ra tệp Excel (.xlsx)
   */
  exportData: async (
    entity: ExcelEntity,
    filter?: Record<string, unknown>
  ): Promise<Blob> => {
    const response = await apiClient.get<Blob>(`/v1/excel/${entity}/export`, {
      params: filter,
      responseType: 'blob',
    });
    return response.data;
  },
};
