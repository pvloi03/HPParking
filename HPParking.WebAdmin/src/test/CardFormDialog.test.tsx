import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { CardFormDialog } from '@/components/cards/CardFormDialog';
import { CardTargetType, CardStatus, type CardDto } from '@/types/card';

vi.mock('@/api/clientApi', () => ({
  clientApi: {
    getPaged: vi.fn().mockResolvedValue({
      items: [
        { id: 'client-1', name: 'Nguyễn Văn A', code: 'NV-001', phoneNumber: '0987654321', isActive: true },
        { id: 'client-2', name: 'Trần Thị B', code: 'NV-002', phoneNumber: '0912345678', isActive: true },
      ],
      pagination: { totalCount: 2, pageIndex: 1, pageSize: 20, totalPages: 1 },
    }),
    getById: vi.fn().mockImplementation((id: string) => {
      if (id === 'client-1') {
        return Promise.resolve({ id: 'client-1', name: 'Nguyễn Văn A', code: 'NV-001', phoneNumber: '0987654321' });
      }
      return Promise.resolve({ id: 'client-2', name: 'Trần Thị B', code: 'NV-002', phoneNumber: '0912345678' });
    }),
  },
}));

vi.mock('@/api/vehicleApi', () => ({
  vehicleApi: {
    getPaged: vi.fn().mockResolvedValue({ items: [], pagination: { totalCount: 0 } }),
    getById: vi.fn(),
  },
}));

vi.mock('@/hooks/use-toast', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
    warning: vi.fn(),
    info: vi.fn(),
  },
}));

describe('CardFormDialog Component', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });
    vi.clearAllMocks();
  });

  const renderDialog = (props: { initialData?: CardDto | null; onSubmit?: any }) => {
    return render(
      <QueryClientProvider client={queryClient}>
        <CardFormDialog
          open={true}
          onOpenChange={vi.fn()}
          onSubmit={props.onSubmit || vi.fn()}
          initialData={props.initialData}
        />
      </QueryClientProvider>
    );
  };

  it('không hiển thị cảnh báo chuyển đổi thẻ khi tạo thẻ mới', () => {
    renderDialog({ initialData: null });

    expect(screen.getByText(/Thêm Thẻ Định Danh Mới/i)).toBeInTheDocument();
    expect(screen.queryByText(/Thẻ đang được gán cho nhân sự này/i)).not.toBeInTheDocument();
  });

  it('hiển thị cảnh báo và hướng dẫn gỡ thẻ về kho khi sửa thẻ đang có chủ', async () => {
    const assignedCard: CardDto = {
      id: 'card-1',
      cardNumber: '0000012345',
      targetType: CardTargetType.Person,
      clientId: 'client-1',
      clientName: 'Nguyễn Văn A',
      status: CardStatus.InUse,
      createdAt: '2026-03-01T00:00:00Z',
    };

    renderDialog({ initialData: assignedCard });

    await waitFor(() => {
      expect(screen.getByText(/Cập Nhật Thông Tin Thẻ/i)).toBeInTheDocument();
      expect(screen.getByText(/Thẻ đang được gán cho nhân sự này/i)).toBeInTheDocument();
      expect(screen.getByText(/Hệ thống không cho phép chuyển thẻ trực tiếp giữa 2 nhân sự/i)).toBeInTheDocument();
    });
  });

  it('cho phép submit bình thường khi giữ nguyên chủ thẻ', async () => {
    const onSubmit = vi.fn();
    const assignedCard: CardDto = {
      id: 'card-1',
      cardNumber: '0000012345',
      targetType: CardTargetType.Person,
      clientId: 'client-1',
      clientName: 'Nguyễn Văn A',
      status: CardStatus.InUse,
      createdAt: '2026-03-01T00:00:00Z',
    };

    renderDialog({ initialData: assignedCard, onSubmit });

    await waitFor(() => {
      expect(screen.getByText(/Lưu thay đổi/i)).toBeInTheDocument();
    });

    const submitBtn = screen.getByRole('button', { name: /Lưu thay đổi/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalledWith(
        expect.objectContaining({
          targetType: CardTargetType.Person,
          clientId: 'client-1',
          status: CardStatus.InUse,
        })
      );
    });
  });

  it('khi thẻ đang gán cho nhân sự thì dropdown Trạng thái không có lựa chọn "Trong kho (Chưa gán)"', async () => {
    const assignedCard: CardDto = {
      id: 'card-1',
      cardNumber: '0000012345',
      targetType: CardTargetType.Person,
      clientId: 'client-1',
      clientName: 'Nguyễn Văn A',
      status: CardStatus.InUse,
      createdAt: '2026-03-01T00:00:00Z',
    };

    renderDialog({ initialData: assignedCard });

    await waitFor(() => {
      expect(screen.getByText(/Cập Nhật Thông Tin Thẻ/i)).toBeInTheDocument();
    });

    // Mở dropdown Trạng thái (combobox thứ 2 sau Loại đối tượng)
    const comboboxes = screen.getAllByRole('combobox');
    const statusSelect = comboboxes[1];
    fireEvent.click(statusSelect);

    await waitFor(() => {
      // Có Đang sử dụng, Tạm khóa, Báo mất
      expect(screen.getByRole('option', { name: /Đang sử dụng/i })).toBeInTheDocument();
      expect(screen.getByRole('option', { name: /Tạm khóa/i })).toBeInTheDocument();
      expect(screen.getByRole('option', { name: /Báo mất/i })).toBeInTheDocument();
      // TUYỆT ĐỐI KHÔNG có Trong kho (Chưa gán) khi thẻ đang có người dùng
      expect(screen.queryByRole('option', { name: /Trong kho \(Chưa gán\)/i })).not.toBeInTheDocument();
    });
  });

  it('khi thẻ chưa gán (trong kho) thì dropdown Trạng thái không có lựa chọn "Đang sử dụng"', async () => {
    renderDialog({ initialData: null });

    await waitFor(() => {
      expect(screen.getByText(/Thêm Thẻ Định Danh Mới/i)).toBeInTheDocument();
    });

    // Mở dropdown Trạng thái (combobox thứ 2 sau Loại đối tượng)
    const comboboxes = screen.getAllByRole('combobox');
    const statusSelect = comboboxes[1];
    fireEvent.click(statusSelect);

    await waitFor(() => {
      // Có Trong kho (Chưa gán), Tạm khóa, Báo mất
      expect(screen.getByRole('option', { name: /Trong kho \(Chưa gán\)/i })).toBeInTheDocument();
      expect(screen.getByRole('option', { name: /Tạm khóa/i })).toBeInTheDocument();
      expect(screen.getByRole('option', { name: /Báo mất/i })).toBeInTheDocument();
      // TUYỆT ĐỐI KHÔNG có Đang sử dụng khi thẻ chưa gán cho ai
      expect(screen.queryByRole('option', { name: /Đang sử dụng/i })).not.toBeInTheDocument();
    });
  });
});
