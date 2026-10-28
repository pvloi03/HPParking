import axios from 'axios';
import { apiClient } from './client';
import type { ApiResponse, PagedResult } from '@/types/masterData';
import type {
  TariffDto,
  TariffFilterQuery,
  CreateTariffRequest,
  UpdateTariffRequest,
} from '@/types/tariff';

export function extractErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as ApiResponse<unknown> | undefined;
    if (data?.message) {
      if (data.message.includes('TARIFF_CODE_DUPLICATE') || data.message.includes('Mã biểu phí đã tồn tại')) {
        return 'Mã biểu phí này đã tồn tại trong hệ thống.';
      }
      if (data.message.includes('TARIFF_IN_USE_BY_CARDS')) {
        return 'Biểu phí đang được áp dụng cho thẻ định danh. Không thể xóa.';
      }
      if (data.message.includes('TARIFF_ACTIVE_CARDS_EXIST')) {
        return 'Biểu phí đang được áp dụng cho thẻ đang hoạt động. Không thể ngưng kích hoạt.';
      }
      return data.message;
    }
    if (data?.errors) {
      return Array.isArray(data.errors)
        ? data.errors.join('. ')
        : Object.values(data.errors).flat().join('. ');
    }
  }
  return error instanceof Error ? error.message : 'Đã có lỗi xảy ra. Vui lòng thử lại sau.';
}

export const tariffApi = {
  async getPaged(query?: TariffFilterQuery): Promise<PagedResult<TariffDto>> {
    const endpoint = query?.onlyDeleted ? '/v1/tariffs/deleted' : '/v1/tariffs';
    const params = query ? { ...query, search: query.search || query.keyword } : undefined;
    const res = await apiClient.get<ApiResponse<PagedResult<TariffDto>>>(endpoint, {
      params,
    });
    return res.data.data;
  },

  async getAllActive(): Promise<TariffDto[]> {
    const res = await apiClient.get<ApiResponse<PagedResult<TariffDto>>>('/v1/tariffs', {
      params: { pageSize: 500, isActive: true },
    });
    return res.data.data.items;
  },

  async getById(id: string): Promise<TariffDto> {
    const res = await apiClient.get<ApiResponse<TariffDto>>(`/v1/tariffs/${id}`);
    return res.data.data;
  },

  async create(req: CreateTariffRequest): Promise<TariffDto> {
    const res = await apiClient.post<ApiResponse<TariffDto>>('/v1/tariffs', req);
    return res.data.data;
  },

  async update(id: string, req: UpdateTariffRequest): Promise<TariffDto> {
    const res = await apiClient.put<ApiResponse<TariffDto>>(`/v1/tariffs/${id}`, req);
    return res.data.data;
  },

  async delete(id: string): Promise<void> {
    await apiClient.delete(`/v1/tariffs/${id}`);
  },

  async restore(id: string): Promise<TariffDto> {
    const res = await apiClient.post<ApiResponse<TariffDto>>(`/v1/tariffs/${id}/restore`);
    return res.data.data;
  },
};
