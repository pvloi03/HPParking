import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { TripDetailDialog } from '@/components/fleet/TripDetailDialog';
import { TripStatus } from '@/types/fleetDispatch';

vi.mock('@/api/fleetDispatchApi', () => ({
  fleetDispatchApi: {
    getById: vi.fn(),
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

describe('TripDetailDialog Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  const mockTrip = {
    id: 'trip-123',
    plateNumber: '29B-123.45',
    vehicleId: 'veh-1',
    cardNumber: 'CARD001',
    originGateId: 'gate-1',
    originGateName: 'Cổng 1 (Nhà Máy A)',
    currentStepIndex: 2,
    status: TripStatus.InTransit,
    startTime: '2026-09-30T08:00:00Z',
    remainingSeconds: 600,
    isOverdue: false,
    isAlertSent: false,
    createdAt: '2026-09-30T08:00:00Z',
    checkpoints: [
      {
        stepIndex: 1,
        gateId: 'gate-1',
        gateName: 'Cổng 1 (Nhà Máy A)',
        direction: 'Out',
        timestamp: '2026-09-30T08:00:00Z',
        overviewImagePath: '/images/cp1_overview.jpg',
        plateImagePath: '/images/cp1_plate.jpg',
        plateDetected: '29B-123.45',
        isRouteCompliant: true,
        note: 'Quẹt ra cổng',
        slaOverdue: { isOverdue: false, overdueSeconds: 0 },
      },
      {
        stepIndex: 2,
        gateId: 'gate-2',
        gateName: 'Cổng 2 (Nhà Máy B)',
        direction: 'In',
        timestamp: '2026-09-30T08:20:00Z',
        overviewImagePath: '',
        plateImagePath: '',
        plateDetected: '29B-123.45',
        isRouteCompliant: true,
        note: 'Không có ảnh',
        slaOverdue: { isOverdue: false, overdueSeconds: 0 },
      },
    ],
  };

  it('hiển thị nút Xem ảnh kèm badge 2 ảnh cho chặng có đủ 2 ảnh và --- cho chặng không có ảnh', () => {
    renderWithClient(
      <TripDetailDialog
        open={true}
        onOpenChange={vi.fn()}
        tripId="trip-123"
        initialTrip={mockTrip as any}
      />
    );

    expect(screen.getByText('Chi Tiết Hành Trình Phương Tiện Nội Bộ')).toBeInTheDocument();
    expect(screen.getAllByText('29B-123.45').length).toBeGreaterThanOrEqual(1);

    // Checkpoint 1 có 2 ảnh -> nút Xem ảnh có badge 2
    const viewButtons = screen.getAllByRole('button', { name: /Xem ảnh/i });
    expect(viewButtons.length).toBe(1);
    expect(screen.getByText('2')).toBeInTheDocument();

    // Checkpoint 2 không có ảnh -> có '---'
    const dashes = screen.getAllByText('---');
    expect(dashes.length).toBeGreaterThan(0);
  });

  it('mở modal xem ảnh bằng chứng song song 2 ảnh khi click nút Xem ảnh', () => {
    renderWithClient(
      <TripDetailDialog
        open={true}
        onOpenChange={vi.fn()}
        tripId="trip-123"
        initialTrip={mockTrip as any}
      />
    );

    const viewButton = screen.getByRole('button', { name: /Xem ảnh/i });
    fireEvent.click(viewButton);

    // Modal tiêu đề
    expect(screen.getByText(/Ảnh Bằng Chứng Mốc Kiểm Soát #1/i)).toBeInTheDocument();
    // Tiêu đề 2 khung ảnh
    expect(screen.getByText('Ảnh Toàn Cảnh / Cabin Xe')).toBeInTheDocument();
    expect(screen.getByText('Ảnh Nhận Dạng Biển Số (LPR)')).toBeInTheDocument();

    // Cả 2 ảnh đều có badge "Có ảnh"
    const badges = screen.getAllByText('Có ảnh');
    expect(badges.length).toBe(2);

    // Nút đóng
    const closeBtn = screen.getByRole('button', { name: 'Đóng xem ảnh' });
    fireEvent.click(closeBtn);
    expect(screen.queryByText(/Ảnh Bằng Chứng Mốc Kiểm Soát #1/i)).not.toBeInTheDocument();
  });
});
