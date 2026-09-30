import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { VehicleFormDialog } from '@/components/vehicles/VehicleFormDialog';
import { cardApi } from '@/api/cardApi';
import { VehicleType, type VehicleDto } from '@/types/vehicle';

vi.mock('@/api/cardApi', () => ({
  cardApi: {
    getCards: vi.fn().mockResolvedValue({
      items: [
        {
          id: 'card-v1',
          cardNumber: '0000888999',
          targetType: 2,
          status: 0,
          createdAt: new Date().toISOString(),
        },
      ],
      totalCount: 1,
      pageIndex: 1,
      pageSize: 20,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false,
    }),
  },
}));

vi.mock('@/api/gateRouteApi', () => ({
  gateRouteApi: {
    getGateRoutes: vi.fn().mockResolvedValue({ items: [] }),
  },
}));

describe('VehicleFormDialog Unassigned Cards Filter Tests', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });
    vi.clearAllMocks();
  });

  const renderDialog = (props = {}) => {
    return render(
      <QueryClientProvider client={queryClient}>
        <VehicleFormDialog
          open={true}
          onOpenChange={vi.fn()}
          clients={[]}
          onSubmit={vi.fn()}
          {...props}
        />
      </QueryClientProvider>
    );
  };

  it('gọi cardApi.getCards với targetType: 2 và unassignedOnly: true khi tạo mới phương tiện', async () => {
    renderDialog();

    await waitFor(() => {
      expect(cardApi.getCards).toHaveBeenCalledWith(
        expect.objectContaining({
          targetType: 2, // Vehicle
          unassignedOnly: true,
        })
      );
    });
  });

  it('truyền assignedVehicleId khi chỉnh sửa phương tiện hiện có', async () => {
    const existingVehicle: VehicleDto = {
      id: 'vehicle-123',
      plateNumber: '15A-123.45',
      type: VehicleType.Car,
      isShared: true,
      cardCode: '0000888999',
      isActive: true,
      createdAt: new Date().toISOString(),
    };

    renderDialog({ initialData: existingVehicle });

    await waitFor(() => {
      expect(cardApi.getCards).toHaveBeenCalledWith(
        expect.objectContaining({
          targetType: 2, // Vehicle
          unassignedOnly: true,
          assignedVehicleId: 'vehicle-123',
        })
      );
    });
  });
});
