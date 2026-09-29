import type { PaginationQuery } from './masterData';

export const TripStatus = {
  Idle: 'Idle',
  InTransit: 'InTransit',
  WorkingAtGate: 'WorkingAtGate',
  OverdueTransit: 'OverdueTransit',
  OverdueStay: 'OverdueStay',
  Completed: 'Completed',
} as const;
export type TripStatus = (typeof TripStatus)[keyof typeof TripStatus];

export const TripStatusLabel: Record<TripStatus, string> = {
  Idle: 'Chờ xuất phát',
  InTransit: 'Đang di chuyển',
  WorkingAtGate: 'Đang làm việc tại điểm',
  OverdueTransit: 'Quá giờ di chuyển',
  OverdueStay: 'Quá giờ lưu lại',
  Completed: 'Hoàn tất',
};

export interface FleetTripDto {
  id: string;
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
  nextDeadline?: string;
  remainingSeconds: number;
  isOverdue: boolean;
  isAlertSent: boolean;
  lastDriverImagePath?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface FleetTripFilterQuery extends PaginationQuery {
  vehicleId?: string;
  status?: TripStatus;
}
