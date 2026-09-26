import type { PaginationQuery } from './masterData';
import type { CreateVehicleRequest, VehicleDto } from './vehicle';

export const ClientType = {
  Employee: 0,
  Contractor: 1,
  Visitor: 2,
  VIP: 3,
  Other: 4,
} as const;

export type ClientType = (typeof ClientType)[keyof typeof ClientType];

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

export interface ClientDetailDto extends ClientDto {
  vehicles: VehicleDto[];
  faceIdTerminals?: TerminalClientStatusDto[];
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
  contractorId?: string;
  type: ClientType;
  email?: string;
  gender: number;
  phoneNumber: string;
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
