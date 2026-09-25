import type { PaginationQuery } from './masterData';

export const LaneDirection = {
  In: 1,
  Out: 2,
  Bidirectional: 3,
} as const;
export type LaneDirection = typeof LaneDirection[keyof typeof LaneDirection];

export const DeviceType = {
  Camera: 1,
  Controller: 2,
  FaceId: 3,
  Other: 4,
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
  isActive: boolean;
}

export interface DevicePingResultDto {
  ipAddress: string;
  isAlive: boolean;
  roundtripTimeMs: number;
  status: string;
}

// =================== LANES ===================

export interface LaneDto {
  id: string;
  code: string;
  name: string;
  gateId?: string;
  gateName?: string;
  direction: LaneDirection;
  overviewCameraDeviceId?: string;
  plateCameraDeviceId?: string;
  controllerDeviceId?: string;
  faceDeviceId?: string;
  outputRelay: number;
  inputReader: number;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface LaneDetailDto extends LaneDto {
  gate?: GateSummaryDto;
  plateCamera?: DeviceSummaryDto;
  overviewCamera?: DeviceSummaryDto;
  controller?: DeviceSummaryDto;
  faceDevice?: DeviceSummaryDto;
}

export interface LaneFilterQuery extends PaginationQuery {
  gateId?: string;
  direction?: LaneDirection;
  keyword?: string;
  isActive?: boolean;
}

export interface CreateLaneRequest {
  code: string;
  name: string;
  gateId: string;
  direction: LaneDirection;
  overviewCameraDeviceId?: string;
  plateCameraDeviceId?: string;
  controllerDeviceId?: string;
  faceDeviceId?: string;
  outputRelay: number;
  inputReader: number;
  isActive: boolean;
}

export interface UpdateLaneRequest {
  code: string;
  name: string;
  gateId: string;
  direction: LaneDirection;
  overviewCameraDeviceId?: string;
  plateCameraDeviceId?: string;
  controllerDeviceId?: string;
  faceDeviceId?: string;
  outputRelay: number;
  inputReader: number;
  isActive: boolean;
}
