import type { PaginationQuery } from './masterData';

export interface ClientDto {
  id: string;
  code: string;
  fullName: string;
  phoneNumber: string;
  identityNumber: string;
  email?: string;
  avatarUrl?: string;
  companyId?: string;
  companyName?: string;
  departmentId?: string;
  departmentName?: string;
  contractorId?: string;
  contractorName?: string;
  isFaceIdEnrolled: boolean;
  lastFaceIdSyncAt?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
  createdBy?: string;
  updatedBy?: string;
}

export interface ClientFilterQuery extends PaginationQuery {
  companyId?: string;
  departmentId?: string;
  contractorId?: string;
  keyword?: string;
  isActive?: boolean;
  isFaceIdEnrolled?: boolean;
  onlyDeleted?: boolean;
}

export interface CreateClientRequest {
  code: string;
  fullName: string;
  phoneNumber: string;
  identityNumber: string;
  email?: string;
  avatarUrl?: string;
  companyId?: string;
  departmentId?: string;
  contractorId?: string;
  isActive: boolean;
}

export interface UpdateClientRequest {
  code: string;
  fullName: string;
  phoneNumber: string;
  identityNumber: string;
  email?: string;
  avatarUrl?: string;
  companyId?: string;
  departmentId?: string;
  contractorId?: string;
  isActive: boolean;
}

export interface SyncFaceIdResultDto {
  success: boolean;
  message: string;
  syncedAt: string;
  deviceCount?: number;
}
