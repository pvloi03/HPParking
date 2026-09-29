import { apiClient } from './client';
import type { ApiResponse, PagedResult } from '@/types/masterData';
import type {
  GateRouteDto,
  GateRouteFilterQuery,
  CreateGateRouteRequest,
  UpdateGateRouteRequest,
} from '@/types/gateRoute';

export const gateRouteApi = {
  async getRoutes(query?: GateRouteFilterQuery): Promise<PagedResult<GateRouteDto>> {
    const res = await apiClient.get<ApiResponse<PagedResult<GateRouteDto>>>('/api/v1/gate-routes', {
      params: query,
    });
    return res.data.data;
  },

  async getAll(): Promise<GateRouteDto[]> {
    const res = await apiClient.get<ApiResponse<PagedResult<GateRouteDto>>>('/api/v1/gate-routes', {
      params: { pageSize: 500, isActive: true },
    });
    return res.data.data.items;
  },

  async getById(id: string): Promise<GateRouteDto> {
    const res = await apiClient.get<ApiResponse<GateRouteDto>>(`/api/v1/gate-routes/${id}`);
    return res.data.data;
  },

  async create(req: CreateGateRouteRequest): Promise<GateRouteDto> {
    const res = await apiClient.post<ApiResponse<GateRouteDto>>('/api/v1/gate-routes', req);
    return res.data.data;
  },

  async update(id: string, req: UpdateGateRouteRequest): Promise<GateRouteDto> {
    const res = await apiClient.put<ApiResponse<GateRouteDto>>(`/api/v1/gate-routes/${id}`, req);
    return res.data.data;
  },

  async delete(id: string): Promise<void> {
    await apiClient.delete(`/api/v1/gate-routes/${id}`);
  },
};
