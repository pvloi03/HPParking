import { describe, it, expect, vi, beforeEach } from 'vitest';
import axios from 'axios';
import {
  gatesApi,
  lanesApi,
  devicesApi,
  extractErrorMessage,
} from '@/api/infrastructureApi';
import { apiClient } from '@/api/client';
import { LaneDirection, DeviceType } from '@/types/infrastructure';

vi.mock('@/api/client', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}));

describe('Infrastructure API & Domain Error Handling', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('extractErrorMessage', () => {
    it('ánh xạ chính xác mã lỗi GATE_HAS_LANES', () => {
      const error = new axios.AxiosError('Conflict');
      error.response = {
        status: 409,
        statusText: 'Conflict',
        data: {
          success: false,
          message: 'Lỗi ràng buộc: GATE_HAS_LANES',
          errors: [],
          timestamp: new Date().toISOString(),
        },
        headers: {},
        config: {} as any,
      };

      const msg = extractErrorMessage(error);
      expect(msg).toContain('vẫn còn làn xe trực thuộc');
    });

    it('ánh xạ chính xác mã lỗi DEVICE_IN_USE_BY_LANE', () => {
      const error = new axios.AxiosError('Conflict');
      error.response = {
        status: 409,
        statusText: 'Conflict',
        data: {
          success: false,
          message: 'DEVICE_IN_USE_BY_LANE',
          errors: [],
          timestamp: new Date().toISOString(),
        },
        headers: {},
        config: {} as any,
      };

      const msg = extractErrorMessage(error);
      expect(msg).toContain('đang được liên kết cấu hình trong một hoặc nhiều làn xe');
    });

    it('ánh xạ chính xác mã lỗi INFRA_ACTIVE_DEPENDENCY_EXISTS', () => {
      const error = new axios.AxiosError('Bad Request');
      error.response = {
        status: 400,
        statusText: 'Bad Request',
        data: {
          success: false,
          message: 'INFRA_ACTIVE_DEPENDENCY_EXISTS',
          errors: [],
          timestamp: new Date().toISOString(),
        },
        headers: {},
        config: {} as any,
      };

      const msg = extractErrorMessage(error);
      expect(msg).toContain('vẫn còn các thành phần phụ thuộc');
    });

    it('ánh xạ chính xác mã lỗi DEVICE_IS_DELETED', () => {
      const error = new axios.AxiosError('Bad Request');
      error.response = {
        status: 400,
        statusText: 'Bad Request',
        data: {
          success: false,
          message: 'DEVICE_IS_DELETED',
          errors: [],
          timestamp: new Date().toISOString(),
        },
        headers: {},
        config: {} as any,
      };

      const msg = extractErrorMessage(error);
      expect(msg).toContain('thiết bị đã chọn nằm trong thùng rác');
    });
  });

  describe('Gates API', () => {
    it('gọi đúng endpoint GET /v1/gates khi lấy danh sách', async () => {
      const mockResult = {
        items: [
          {
            id: 'gate-1',
            code: 'GATE_01',
            name: 'Cổng 1',
            machineCode: 'BOT_01',
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

      const res = await gatesApi.getPaged({ pageIndex: 1, pageSize: 15 });
      expect(apiClient.get).toHaveBeenCalledWith('/v1/gates', {
        params: { pageIndex: 1, pageSize: 15 },
      });
      expect(res.items).toHaveLength(1);
      expect(res.items[0].code).toBe('GATE_01');
    });

    it('gọi đúng endpoint POST /v1/gates khi tạo mới', async () => {
      const payload = {
        companyId: 'comp-1',
        code: 'GATE_02',
        name: 'Cổng 2',
        machineCode: 'BOT_02',
        isActive: true,
      };

      vi.mocked(apiClient.post).mockResolvedValueOnce({
        data: { success: true, data: { id: 'gate-2', ...payload, createdAt: '' } },
      });

      const res = await gatesApi.create(payload);
      expect(apiClient.post).toHaveBeenCalledWith('/v1/gates', payload);
      expect(res.id).toBe('gate-2');
    });

    it('gọi đúng endpoint DELETE /v1/gates/{id} khi xóa mềm', async () => {
      vi.mocked(apiClient.delete).mockResolvedValueOnce({
        data: { success: true, data: true },
      });

      const res = await gatesApi.delete('gate-1', false);
      expect(apiClient.delete).toHaveBeenCalledWith('/v1/gates/gate-1', {
        params: { hardDelete: false },
      });
      expect(res).toBe(true);
    });
  });

  describe('Lanes API', () => {
    it('gọi đúng endpoint GET /v1/lanes/{id}/detail khi xem chi tiết ngoại vi', async () => {
      const mockDetail = {
        id: 'lane-1',
        code: 'LANE_01',
        name: 'Làn Vào 1',
        direction: LaneDirection.In,
        outputRelay: 1,
        inputReader: 1,
        isActive: true,
        createdAt: '',
        plateCamera: {
          id: 'dev-cam-1',
          code: 'CAM_01',
          name: 'Cam LPR',
          type: DeviceType.Camera,
          ipAddress: '192.168.1.101',
          port: 80,
          isActive: true,
        },
      };

      vi.mocked(apiClient.get).mockResolvedValueOnce({
        data: { success: true, data: mockDetail },
      });

      const res = await lanesApi.getDetail('lane-1');
      expect(apiClient.get).toHaveBeenCalledWith('/v1/lanes/lane-1/detail');
      expect(res.plateCamera?.ipAddress).toBe('192.168.1.101');
    });
  });

  describe('Devices API', () => {
    it('gọi đúng endpoint GET /v1/devices kèm lọc theo type', async () => {
      vi.mocked(apiClient.get).mockResolvedValueOnce({
        data: {
          success: true,
          data: {
            items: [],
            pagination: { pageIndex: 1, pageSize: 15, totalCount: 0, totalPages: 0, hasPreviousPage: false, hasNextPage: false },
          },
        },
      });

      await devicesApi.getPaged({ type: DeviceType.Camera });
      expect(apiClient.get).toHaveBeenCalledWith('/v1/devices', {
        params: { type: DeviceType.Camera },
      });
    });
  });
});
