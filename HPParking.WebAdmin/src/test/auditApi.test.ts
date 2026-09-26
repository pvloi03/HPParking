import { describe, it, expect, vi, beforeEach } from 'vitest';
import { auditApi } from '@/api/auditApi';
import { apiClient } from '@/api/client';
import { AuditActionType } from '@/types/auditLog';

vi.mock('@/api/client', () => ({
  apiClient: {
    get: vi.fn(),
  },
}));

describe('auditApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('getPaged', () => {
    it('gọi đúng endpoint GET /v1/audit-logs với params phân trang và bộ lọc', async () => {
      const mockPagedResult = {
        items: [
          {
            id: 'audit-01',
            actorId: 'user-01',
            actorUsername: 'admin',
            actorRole: 'Admin',
            source: '127.0.0.1',
            actionType: AuditActionType.Create,
            targetEntity: 'Client',
            targetId: 'client-01',
            targetDisplay: 'Nguyễn Văn A',
            isSuccess: true,
            createdAt: '2026-09-26T08:00:00Z',
          },
        ],
        pagination: {
          pageIndex: 1,
          pageSize: 20,
          totalCount: 1,
          totalPages: 1,
          hasPreviousPage: false,
          hasNextPage: false,
        },
      };

      vi.mocked(apiClient.get).mockResolvedValueOnce({
        data: { success: true, data: mockPagedResult },
      });

      const query = {
        pageIndex: 1,
        pageSize: 20,
        actorUsername: 'admin',
        actionType: AuditActionType.Create,
      };

      const result = await auditApi.getPaged(query);

      expect(apiClient.get).toHaveBeenCalledWith('/v1/audit-logs', {
        params: query,
      });
      expect(result.items).toHaveLength(1);
      expect(result.items[0].targetEntity).toBe('Client');
      expect(result.items[0].actorUsername).toBe('admin');
    });
  });

  describe('getById', () => {
    it('gọi đúng endpoint GET /v1/audit-logs/{id} và trả về chi tiết kèm payload JSON', async () => {
      const mockDetail = {
        id: 'audit-02',
        actorId: 'user-01',
        actorUsername: 'admin',
        actorRole: 'Admin',
        source: '127.0.0.1',
        actionType: AuditActionType.Update,
        targetEntity: 'Vehicle',
        targetId: 'veh-01',
        targetDisplay: '30A-12345',
        isSuccess: true,
        createdAt: '2026-09-26T08:30:00Z',
        reason: 'Cập nhật loại phương tiện sang ô tô tải',
        errorMessage: null,
      };

      vi.mocked(apiClient.get).mockResolvedValueOnce({
        data: { success: true, data: mockDetail },
      });

      const result = await auditApi.getById('audit-02');

      expect(apiClient.get).toHaveBeenCalledWith('/v1/audit-logs/audit-02');
      expect(result.id).toBe('audit-02');
      expect(result.reason).toBe('Cập nhật loại phương tiện sang ô tô tải');
    });
  });
});
