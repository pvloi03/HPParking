import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { DepartmentsPage } from '@/pages/DepartmentsPage';
import { departmentsApi, companiesApi } from '@/api/masterDataApi';

vi.mock('@/api/masterDataApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/masterDataApi')>();
  return {
    ...actual,
    companiesApi: {
      getPaged: vi.fn(),
    },
    departmentsApi: {
      getPaged: vi.fn(),
      getById: vi.fn(),
      create: vi.fn(),
      update: vi.fn(),
      delete: vi.fn(),
      restore: vi.fn(),
    },
  };
});

describe('DepartmentsPage Component', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    vi.clearAllMocks();
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });
  });

  it('hiển thị tiêu đề trang và danh sách phòng ban kèm tên công ty cha', async () => {
    vi.mocked(companiesApi.getPaged).mockResolvedValueOnce({
      items: [
        { id: 'c1', code: 'HP', name: 'Hoàng Phát', isActive: true, createdAt: '' },
      ],
      pagination: { pageIndex: 1, pageSize: 100, totalCount: 1, totalPages: 1, hasPreviousPage: false, hasNextPage: false },
    });

    vi.mocked(departmentsApi.getPaged).mockResolvedValueOnce({
      items: [
        {
          id: 'dept-1',
          companyId: 'c1',
          companyName: 'Hoàng Phát',
          code: 'PB_IT',
          name: 'Phòng Công Nghệ Thông Tin',
          managerName: 'Lê Quản Trị',
          phoneNumber: '0911222333',
          isActive: true,
          createdAt: new Date().toISOString(),
        },
      ],
      pagination: {
        pageIndex: 1,
        pageSize: 10,
        totalCount: 1,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    });

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <DepartmentsPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    expect(
      screen.getByText(/Quản Lý Phòng Ban Trực Thuộc/i)
    ).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getAllByText('PB_IT').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Phòng Công Nghệ Thông Tin').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Hoàng Phát').length).toBeGreaterThan(0);
    });
  });

  it('mở modal DepartmentDetailDialog khi click nút Xem chi tiết của một phòng ban', async () => {
    const mockDept = {
      id: 'dept-2',
      companyId: 'c1',
      companyName: 'Hoàng Phát',
      code: 'PB_HR',
      name: 'Phòng Tổ Chức Nhân Sự',
      managerName: 'Nguyễn Văn Nhân Sự',
      phoneNumber: '0988777888',
      isActive: true,
      createdAt: new Date().toISOString(),
    };

    vi.mocked(companiesApi.getPaged).mockResolvedValue({
      items: [{ id: 'c1', code: 'HP', name: 'Hoàng Phát', isActive: true, createdAt: '' }],
      pagination: { pageIndex: 1, pageSize: 100, totalCount: 1, totalPages: 1, hasPreviousPage: false, hasNextPage: false },
    });

    vi.mocked(departmentsApi.getPaged).mockResolvedValue({
      items: [mockDept],
      pagination: {
        pageIndex: 1,
        pageSize: 10,
        totalCount: 1,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    });

    vi.mocked(departmentsApi.getById).mockResolvedValue(mockDept);

    const { fireEvent } = await import('@testing-library/react');

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <DepartmentsPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getAllByText('PB_HR').length).toBeGreaterThan(0);
    });

    const viewButtons = screen.getAllByRole('button', { name: /Xem chi tiết/i });
    expect(viewButtons.length).toBeGreaterThan(0);
    fireEvent.click(viewButtons[0]);

    await waitFor(() => {
      expect(departmentsApi.getById).toHaveBeenCalledWith('dept-2');
    });
  });
});
