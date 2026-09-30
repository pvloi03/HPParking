import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { CardsPage } from '@/pages/CardsPage';
import { cardApi } from '@/api/cardApi';
import { clientApi } from '@/api/clientApi';
import { vehicleApi } from '@/api/vehicleApi';
import { CardTargetType, CardStatus } from '@/types/card';
import { VehicleType } from '@/types/vehicle';

vi.mock('@/api/cardApi', () => ({
  cardApi: {
    getCards: vi.fn(),
    create: vi.fn(),
    delete: vi.fn(),
  },
}));

vi.mock('@/api/clientApi', () => ({
  clientApi: {
    getPaged: vi.fn(),
  },
}));

vi.mock('@/api/vehicleApi', () => ({
  vehicleApi: {
    getPaged: vi.fn(),
  },
}));

vi.mock('@/hooks/usePermissions', () => ({
  usePermissions: () => ({
    canWrite: true,
    canDelete: true,
    role: 'Admin',
  }),
}));

describe('CardsPage Component', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });
    vi.clearAllMocks();

    vi.mocked(cardApi.getCards).mockResolvedValue({
      items: [
        {
          id: 'card-1',
          cardNumber: '0000012345',
          targetType: CardTargetType.Person,
          clientId: 'client-1',
          clientName: 'Nguyễn Văn A',
          status: CardStatus.InUse,
          createdAt: '2026-03-01T00:00:00Z',
        },
        {
          id: 'card-2',
          cardNumber: '0000067890',
          targetType: CardTargetType.Vehicle,
          vehicleId: 'veh-1',
          plateNumber: '30A-999.99',
          status: CardStatus.InUse,
          createdAt: '2026-03-02T00:00:00Z',
        },
      ],
      pagination: {
        pageIndex: 1,
        pageSize: 15,
        totalCount: 2,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    });

    vi.mocked(clientApi.getPaged).mockResolvedValue({
      items: [
        {
          id: 'client-1',
          code: '001200000001',
          name: 'Nguyễn Văn A',
          phoneNumber: '0987654321',
          type: 0,
          gender: 1,
          birthDay: '1990-01-01',
          isActive: true,
          createdAt: '2026-01-01T00:00:00Z',
        },
      ] as any,
      pagination: {
        pageIndex: 1,
        pageSize: 500,
        totalCount: 1,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    });

    vi.mocked(vehicleApi.getPaged).mockResolvedValue({
      items: [
        {
          id: 'veh-1',
          plateNumber: '30A-999.99',
          type: VehicleType.Car,
          isShared: true,
          isActive: true,
          createdAt: '2026-01-01T00:00:00Z',
        },
      ],
      pagination: {
        pageIndex: 1,
        pageSize: 500,
        totalCount: 1,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    });
  });

  const renderComponent = () =>
    render(
      <QueryClientProvider client={queryClient}>
        <CardsPage />
      </QueryClientProvider>
    );

  it('hiển thị tiêu đề và danh sách thẻ định danh từ API', async () => {
    renderComponent();

    expect(screen.getByText(/Quản Lý Thẻ Định Danh/i)).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getAllByText('0000012345').length).toBeGreaterThan(0);
      expect(screen.getAllByText('0000067890').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Nguyễn Văn A').length).toBeGreaterThan(0);
      expect(screen.getAllByText('30A-999.99').length).toBeGreaterThan(0);
    });
  });

  it('mở modal Thêm thẻ mới và hiển thị form đầy đủ với tùy chọn gán đối tượng', async () => {
    renderComponent();

    const addButtons = screen.getAllByRole('button', { name: /Thêm thẻ mới/i });
    fireEvent.click(addButtons[0]);

    await waitFor(() => {
      expect(screen.getByText(/Thêm Thẻ Định Danh Mới/i)).toBeInTheDocument();
      expect(screen.getByPlaceholderText(/12345 hoặc 0000012345/i)).toBeInTheDocument();
      expect(screen.getByText(/Gán cho Nhân sự \(Khách hàng\)/i)).toBeInTheDocument();
    });
  });
});
