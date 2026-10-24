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

  it('yêu cầu gán Thẻ từ khi phương thức xác thực không phải None và chưa có thẻ', async () => {
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

    // Chờ thông báo lỗi yêu cầu Thẻ từ hiển thị
    await waitFor(() => {
      expect(
        screen.getByText(/Vui lòng gán thẻ định danh RFID cho nhân sự khi kích hoạt phương thức xác thực/i)
      ).toBeInTheDocument();
    });
  });

  it('bắt buộc thêm xe khi verifyVehiclePlate = true và không có phương tiện nào', async () => {
    renderDialog({
      initialData: {
        id: 'client-1',
        code: 'NV-001',
        name: 'Nguyễn Văn Test',
        phoneNumber: '0912345678',
        cardCode: '0000123456',
        authMethods: ['Card'],
        verifyVehiclePlate: true,
        isActive: true,
      } as any,
    });

    // Submit form (lúc này verifyVehiclePlate là true và chưa có xe nào)
    const submitBtn = screen.getByRole('button', { name: /Cập nhật/i });
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

  it('không xóa dữ liệu người dùng đã nhập khi danh sách departments được tải xong', async () => {
    const { rerender } = renderDialog({
      departments: [],
    });

    const nameInput = screen.getByPlaceholderText('VD: Nguyễn Văn Nam');
    fireEvent.change(nameInput, { target: { value: 'Nguyễn Văn Test' } });
    expect(nameInput).toHaveValue('Nguyễn Văn Test');

    const loadedDepts = [
      {
        id: 'dept-hr-1',
        name: 'Phòng Nhân Sự',
        email: 'hr@example.com',
        isActive: true,
      },
    ];

    rerender(
      <QueryClientProvider client={queryClient}>
        <ClientFormDialog
          open={true}
          onOpenChange={vi.fn()}
          companies={[]}
          departments={loadedDepts as any}
          contractors={[]}
          onSubmit={vi.fn()}
        />
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(nameInput).toHaveValue('Nguyễn Văn Test');
    });
  });

  it('tự động commit approvingDepartmentId mặc định vào form state mà không cần click chọn thủ công', async () => {
    const mockSubmit = vi.fn();
    const depts = [
      {
        id: 'dept-hr-default',
        name: 'Phòng Tổ chức Nhân sự',
        email: 'hr@example.com',
        isActive: true,
      },
    ];

    renderDialog({
      departments: depts as any,
      onSubmit: mockSubmit,
    });

    const codeInput = screen.getByPlaceholderText('VD: NV-001 hoặc 001200012345');
    const nameInput = screen.getByPlaceholderText('VD: Nguyễn Văn Nam');
    const phoneInput = screen.getByPlaceholderText('VD: 0987654321');

    fireEvent.change(codeInput, { target: { value: '001200999999' } });
    fireEvent.change(nameInput, { target: { value: 'Trần Văn Auto' } });
    fireEvent.change(phoneInput, { target: { value: '0987654321' } });

    // Chọn Làn tự do (None) để không yêu cầu Thẻ từ và tự động tắt/ẩn đối soát xe
    const noneCheckbox = screen.getByRole('checkbox', { name: /Làn tự do \(None\)/i });
    fireEvent.click(noneCheckbox);

    const submitBtn = screen.getByRole('button', { name: /Thêm mới/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(mockSubmit).toHaveBeenCalledTimes(1);
      const submittedData = mockSubmit.mock.calls[0][0];
      expect(submittedData.approvingDepartmentId).toBe('dept-hr-default');
    });
  });

  it('khi chọn làn tự do (None) thì xóa dữ liệu thẻ và không cho phép gán Thẻ từ', async () => {
    const mockSubmit = vi.fn();
    renderDialog({
      initialData: {
        id: 'client-1',
        code: 'NV-001',
        name: 'Nguyễn Văn A',
        phoneNumber: '0987654321',
        cardCode: '0000123456',
        authMethods: ['FaceId', 'Card'],
        verifyVehiclePlate: false,
        isActive: true,
      } as any,
      onSubmit: mockSubmit,
    });

    // Ban đầu có mã thẻ
    expect(screen.getByText(/Mã thẻ: 0000123456/i)).toBeInTheDocument();

    // Chọn Làn tự do (None)
    const noneCheckbox = screen.getByRole('checkbox', { name: /Làn tự do \(None\)/i });
    fireEvent.click(noneCheckbox);

    // Badge hiển thị mã thẻ phải biến mất
    await waitFor(() => {
      expect(screen.queryByText(/Mã thẻ: 0000123456/i)).not.toBeInTheDocument();
    });

    // Thông báo Làn tự do được hiển thị: ảnh lưu trong hệ thống nhưng không đồng bộ FaceID
    expect(
      screen.getByText(/Ảnh chân dung sẽ được lưu làm ảnh đại diện hồ sơ trong hệ thống \(không đồng bộ lên thiết bị FaceID\)/i)
    ).toBeInTheDocument();

    // Khối cờ bắt buộc đối soát biển số xe phải bị ẩn khi chọn Làn tự do (None)
    expect(
      screen.queryByRole('checkbox', { name: /Bắt buộc xác thực đối chiếu biển số xe/i })
    ).not.toBeInTheDocument();

    // Submit form
    const submitBtn = screen.getByRole('button', { name: /Cập nhật/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(mockSubmit).toHaveBeenCalledTimes(1);
      const payload = mockSubmit.mock.calls[0][0];
      expect(payload.authMethods).toEqual(['None']);
      expect(payload.cardCode).toBeFalsy();
      expect(payload.verifyVehiclePlate).toBe(false);
    });
  });

  it('khi chọn phương thức khuôn mặt thì tick luôn cả Thẻ từ và hiển thị chú thích', async () => {
    renderDialog({
      initialData: {
        id: 'client-2',
        code: 'NV-002',
        name: 'Trần Thị B',
        phoneNumber: '0912345678',
        cardCode: '0000123456',
        authMethods: ['Card'], // Chỉ chọn Card ban đầu
        verifyVehiclePlate: false,
        isActive: true,
      } as any,
    });

    const cardCheckbox = screen.getByRole('checkbox', { name: /Thẻ từ/i });
    const faceIdCheckbox = screen.getByRole('checkbox', { name: /Khuôn mặt FaceID/i });

    expect(cardCheckbox).toBeChecked();
    expect(faceIdCheckbox).not.toBeChecked();

    // Click chọn Khuôn mặt FaceID
    fireEvent.click(faceIdCheckbox);

    // Cả hai checkbox đều phải được tick
    expect(faceIdCheckbox).toBeChecked();
    expect(cardCheckbox).toBeChecked();

    // Hiển thị chú thích cần mã thẻ để định danh cho dữ liệu khuôn mặt
    expect(screen.getByText(/Chú thích FaceID:/i)).toBeInTheDocument();
    expect(
      screen.getAllByText(/Cần mã thẻ để định danh cho dữ liệu khuôn mặt/i).length
    ).toBeGreaterThan(0);
  });

  it('hiển thị 2 checkbox tự động mở barrier làn Vào và Ra khi không phải None và submit đúng giá trị', async () => {
    const mockSubmit = vi.fn();
    renderDialog({
      initialData: {
        id: 'client-barrier-1',
        code: 'NV-BARRIER',
        name: 'Trần Văn Barrier',
        phoneNumber: '0912345999',
        cardCode: '0000123456',
        authMethods: ['Card'],
        verifyVehiclePlate: false,
        autoOpenBarrier: { onEntry: true, onExit: true },
        isActive: true,
      } as any,
      onSubmit: mockSubmit,
    });

    const entryBarrierCheckbox = screen.getByRole('checkbox', { name: /Tự động mở barrier làn VÀO/i });
    const exitBarrierCheckbox = screen.getByRole('checkbox', { name: /Tự động mở barrier làn RA/i });

    expect(entryBarrierCheckbox).toBeInTheDocument();
    expect(exitBarrierCheckbox).toBeInTheDocument();
    expect(entryBarrierCheckbox).toBeChecked();
    expect(exitBarrierCheckbox).toBeChecked();

    // Bỏ tick làn VÀO
    fireEvent.click(entryBarrierCheckbox);
    expect(entryBarrierCheckbox).not.toBeChecked();
    expect(exitBarrierCheckbox).toBeChecked();

    const submitBtn = screen.getByRole('button', { name: /Cập nhật/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(mockSubmit).toHaveBeenCalledTimes(1);
      const payload = mockSubmit.mock.calls[0][0];
      expect(payload.autoOpenBarrier).toEqual({ onEntry: false, onExit: true });
    });
  });

  it('ẩn 2 checkbox tự động mở barrier khi chọn làn tự do (None) và ép autoOpenBarrier thành true/true khi submit', async () => {
    const mockSubmit = vi.fn();
    renderDialog({
      initialData: {
        id: 'client-barrier-2',
        code: 'NV-BARRIER-2',
        name: 'Lê Văn Tự Do',
        phoneNumber: '0912345888',
        cardCode: '0000123456',
        authMethods: ['Card'],
        verifyVehiclePlate: false,
        autoOpenBarrier: { onEntry: false, onExit: false },
        isActive: true,
      } as any,
      onSubmit: mockSubmit,
    });

    // Chọn Làn tự do (None)
    const noneCheckbox = screen.getByRole('checkbox', { name: /Làn tự do \(None\)/i });
    fireEvent.click(noneCheckbox);

    // 2 checkbox Barrier phải bị ẩn
    expect(screen.queryByRole('checkbox', { name: /Tự động mở barrier làn VÀO/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('checkbox', { name: /Tự động mở barrier làn RA/i })).not.toBeInTheDocument();

    const submitBtn = screen.getByRole('button', { name: /Cập nhật/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(mockSubmit).toHaveBeenCalledTimes(1);
      const payload = mockSubmit.mock.calls[0][0];
      expect(payload.authMethods).toEqual(['None']);
      expect(payload.autoOpenBarrier).toEqual({ onEntry: true, onExit: true });
    });
  });
});
