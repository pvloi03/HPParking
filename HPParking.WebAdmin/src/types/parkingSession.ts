import type { PaginationQuery } from './masterData';
import type { VehicleType } from './vehicle';

export const ParkingSessionStatus = {
  Active: 1, // Đang đỗ
  Completed: 2, // Hoàn tất
  UnmatchedOut: 3, // Ra không vào / Lệch biển
  Cancelled: 4, // Hủy bỏ
} as const;

export type ParkingSessionStatus =
  (typeof ParkingSessionStatus)[keyof typeof ParkingSessionStatus];

/**
 * DTO tóm tắt danh sách phiên đỗ xe phục vụ hiển thị bảng (ParkingSessionDto.cs)
 */
export interface ParkingSessionDto {
  id: string;
  plateNumber: string;
  vehicleType: VehicleType;
  status: ParkingSessionStatus;
  personId?: string;

  // --- LƯỢT VÀO ---
  inTime?: string;
  inLaneName?: string;
  inOverviewImagePath: string;
  inPlateImagePath: string;

  // --- LƯỢT RA ---
  outTime?: string;
  outLaneName?: string;
  outOverviewImagePath: string;
  outPlateImagePath: string;

  // --- TÍNH TOÁN ---
  durationMinutes?: number;

  // --- GHI CHÚ ---
  note?: string;

  // --- THÔNG TIN CHỦ PHƯƠNG TIỆN (CLIENT) ---
  personFullName?: string;
  personPhoneNumber?: string;
  personCode?: string;

  // --- AUDIT TRAIL (AuditableDto) ---
  createdAt: string;
  updatedAt?: string;
}

/**
 * DTO chi tiết phiên đỗ xe kèm thông tin khách hàng và định dạng thời lượng (ParkingSessionDetailDto.cs)
 */
export interface ParkingSessionDetailDto extends ParkingSessionDto {
  durationFormatted?: string;
}

/**
 * Tham số lọc và phân trang tra cứu danh sách phiên đỗ xe (ParkingSessionFilterQuery.cs)
 */
export interface ParkingSessionFilterQuery extends PaginationQuery {
  plateNumber?: string;
  vehicleType?: VehicleType;
  status?: ParkingSessionStatus;
  inLaneName?: string;
  outLaneName?: string;
  personId?: string;
  fromDate?: string;
  toDate?: string;
}
