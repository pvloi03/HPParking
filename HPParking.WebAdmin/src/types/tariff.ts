import type { PaginationQuery } from './masterData';
import type { VehicleType } from './vehicle';

export const TariffPricingMode = {
  Flat: 1,
  Block: 2,
} as const;
export type TariffPricingMode = (typeof TariffPricingMode)[keyof typeof TariffPricingMode];

export function getPricingModeLabel(mode: TariffPricingMode): string {
  switch (mode) {
    case TariffPricingMode.Flat:
      return 'Cố định theo lượt (Flat)';
    case TariffPricingMode.Block:
      return 'Lũy tiến theo block (Block)';
    default:
      return 'Không xác định';
  }
}

export interface TariffDto {
  id: string;
  code: string;
  name: string;
  vehicleType: VehicleType;
  pricingMode: TariffPricingMode;
  gracePeriodMinutes: number;
  flatPrice: number;
  basePrice: number;
  baseBlockMinutes: number;
  additionalBlockPrice: number;
  additionalBlockMinutes: number;
  overnightSurcharge: number;
  maxDailyCap: number;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateTariffRequest {
  code: string;
  name: string;
  vehicleType: VehicleType;
  pricingMode: TariffPricingMode;
  gracePeriodMinutes?: number;
  flatPrice?: number;
  basePrice?: number;
  baseBlockMinutes?: number;
  additionalBlockPrice?: number;
  additionalBlockMinutes?: number;
  overnightSurcharge?: number;
  maxDailyCap?: number;
  isActive?: boolean;
}

export interface UpdateTariffRequest {
  code: string;
  name: string;
  vehicleType: VehicleType;
  pricingMode: TariffPricingMode;
  gracePeriodMinutes?: number;
  flatPrice?: number;
  basePrice?: number;
  baseBlockMinutes?: number;
  additionalBlockPrice?: number;
  additionalBlockMinutes?: number;
  overnightSurcharge?: number;
  maxDailyCap?: number;
  isActive?: boolean;
}

export interface TariffFilterQuery extends PaginationQuery {
  search?: string;
  keyword?: string;
  vehicleType?: VehicleType;
  pricingMode?: TariffPricingMode;
  isActive?: boolean;
}
