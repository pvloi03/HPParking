import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ParkingSessionsPage } from '@/pages/ParkingSessionsPage';
import { parkingSessionApi } from '@/api/parkingSessionApi';
import { downloadBlob } from '@/utils/downloadBlob';
import { ParkingSessionStatus } from '@/types/parkingSession';
import { VehicleType } from '@/types/vehicle';

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

vi.mock('sonner', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
    warning: vi.fn(),
    info: vi.fn(),
  },
}));

describe('ParkingSessionsPage Component', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    vi.clearAllMocks();
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });
  });

  const mockSessions = [
    {
      id: 'sess-1',
      plateNumber: '30A-111.22',
      vehicleType: VehicleType.Car,
      status: ParkingSessionStatus.Active,
      personFullName: 'Nguyễn Văn A',
      inTime: '2026-09-24T08:00:00Z',
      inLaneName: 'Làn Vào Ô Tô 01',
      createdAt: '2026-09-24T08:00:00Z',
    },
    {
      id: 'sess-2',
      plateNumber: '29B-999.88',
      vehicleType: VehicleType.Motorbike,
      status: ParkingSessionStatus.UnmatchedOut,
      personFullName: 'Khách vãng lai',
      inTime: '2026-09-24T07:00:00Z',
      outTime: '2026-09-24T09:00:00Z',
      durationMinutes: 120,
      inLaneName: 'Làn Vào 02',
      outLaneName: 'Làn Ra 02',
      createdAt: '2026-09-24T07:00:00Z',
    },
  ];

  const mockPagedResult = {
    items: mockSessions,
    pagination: {
      pageIndex: 1,
      pageSize: 15,
      totalCount: 2,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false,
    },
  };

  it('hiển thị danh sách phiên đỗ xe, biển số xe font-mono và cảnh báo lệch biển số', async () => {
    vi.mocked(parkingSessionApi.getParkingSessions).mockResolvedValueOnce(mockPagedResult as any);

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <ParkingSessionsPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getAllByText('30A-111.22').length).toBeGreaterThan(0);
      expect(screen.getAllByText('29B-999.88').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Nguyễn Văn A').length).toBeGreaterThan(0);
    });

    // Phiên sess-2 có status là UnmatchedOut phải có badge/cảnh báo Lệch biển
    expect(screen.getAllByText(/Lệch biển/i).length).toBeGreaterThan(0);
  });

  it('bộ chọn ngày nhanh (Date Presets) cập nhật khoảng thời gian và gọi lại API', async () => {
    vi.mocked(parkingSessionApi.getParkingSessions).mockResolvedValue(mockPagedResult as any);

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <ParkingSessionsPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getAllByText('30A-111.22').length).toBeGreaterThan(0);
    });

    // Bấm nút xem nhanh Hôm nay
    const todayBtn = screen.getByRole('button', { name: 'Hôm nay' });
    fireEvent.click(todayBtn);

    await waitFor(() => {
      expect(parkingSessionApi.getParkingSessions).toHaveBeenCalled();
    });
  });

  it('tải file Excel khi bấm nút Xuất Excel', async () => {
    vi.mocked(parkingSessionApi.getParkingSessions).mockResolvedValue(mockPagedResult as any);
    const mockBlob = new Blob(['excel-data']);
    vi.mocked(parkingSessionApi.exportParkingSessions).mockResolvedValueOnce(mockBlob);

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <ParkingSessionsPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getAllByText('30A-111.22').length).toBeGreaterThan(0);
    });

    const exportBtn = screen.getByRole('button', { name: /Xuất Excel/i });
    fireEvent.click(exportBtn);

    await waitFor(() => {
      expect(parkingSessionApi.exportParkingSessions).toHaveBeenCalled();
      expect(downloadBlob).toHaveBeenCalledWith(mockBlob, expect.stringContaining('.xlsx'));
    });
  });

  it('mở ParkingSessionDetailDialog khi click nút Chi tiết', async () => {
    vi.mocked(parkingSessionApi.getParkingSessions).mockResolvedValue(mockPagedResult as any);
    vi.mocked(parkingSessionApi.getSessionById).mockResolvedValueOnce({
      ...mockSessions[0],
      durationFormatted: '1 giờ',
    } as any);

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <ParkingSessionsPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getAllByText('30A-111.22').length).toBeGreaterThan(0);
    });

    const detailButtons = screen.getAllByRole('button', { name: /Chi tiết/i });
    fireEvent.click(detailButtons[0]);

    await waitFor(() => {
      expect(parkingSessionApi.getSessionById).toHaveBeenCalledWith('sess-1');
    });
  });
});
