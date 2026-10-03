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
      getById: vi.fn(),
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

  it('mở modal CompanyDetailDialog khi click nút Xem chi tiết của một công ty', async () => {
    const mockCompany = {
      id: 'comp-2',
      code: 'CTY_BETA',
      name: 'Công Ty TNHH Beta',
      phoneNumber: '0912345678',
      email: 'beta@company.com',
      isActive: true,
      createdAt: new Date().toISOString(),
    };

    vi.mocked(companiesApi.getPaged).mockResolvedValue({
      items: [mockCompany],
      pagination: {
        pageIndex: 1,
        pageSize: 10,
        totalCount: 1,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    });

    vi.mocked(companiesApi.getById).mockResolvedValue(mockCompany);

    const { fireEvent } = await import('@testing-library/react');

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <CompaniesPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getAllByText('CTY_BETA').length).toBeGreaterThan(0);
    });

    const viewButtons = screen.getAllByRole('button', { name: /Xem chi tiết/i });
    expect(viewButtons.length).toBeGreaterThan(0);
    fireEvent.click(viewButtons[0]);

    await waitFor(() => {
      expect(companiesApi.getById).toHaveBeenCalledWith('comp-2');
    });
  });
});
