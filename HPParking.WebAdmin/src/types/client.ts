import type { PaginationQuery } from './masterData';
import type { CreateVehicleRequest, VehicleDto } from './vehicle';

export const ClientType = {
  Employee: 0,
  Contractor: 1,
  Visitor: 2,
  VIP: 3,
  Guest: 4,
} as const;

export type ClientType = (typeof ClientType)[keyof typeof ClientType];

export const AuthMethod = {
  Card: 'Card',
  FaceId: 'FaceId',
  None: 'None',
} as const;

export type AuthMethod = (typeof AuthMethod)[keyof typeof AuthMethod];

export interface Expired {
  enable: boolean;
  startDay: string;
  endDay: string;
}

export interface ClientDto {
  id: string;
  code: string;
  name: string;
  birthDay: string;
  address: string;
  companyId?: string;
  departmentId?: string;
  contractorId?: string;
  type: ClientType;
  email?: string;
  avatar: string;
  gender: number;
  phoneNumber: string;
  cardCode: string;
  authMethods: string[];
  verifyVehiclePlate: boolean;
  isActive: boolean;
  expired: Expired;
  note?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface TerminalClientStatusDto {
  deviceIp: string;
  deviceName: string;
  isOnline: boolean;
  userExists: boolean;
  hasFace: boolean;
  cardCount: number;
  cards: string[];
  errorMessage?: string;
  timestamp: string;
}

export interface ClientFaceIdStatusResponse {
  clientId: string;
  clientCode: string;
  clientName: string;
  totalDevices: number;
  onlineDevices: number;
  enrolledFaceDevices: number;
  terminals: TerminalClientStatusDto[];
}

export const ClientApprovalActionType = {
  Activate: 1,
  Deactivate: 2,
} as const;

export type ClientApprovalActionType =
  (typeof ClientApprovalActionType)[keyof typeof ClientApprovalActionType];

export interface ClientApprovalRequestDto {
  id: string;
  clientId: string;
  approvingDepartmentId: string;
  approvingDepartmentName: string;
  approvingDepartmentEmail: string;
  actionType: ClientApprovalActionType;
  expiresAt: string;
  isProcessed: boolean;
  processedAt?: string;
  createdAt: string;
  note?: string;
  emailSent?: boolean;
  emailError?: string;
}

export interface RequestClientStatusChangeRequest {
  approvingDepartmentId: string;
  actionType: ClientApprovalActionType;
  note?: string;
}

export interface ConfirmClientStatusResponse {
  clientId: string;
  clientName: string;
  clientCode: string;
  actionType: ClientApprovalActionType;
  newActiveStatus: boolean;
  processedAt: string;
  approvingDepartmentName: string;
  message: string;
}

export interface ClientDetailDto extends ClientDto {
  vehicles: VehicleDto[];
  faceIdTerminals?: TerminalClientStatusDto[];
  latestApprovalRequest?: ClientApprovalRequestDto;
}

export interface ClientFilterQuery extends PaginationQuery {
  keyword?: string;
  type?: ClientType;
  isActive?: boolean;
  companyId?: string;
  departmentId?: string;
  contractorId?: string;
  onlyDeleted?: boolean;
}

export interface CreateClientRequest {
  code: string;
  name: string;
  birthDay: string;
  address: string;
  companyId?: string;
  departmentId?: string;
  approvingDepartmentId?: string;
  contractorId?: string;
  type: ClientType;
  email?: string;
  gender: number;
  phoneNumber: string;
  cardCode?: string;
  authMethods: string[];
  verifyVehiclePlate: boolean;
  isActive: boolean;
  expired: Expired;
  note?: string;
  vehicles?: CreateVehicleRequest[];
}

export interface UpdateClientRequest {
  code: string;
  name: string;
  birthDay: string;
  address: string;
  companyId?: string;
  departmentId?: string;
  contractorId?: string;
  type: ClientType;
  email?: string;
  gender: number;
  phoneNumber: string;
  cardCode?: string;
  authMethods: string[];
  verifyVehiclePlate: boolean;
  isActive: boolean;
  expired: Expired;
  note?: string;
  vehicles?: CreateVehicleRequest[];
}

export interface FaceIdTerminalResultDto {
  deviceIp: string;
  deviceName: string;
  isSuccess: boolean;
  errorMessage?: string;
  timestamp: string;
}

export interface SyncFaceIdResponse {
  clientId: string;
  clientName: string;
  totalDevices: number;
  successCount: number;
  failureCount: number;
  results: FaceIdTerminalResultDto[];
}
