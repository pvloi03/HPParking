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
  Active: 0, // Đang đỗ
  Completed: 1, // Hoàn tất
} as const;

export type ParkingSessionStatus =
  (typeof ParkingSessionStatus)[keyof typeof ParkingSessionStatus];

export interface ParkingSessionDto {
  id: string;
  clientName?: string | null;
  phoneNumber?: string | null;
  plateNumber: string;
  vehicleType?: string | null;
  inTime: string;
  outTime?: string | null;
  laneInName?: string | null;
  laneOutName?: string | null;
  gateInName?: string | null;
  gateOutName?: string | null;
  status: ParkingSessionStatus;
  isPlateMismatch: boolean;
  inVehicleImage?: string | null;
  outVehicleImage?: string | null;
  duration?: string | null;
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
