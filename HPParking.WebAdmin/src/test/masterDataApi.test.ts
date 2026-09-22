import { describe, it, expect, vi, beforeEach } from 'vitest';
import axios from 'axios';
import {
  companiesApi,
  departmentsApi,
  contractorsApi,
  extractErrorMessage,
} from '@/api/masterDataApi';
import { apiClient } from '@/api/client';

vi.mock('@/api/client', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}));

describe('Master Data API & Error Extraction', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('extractErrorMessage', () => {
    it('ánh xạ chính xác mã lỗi COMPANY_HAS_DEPARTMENTS', () => {
      const error = new axios.AxiosError('Conflict');
      error.response = {
        status: 409,
        statusText: 'Conflict',
        data: {
          success: false,
          message: 'Lỗi ràng buộc: COMPANY_HAS_DEPARTMENTS',
          errors: [],
          timestamp: new Date().toISOString(),
        },
        headers: {},
        config: {} as any,
      };

      const msg = extractErrorMessage(error);
      expect(msg).toContain('vẫn còn phòng ban trực thuộc');
    });

    it('ánh xạ chính xác mã lỗi DEPARTMENT_HAS_CLIENTS', () => {
      const error = new axios.AxiosError('Conflict');
      error.response = {
        status: 409,
        statusText: 'Conflict',
        data: {
          success: false,
          message: 'DEPARTMENT_HAS_CLIENTS',
          errors: [],
          timestamp: new Date().toISOString(),
        },
        headers: {},
        config: {} as any,
      };

      const msg = extractErrorMessage(error);
      expect(msg).toContain('vẫn còn khách hàng/nhân sự trực thuộc');
    });

    it('ánh xạ chính xác mã lỗi CONTRACTOR_HAS_CLIENTS', () => {
      const error = new axios.AxiosError('Conflict');
      error.response = {
        status: 409,
        statusText: 'Conflict',
        data: {
          success: false,
          message: 'CONTRACTOR_HAS_CLIENTS',
          errors: [],
          timestamp: new Date().toISOString(),
        },
        headers: {},
        config: {} as any,
      };

      const msg = extractErrorMessage(error);
      expect(msg).toContain('vẫn còn nhân sự/khách hàng trực thuộc');
    });

    it('ánh xạ chính xác mã lỗi PARENT_IS_DELETED khi khôi phục', () => {
      const error = new axios.AxiosError('Bad Request');
      error.response = {
        status: 400,
        statusText: 'Bad Request',
        data: {
          success: false,
          message: 'PARENT_IS_DELETED',
          errors: [],
          timestamp: new Date().toISOString(),
        },
        headers: {},
        config: {} as any,
      };

      const msg = extractErrorMessage(error);
      expect(msg).toContain('đơn vị cha đang nằm trong thùng rác');
    });

    it('trả về lỗi 403 Forbidden thân thiện', () => {
      const error = new axios.AxiosError('Forbidden');
      error.response = {
        status: 403,
        statusText: 'Forbidden',
        data: null,
        headers: {},
        config: {} as any,
      };

      const msg = extractErrorMessage(error);
      expect(msg).toContain('không có quyền thực hiện thao tác này');
    });
  });

  describe('Companies API', () => {
    it('gọi đúng endpoint GET /v1/companies khi lấy danh sách phân trang', async () => {
      const mockResult = {
        items: [
          { id: 'c1', code: 'HP_CORP', name: 'Công ty Hoàng Phát', isActive: true, createdAt: '' },
        ],
        pagination: {
          pageIndex: 1,
          pageSize: 15,
          totalCount: 1,
          totalPages: 1,
          hasPreviousPage: false,
          hasNextPage: false,
        },
      };

      vi.mocked(apiClient.get).mockResolvedValueOnce({
        data: { success: true, data: mockResult },
      });

      const res = await companiesApi.getPaged({ pageIndex: 1, pageSize: 15 });
      expect(apiClient.get).toHaveBeenCalledWith('/v1/companies', {
        params: { pageIndex: 1, pageSize: 15 },
      });
      expect(res.items).toHaveLength(1);
      expect(res.items[0].code).toBe('HP_CORP');
    });

    it('gọi đúng endpoint POST /v1/companies khi tạo mới', async () => {
      const payload = {
        code: 'HP_NEW',
        name: 'Hoàng Phát Mới',
        isActive: true,
      };

      vi.mocked(apiClient.post).mockResolvedValueOnce({
        data: { success: true, data: { id: 'c2', ...payload, createdAt: '' } },
      });

      const res = await companiesApi.create(payload);
      expect(apiClient.post).toHaveBeenCalledWith('/v1/companies', payload);
      expect(res.id).toBe('c2');
    });

    it('gọi đúng endpoint DELETE /v1/companies/{id} khi xóa mềm', async () => {
      vi.mocked(apiClient.delete).mockResolvedValueOnce({
        data: { success: true, data: true },
      });

      const res = await companiesApi.delete('c1', false);
      expect(apiClient.delete).toHaveBeenCalledWith('/v1/companies/c1', {
        params: { hardDelete: false },
      });
      expect(res).toBe(true);
    });

    it('gọi đúng endpoint POST /v1/companies/{id}/restore khi khôi phục', async () => {
      vi.mocked(apiClient.post).mockResolvedValueOnce({
        data: {
          success: true,
          data: { id: 'c1', code: 'HP_CORP', name: 'Hoàng Phát', isActive: true, createdAt: '' },
        },
      });

      const res = await companiesApi.restore('c1');
      expect(apiClient.post).toHaveBeenCalledWith('/v1/companies/c1/restore');
      expect(res.id).toBe('c1');
    });
  });

  describe('Departments API', () => {
    it('gọi đúng endpoint GET /v1/departments kèm companyId filter', async () => {
      vi.mocked(apiClient.get).mockResolvedValueOnce({
        data: {
          success: true,
          data: {
            items: [],
            pagination: { pageIndex: 1, pageSize: 15, totalCount: 0, totalPages: 0, hasPreviousPage: false, hasNextPage: false },
          },
        },
      });

      await departmentsApi.getPaged({ companyId: 'comp-123' });
      expect(apiClient.get).toHaveBeenCalledWith('/v1/departments', {
        params: { companyId: 'comp-123' },
      });
    });
  });

  describe('Contractors API', () => {
    it('gọi đúng endpoint GET /v1/contractors kèm keyword', async () => {
      vi.mocked(apiClient.get).mockResolvedValueOnce({
        data: {
          success: true,
          data: {
            items: [],
            pagination: { pageIndex: 1, pageSize: 15, totalCount: 0, totalPages: 0, hasPreviousPage: false, hasNextPage: false },
          },
        },
      });

      await contractorsApi.getPaged({ keyword: 'xây dựng' });
      expect(apiClient.get).toHaveBeenCalledWith('/v1/contractors', {
        params: { keyword: 'xây dựng' },
      });
    });
  });
});
