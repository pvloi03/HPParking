import type { PaginationQuery } from './masterData';

export const VehicleType = {
  Car: 1,
  Motorbike: 2,
  Bicycle: 3,
  Other: 4,
} as const;
export type VehicleType = typeof VehicleType[keyof typeof VehicleType];

/**
 * Chuẩn hóa biển số xe: loại bỏ dấu chấm, gạch ngang, khoảng trắng và chuyển sang chữ hoa (30A-123.45 -> 30A12345)
 */
export function normalizePlateNumber(plate: string): string {
  if (!plate) return '';
  return plate
    .trim()
    .replace(/[.\-\s_]/g, '')
    .toUpperCase();
}

export interface VehicleDto {
  id: string;
  plateNumber: string;
  type: VehicleType;
  ownerClientId?: string;
  isActive: boolean;
  note?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface VehicleFilterQuery extends PaginationQuery {
  keyword?: string;
  type?: VehicleType;
  ownerClientId?: string;
  isActive?: boolean;
  onlyDeleted?: boolean;
}

export interface CreateVehicleRequest {
  clientId?: string;
  plateNumber: string;
  type: VehicleType;
  isActive: boolean;
  note?: string;
}

export interface UpdateVehicleRequest {
  plateNumber: string;
  type: VehicleType;
  isActive: boolean;
  note?: string;
}
