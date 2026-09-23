import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ClientsPage } from '@/pages/ClientsPage';
import { clientApi } from '@/api/clientApi';
import { companiesApi, departmentsApi, contractorsApi } from '@/api/masterDataApi';

vi.mock('@/api/clientApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/clientApi')>();
  return {
    ...actual,
    clientApi: {
      getPaged: vi.fn(),
      create: vi.fn(),
      update: vi.fn(),
      delete: vi.fn(),
      restore: vi.fn(),
      uploadAvatar: vi.fn(),
      syncFaceId: vi.fn(),
    },
  };
});

vi.mock('@/api/masterDataApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/masterDataApi')>();
  return {
    ...actual,
    companiesApi: {
      getPaged: vi.fn(),
    },
    departmentsApi: {
      getPaged: vi.fn(),
    },
    contractorsApi: {
      getPaged: vi.fn(),
    },
  };
});

describe('ClientsPage Component', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    vi.clearAllMocks();
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });
  });

  it('hiển thị danh sách khách hàng và nút đồng bộ FaceID từ API', async () => {
    vi.mocked(companiesApi.getPaged).mockResolvedValueOnce({
      items: [],
      pagination: {
        pageIndex: 1,
        pageSize: 100,
        totalCount: 0,
        totalPages: 0,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    });

    vi.mocked(departmentsApi.getPaged).mockResolvedValueOnce({
      items: [],
      pagination: {
        pageIndex: 1,
        pageSize: 200,
        totalCount: 0,
        totalPages: 0,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    });

    vi.mocked(contractorsApi.getPaged).mockResolvedValueOnce({
      items: [],
      pagination: {
        pageIndex: 1,
        pageSize: 100,
        totalCount: 0,
        totalPages: 0,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    });

    vi.mocked(clientApi.getPaged).mockResolvedValueOnce({
      items: [
        {
          id: 'cli-1',
          code: 'KH_HOANG_NAM',
          name: 'Hoàng Nam',
          phoneNumber: '0988111222',
          address: 'Hà Nội',
          birthDay: new Date().toISOString(),
          type: 0,
          avatar: 'http://example.com/avatar.jpg',
          gender: 1,
          isActive: true,
          expired: { enable: false, startDay: '', endDay: '' },
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
          <ClientsPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    expect(
      screen.getByText(/Quản Lý Hồ Sơ Khách Hàng & FaceID/i)
    ).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getAllByText('KH_HOANG_NAM').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Hoàng Nam').length).toBeGreaterThan(0);
      expect(screen.getAllByText('0988111222').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Đã có ảnh').length).toBeGreaterThan(0);
    });
  });

  it('xử lý an toàn không crash khi dữ liệu name bị null hoặc undefined', async () => {
    vi.mocked(companiesApi.getPaged).mockResolvedValueOnce({
      items: [],
      pagination: { pageIndex: 1, pageSize: 100, totalCount: 0, totalPages: 0, hasPreviousPage: false, hasNextPage: false },
    });
    vi.mocked(departmentsApi.getPaged).mockResolvedValueOnce({
      items: [],
      pagination: { pageIndex: 1, pageSize: 200, totalCount: 0, totalPages: 0, hasPreviousPage: false, hasNextPage: false },
    });
    vi.mocked(contractorsApi.getPaged).mockResolvedValueOnce({
      items: [],
      pagination: { pageIndex: 1, pageSize: 100, totalCount: 0, totalPages: 0, hasPreviousPage: false, hasNextPage: false },
    });

    vi.mocked(clientApi.getPaged).mockResolvedValueOnce({
      items: [
        {
          id: 'cli-null-name',
          code: 'KH_NO_NAME',
          name: undefined as any,
          phoneNumber: '0999888777',
          address: 'Hải Phòng',
          birthDay: new Date().toISOString(),
          type: 0,
          avatar: '',
          gender: 1,
          isActive: true,
          expired: { enable: false, startDay: '', endDay: '' },
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
          <ClientsPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getAllByText('KH_NO_NAME').length).toBeGreaterThan(0);
      expect(screen.getAllByText('KH').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Chưa có ảnh').length).toBeGreaterThan(0);
    });
  });
});
