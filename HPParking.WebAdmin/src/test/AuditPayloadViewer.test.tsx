import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuditPayloadViewer } from '@/components/auditLogs/AuditPayloadViewer';
import { auditApi } from '@/api/auditApi';
import { AuditActionType } from '@/types/auditLog';

vi.mock('@/api/auditApi', () => ({
  auditApi: {
    getById: vi.fn(),
  },
}));

function renderWithClient(ui: React.ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
      },
    },
  });
  return render(
    <QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>
  );
}

describe('AuditPayloadViewer Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  const mockDetail = {
    id: 'audit-001',
    actorId: 'admin-id',
    actorUsername: 'admin_master',
    actorRole: 'Admin',
    source: 'WebAdmin',
    actionType: AuditActionType.Update,
    targetEntity: 'Client',
    targetId: 'client-123',
    targetDisplay: 'Nguyễn Văn Test',
    isSuccess: true,
    createdAt: '2026-09-26T10:00:00Z',
    reason: 'Khách hàng cập nhật số điện thoại mới',
    errorMessage: null,
  };

  it('hiển thị thông tin sự kiện kiểm toán và ghi chú lý do thao tác', async () => {
    vi.mocked(auditApi.getById).mockResolvedValueOnce(mockDetail);

    renderWithClient(
      <AuditPayloadViewer
        open={true}
        onOpenChange={vi.fn()}
        auditLogId="audit-001"
      />
    );

    // Kiểm tra thông tin người thực hiện, nguồn và thực thể
    expect(await screen.findByText('admin_master')).toBeInTheDocument();
    expect(screen.getByText('WebAdmin')).toBeInTheDocument();
    expect(screen.getByText('Client')).toBeInTheDocument();
    expect(screen.getByText('Nguyễn Văn Test')).toBeInTheDocument();

    // Kiểm tra hiển thị lý do
    expect(screen.getByText('Khách hàng cập nhật số điện thoại mới')).toBeInTheDocument();
  });

  it('xử lý hiển thị phù hợp khi sự kiện là Đăng nhập', async () => {
    const loginDetail = {
      id: 'audit-002',
      actorId: 'user-02',
      actorUsername: 'operator01',
      actorRole: 'Manager',
      source: 'WebAdmin',
      actionType: AuditActionType.Login,
      targetEntity: 'Auth',
      targetId: 'auth-session-1',
      targetDisplay: 'Phiên đăng nhập thành công',
      isSuccess: true,
      createdAt: '2026-09-26T09:00:00Z',
      reason: null,
      errorMessage: null,
    };

    vi.mocked(auditApi.getById).mockResolvedValueOnce(loginDetail);

    renderWithClient(
      <AuditPayloadViewer
        open={true}
        onOpenChange={vi.fn()}
        auditLogId="audit-002"
      />
    );

    expect(await screen.findByText('operator01')).toBeInTheDocument();
    expect(screen.getByText('Phiên đăng nhập thành công')).toBeInTheDocument();
    expect(screen.getByText('Thành công')).toBeInTheDocument();
  });
});
