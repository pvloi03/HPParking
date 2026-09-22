import axios from 'axios';
import { apiClient } from './client';
import type {
  ApiResponse,
  PagedResult,
  CompanyDto,
  CompanyFilterQuery,
  CreateCompanyRequest,
  UpdateCompanyRequest,
  DepartmentDto,
  DepartmentFilterQuery,
  CreateDepartmentRequest,
  UpdateDepartmentRequest,
  ContractorDto,
  ContractorFilterQuery,
  CreateContractorRequest,
  UpdateContractorRequest,
} from '@/types/masterData';

/**
 * Trích xuất thông báo lỗi thân thiện cho người dùng từ AxiosError,
 * đặc biệt ánh xạ các mã lỗi Toàn Vẹn Tham Chiếu (Universal Restrict Deletion - 409 Conflict)
 * và Bảo Vệ Trạng Thái Hoạt Động (Active State Protection - 400 Bad Request).
 */
export function extractErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as ApiResponse<unknown> | undefined;

    if (data?.message) {
      // Ánh xạ các mã lỗi đặc thù nếu server trả về mã lỗi kỹ thuật
      if (data.message.includes('COMPANY_HAS_DEPARTMENTS')) {
        return 'Không thể xóa công ty vì vẫn còn phòng ban trực thuộc. Vui lòng chuyển hoặc xóa phòng ban trước.';
      }
      if (data.message.includes('COMPANY_HAS_GATES')) {
        return 'Không thể xóa công ty vì vẫn còn cổng kiểm soát trực thuộc.';
      }
      if (data.message.includes('COMPANY_HAS_CLIENTS')) {
        return 'Không thể xóa công ty vì vẫn còn khách hàng/nhân sự trực thuộc.';
      }
      if (data.message.includes('DEPARTMENT_HAS_CLIENTS')) {
        return 'Không thể xóa phòng ban vì vẫn còn khách hàng/nhân sự trực thuộc.';
      }
      if (data.message.includes('CONTRACTOR_HAS_CLIENTS')) {
        return 'Không thể xóa nhà thầu vì vẫn còn nhân sự/khách hàng trực thuộc.';
      }
      if (data.message.includes('PARENT_IS_DELETED')) {
        return 'Không thể khôi phục vì đơn vị cha đang nằm trong thùng rác hoặc đã bị xóa. Vui lòng khôi phục đơn vị cha trước.';
      }
      if (data.message.includes('COMPANY_ACTIVE_DEPENDENCY_EXISTS')) {
        return 'Không thể ngừng hoạt động công ty vì vẫn còn phòng ban, cổng hoặc nhân sự đang hoạt động.';
      }
      return data.message;
    }

    if (data?.errors && data.errors.length > 0) {
      return data.errors.join('. ');
    }

    if (error.response?.status === 409) {
      return 'Dữ liệu bị trùng lặp hoặc vi phạm ràng buộc toàn vẹn dữ liệu (409 Conflict).';
    }
    if (error.response?.status === 403) {
      return 'Bạn không có quyền thực hiện thao tác này (403 Forbidden).';
    }
    if (error.response?.status === 404) {
      return 'Không tìm thấy bản ghi tương ứng (404 Not Found).';
    }

    if (error.message) {
      return error.message;
    }
  }

  if (error instanceof Error) {
    return error.message;
  }

  return 'Đã xảy ra lỗi không xác định. Vui lòng thử lại sau.';
}

// =================== COMPANIES API ===================

