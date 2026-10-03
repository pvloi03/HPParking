import axios from 'axios';
import { apiClient } from './client';
import type { ApiResponse, PagedResult } from '@/types/masterData';
import type { CardDto, CardFilterQuery, CreateCardRequest, UpdateCardRequest } from '@/types/card';

export function extractErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as ApiResponse<unknown> | undefined;
    if (data?.message) {
      if (data.message.includes('CARD_NUMBER_DUPLICATE') || data.message.includes('đã tồn tại')) {
        return 'Mã thẻ định danh này đã tồn tại trong hệ thống.';
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

export const cardApi = {
  async getCards(query?: CardFilterQuery): Promise<PagedResult<CardDto>> {
    const res = await apiClient.get<ApiResponse<PagedResult<CardDto>>>('/v1/cards', {
      params: query,
    });
    return res.data.data;
  },

  async getById(id: string): Promise<CardDto> {
    const res = await apiClient.get<ApiResponse<CardDto>>(`/v1/cards/${id}`);
    return res.data.data;
  },

  async create(req: CreateCardRequest): Promise<CardDto> {
    const res = await apiClient.post<ApiResponse<CardDto>>('/v1/cards', req);
    return res.data.data;
  },

  async update(id: string, req: UpdateCardRequest): Promise<CardDto> {
    const res = await apiClient.put<ApiResponse<CardDto>>(`/v1/cards/${id}`, req);
    return res.data.data;
  },

  async delete(id: string): Promise<void> {
    await apiClient.delete(`/v1/cards/${id}`);
  },
};
