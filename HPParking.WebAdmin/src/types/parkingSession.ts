export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageIndex: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export const ParkingSessionStatus = {
  Active: 1, // Đang đỗ
  Completed: 2, // Hoàn tất
  UnmatchedOut: 3, // Ra không vào
  Cancelled: 4, // Hủy bỏ
} as const;

export type ParkingSessionStatus =
  (typeof ParkingSessionStatus)[keyof typeof ParkingSessionStatus];

export const VehicleType = {
  Car: 1, // Ô tô
  Motorbike: 2, // Xe máy
  Bicycle: 3, // Xe đạp
  Other: 4, // Khác
} as const;

export type VehicleType = (typeof VehicleType)[keyof typeof VehicleType];

export interface ParkingSessionDto {
  id: string;
  plateNumber: string;
  vehicleType: VehicleType | number;
  status: ParkingSessionStatus | number;
  personId?: string | null;
  clientName?: string | null;
  phoneNumber?: string | null;
  inTime: string;
  outTime?: string | null;
  inLaneName?: string | null;
  outLaneName?: string | null;
  laneInName?: string | null;
  laneOutName?: string | null;
  gateInName?: string | null;
  gateOutName?: string | null;
  inOverviewImagePath?: string;
  inPlateImagePath?: string;
  outOverviewImagePath?: string;
  outPlateImagePath?: string;
  durationMinutes?: number | null;
  duration?: string | null;
  isPlateMismatch?: boolean;
}

export interface ParkingSessionFilterQuery {
  pageIndex?: number;
  pageSize?: number;
  searchTerm?: string;
  status?: ParkingSessionStatus;
  isPlateMismatch?: boolean;
  fromDate?: string;
  toDate?: string;
}
