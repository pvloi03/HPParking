export interface PaginationMetadata {
  pageIndex: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface PagedResult<T> {
  items: T[];
  pagination: PaginationMetadata;
}

export interface ApiResponse<T> {
  success: boolean;
  data: T;
  message: string;
  errors: string[];
  traceId?: string;
  timestamp: string;
}

export interface PaginationQuery {
  pageIndex?: number;
  pageSize?: number;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
  onlyDeleted?: boolean;
}

// =================== COMPANY ===================

export interface CompanyDto {
  id: string;
  code: string;
  name: string;
  phoneNumber?: string;
  email?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
  createdBy?: string;
  updatedBy?: string;
}

export interface CompanyFilterQuery extends PaginationQuery {
  keyword?: string;
  isActive?: boolean;
}

export interface CreateCompanyRequest {
  code: string;
  name: string;
  phoneNumber?: string;
  email?: string;
  isActive: boolean;
}

export interface UpdateCompanyRequest {
  code: string;
  name: string;
  phoneNumber?: string;
  email?: string;
  isActive: boolean;
}

// =================== DEPARTMENT ===================

export interface DepartmentDto {
  id: string;
  companyId?: string;
  companyName?: string;
  code: string;
  name: string;
  managerName?: string;
  phoneNumber?: string;
  email?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
  createdBy?: string;
  updatedBy?: string;
}

export interface DepartmentFilterQuery extends PaginationQuery {
  companyId?: string;
  keyword?: string;
  isActive?: boolean;
}

export interface CreateDepartmentRequest {
  companyId: string;
  code: string;
  name: string;
  managerName?: string;
  phoneNumber?: string;
  email?: string;
  isActive: boolean;
}

export interface UpdateDepartmentRequest {
  companyId: string;
  code: string;
  name: string;
  managerName?: string;
  phoneNumber?: string;
  email?: string;
  isActive: boolean;
}

// =================== CONTRACTOR ===================

export interface ContractorDto {
  id: string;
  code: string;
  name: string;
  contactPerson?: string;
  phoneNumber?: string;
  email?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
  createdBy?: string;
  updatedBy?: string;
}

export interface ContractorFilterQuery extends PaginationQuery {
  keyword?: string;
  isActive?: boolean;
}

export interface CreateContractorRequest {
  code: string;
  name: string;
  contactPerson?: string;
  phoneNumber?: string;
  email?: string;
  isActive: boolean;
}

export interface UpdateContractorRequest {
  code: string;
  name: string;
  contactPerson?: string;
  phoneNumber?: string;
  email?: string;
  isActive: boolean;
}