export const companiesApi = {
  getPaged: async (query?: CompanyFilterQuery): Promise<PagedResult<CompanyDto>> => {
    const response = await apiClient.get<ApiResponse<PagedResult<CompanyDto>>>(
      '/v1/companies',
      { params: query }
    );
    return response.data.data;
  },

  getById: async (id: string): Promise<CompanyDto> => {
    const response = await apiClient.get<ApiResponse<CompanyDto>>(
      `/v1/companies/${id}`
    );
    return response.data.data;
  },

  create: async (payload: CreateCompanyRequest): Promise<CompanyDto> => {
    const response = await apiClient.post<ApiResponse<CompanyDto>>(
      '/v1/companies',
      payload
    );
    return response.data.data;
  },

  update: async (id: string, payload: UpdateCompanyRequest): Promise<CompanyDto> => {
    const response = await apiClient.put<ApiResponse<CompanyDto>>(
      `/v1/companies/${id}`,
      payload
    );
    return response.data.data;
  },

  delete: async (id: string, hardDelete = false): Promise<boolean> => {
    const response = await apiClient.delete<ApiResponse<boolean>>(
      `/v1/companies/${id}`,
      { params: { hardDelete } }
    );
    return response.data.data;
  },

  restore: async (id: string): Promise<CompanyDto> => {
    const response = await apiClient.post<ApiResponse<CompanyDto>>(
      `/v1/companies/${id}/restore`
    );
    return response.data.data;
  },
};

// =================== DEPARTMENTS API ===================

export const departmentsApi = {
  getPaged: async (query?: DepartmentFilterQuery): Promise<PagedResult<DepartmentDto>> => {
    const response = await apiClient.get<ApiResponse<PagedResult<DepartmentDto>>>(
      '/v1/departments',
      { params: query }
    );
    return response.data.data;
  },

  getById: async (id: string): Promise<DepartmentDto> => {
    const response = await apiClient.get<ApiResponse<DepartmentDto>>(
      `/v1/departments/${id}`
    );
    return response.data.data;
  },

  create: async (payload: CreateDepartmentRequest): Promise<DepartmentDto> => {
    const response = await apiClient.post<ApiResponse<DepartmentDto>>(
      '/v1/departments',
      payload
    );
    return response.data.data;
  },

  update: async (id: string, payload: UpdateDepartmentRequest): Promise<DepartmentDto> => {
    const response = await apiClient.put<ApiResponse<DepartmentDto>>(
      `/v1/departments/${id}`,
      payload
    );
    return response.data.data;
  },

  delete: async (id: string, hardDelete = false): Promise<boolean> => {
    const response = await apiClient.delete<ApiResponse<boolean>>(
      `/v1/departments/${id}`,
      { params: { hardDelete } }
    );
    return response.data.data;
  },

  restore: async (id: string): Promise<DepartmentDto> => {
    const response = await apiClient.post<ApiResponse<DepartmentDto>>(
      `/v1/departments/${id}/restore`
    );
    return response.data.data;
  },
};

// =================== CONTRACTORS API ===================

export const contractorsApi = {
  getPaged: async (query?: ContractorFilterQuery): Promise<PagedResult<ContractorDto>> => {
    const response = await apiClient.get<ApiResponse<PagedResult<ContractorDto>>>(
      '/v1/contractors',
      { params: query }
    );
    return response.data.data;
  },

  getById: async (id: string): Promise<ContractorDto> => {
    const response = await apiClient.get<ApiResponse<ContractorDto>>(
      `/v1/contractors/${id}`
    );
    return response.data.data;
  },

  create: async (payload: CreateContractorRequest): Promise<ContractorDto> => {
    const response = await apiClient.post<ApiResponse<ContractorDto>>(
      '/v1/contractors',
      payload
    );
    return response.data.data;
  },

  update: async (id: string, payload: UpdateContractorRequest): Promise<ContractorDto> => {
    const response = await apiClient.put<ApiResponse<ContractorDto>>(
      `/v1/contractors/${id}`,
      payload
    );
    return response.data.data;
  },

  delete: async (id: string, hardDelete = false): Promise<boolean> => {
    const response = await apiClient.delete<ApiResponse<boolean>>(
      `/v1/contractors/${id}`,
      { params: { hardDelete } }
    );
    return response.data.data;
  },

  restore: async (id: string): Promise<ContractorDto> => {
    const response = await apiClient.post<ApiResponse<ContractorDto>>(
      `/v1/contractors/${id}/restore`
    );
    return response.data.data;
  },
};
