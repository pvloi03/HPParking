import { apiClient } from './client';
import type { ApiResponse } from '@/types/masterData';

export interface SystemFeaturesDto {
  enableFleetDispatch: boolean;
}

export const systemApi = {
  async getFeatures(): Promise<SystemFeaturesDto> {
    const res = await apiClient.get<ApiResponse<SystemFeaturesDto>>('/v1/system/features');
    return res.data.data;
  },
};
