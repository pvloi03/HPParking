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
        pageSize: 15,
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
});
