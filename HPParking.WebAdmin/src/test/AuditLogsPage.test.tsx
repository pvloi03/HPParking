import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { AuditLogsPage } from '@/pages/AuditLogsPage';
import { auditApi } from '@/api/auditApi';
import { AuditActionType } from '@/types/auditLog';

vi.mock('@/api/auditApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/auditApi')>();
  return {
    ...actual,
    auditApi: {
      getPaged: vi.fn(),
      getById: vi.fn(),
    },
  };
});

describe('AuditLogsPage Component', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    vi.clearAllMocks();
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });
  });

  const mockPagedLogs = {
    items: [
      {
        id: 'log-001',
        actorId: 'user-01',
        actorUsername: 'admin_root',
        actorRole: 'Admin',
        source: '192.168.1.15',
        actionType: AuditActionType.Create,
        targetEntity: 'Client',
        targetId: 'client-10',
        targetDisplay: 'Trần Thị Mai',
        isSuccess: true,
        createdAt: '2026-09-26T08:15:00Z',
      },
      {
        id: 'log-002',
        actorId: 'user-02',
        actorUsername: 'manager_vinh',
        actorRole: 'Manager',
        source: '192.168.1.20',
        actionType: AuditActionType.ChangePassword,
        targetEntity: 'User',
        targetId: 'user-02',
        targetDisplay: 'manager_vinh',
        isSuccess: false,
        createdAt: '2026-09-26T08:20:00Z',
      },
    ],
    pagination: {
      pageIndex: 1,
      pageSize: 15,
      totalCount: 2,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false,
    },
  };

  it('hiển thị tiêu đề trang và danh sách các sự kiện kiểm toán tải về từ API', async () => {
    vi.mocked(auditApi.getPaged).mockResolvedValue(mockPagedLogs);

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <AuditLogsPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    expect(await screen.findByText('Nhật Ký Kiểm Toán')).toBeInTheDocument();
    expect(await screen.findAllByText('admin_root')).toHaveLength(2); // desktop + mobile
    expect(screen.getAllByText('manager_vinh').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Trần Thị Mai').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Thành công').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Thất bại').length).toBeGreaterThan(0);
  });

  it('mở modal chi tiết AuditPayloadViewer khi người dùng nhấn nút xem chi tiết', async () => {
    vi.mocked(auditApi.getPaged).mockResolvedValue(mockPagedLogs);
    vi.mocked(auditApi.getById).mockResolvedValue({
      ...mockPagedLogs.items[0],
      reason: 'Đăng ký khách hàng mới',
      errorMessage: null,
    });

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <AuditLogsPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    // Chờ danh sách xuất hiện
    expect((await screen.findAllByText('admin_root')).length).toBeGreaterThan(0);

    // Nhấn nút xem chi tiết của dòng đầu tiên
    const viewButtons = screen.getAllByTitle('Xem chi tiết');
    expect(viewButtons.length).toBeGreaterThan(0);
    fireEvent.click(viewButtons[0]);

    // Kiểm tra Dialog chi tiết mở lên
    expect(await screen.findByText('Chi Tiết Nhật Ký Kiểm Toán')).toBeInTheDocument();
  });
});
