import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { LanesPage } from '@/pages/LanesPage';
import { lanesApi, gatesApi, devicesApi } from '@/api/infrastructureApi';
import { LaneDirection } from '@/types/infrastructure';

vi.mock('@/api/infrastructureApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/infrastructureApi')>();
  return {
    ...actual,
    lanesApi: {
      getPaged: vi.fn(),
      getDetail: vi.fn(),
      create: vi.fn(),
      update: vi.fn(),
      delete: vi.fn(),
      restore: vi.fn(),
    },
    gatesApi: {
      getPaged: vi.fn(),
    },
    devicesApi: {
      getPaged: vi.fn(),
    },
  };
});

describe('LanesPage Component', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    vi.clearAllMocks();
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });
  });

  it('hiển thị danh sách làn xe và nút xem cấu hình', async () => {
    vi.mocked(gatesApi.getPaged).mockResolvedValueOnce({
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

    vi.mocked(devicesApi.getPaged).mockResolvedValueOnce({
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

    vi.mocked(lanesApi.getPaged).mockResolvedValueOnce({
      items: [
        {
          id: 'lane-1',
          code: 'LAN_VAO_XE_MAY_1',
          name: 'Làn Xe Máy Vào 1',
          gateName: 'Cổng 1',
          direction: LaneDirection.In,
          outputRelay: 1,
          inputReader: 1,
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
          <LanesPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    expect(
      screen.getByText(/Quản Lý Làn Xe & Cấu Hình Ngoại Vi/i)
    ).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getAllByText('LAN_VAO_XE_MAY_1').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Làn Xe Máy Vào 1').length).toBeGreaterThan(0);
      expect(screen.getAllByText(/Làn Vào/i).length).toBeGreaterThan(0);
      expect(screen.getAllByText(/Xem chi tiết/i).length).toBeGreaterThan(0);
    });
  });
});
