import type { PaginationQuery } from './masterData';
import type { VehicleType } from './vehicle';
import type { CardTargetType } from './card';

export const ParkingSessionStatus = {
  Active: 1, // Đang đỗ
  Completed: 2, // Hoàn tất
  UnmatchedOut: 3, // Ra không vào / Lệch biển
  Cancelled: 4, // Hủy bỏ
} as const;

export type ParkingSessionStatus =
  (typeof ParkingSessionStatus)[keyof typeof ParkingSessionStatus];

export const SessionCloseReason = {
  Normal: 1,
  LostCard: 2,
  AdminForceClose: 3,
} as const;
export type SessionCloseReason =
  (typeof SessionCloseReason)[keyof typeof SessionCloseReason];

export function getSessionCloseReasonLabel(reason?: SessionCloseReason | null): string {
  switch (reason) {
    case SessionCloseReason.Normal:
      return 'Đóng thường (Qua làn)';
    case SessionCloseReason.LostCard:
      return 'Báo mất thẻ';
    case SessionCloseReason.AdminForceClose:
      return 'Admin cưỡng bức';
    default:
      return '—';
  }
}

/**
 * Lấy hậu tố phân loại thẻ hiển thị trong ngoặc tròn: (Tháng), (VIP), (Lượt), (Xe nội bộ)
 */
export function getCardTypeSuffix(session?: {
  cardTargetType?: CardTargetType;
  personId?: string;
  personFullName?: string;
  cardNumber?: string;
} | null): string {
  if (!session) return '';
  if (session.cardTargetType !== undefined && session.cardTargetType !== null) {
    switch (session.cardTargetType) {
      case 2:
        return ' (Tháng)';
      case 3:
        return ' (VIP)';
      case 1:
        return ' (Lượt)';
      case 0:
        return ' (Xe nội bộ)';
    }
  }
  // Fallback nếu chưa lưu cardTargetType trong phiên
  if (session.personId || session.personFullName) {
    return ' (Tháng)';
  }
  if (session.cardNumber) {
    return ' (Lượt)';
  }
  return '';
}

/**
 * DTO tóm tắt danh sách phiên đỗ xe phục vụ hiển thị bảng (ParkingSessionDto.cs)
 */
export interface ParkingSessionDto {
  id: string;
  plateNumber: string;
  vehicleType?: VehicleType;
  targetType?: number;
  status: ParkingSessionStatus;
  personId?: string;
  faceMatchScore?: number;

  // --- ĐỊNH DANH THẺ & PHÍ ---
  cardId?: string;
  cardNumber?: string;
  cardTargetType?: CardTargetType;
  tariffId?: string;
  feeAmount?: number;
  isFeeCollected?: boolean;

  // --- LƯỢT VÀO ---
  inOrderNumber?: number;
  inTime?: string;
  inLaneName?: string;
  inOverviewImagePath: string;
  inPlateImagePath: string;
  inFaceImagePath?: string;
  rawInPlateOcr?: string;
  isManualPlateIn?: boolean;

  // --- LƯỢT RA ---
  outTime?: string;
  outLaneName?: string;
  outOverviewImagePath: string;
  outPlateImagePath: string;
  outFaceImagePath?: string;
  rawOutPlateOcr?: string;
  isManualPlateOut?: boolean;

  // --- TÍNH TOÁN ---
  durationMinutes?: number;

  // --- GHI CHÚ & ĐÓNG PHIÊN ---
  note?: string;
  closeReason?: SessionCloseReason;
  closedBy?: string;

  // --- THÔNG TIN CHỦ PHƯƠNG TIỆN (CLIENT) ---
  personFullName?: string;
  personPhoneNumber?: string;
  personCode?: string;
  personAvatar?: string;

  // --- AUDIT TRAIL (AuditableDto) ---
  createdAt: string;
  updatedAt?: string;
}

export interface ForceCloseParkingSessionRequest {
  closeReason: SessionCloseReason;
  overrideFeeAmount?: number;
  isFeeCollected?: boolean;
  markCardAsLost?: boolean;
  note?: string;
}

/**
 * DTO chi tiết phiên đỗ xe kèm thông tin nhân sự và định dạng thời lượng (ParkingSessionDetailDto.cs)
 */
export interface ParkingSessionDetailDto extends ParkingSessionDto {
  durationFormatted?: string;
}

/**
 * Tham số lọc và phân trang tra cứu danh sách phiên đỗ xe (ParkingSessionFilterQuery.cs)
 */
export interface ParkingSessionFilterQuery extends PaginationQuery {
  keyword?: string;
  plateNumber?: string;
  vehicleType?: VehicleType;
  targetType?: number;
  status?: ParkingSessionStatus;
  inLaneName?: string;
  outLaneName?: string;
  personId?: string;
  fromDate?: string;
  toDate?: string;
}
