import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { VehicleDetailDialog } from '@/components/vehicles/VehicleDetailDialog';
import { vehicleApi } from '@/api/vehicleApi';
import { clientApi } from '@/api/clientApi';
import { VehicleType } from '@/types/vehicle';

vi.mock('@/api/vehicleApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/vehicleApi')>();
  return {
    ...actual,
    vehicleApi: {
      getById: vi.fn(),
    },
  };
});

vi.mock('@/api/clientApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/clientApi')>();
  return {
    ...actual,
    clientApi: {
      getById: vi.fn(),
    },
  };
});

describe('VehicleDetailDialog Component', () => {
  let queryClient: QueryClient;

  const mockVehicle = {
    id: 'veh-101',
    plateNumber: '15A-999.88',
    type: VehicleType.Car,
    ownerClientId: 'client-1',
    isShared: false,
    isActive: true,
    note: 'Xe giám đốc lưu ý ưu tiên làn',
    createdAt: '2026-05-01T08:00:00Z',
    updatedAt: '2026-09-01T10:00:00Z',
  };

  const mockOwner = {
    id: 'client-1',
    code: 'KH_VIP_01',
    name: 'Nguyễn Văn Chủ Xe',
    phoneNumber: '0988666555',
    address: 'Số 123 Đường Cầu Giấy, Hà Nội',
    email: 'chuxe@hpparking.vn',
    isActive: true,
  };

  beforeEach(() => {
    vi.clearAllMocks();
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
      },
    });

    vi.mocked(vehicleApi.getById).mockResolvedValue(mockVehicle);
    vi.mocked(clientApi.getById).mockResolvedValue(mockOwner as any);
  });

  it('hiển thị khung biển số xe sắc nét, loại xe và thông tin chủ sở hữu', async () => {
    render(
      <QueryClientProvider client={queryClient}>
        <VehicleDetailDialog
          open={true}
          onOpenChange={vi.fn()}
          vehicleId="veh-101"
        />
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('15A-999.88')).toBeInTheDocument();
      expect(screen.getByText('Ô tô')).toBeInTheDocument();
      expect(screen.getByText('Nguyễn Văn Chủ Xe')).toBeInTheDocument();
      expect(screen.getByText('KH_VIP_01', { exact: false })).toBeInTheDocument();
      expect(screen.getByText('0988666555')).toBeInTheDocument();
      expect(screen.getByText('Số 123 Đường Cầu Giấy, Hà Nội')).toBeInTheDocument();
      expect(screen.getByText('chuxe@hpparking.vn')).toBeInTheDocument();
      expect(screen.getByText('Xe giám đốc lưu ý ưu tiên làn')).toBeInTheDocument();
    });
  });

  it('không hiển thị phần lịch sử ra vào trong chi tiết phương tiện', async () => {
    render(
      <QueryClientProvider client={queryClient}>
        <VehicleDetailDialog
          open={true}
          onOpenChange={vi.fn()}
          vehicleId="veh-101"
        />
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('15A-999.88')).toBeInTheDocument();
    });

    expect(screen.queryByText(/Lịch sử ra vào/i)).not.toBeInTheDocument();
  });

  it('gọi callback onEdit khi click nút Chỉnh sửa trong dialog', async () => {
    const handleEdit = vi.fn();
    const handleOpenChange = vi.fn();

    render(
      <QueryClientProvider client={queryClient}>
        <VehicleDetailDialog
          open={true}
          onOpenChange={handleOpenChange}
          vehicleId="veh-101"
          onEdit={handleEdit}
        />
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('15A-999.88')).toBeInTheDocument();
    });

    const editBtn = screen.getByRole('button', { name: /Chỉnh sửa/i });
    fireEvent.click(editBtn);

    expect(handleOpenChange).toHaveBeenCalledWith(false);
    expect(handleEdit).toHaveBeenCalledWith(mockVehicle);
  });
});
