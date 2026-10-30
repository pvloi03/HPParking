import type { PaginationQuery } from './masterData';

export const LaneDirection = {
  In: 1,
  Out: 2,
  Bidirectional: 3,
} as const;
export type LaneDirection = typeof LaneDirection[keyof typeof LaneDirection];

export const LaneTargetType = {
  Vehicle: 0,
  Pedestrian: 1,
} as const;
export type LaneTargetType = typeof LaneTargetType[keyof typeof LaneTargetType];

export const DeviceType = {
  Camera: 1,
  Controller: 2,
  FaceId: 3,
  Other: 4,
  LedBoard: 5,
} as const;
export type DeviceType = typeof DeviceType[keyof typeof DeviceType];

// =================== GATES ===================

export interface GateDto {
  id: string;
  code: string;
  name: string;
  companyId?: string;
  companyName?: string;
  machineCode: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface GateSummaryDto {
  id: string;
  code: string;
  name: string;
  companyId?: string;
  machineCode: string;
  isActive: boolean;
}

export interface GateFilterQuery extends PaginationQuery {
  companyId?: string;
  keyword?: string;
  isActive?: boolean;
}

export interface CreateGateRequest {
  code: string;
  name: string;
  companyId?: string;
  machineCode: string;
  isActive: boolean;
}

export interface UpdateGateRequest {
  code: string;
  name: string;
  companyId?: string;
  machineCode: string;
  isActive: boolean;
}

// =================== DEVICES ===================

export interface DeviceDto {
  id: string;
  code: string;
  name: string;
  type: DeviceType;
  ipAddress: string;
  port: number;
  userName?: string;
  rtspUrl?: string;
  channel?: number;
  hasPassword?: boolean;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface DeviceSummaryDto {
  id: string;
  code: string;
  name: string;
  type: DeviceType;
  ipAddress: string;
  port: number;
  rtspUrl?: string;
  channel?: number;
  isActive: boolean;
}

export interface DeviceFilterQuery extends PaginationQuery {
  type?: DeviceType;
  keyword?: string;
  isActive?: boolean;
}

export interface CreateDeviceRequest {
  code: string;
  name: string;
  type: DeviceType;
  ipAddress: string;
  port: number;
  userName?: string;
  password?: string;
  rtspUrl?: string;
  channel?: number;
  isActive: boolean;
}

export interface UpdateDeviceRequest {
  code: string;
  name: string;
  type: DeviceType;
  ipAddress: string;
  port: number;
  userName?: string;
  password?: string;
  rtspUrl?: string;
  channel?: number;
  isActive: boolean;
}

export interface DevicePingResultDto {
  ipAddress: string;
  isAlive: boolean;
  roundtripTimeMs: number;
  method?: string;
  message?: string;
  timestamp?: string;
  status?: string;
}

// =================== LANES ===================

export interface LaneDto {
  id: string;
  code: string;
  name: string;
  gateId?: string;
  gateName?: string;
  direction: LaneDirection;
  targetType?: LaneTargetType;
  useOverviewCam?: boolean;
  usePlateCam?: boolean;
  useFaceCam?: boolean;
  overviewCameraDeviceId?: string;
  plateCameraDeviceId?: string;
  controllerDeviceId?: string;
  faceDeviceId?: string;
  ledBoardDeviceId?: string;
  outputRelay: number;
  inputReader: number;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}


/**
 * Nguồn chân lý duy nhất (SSOT): Lấy toàn bộ ID thiết bị ngoại vi được gán vào làn xe.
 */
export function getLaneDeviceIds(lane: LaneDto): string[] {
  const ids = [
    lane.overviewCameraDeviceId,
    lane.plateCameraDeviceId,
    lane.controllerDeviceId,
    lane.faceDeviceId,
    lane.ledBoardDeviceId,
  ];
  return ids.filter((id): id is string => Boolean(id && id.trim()));
}

/**
 * Kiểm tra xem làn xe có đang gắn thiết bị với ID chỉ định hay không.
 */
export function laneUsesDevice(lane: LaneDto, deviceId?: string | null): boolean {
  if (!deviceId) return false;
  return getLaneDeviceIds(lane).includes(deviceId);
}

export interface LaneDetailDto extends LaneDto {
  gate?: GateSummaryDto;
  plateCamera?: DeviceSummaryDto;
  overviewCamera?: DeviceSummaryDto;
  controller?: DeviceSummaryDto;
  faceDevice?: DeviceSummaryDto;
  ledBoard?: DeviceSummaryDto;
}

export interface LaneFilterQuery extends PaginationQuery {
  gateId?: string;
  direction?: LaneDirection;
  targetType?: LaneTargetType;
  keyword?: string;
  isActive?: boolean;
}

export interface CreateLaneRequest {
  code: string;
  name: string;
  gateId: string;
  direction: LaneDirection;
  targetType?: LaneTargetType;
  useOverviewCam?: boolean;
  usePlateCam?: boolean;
  useFaceCam?: boolean;
  overviewCameraDeviceId?: string;
  plateCameraDeviceId?: string;
  controllerDeviceId?: string;
  faceDeviceId?: string;
  ledBoardDeviceId?: string;
  outputRelay: number;
  inputReader: number;
  isActive: boolean;
}

export interface UpdateLaneRequest {
  code: string;
  name: string;
  gateId: string;
  direction: LaneDirection;
  targetType?: LaneTargetType;
  useOverviewCam?: boolean;
  usePlateCam?: boolean;
  useFaceCam?: boolean;
  overviewCameraDeviceId?: string;
  plateCameraDeviceId?: string;
  controllerDeviceId?: string;
  faceDeviceId?: string;
  ledBoardDeviceId?: string;
  outputRelay: number;
  inputReader: number;
  isActive: boolean;
}
