import { describe, it, expect, vi, beforeEach } from 'vitest';
import axios from 'axios';
import { vehicleApi, extractErrorMessage } from '@/api/vehicleApi';
import { apiClient } from '@/api/client';
import { normalizePlateNumber, VehicleType } from '@/types/vehicle';

vi.mock('@/api/client', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}));

describe('Vehicle API & Plate Normalization', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('normalizePlateNumber', () => {
    it('chuẩn hóa biển số xe chính xác: loại bỏ dấu chấm, gạch ngang, khoảng trắng và viết hoa', () => {
      expect(normalizePlateNumber('30A-123.45')).toBe('30A12345');
      expect(normalizePlateNumber('  29-b1 999.88 ')).toBe('29B199988');
      expect(normalizePlateNumber('51f_11122')).toBe('51F11122');
      expect(normalizePlateNumber('')).toBe('');
    });
  });

  describe('extractErrorMessage', () => {
    it('ánh xạ chính xác mã lỗi VEHICLE_PLATE_DUPLICATE', () => {
      const error = new axios.AxiosError('Conflict');
      error.response = {
        status: 409,
        statusText: 'Conflict',
        data: {
          success: false,
          message: 'Lỗi: VEHICLE_PLATE_DUPLICATE',
          errors: [],
          timestamp: new Date().toISOString(),
        },
        headers: {},
        config: {} as any,
      };

      const msg = extractErrorMessage(error);
      expect(msg).toContain('Biển số xe này đã được đăng ký và đang hoạt động');
    });

    it('ánh xạ chính xác mã lỗi CLIENT_NOT_FOUND', () => {
      const error = new axios.AxiosError('Not Found');
      error.response = {
        status: 404,
        statusText: 'Not Found',
        data: {
          success: false,
          message: 'CLIENT_NOT_FOUND',
          errors: [],
          timestamp: new Date().toISOString(),
        },
        headers: {},
        config: {} as any,
      };

      const msg = extractErrorMessage(error);
      expect(msg).toContain('Không tìm thấy hồ sơ khách hàng');
    });
  });

  describe('Vehicle API calls', () => {
    it('gọi đúng endpoint GET /v1/vehicles kèm tham số phân trang', async () => {
      const mockResult = {
        items: [
          {
            id: 'veh-1',
            plateNumber: '30A12345',
            type: VehicleType.Car,
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

      const res = await vehicleApi.getPaged({ pageIndex: 1, pageSize: 15 });
      expect(apiClient.get).toHaveBeenCalledWith('/v1/vehicles', {
        params: { pageIndex: 1, pageSize: 15 },
      });
      expect(res.items[0].plateNumber).toBe('30A12345');
    });

    it('gọi đúng endpoint POST /v1/vehicles khi tạo mới phương tiện', async () => {
      const payload = {
        clientId: 'client-1',
        plateNumber: '29B199988',
        type: VehicleType.Motorbike,
        isActive: true,
      };

      vi.mocked(apiClient.post).mockResolvedValueOnce({
        data: {
          success: true,
          data: { id: 'veh-2', ...payload, createdAt: '' },
        },
      });

      const res = await vehicleApi.create(payload);
      expect(apiClient.post).toHaveBeenCalledWith('/v1/vehicles', payload);
      expect(res.id).toBe('veh-2');
    });
  });
});
