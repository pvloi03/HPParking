import type { PaginationQuery } from './masterData';

export const TripStatus = {
  Idle: 0,
  InTransit: 1,
  WorkingAtGate: 2,
  OverdueTransit: 3,
  OverdueStay: 4,
  Completed: 5,
} as const;
export type TripStatus = (typeof TripStatus)[keyof typeof TripStatus];

export const TripStatusLabel: Record<number | string, string> = {
  0: 'Chờ xuất phát',
  1: 'Đang di chuyển',
  2: 'Đang làm việc tại điểm',
  3: 'Quá giờ di chuyển',
  4: 'Quá giờ lưu lại',
  5: 'Hoàn tất',
  Idle: 'Chờ xuất phát',
  InTransit: 'Đang di chuyển',
  WorkingAtGate: 'Đang làm việc tại điểm',
  OverdueTransit: 'Quá giờ di chuyển',
  OverdueStay: 'Quá giờ lưu lại',
  Completed: 'Hoàn tất',
};

export interface FleetTripDto {
  id: string;
  isActive?: boolean;
  vehicleId: string;
  plateNumber: string;
  cardId?: string;
  cardNumber: string;
  originGateId: string;
  originGateName?: string;
  currentGateId?: string;
  currentGateName?: string;
  assignedRouteId?: string;
  routeName?: string;
  currentStepIndex: number;
  status: TripStatus;
  startTime: string;
  lastExitTime?: string;
  lastEntryTime?: string;
  endTime?: string;
  nextDeadline?: string;
  remainingSeconds: number;
  isOverdue: boolean;
  isAlertSent: boolean;
  lastDriverImagePath?: string;
  checkpoints?: TripCheckpointDto[];
  createdAt: string;
  updatedAt?: string;
}

export interface SlaOverdueInfoDto {
  isOverdue: boolean;
  overdueSeconds: number;
}

export interface TripCheckpointDto {
  stepIndex: number;
  gateId: string;
  gateName: string;
  direction: number | string;
  timestamp: string;
  imagePath?: string;
  plateDetected: string;
  isRouteCompliant: boolean;
  note?: string;
  slaOverdue?: SlaOverdueInfoDto;
}

export interface FleetTripFilterQuery extends PaginationQuery {
  vehicleId?: string;
  status?: TripStatus;
}
