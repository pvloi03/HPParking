import { describe, it, expect, vi, beforeEach } from 'vitest';
import axios from 'axios';
import { excelApi, extractErrorMessage } from '@/api/excelApi';
import { apiClient } from '@/api/client';
import { DuplicateMode } from '@/types/excel';

vi.mock('@/api/client', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
  },
}));

describe('Excel API & Error Handling', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('extractErrorMessage', () => {
    it('trích xuất thông báo từ response.data.message', () => {
      const error = new axios.AxiosError('Bad Request');
      error.response = {
        status: 400,
        statusText: 'Bad Request',
        data: {
          success: false,
          message: 'Tệp Excel bị thiếu cột Bắt buộc: Tên công ty',
          errors: [],
          timestamp: new Date().toISOString(),
        },
        headers: {},
        config: {} as any,
      };

      const msg = extractErrorMessage(error);
      expect(msg).toBe('Tệp Excel bị thiếu cột Bắt buộc: Tên công ty');
    });

    it('trích xuất thông báo từ response.data.errors array', () => {
      const error = new axios.AxiosError('Bad Request');
      error.response = {
        status: 400,
        statusText: 'Bad Request',
        data: {
          success: false,
          message: '',
          errors: ['Dòng 2: Mã công ty đã tồn tại', 'Dòng 5: SĐT không hợp lệ'],
          timestamp: new Date().toISOString(),
        },
        headers: {},
        config: {} as any,
      };

      const msg = extractErrorMessage(error);
      expect(msg).toBe('Dòng 2: Mã công ty đã tồn tại. Dòng 5: SĐT không hợp lệ');
    });

    it('trả về câu thông báo mặc định cho mã HTTP 400', () => {
      const error = new axios.AxiosError('Bad Request');
      error.response = {
        status: 400,
        statusText: 'Bad Request',
        data: {},
        headers: {},
        config: {} as any,
      };

      const msg = extractErrorMessage(error);
      expect(msg).toBe('Tệp Excel không đúng định dạng hoặc dữ liệu cột không hợp lệ.');
    });

    it('trả về câu thông báo mặc định cho mã HTTP 403', () => {
      const error = new axios.AxiosError('Forbidden');
      error.response = {
        status: 403,
        statusText: 'Forbidden',
        data: {},
        headers: {},
        config: {} as any,
      };

      const msg = extractErrorMessage(error);
      expect(msg).toContain('Bạn không có quyền thực hiện');
    });

    it('trích xuất error.message cho Error thông thường', () => {
      const error = new Error('Lỗi mạng cục bộ');
      expect(extractErrorMessage(error)).toBe('Lỗi mạng cục bộ');
    });

    it('trả về thông báo chung khi gặp kiểu dữ liệu không xác định', () => {
      expect(extractErrorMessage('string error')).toBe(
        'Đã xảy ra lỗi không xác định khi xử lý tệp Excel.'
      );
    });
  });

  describe('excelApi methods', () => {
    it('downloadTemplate gọi đúng endpoint GET và responseType blob', async () => {
      const mockBlob = new Blob(['sample content'], {
        type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
      });
      vi.mocked(apiClient.get).mockResolvedValueOnce({ data: mockBlob });

      const result = await excelApi.downloadTemplate('companies');

      expect(apiClient.get).toHaveBeenCalledWith('/v1/excel/companies/template', {
        responseType: 'blob',
      });
      expect(result).toBe(mockBlob);
    });

    it('importData gửi FormData với file, dryRun và duplicateMode', async () => {
      const mockResult = {
        success: true,
        data: {
          totalRows: 10,
          successCount: 8,
          skippedCount: 1,
          failedCount: 1,
          isDryRun: true,
          errors: [
            {
              row: 5,
              column: 'Code',
              value: 'COMP_01',
              errorMessage: 'Trùng mã công ty',
            },
          ],
        },
      };
      vi.mocked(apiClient.post).mockResolvedValueOnce({ data: mockResult });

      const file = new File(['content'], 'test.xlsx', {
        type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
      });

      const res = await excelApi.importData('companies', file, true, DuplicateMode.Update);

      expect(apiClient.post).toHaveBeenCalledWith(
        '/v1/excel/companies/import',
        expect.any(FormData),
        {
          params: {
            dryRun: true,
            duplicateMode: DuplicateMode.Update,
          },
          headers: {
            'Content-Type': 'multipart/form-data',
          },
        }
      );
      expect(res.totalRows).toBe(10);
      expect(res.successCount).toBe(8);
      expect(res.isDryRun).toBe(true);
      expect(res.errors).toHaveLength(1);
    });

    it('exportData gọi đúng endpoint GET với params filter và responseType blob', async () => {
      const mockBlob = new Blob(['exported binary'], {
        type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
      });
      vi.mocked(apiClient.get).mockResolvedValueOnce({ data: mockBlob });

      const filter = { keyword: 'VinFast', isActive: true };
      const res = await excelApi.exportData('vehicles', filter);

      expect(apiClient.get).toHaveBeenCalledWith('/v1/excel/vehicles/export', {
        params: filter,
        responseType: 'blob',
      });
      expect(res).toBe(mockBlob);
    });
  });
});
