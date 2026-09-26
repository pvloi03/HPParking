import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ParkingSessionDetailDialog } from '@/components/parkingSessions/ParkingSessionDetailDialog';
import { parkingSessionApi } from '@/api/parkingSessionApi';
import { ParkingSessionStatus } from '@/types/parkingSession';
import { VehicleType } from '@/types/vehicle';

vi.mock('@/api/parkingSessionApi', () => ({
  parkingSessionApi: {
    getSessionById: vi.fn(),
  },
}));

function renderWithClient(ui: React.ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
    },
  });
  return render(
    <QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>
  );
}

describe('ParkingSessionDetailDialog Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  const mockSessionNormal = {
    id: 'session-normal',
    plateNumber: '30A-999.88',
    vehicleType: VehicleType.Car,
    status: ParkingSessionStatus.Completed,
    personFullName: 'Trần Văn B',
    personPhoneNumber: '0988776655',
    inTime: '2026-09-24T08:00:00Z',
    outTime: '2026-09-24T10:30:00Z',
    durationMinutes: 150,
    durationFormatted: '2 giờ 30 phút',
    inLaneName: 'Làn Vào Ô Tô 01',
    outLaneName: 'Làn Ra Ô Tô 01',
    inPlateImagePath: '/images/in_plate.jpg',
    inOverviewImagePath: '/images/in_ov.jpg',
    outPlateImagePath: '/images/out_plate.jpg',
    outOverviewImagePath: '/images/out_ov.jpg',
    createdAt: '2026-09-24T08:00:00Z',
  };

  const mockSessionMismatch = {
    ...mockSessionNormal,
    id: 'session-mismatch',
    plateNumber: '51F-111.22',
    status: ParkingSessionStatus.UnmatchedOut,
  };

  it('hiển thị thông tin phiên đỗ xe, họ tên chủ xe và thời lượng đỗ xe', async () => {
    vi.mocked(parkingSessionApi.getSessionById).mockResolvedValueOnce(mockSessionNormal as any);

    renderWithClient(
      <ParkingSessionDetailDialog
        open={true}
        onOpenChange={vi.fn()}
        sessionId="session-normal"
      />
    );

    await waitFor(() => {
      expect(screen.getAllByText(/30A-999.88/i).length).toBeGreaterThan(0);
      expect(screen.getAllByText(/Trần Văn B/i).length).toBeGreaterThan(0);
      expect(screen.getAllByText(/2 giờ 30 phút/i).length).toBeGreaterThan(0);
    });

    // Phiên bình thường không hiển thị banner cảnh báo đỏ
    expect(screen.queryByText(/CẢNH BÁO AN NINH/i)).not.toBeInTheDocument();
  });

  it('hiển thị CẢNH BÁO AN NINH ĐỎ RỰC khi trạng thái là UnmatchedOut (lệch biển số)', async () => {
    vi.mocked(parkingSessionApi.getSessionById).mockResolvedValueOnce(mockSessionMismatch as any);

    renderWithClient(
      <ParkingSessionDetailDialog
        open={true}
        onOpenChange={vi.fn()}
        sessionId="session-mismatch"
      />
    );

    await waitFor(() => {
      expect(screen.getAllByText(/51F-111.22/i).length).toBeGreaterThan(0);
      expect(screen.getByText(/CẢNH BÁO AN NINH/i)).toBeInTheDocument();
      expect(screen.getByText(/sai lệch biển số/i)).toBeInTheDocument();
    });
  });

  it('hỗ trợ chuyển đổi tab giữa Tổng Quan, Bằng Chứng Ảnh và Thông Số Chi Tiết', async () => {
    vi.mocked(parkingSessionApi.getSessionById).mockResolvedValueOnce(mockSessionNormal as any);

    renderWithClient(
      <ParkingSessionDetailDialog
        open={true}
        onOpenChange={vi.fn()}
        sessionId="session-normal"
      />
    );

    await waitFor(() => {
      expect(screen.getAllByText(/30A-999.88/i).length).toBeGreaterThan(0);
    });

    // Bấm tab Bảng Thông Số Chi Tiết
    const specsTab = screen.getByRole('button', { name: /Bảng Thông Số Chi Tiết/i });
    fireEvent.click(specsTab);

    expect(screen.getByText(/Phương Tiện & Chủ Xe/i)).toBeInTheDocument();
    expect(screen.getByText(/Chi Tiết Lượt Vào/i)).toBeInTheDocument();
    expect(screen.getByText('Ghi Chú')).toBeInTheDocument();
  });

  it('hiển thị nội dung ghi chú khi session có trường note', async () => {
    vi.mocked(parkingSessionApi.getSessionById).mockResolvedValueOnce({
      ...mockSessionNormal,
      note: 'Xe chở sếp đi công tác, gửi qua đêm',
    } as any);

    renderWithClient(
      <ParkingSessionDetailDialog
        open={true}
        onOpenChange={vi.fn()}
        sessionId="session-with-note"
      />
    );

    await waitFor(() => {
      expect(screen.getByText('Ghi Chú')).toBeInTheDocument();
      expect(screen.getByText('Xe chở sếp đi công tác, gửi qua đêm')).toBeInTheDocument();
    });
  });
});
