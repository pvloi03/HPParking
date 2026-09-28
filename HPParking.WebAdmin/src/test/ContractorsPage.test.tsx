import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ContractorsPage } from '@/pages/ContractorsPage';
import { contractorsApi } from '@/api/masterDataApi';

vi.mock('@/api/masterDataApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/masterDataApi')>();
  return {
    ...actual,
    contractorsApi: {
      getPaged: vi.fn(),
      getById: vi.fn(),
      create: vi.fn(),
      update: vi.fn(),
      delete: vi.fn(),
      restore: vi.fn(),
    },
  };
});

describe('ContractorsPage Component', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    vi.clearAllMocks();
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });
  });

  it('hiển thị tiêu đề trang và danh sách nhà thầu đối tác', async () => {
    vi.mocked(contractorsApi.getPaged).mockResolvedValueOnce({
      items: [
        {
          id: 'con-1',
          code: 'NT_DELTA',
          name: 'Nhà Thầu Cơ Điện Delta',
          contactPerson: 'Phạm Kỹ Sư',
          phoneNumber: '0909888777',
          email: 'delta@codien.vn',
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
          <ContractorsPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    expect(
      screen.getByText(/Quản Lý Nhà Thầu & Đối Tác Thi Công/i)
    ).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getAllByText('NT_DELTA').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Nhà Thầu Cơ Điện Delta').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Phạm Kỹ Sư').length).toBeGreaterThan(0);
    });
  });

  it('mở modal ContractorDetailDialog khi click nút Xem chi tiết của một nhà thầu', async () => {
    const mockContractor = {
      id: 'con-2',
      code: 'NT_GAMMA',
      name: 'Nhà Thầu PCCC Gamma',
      contactPerson: 'Đặng Văn PCCC',
      phoneNumber: '0933222111',
      email: 'gamma@pccc.vn',
      isActive: true,
      createdAt: new Date().toISOString(),
    };

    vi.mocked(contractorsApi.getPaged).mockResolvedValue({
      items: [mockContractor],
      pagination: {
        pageIndex: 1,
        pageSize: 10,
        totalCount: 1,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    });

    vi.mocked(contractorsApi.getById).mockResolvedValue(mockContractor);

    const { fireEvent } = await import('@testing-library/react');

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <ContractorsPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getAllByText('NT_GAMMA').length).toBeGreaterThan(0);
    });

    const viewButtons = screen.getAllByRole('button', { name: /Xem chi tiết/i });
    expect(viewButtons.length).toBeGreaterThan(0);
    fireEvent.click(viewButtons[0]);

    await waitFor(() => {
      expect(contractorsApi.getById).toHaveBeenCalledWith('con-2');
    });
  });
});
