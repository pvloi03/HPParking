import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, waitFor, screen, fireEvent } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { VehicleFormDialog } from '@/components/vehicles/VehicleFormDialog';
import { cardApi } from '@/api/cardApi';
import { VehicleType, type VehicleDto } from '@/types/vehicle';
import { CardTargetType } from '@/types/card';
import { useFeatureFlagsStore } from '@/config/featureFlags';

vi.mock('@/api/cardApi', () => ({
  cardApi: {
    getCards: vi.fn().mockResolvedValue({
      items: [
        {
          id: 'card-v1',
          cardNumber: '0000888999',
          targetType: 0, // CardTargetType.Vehicle
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

  it('ẩn công tắc xe dùng chung khi tạo mới nếu enableFleetDispatch = false (mặc định)', () => {
    useFeatureFlagsStore.setState({ enableFleetDispatch: false });
    renderDialog();
    expect(screen.queryByText(/Phương tiện nội bộ \/ xe dùng chung/i)).not.toBeInTheDocument();
  });

  it('gọi cardApi.getCards với targetType: CardTargetType.Vehicle và unassignedOnly: true khi tạo mới phương tiện dùng chung', async () => {
    useFeatureFlagsStore.setState({ enableFleetDispatch: true });
    renderDialog();

    // Bật toggle Phương tiện nội bộ / xe dùng chung để hiển thị mục thẻ xe
    const toggleButtons = screen.getAllByRole('button');
    const sharedToggle = toggleButtons.find(b => b.className?.includes('rounded-full'));
    if (sharedToggle) {
      fireEvent.click(sharedToggle);
    }

    await waitFor(() => {
      expect(cardApi.getCards).toHaveBeenCalledWith(
        expect.objectContaining({
          targetType: CardTargetType.Vehicle,
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
          targetType: CardTargetType.Vehicle,
          unassignedOnly: true,
          assignedVehicleId: 'vehicle-123',
        })
      );
    });
  });

  it('giữ nguyên isShared và assignedRouteId khi tắt cờ enableFleetDispatch lúc chỉnh sửa xe dùng chung', async () => {
    useFeatureFlagsStore.setState({ enableFleetDispatch: false });
    const onSubmit = vi.fn();
    const existingSharedVehicle: VehicleDto = {
      id: 'vehicle-shared-flag-off',
      plateNumber: '15A-888.88',
      type: VehicleType.Car,
      isShared: true,
      assignedRouteId: 'route-fixed-888',
      cardCode: '0000888999',
      isActive: true,
      createdAt: new Date().toISOString(),
    };

    renderDialog({ initialData: existingSharedVehicle, onSubmit });

    // Khi enableFleetDispatch = false, nhưng xe đang edit là isShared = true:
    // Khối cấu hình phương tiện nội bộ vẫn hiển thị
    expect(screen.getByText(/Phương tiện nội bộ \/ Xe dùng chung/i)).toBeInTheDocument();

    const submitBtn = screen.getByRole('button', { name: /cập nhật/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalledWith(
        expect.objectContaining({
          isShared: true,
          assignedRouteId: 'route-fixed-888',
        })
      );
    });
  });
});
