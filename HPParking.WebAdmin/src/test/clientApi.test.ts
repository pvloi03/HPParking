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
    it('ánh xạ chính xác mã lỗi CLIENT_PHONE_DUPLICATED', () => {
      const error = new axios.AxiosError('Conflict');
      error.response = {
        status: 409,
        statusText: 'Conflict',
        data: {
          success: false,
          message: 'CLIENT_PHONE_DUPLICATED',
          errors: [],
          timestamp: new Date().toISOString(),
        },
        headers: {},
        config: {} as any,
      };

      const msg = extractErrorMessage(error);
      expect(msg).toContain('Số điện thoại này đã được đăng ký');
    });

    it('ánh xạ chính xác mã lỗi CLIENT_IDENTITY_DUPLICATED', () => {
      const error = new axios.AxiosError('Conflict');
      error.response = {
        status: 409,
        statusText: 'Conflict',
        data: {
          success: false,
          message: 'CLIENT_IDENTITY_DUPLICATED',
          errors: [],
          timestamp: new Date().toISOString(),
        },
        headers: {},
        config: {} as any,
      };

      const msg = extractErrorMessage(error);
      expect(msg).toContain('Số CCCD/Định danh này đã tồn tại');
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
            fullName: 'Lê Văn C',
            phoneNumber: '0912345678',
            identityNumber: '001234567890',
            isFaceIdEnrolled: true,
            isActive: true,
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
      expect(res.items[0].fullName).toBe('Lê Văn C');
    });

    it('gọi đúng endpoint POST /v1/clients/{id}/sync-faceid khi đồng bộ FaceID', async () => {
      vi.mocked(apiClient.post).mockResolvedValueOnce({
        data: {
          success: true,
          data: {
            success: true,
            message: 'Đồng bộ thành công tới 2 thiết bị',
            syncedAt: new Date().toISOString(),
            deviceCount: 2,
          },
        },
      });

      const res = await clientApi.syncFaceId('client-1');
      expect(apiClient.post).toHaveBeenCalledWith('/v1/clients/client-1/sync-faceid');
      expect(res.deviceCount).toBe(2);
    });
  });
});
