import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { CardDetailDialog } from '@/components/cards/CardDetailDialog';
import { cardApi } from '@/api/cardApi';
import { clientApi } from '@/api/clientApi';
import { vehicleApi } from '@/api/vehicleApi';
import { CardTargetType, CardStatus, type CardDto } from '@/types/card';
import { VehicleType } from '@/types/vehicle';

vi.mock('@/api/cardApi', () => ({
  cardApi: {
    getById: vi.fn(),
  },
}));

vi.mock('@/api/clientApi', () => ({
  clientApi: {
    getById: vi.fn(),
  },
}));

vi.mock('@/api/vehicleApi', () => ({
  vehicleApi: {
    getById: vi.fn(),
  },
}));

vi.mock('@/hooks/usePermissions', () => ({
  usePermissions: () => ({
    canWrite: true,
  }),
}));

describe('CardDetailDialog Component', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });
    vi.clearAllMocks();
  });

  const renderComponent = (props: {
    open: boolean;
    cardId: string | null;
    onOpenChange?: (open: boolean) => void;
    onEdit?: (card: CardDto) => void;
  }) =>
    render(
      <QueryClientProvider client={queryClient}>
        <CardDetailDialog
          open={props.open}
          cardId={props.cardId}
          onOpenChange={props.onOpenChange || vi.fn()}
          onEdit={props.onEdit}
        />
      </QueryClientProvider>
    );

  it('hiển thị đầy đủ thông tin thẻ nhân sự và thông tin người sở hữu', async () => {
    const mockCard: CardDto = {
      id: 'card-1',
      cardNumber: '0000012345',
      targetType: CardTargetType.Person,
      clientId: 'client-1',
      clientName: 'Trần Văn B',
      status: CardStatus.InUse,
      note: 'Thẻ cấp ngày 01/03',
      createdAt: '2026-03-01T10:00:00Z',
      updatedAt: '2026-03-02T15:30:00Z',
    };

    vi.mocked(cardApi.getById).mockResolvedValue(mockCard);
    vi.mocked(clientApi.getById).mockResolvedValue({
      id: 'client-1',
      code: 'EMP-001',
      name: 'Trần Văn B',
      phoneNumber: '0912345678',
      type: 0,
      gender: 1,
      birthDay: '1992-05-10',
      isActive: true,
      createdAt: '2026-01-01T00:00:00Z',
    } as any);

    renderComponent({ open: true, cardId: 'card-1' });

    await waitFor(() => {
      expect(screen.getByText('0000012345')).toBeInTheDocument();
      expect(screen.getByText(/Hồ Sơ Thẻ Định Danh/i)).toBeInTheDocument();
      expect(screen.getByText('Đang sử dụng')).toBeInTheDocument();
      expect(screen.getByText('Trần Văn B')).toBeInTheDocument();
      expect(screen.getByText('EMP-001')).toBeInTheDocument();
      expect(screen.getByText('0912345678')).toBeInTheDocument();
      expect(screen.getByText('Thẻ cấp ngày 01/03')).toBeInTheDocument();
    });
  });

  it('hiển thị đầy đủ thông tin thẻ phương tiện nội bộ', async () => {
    const mockCard: CardDto = {
      id: 'card-2',
      cardNumber: '0000088888',
      targetType: CardTargetType.Vehicle,
      vehicleId: 'veh-1',
      plateNumber: '29A-123.45',
      status: CardStatus.InUse,
      note: 'Xe chở khách nội bộ',
      createdAt: '2026-03-01T10:00:00Z',
    };

    vi.mocked(cardApi.getById).mockResolvedValue(mockCard);
    vi.mocked(vehicleApi.getById).mockResolvedValue({
      id: 'veh-1',
      plateNumber: '29A-123.45',
      type: VehicleType.Car,
      isShared: true,
      isActive: true,
      createdAt: '2026-01-01T00:00:00Z',
    } as any);

    renderComponent({ open: true, cardId: 'card-2' });

    await waitFor(() => {
      expect(screen.getByText('0000088888')).toBeInTheDocument();
      expect(screen.getByText(/Phương tiện nội bộ \(Gán xe\)/i)).toBeInTheDocument();
      expect(screen.getByText('29A-123.45')).toBeInTheDocument();
      expect(screen.getByText('Ô tô')).toBeInTheDocument();
      expect(screen.getByText('Xe dùng chung / Công vụ')).toBeInTheDocument();
    });
  });

  it('gọi callback onEdit khi người dùng bấm Chỉnh sửa thẻ', async () => {
    const mockCard: CardDto = {
      id: 'card-1',
      cardNumber: '0000012345',
      targetType: CardTargetType.Person,
      status: CardStatus.Available,
      createdAt: '2026-03-01T10:00:00Z',
    };

    vi.mocked(cardApi.getById).mockResolvedValue(mockCard);
    const onEditMock = vi.fn();
    const onOpenChangeMock = vi.fn();

    renderComponent({
      open: true,
      cardId: 'card-1',
      onEdit: onEditMock,
      onOpenChange: onOpenChangeMock,
    });

    await waitFor(() => {
      expect(screen.getByText('0000012345')).toBeInTheDocument();
    });

    const editButton = screen.getByRole('button', { name: /Chỉnh sửa thẻ/i });
    fireEvent.click(editButton);

    expect(onOpenChangeMock).toHaveBeenCalledWith(false);
    expect(onEditMock).toHaveBeenCalledWith(mockCard);
  });
});
