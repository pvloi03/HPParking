import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { VehicleSessionsPage } from '@/pages/VehicleSessionsPage';
import { PedestrianSessionsPage } from '@/pages/PedestrianSessionsPage';
import { parkingSessionApi } from '@/api/parkingSessionApi';
import { downloadBlob } from '@/utils/downloadBlob';
import { ParkingSessionStatus } from '@/types/parkingSession';
import { VehicleType } from '@/types/vehicle';
import { LaneTargetType } from '@/types/infrastructure';

vi.mock('@/api/parkingSessionApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/parkingSessionApi')>();
  return {
    ...actual,
    parkingSessionApi: {
      getParkingSessions: vi.fn(),
      getSessionById: vi.fn(),
      exportParkingSessions: vi.fn(),
    },
    extractErrorMessage: vi.fn((err: any) => err?.message || 'Có lỗi xảy ra'),
  };
});

vi.mock('@/utils/downloadBlob', () => ({
  downloadBlob: vi.fn(),
}));

vi.mock('@/hooks/use-toast', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
    warning: vi.fn(),
    info: vi.fn(),
  },
}));

describe('VehicleSessionsPage & PedestrianSessionsPage', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    vi.clearAllMocks();
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });
  });

  const mockVehicleSessions = [
    {
      id: 'sess-v1',
      plateNumber: '30A-111.22',
      vehicleType: VehicleType.Car,
      status: ParkingSessionStatus.Active,
      personFullName: 'Nguyễn Văn A',
      inTime: '2026-09-24T08:00:00Z',
      inLaneName: 'Làn Ô tô 01',
      createdAt: '2026-09-24T08:00:00Z',
    },
  ];

  const mockPedestrianSessions = [
    {
      id: 'sess-p1',
      cardNumber: 'CARD-PED-001',
      cardCode: 'RFID001',
      personFullName: 'Trần Thị B',
      personCode: 'EMP-002',
      companyName: 'Công ty ABC',
      departmentName: 'Phòng Kỹ thuật',
      status: ParkingSessionStatus.Active,
      inTime: '2026-09-24T08:30:00Z',
      inLaneName: 'Làn Đi Bộ 01',
      createdAt: '2026-09-24T08:30:00Z',
    },
  ];

  it('VehicleSessionsPage hiển thị đúng tiêu đề, gọi getParkingSessions với targetType=0 và xuất Excel với targetType=0', async () => {
    vi.mocked(parkingSessionApi.getParkingSessions).mockResolvedValue({
      items: mockVehicleSessions,
      pagination: {
        pageIndex: 1,
        pageSize: 10,
        totalCount: 1,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    } as any);

    const mockBlob = new Blob(['excel-vehicle']);
    vi.mocked(parkingSessionApi.exportParkingSessions).mockResolvedValueOnce(mockBlob);

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <VehicleSessionsPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    // Tiêu đề trang
    expect(screen.getByText(/Lịch sử phương tiện vào ra/i)).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getAllByText('30A-111.22').length).toBeGreaterThan(0);
    });

    // Xác nhận API được gọi với targetType = 0 (LaneTargetType.Vehicle)
    expect(parkingSessionApi.getParkingSessions).toHaveBeenCalledWith(
      expect.objectContaining({
        targetType: LaneTargetType.Vehicle,
      })
    );

    // Có bộ lọc Loại xe
    expect(screen.getByPlaceholderText('Tìm theo biển số xe...')).toBeInTheDocument();

    // Xuất excel
    const exportBtn = screen.getByRole('button', { name: /Xuất Excel/i });
    fireEvent.click(exportBtn);

    await waitFor(() => {
      expect(parkingSessionApi.exportParkingSessions).toHaveBeenCalledWith(
        expect.objectContaining({
          targetType: LaneTargetType.Vehicle,
        })
      );
      expect(downloadBlob).toHaveBeenCalledWith(mockBlob, expect.stringContaining('.xlsx'));
    });
  });

  it('PedestrianSessionsPage hiển thị đúng tiêu đề, gọi getParkingSessions với targetType=1 và không có bộ lọc loại xe', async () => {
    vi.mocked(parkingSessionApi.getParkingSessions).mockResolvedValue({
      items: mockPedestrianSessions,
      pagination: {
        pageIndex: 1,
        pageSize: 10,
        totalCount: 1,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    } as any);

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <PedestrianSessionsPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    // Tiêu đề trang
    expect(screen.getByText(/Lịch sử người vào ra/i)).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getAllByText('Trần Thị B').length).toBeGreaterThan(0);
      expect(screen.getAllByText('EMP-002').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Làn Đi Bộ 01').length).toBeGreaterThan(0);
    });

    // Xác nhận API được gọi với targetType = 1 (LaneTargetType.Pedestrian)
    expect(parkingSessionApi.getParkingSessions).toHaveBeenCalledWith(
      expect.objectContaining({
        targetType: LaneTargetType.Pedestrian,
      })
    );

    // Placeholder tìm kiếm nhân sự, không có placeholder tìm biển số xe
    const searchInput = screen.getByPlaceholderText('Tìm theo họ tên, mã nhân sự, mã thẻ...');
    expect(searchInput).toBeInTheDocument();
    expect(screen.queryByPlaceholderText('Tìm theo biển số xe...')).not.toBeInTheDocument();

    // Nhập từ khóa tìm kiếm theo tên nhân sự
    fireEvent.change(searchInput, { target: { value: 'Trần Thị B' } });

    await waitFor(() => {
      expect(parkingSessionApi.getParkingSessions).toHaveBeenCalledWith(
        expect.objectContaining({
          targetType: LaneTargetType.Pedestrian,
          keyword: 'Trần Thị B',
        })
      );
    });
  });
});
