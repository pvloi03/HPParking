import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { VehicleFormDialog } from '@/components/vehicles/VehicleFormDialog';
import { VehicleType, type VehicleDto } from '@/types/vehicle';

const { mockDefaultRoute } = vi.hoisted(() => ({
  mockDefaultRoute: {
    id: 'route-default-id',
    routeCode: 'DEFAULT',
    routeName: 'Tuyến tự do mặc định (Free-roam SLA)',
    description: 'Tuyến mặc định',
    gateSteps: [],
    isClosedLoop: false,
    alertEmails: [],
    isDefault: true,
    isActive: true,
    createdAt: new Date().toISOString(),
  },
}));

vi.mock('@/api/cardApi', () => ({
  cardApi: {
    getCards: vi.fn().mockResolvedValue({
      items: [],
      totalCount: 0,
      pageIndex: 1,
      pageSize: 20,
    }),
  },
}));

vi.mock('@/api/gateRouteApi', () => ({
  gateRouteApi: {
    getRoutes: vi.fn().mockResolvedValue({
      items: [mockDefaultRoute],
      totalCount: 1,
      pageIndex: 1,
      pageSize: 20,
      totalPages: 1,
      hasNextPage: false,
      hasPreviousPage: false,
    }),
    getById: vi.fn().mockImplementation((id: string) => {
      if (id === 'route-default-id') return Promise.resolve(mockDefaultRoute);
      return Promise.resolve(null);
    }),
  },
}));

describe('VehicleFormDialog Shared Route Default & Clear Option Tests', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });
    vi.clearAllMocks();
  });

  it('tự động chọn tuyến DEFAULT khi mở form xe nội bộ chưa có assignedRouteId', async () => {
    const onSubmit = vi.fn();
    const existingSharedVehicle: VehicleDto = {
      id: 'vehicle-shared-1',
      plateNumber: '35B2633',
      type: VehicleType.Car,
      isShared: true,
      assignedRouteId: undefined, // Chưa gán tuyến như trong ảnh của user
      isActive: true,
      createdAt: new Date().toISOString(),
    };

    render(
      <QueryClientProvider client={queryClient}>
        <VehicleFormDialog
          open={true}
          onOpenChange={vi.fn()}
          clients={[]}
          initialData={existingSharedVehicle}
          onSubmit={onSubmit}
        />
      </QueryClientProvider>
    );

    // Chờ tuyến đường DEFAULT được tự động chọn sẵn vào dropdown
    await waitFor(() => {
      const elements = screen.getAllByText(/Tuyến tự do mặc định/i);
      expect(elements.length).toBeGreaterThan(0);
    });

    const submitBtn = screen.getByRole('button', { name: /cập nhật/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalledWith(
        expect.objectContaining({
          isShared: true,
          assignedRouteId: 'route-default-id',
        })
      );
    });
  });

  it('không hiển thị lựa chọn "-- Không gán tuyến --" trong dropdown tuyến', async () => {
    const existingSharedVehicle: VehicleDto = {
      id: 'vehicle-shared-1',
      plateNumber: '35B2633',
      type: VehicleType.Car,
      isShared: true,
      assignedRouteId: undefined,
      isActive: true,
      createdAt: new Date().toISOString(),
    };

    render(
      <QueryClientProvider client={queryClient}>
        <VehicleFormDialog
          open={true}
          onOpenChange={vi.fn()}
          clients={[]}
          initialData={existingSharedVehicle}
          onSubmit={vi.fn()}
        />
      </QueryClientProvider>
    );

    // Chờ tuyến default hiển thị
    await waitFor(() => {
      const elements = screen.getAllByText(/Tuyến tự do mặc định/i);
      expect(elements.length).toBeGreaterThan(0);
    });

    // Mở combobox
    const comboboxes = screen.getAllByRole('combobox');
    // Tuyến đường là combobox chứa text DEFAULT
    const routeCombobox = comboboxes.find((cb) =>
      cb.textContent?.includes('Tuyến tự do mặc định')
    );
    expect(routeCombobox).toBeDefined();
    if (routeCombobox) {
      fireEvent.click(routeCombobox);
    }

    // Kiểm tra không có lựa chọn "-- Không gán tuyến --"
    await waitFor(() => {
      expect(screen.queryByText('-- Không gán tuyến --')).toBeNull();
    });
  });
});
