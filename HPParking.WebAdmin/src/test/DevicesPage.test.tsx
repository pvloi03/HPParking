import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { DevicesPage } from '@/pages/DevicesPage';
import { devicesApi } from '@/api/infrastructureApi';
import { DeviceType } from '@/types/infrastructure';

vi.mock('@/api/infrastructureApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/infrastructureApi')>();
  return {
    ...actual,
    devicesApi: {
      getPaged: vi.fn(),
      create: vi.fn(),
      update: vi.fn(),
      delete: vi.fn(),
      restore: vi.fn(),
    },
  };
});

describe('DevicesPage Component', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    vi.clearAllMocks();
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });
  });

  it('hiển thị danh sách thiết bị và loại thiết bị từ API', async () => {
    vi.mocked(devicesApi.getPaged).mockResolvedValueOnce({
      items: [
        {
          id: 'dev-1',
          code: 'CAM_LPR_01',
          name: 'Camera Biển Số Cổng 1',
          type: DeviceType.Camera,
          ipAddress: '192.168.1.50',
          port: 80,
          userName: 'admin',
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
          <DevicesPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    expect(
      screen.getByText(/Quản Lý Thiết Bị Ngoại Vi/i)
    ).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getAllByText('CAM_LPR_01').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Camera Biển Số Cổng 1').length).toBeGreaterThan(0);
      expect(screen.getAllByText(/192.168.1.50:80/i).length).toBeGreaterThan(0);
      expect(screen.getAllByText('Camera').length).toBeGreaterThan(0);
    });
  });
});
