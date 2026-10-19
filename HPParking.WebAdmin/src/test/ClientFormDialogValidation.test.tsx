import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ClientFormDialog } from '@/components/clients/ClientFormDialog';
import { cardApi } from '@/api/cardApi';

vi.mock('@/api/cardApi', () => ({
  cardApi: {
    getCards: vi.fn().mockResolvedValue({
      items: [
        {
          id: 'card-1',
          cardNumber: '0000123456',
          targetType: 1,
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

vi.mock('@/api/vehicleApi', () => ({
  vehicleApi: {
    getByClientId: vi.fn().mockResolvedValue([]),
  },
}));

describe('ClientFormDialog Validation & Unassigned Filter Tests', () => {
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
        <ClientFormDialog
          open={true}
          onOpenChange={vi.fn()}
          companies={[]}
          departments={[]}
          contractors={[]}
          onSubmit={vi.fn()}
          {...props}
        />
      </QueryClientProvider>
    );
  };

  it('yêu cầu gán thẻ RFID khi phương thức xác thực không phải None và chưa có thẻ', async () => {
    renderDialog();

    // Điền các trường cơ bản bắt buộc
    const codeInput = screen.getByPlaceholderText('VD: NV-001 hoặc 001200012345');
    fireEvent.change(codeInput, { target: { value: '001200012345' } });

    const nameInput = screen.getByPlaceholderText('VD: Nguyễn Văn Nam');
    fireEvent.change(nameInput, { target: { value: 'Nguyễn Văn Test' } });

    const phoneInput = screen.getByPlaceholderText('VD: 0987654321');
    fireEvent.change(phoneInput, { target: { value: '0912345678' } });

    // Submit form
    const submitBtn = screen.getByRole('button', { name: /Thêm mới/i });
    fireEvent.click(submitBtn);

    // Chờ thông báo lỗi yêu cầu thẻ RFID hiển thị
    await waitFor(() => {
      expect(
        screen.getByText(/Vui lòng gán thẻ định danh RFID cho nhân sự khi kích hoạt phương thức xác thực/i)
      ).toBeInTheDocument();
    });
  });

  it('bắt buộc thêm xe khi verifyVehiclePlate = true và không có phương tiện nào', async () => {
    renderDialog();

    // Điền các trường cơ bản
    const codeInput = screen.getByPlaceholderText('VD: NV-001 hoặc 001200012345');
    fireEvent.change(codeInput, { target: { value: '001200012345' } });

    const nameInput = screen.getByPlaceholderText('VD: Nguyễn Văn Nam');
    fireEvent.change(nameInput, { target: { value: 'Nguyễn Văn Test' } });

    const phoneInput = screen.getByPlaceholderText('VD: 0987654321');
    fireEvent.change(phoneInput, { target: { value: '0912345678' } });

    // Chuyển phương thức xác thực sang None để không bị chặn bởi lỗi thẻ
    const noneCheckbox = screen.getByRole('checkbox', { name: /Làn tự do \(None\)/i });
    fireEvent.click(noneCheckbox);

    // Submit form (lúc này verifyVehiclePlate mặc định là true và chưa có xe nào)
    const submitBtn = screen.getByRole('button', { name: /Thêm mới/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(
        screen.getByText(/Vui lòng thêm ít nhất một phương tiện đăng ký khi kích hoạt xác thực đối chiếu xe/i)
      ).toBeInTheDocument();
    });
  });

  it('truyền tham số unassignedOnly: true khi fetch thẻ cho nhân sự', async () => {
    renderDialog();

    await waitFor(() => {
      expect(cardApi.getCards).toHaveBeenCalledWith(
        expect.objectContaining({
          targetType: 1, // Person
          unassignedOnly: true,
        })
      );
    });
  });

  it('báo lỗi khi mã định danh chứa khoảng trắng hoặc ký tự đặc biệt', async () => {
    renderDialog();

    const codeInput = screen.getByPlaceholderText('VD: NV-001 hoặc 001200012345');
    fireEvent.change(codeInput, { target: { value: 'NV@001' } });

    const submitBtn = screen.getByRole('button', { name: /Thêm mới/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(
        screen.getByText(/Mã định danh chỉ chứa chữ cái, chữ số, gạch dưới \(_\) hoặc gạch ngang \(-\)/i)
      ).toBeInTheDocument();
    });
  });

  it('chấp nhận mã định danh hợp lệ chứa chữ, số, dấu gạch nối và gạch dưới (NV-001, EMP_2026)', async () => {
    renderDialog();

    const codeInput = screen.getByPlaceholderText('VD: NV-001 hoặc 001200012345');
    fireEvent.change(codeInput, { target: { value: 'NV-001' } });

    const submitBtn = screen.getByRole('button', { name: /Thêm mới/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(
        screen.queryByText(/Mã định danh chỉ chứa chữ cái, chữ số, gạch dưới \(_\) hoặc gạch ngang \(-\)/i)
      ).not.toBeInTheDocument();
      expect(screen.queryByText(/Mã định danh không được để trống/i)).not.toBeInTheDocument();
    });
  });

  it('mặc định không giới hạn thời gian (expiredEnable = false) và hiển thị thông báo ra vào tự do', async () => {
    renderDialog();

    expect(
      screen.getByText(/Không giới hạn thời gian \(Nhân sự được ra vào tự do, không áp dụng thời hạn hết hạn\)/i)
    ).toBeInTheDocument();
  });

  it('báo lỗi khi bật thời hạn ra vào nhưng ngày kết thúc nhỏ hơn ngày bắt đầu', async () => {
    renderDialog();

    // Điền các trường cơ bản bắt buộc
    const codeInput = screen.getByPlaceholderText('VD: NV-001 hoặc 001200012345');
    fireEvent.change(codeInput, { target: { value: '001200012345' } });

    const nameInput = screen.getByPlaceholderText('VD: Nguyễn Văn Nam');
    fireEvent.change(nameInput, { target: { value: 'Nguyễn Văn Test' } });

    const phoneInput = screen.getByPlaceholderText('VD: 0987654321');
    fireEvent.change(phoneInput, { target: { value: '0912345678' } });

    // Chọn Làn tự do (None) để không bị chặn bởi thẻ
    const noneCheckbox = screen.getByRole('checkbox', { name: /Làn tự do \(None\)/i });
    fireEvent.click(noneCheckbox);

    // Tắt kiểm tra biển số xe nếu đang bật
    const verifyPlateSwitch = screen.queryByRole('switch', { name: /Bắt buộc đối chiếu biển số/i });
    if (verifyPlateSwitch) {
      fireEvent.click(verifyPlateSwitch);
    }

    // Bật công tắc áp dụng thời hạn
    const expiredSwitch = screen.getByRole('switch', { name: /Áp dụng thời hạn ra vào/i });
    fireEvent.click(expiredSwitch);

    // Chờ 2 input ngày xuất hiện
    await waitFor(() => {
      expect(screen.getByText(/Từ ngày \(StartDay\)/i)).toBeInTheDocument();
      expect(screen.getByText(/Đến ngày \(EndDay\)/i)).toBeInTheDocument();
    });

    const inputs = screen.getAllByDisplayValue(/\d{4}-\d{2}-\d{2}/);
    // Điền ngày kết thúc nhỏ hơn ngày bắt đầu
    const startInput = inputs[1]; // input[0] là ngày sinh (birthDay)
    const endInput = inputs[2];

    fireEvent.change(startInput, { target: { value: '2026-12-31' } });
    fireEvent.change(endInput, { target: { value: '2026-01-01' } });

    const submitBtn = screen.getByRole('button', { name: /Thêm mới/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(
        screen.getByText(/Ngày bắt đầu không được lớn hơn ngày hết hạn/i)
      ).toBeInTheDocument();
    });
  });
});
