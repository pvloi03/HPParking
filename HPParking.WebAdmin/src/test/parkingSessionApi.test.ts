import { describe, it, expect, vi, beforeEach } from 'vitest';
import axios from 'axios';
import { parkingSessionApi, extractErrorMessage } from '@/api/parkingSessionApi';
import { apiClient } from '@/api/client';
import { ParkingSessionStatus } from '@/types/parkingSession';
import { VehicleType } from '@/types/vehicle';

vi.mock('@/api/client', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}));

describe('Parking Session API & Helper Tests', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('extractErrorMessage', () => {
    it('trích xuất thông báo lỗi chính xác khi gặp PARKING_SESSION_NOT_FOUND', () => {
      const error = new axios.AxiosError('Not Found');
      error.response = {
        status: 404,
        statusText: 'Not Found',
        data: {
          success: false,
          message: 'Không tìm thấy thông tin phiên đỗ xe (PARKING_SESSION_NOT_FOUND)',
          errors: [],
          timestamp: new Date().toISOString(),
        },
        headers: {},
        config: {} as any,
      };

      const msg = extractErrorMessage(error);
      expect(msg).toContain('Không tìm thấy thông tin phiên đỗ xe');
    });

    it('trích xuất thông báo lỗi 403 Forbidden khi người dùng không có quyền', () => {
      const error = new axios.AxiosError('Forbidden');
      error.response = {
        status: 403,
        statusText: 'Forbidden',
        data: {},
        headers: {},
        config: {} as any,
      };

      const msg = extractErrorMessage(error);
      expect(msg).toContain('403 Forbidden');
    });
  });

  describe('parkingSessionApi endpoints', () => {
    it('gọi đúng GET /v1/parking-sessions kèm bộ lọc phân trang và ngày tháng', async () => {
      const mockResult = {
        items: [
          {
            id: 'session-1',
            plateNumber: '30A12345',
            vehicleType: VehicleType.Car,
            status: ParkingSessionStatus.Active,
            inTime: '2026-09-24T08:00:00Z',
            inLaneName: 'Làn Vào Ô Tô 01',
            createdAt: '2026-09-24T08:00:00Z',
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

      const filter = {
        pageIndex: 1,
        pageSize: 15,
        plateNumber: '30A12345',
        status: ParkingSessionStatus.Active,
        fromDate: '2026-09-24T00:00:00',
        toDate: '2026-09-24T23:59:59',
      };

      const res = await parkingSessionApi.getParkingSessions(filter);
      expect(apiClient.get).toHaveBeenCalledWith('/v1/parking-sessions', {
        params: filter,
      });
      expect(res.items).toHaveLength(1);
      expect(res.items[0].plateNumber).toBe('30A12345');
    });

    it('gọi đúng GET /v1/parking-sessions/:id khi lấy chi tiết phiên đỗ', async () => {
      const mockDetail = {
        id: 'session-1',
        plateNumber: '30A12345',
        vehicleType: VehicleType.Car,
        status: ParkingSessionStatus.Completed,
        personFullName: 'Nguyễn Văn A',
        personPhoneNumber: '0901234567',
        inTime: '2026-09-24T08:00:00Z',
        outTime: '2026-09-24T10:00:00Z',
        durationMinutes: 120,
        durationFormatted: '2 giờ',
        inPlateImagePath: '/images/in_plate.jpg',
        inOverviewImagePath: '/images/in_ov.jpg',
        outPlateImagePath: '/images/out_plate.jpg',
        outOverviewImagePath: '/images/out_ov.jpg',
        createdAt: '2026-09-24T08:00:00Z',
      };

      vi.mocked(apiClient.get).mockResolvedValueOnce({
        data: { success: true, data: mockDetail },
      });

      const res = await parkingSessionApi.getSessionById('session-1');
      expect(apiClient.get).toHaveBeenCalledWith('/v1/parking-sessions/session-1');
      expect(res.personFullName).toBe('Nguyễn Văn A');
      expect(res.durationMinutes).toBe(120);
    });

    it('gọi đúng endpoint xuất Excel GET /v1/excel/parking-sessions/export với responseType blob', async () => {
      const fakeBlob = new Blob(['mock excel content'], {
        type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
      });

      vi.mocked(apiClient.get).mockResolvedValueOnce({
        data: fakeBlob,
      });

      const filter = {
        plateNumber: '30A',
        fromDate: '2026-09-01T00:00:00',
        toDate: '2026-09-24T23:59:59',
      };

      const result = await parkingSessionApi.exportParkingSessions(filter);
      expect(apiClient.get).toHaveBeenCalledWith('/v1/excel/parking-sessions/export', {
        params: filter,
        responseType: 'blob',
      });
      expect(result).toBe(fakeBlob);
    });
  });
});
