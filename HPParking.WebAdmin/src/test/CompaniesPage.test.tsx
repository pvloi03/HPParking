import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { CompaniesPage } from '@/pages/CompaniesPage';
import { companiesApi } from '@/api/masterDataApi';

vi.mock('@/api/masterDataApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/masterDataApi')>();
  return {
    ...actual,
    companiesApi: {
      getPaged: vi.fn(),
      create: vi.fn(),
      update: vi.fn(),
      delete: vi.fn(),
      restore: vi.fn(),
    },
  };
});

describe('CompaniesPage Component', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    vi.clearAllMocks();
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });
  });

  it('hiển thị tiêu đề trang và danh sách công ty tải về từ API', async () => {
    vi.mocked(companiesApi.getPaged).mockResolvedValueOnce({
      items: [
        {
          id: 'comp-1',
          code: 'CTY_ALPHA',
          name: 'Tập Đoàn Alpha',
          phoneNumber: '0988777666',
          email: 'alpha@test.vn',
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
          <CompaniesPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    expect(
      screen.getByText(/Quản Lý Công Ty & Đơn Vị Thành Viên/i)
    ).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getAllByText('CTY_ALPHA').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Tập Đoàn Alpha').length).toBeGreaterThan(0);
    });
  });
});
