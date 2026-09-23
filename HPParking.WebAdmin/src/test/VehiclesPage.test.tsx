import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { VehiclesPage } from '@/pages/VehiclesPage';
import { vehicleApi } from '@/api/vehicleApi';
import { clientApi } from '@/api/clientApi';
import { VehicleType } from '@/types/vehicle';

vi.mock('@/api/vehicleApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/vehicleApi')>();
  return {
    ...actual,
    vehicleApi: {
      getPaged: vi.fn(),
      create: vi.fn(),
      update: vi.fn(),
      delete: vi.fn(),
      restore: vi.fn(),
    },
  };
});

vi.mock('@/api/clientApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/clientApi')>();
  return {
    ...actual,
    clientApi: {
      getPaged: vi.fn(),
    },
  };
});

describe('VehiclesPage Component', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    vi.clearAllMocks();
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });
  });

  it('hiển thị danh sách phương tiện, biển số xe và loại xe từ API', async () => {
    vi.mocked(clientApi.getPaged).mockResolvedValueOnce({
      items: [
        {
          id: 'client-1',
          code: 'KH_01',
          name: 'Trần Văn Bảo',
          birthDay: '',
          address: 'Hà Nội',
          type: 0,
          avatar: '',
          gender: 1,
          phoneNumber: '0977888999',
          isActive: true,
          expired: { enable: false, startDay: '', endDay: '' },
          createdAt: '',
        },
      ],
      pagination: {
        pageIndex: 1,
        pageSize: 200,
        totalCount: 1,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    });

    vi.mocked(vehicleApi.getPaged).mockResolvedValueOnce({
      items: [
        {
          id: 'veh-1',
          plateNumber: '30A88888',
          type: VehicleType.Car,
          ownerClientId: 'client-1',
          note: 'Xe giám đốc',
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
          <VehiclesPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    expect(
      screen.getByText(/Quản Lý Phương Tiện & Biển Số Xe/i)
    ).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getAllByText('30A88888').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Ô tô').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Trần Văn Bảo').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Xe giám đốc').length).toBeGreaterThan(0);
    });
  });
});
