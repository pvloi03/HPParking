import { apiClient } from './client';
import type { ApiResponse, PagedResult } from '@/types/masterData';
import type { CardDto, CardFilterQuery, CreateCardRequest, UpdateCardRequest } from '@/types/card';

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
