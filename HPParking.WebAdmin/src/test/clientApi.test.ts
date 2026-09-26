import { describe, it, expect, vi, beforeEach } from 'vitest';
import axios from 'axios';
import { clientApi, extractErrorMessage } from '@/api/clientApi';
import { apiClient } from '@/api/client';

vi.mock('@/api/client', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}));

describe('Client API & FaceID Biometrics', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('extractErrorMessage', () => {
    it('ánh xạ chính xác mã lỗi CLIENT_PHONE_DUPLICATE', () => {
      const error = new axios.AxiosError('Conflict');
      error.response = {
        status: 409,
        statusText: 'Conflict',
        data: {
          success: false,
          message: 'CLIENT_PHONE_DUPLICATE',
          errors: [],
          timestamp: new Date().toISOString(),
        },
        headers: {},
        config: {} as any,
      };

      const msg = extractErrorMessage(error);
      expect(msg).toContain('Số điện thoại này đã được đăng ký');
    });

    it('ánh xạ chính xác mã lỗi CLIENT_CODE_DUPLICATE', () => {
      const error = new axios.AxiosError('Conflict');
      error.response = {
        status: 409,
        statusText: 'Conflict',
        data: {
          success: false,
          message: 'CLIENT_CODE_DUPLICATE',
          errors: [],
          timestamp: new Date().toISOString(),
        },
        headers: {},
        config: {} as any,
      };

      const msg = extractErrorMessage(error);
      expect(msg).toContain('Mã khách hàng / Số CCCD này đã tồn tại');
    });

    it('ánh xạ chính xác mã lỗi FACEID_SYNC_FAILED', () => {
      const error = new axios.AxiosError('Bad Request');
      error.response = {
        status: 400,
        statusText: 'Bad Request',
        data: {
          success: false,
          message: 'FACEID_SYNC_FAILED',
          errors: [],
          timestamp: new Date().toISOString(),
        },
        headers: {},
        config: {} as any,
      };

      const msg = extractErrorMessage(error);
      expect(msg).toContain('Đồng bộ khuôn mặt lên thiết bị FaceID thất bại');
    });
  });

  describe('Client API calls', () => {
    it('gọi đúng endpoint GET /v1/clients khi lấy danh sách phân trang', async () => {
      const mockResult = {
        items: [
          {
            id: 'client-1',
            code: 'KH_01',
            name: 'Lê Văn C',
            birthDay: new Date().toISOString(),
            address: 'Hà Nội',
            type: 0,
            avatar: '',
            gender: 1,
            phoneNumber: '0912345678',
            isActive: true,
            expired: { enable: false, startDay: '', endDay: '' },
            createdAt: '',
          },
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

      const res = await clientApi.getPaged({ pageIndex: 1, pageSize: 15 });
      expect(apiClient.get).toHaveBeenCalledWith('/v1/clients', {
        params: { pageIndex: 1, pageSize: 15 },
      });
      expect(res.items).toHaveLength(1);
      expect(res.items[0].name).toBe('Lê Văn C');
    });

    it('gọi đúng endpoint POST /v1/clients/{id}/sync-faceid khi đồng bộ FaceID', async () => {
      vi.mocked(apiClient.post).mockResolvedValueOnce({
        data: {
          success: true,
          data: {
            clientId: 'client-1',
            clientName: 'Lê Văn C',
            totalDevices: 2,
            successCount: 2,
            failureCount: 0,
            results: [],
          },
        },
      });

      const res = await clientApi.syncFaceId('client-1');
      expect(apiClient.post).toHaveBeenCalledWith('/v1/clients/client-1/sync-faceid');
      expect(res.totalDevices).toBe(2);
    });
  });
});
