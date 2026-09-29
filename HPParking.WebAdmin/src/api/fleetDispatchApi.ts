import { apiClient } from './client';
import type { ApiResponse, PagedResult } from '@/types/masterData';
import type { FleetTripDto, FleetTripFilterQuery } from '@/types/fleetDispatch';

export const fleetDispatchApi = {
  async getActiveTrips(): Promise<FleetTripDto[]> {
    const res = await apiClient.get<ApiResponse<FleetTripDto[]>>('/v1/fleet-dispatch/active');
    return res.data.data;
  },

  async getTripHistory(query?: FleetTripFilterQuery): Promise<PagedResult<FleetTripDto>> {
    const res = await apiClient.get<ApiResponse<PagedResult<FleetTripDto>>>(
      '/v1/fleet-dispatch/history',
      {
        params: query,
      }
    );
    return res.data.data;
  },

  async getById(id: string): Promise<FleetTripDto> {
    const res = await apiClient.get<ApiResponse<FleetTripDto>>(`/v1/fleet-dispatch/${id}`);
    return res.data.data;
  },
};
