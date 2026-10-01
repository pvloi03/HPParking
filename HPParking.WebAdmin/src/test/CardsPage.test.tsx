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
    getById: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    delete: vi.fn(),
  },
  extractErrorMessage: vi.fn((err: any) => err?.message || 'Lỗi'),
}));

vi.mock('@/api/clientApi', () => ({
  clientApi: {
    getPaged: vi.fn(),
    getById: vi.fn(),
  },
}));

vi.mock('@/api/vehicleApi', () => ({
  vehicleApi: {
    getPaged: vi.fn(),
    getById: vi.fn(),
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
          note: 'Thẻ nhân viên cấp 1',
          createdAt: '2026-03-01T00:00:00Z',
        },
        {
          id: 'card-2',
          cardNumber: '0000067890',
          targetType: CardTargetType.Vehicle,
          vehicleId: 'veh-1',
          plateNumber: '30A-999.99',
          status: CardStatus.InUse,
          note: 'Thẻ xe cứu thương',
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

    vi.mocked(cardApi.getById).mockImplementation(async (id: string) => {
      if (id === 'card-1') {
        return {
          id: 'card-1',
          cardNumber: '0000012345',
          targetType: CardTargetType.Person,
          clientId: 'client-1',
          clientName: 'Nguyễn Văn A',
          status: CardStatus.InUse,
          note: 'Thẻ nhân viên cấp 1',
          createdAt: '2026-03-01T00:00:00Z',
        };
      }
      return {
        id: 'card-2',
        cardNumber: '0000067890',
        targetType: CardTargetType.Vehicle,
        vehicleId: 'veh-1',
        plateNumber: '30A-999.99',
        status: CardStatus.InUse,
        note: 'Thẻ xe cứu thương',
        createdAt: '2026-03-02T00:00:00Z',
      };
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

    vi.mocked(clientApi.getById).mockResolvedValue({
      id: 'client-1',
      code: '001200000001',
      name: 'Nguyễn Văn A',
      phoneNumber: '0987654321',
      type: 0,
      gender: 1,
      birthDay: '1990-01-01',
      isActive: true,
      createdAt: '2026-01-01T00:00:00Z',
    } as any);

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

    vi.mocked(vehicleApi.getById).mockResolvedValue({
      id: 'veh-1',
      plateNumber: '30A-999.99',
      type: VehicleType.Car,
      isShared: true,
      isActive: true,
      createdAt: '2026-01-01T00:00:00Z',
    } as any);

    vi.mocked(cardApi.create).mockResolvedValue({
      id: 'card-new',
      cardNumber: '0000099999',
      targetType: CardTargetType.Person,
      status: CardStatus.Available,
      createdAt: '2026-03-03T00:00:00Z',
    });

    vi.mocked(cardApi.update).mockResolvedValue({
      id: 'card-1',
      cardNumber: '0000012345',
      targetType: CardTargetType.Person,
      status: CardStatus.Locked,
      createdAt: '2026-03-01T00:00:00Z',
    });
  });

  const renderComponent = () =>
    render(
      <QueryClientProvider client={queryClient}>
        <CardsPage />
      </QueryClientProvider>
    );

  it('hiển thị tiêu đề và danh sách thẻ định danh từ API mà không có cột Ghi Chú', async () => {
    renderComponent();

    expect(screen.getByText(/Quản Lý Thẻ Định Danh/i)).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getAllByText('0000012345').length).toBeGreaterThan(0);
      expect(screen.getAllByText('0000067890').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Nguyễn Văn A').length).toBeGreaterThan(0);
      expect(screen.getAllByText('30A-999.99').length).toBeGreaterThan(0);
    });

    // Cột Ghi Chú đã được loại bỏ khỏi header bảng
    expect(screen.queryByRole('columnheader', { name: /^Ghi Chú$/i })).not.toBeInTheDocument();
  });

  it('chỉ có duy nhất 1 nút Thêm thẻ mới trên giao diện (trong toolbar của DataTable)', async () => {
    renderComponent();

    const addButtons = screen.getAllByRole('button', { name: /Thêm thẻ mới/i });
    expect(addButtons.length).toBe(1);

    fireEvent.click(addButtons[0]);

    await waitFor(() => {
      expect(screen.getByText(/Thêm Thẻ Định Danh Mới/i)).toBeInTheDocument();
      const input = screen.getByPlaceholderText(/12345 hoặc 0000012345/i);
      expect(input).toBeInTheDocument();
      expect(input).not.toBeDisabled();
    });
  });

  it('mở modal Xem chi tiết khi bấm vào icon xem chi tiết', async () => {
    renderComponent();

    await waitFor(() => {
      expect(screen.getAllByText('0000012345').length).toBeGreaterThan(0);
    });

    const viewButtons = screen.getAllByRole('button', { name: /Xem chi tiết/i });
    expect(viewButtons.length).toBeGreaterThan(0);
    fireEvent.click(viewButtons[0]);

    await waitFor(() => {
      expect(screen.getByText(/Hồ Sơ Thẻ Định Danh/i)).toBeInTheDocument();
      expect(cardApi.getById).toHaveBeenCalledWith('card-1');
      expect(screen.getByText(/Nhân sự nội bộ \(Gán người\)/i)).toBeInTheDocument();
    });
  });

  it('mở modal Chỉnh sửa khi bấm vào nút Chỉnh sửa với mã thẻ bị disabled và submit update', async () => {
    renderComponent();

    await waitFor(() => {
      expect(screen.getAllByText('0000012345').length).toBeGreaterThan(0);
    });

    const editButtons = screen.getAllByRole('button', { name: /Chỉnh sửa/i });
    expect(editButtons.length).toBeGreaterThan(0);
    fireEvent.click(editButtons[0]);

    await waitFor(() => {
      expect(screen.getByText(/Cập Nhật Thông Tin Thẻ/i)).toBeInTheDocument();
      const cardInput = screen.getByPlaceholderText(/12345 hoặc 0000012345/i);
      expect(cardInput).toBeDisabled();
      expect(screen.getByText(/Mã chip RFID cố định/i)).toBeInTheDocument();
    });

    const saveButton = screen.getByRole('button', { name: /Lưu thay đổi/i });
    fireEvent.click(saveButton);

    await waitFor(() => {
      expect(cardApi.update).toHaveBeenCalledWith(
        'card-1',
        expect.objectContaining({
          targetType: CardTargetType.Person,
        })
      );
    });
  });
});
