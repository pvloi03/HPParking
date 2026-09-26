import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ClientDetailDialog } from '@/components/clients/ClientDetailDialog';
import { clientApi } from '@/api/clientApi';
import { ClientType, type ClientDetailDto } from '@/types/client';
import { VehicleType } from '@/types/vehicle';

vi.mock('@/api/clientApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/clientApi')>();
  return {
    ...actual,
    clientApi: {
      getById: vi.fn(),
      syncFaceId: vi.fn(),
    },
  };
});

describe('ClientDetailDialog Component', () => {
  let queryClient: QueryClient;

  const mockClientDetail: ClientDetailDto = {
    id: 'cli-test-01',
    code: '001099012345',
    name: 'Nguyễn Văn An',
    phoneNumber: '0912345678',
    email: 'an.nguyen@example.com',
    address: '123 Đường Lạch Tray, Ngô Quyền, Hải Phòng',
    birthDay: '1995-05-15T00:00:00Z',
    gender: 1,
    type: ClientType.Employee,
    avatar: 'http://example.com/avatar.jpg',
    companyId: 'comp-1',
    departmentId: 'dept-1',
    contractorId: undefined,
    isActive: true,
    note: 'Cán bộ kỹ thuật cao cấp',
    expired: {
      enable: true,
      startDay: '',
      endDay: '',
    },
    createdAt: '2026-01-01T08:00:00Z',
    updatedAt: '2026-02-01T10:00:00Z',
    vehicles: [
      {
        id: 'veh-1',
        ownerClientId: 'cli-test-01',
        plateNumber: '15A-999.88',
        type: VehicleType.Car,
        isActive: true,
        note: 'Xe cá nhân',
        createdAt: '2026-01-02T08:00:00Z',
      },
    ],
    faceIdTerminals: [
      {
        deviceName: 'FaceID Cổng Chính Làn Vào',
        deviceIp: '192.168.1.101',
        isOnline: true,
        userExists: true,
        hasFace: true,
        cardCount: 1,
        cards: ['CARD-01'],
        timestamp: '2026-01-01T08:00:00Z',
      },
    ],
  };

  const mockCompanies = [{ id: 'comp-1', code: 'CP1', name: 'Công ty Cổ phần Hải Phòng', isActive: true, createdAt: '' }];
  const mockDepartments = [{ id: 'dept-1', companyId: 'comp-1', code: 'DP1', name: 'Phòng Công nghệ Thông tin', isActive: true, createdAt: '' }];
  const mockContractors = [{ id: 'cont-1', code: 'CT1', name: 'Nhà thầu Xây Dựng Số 1', isActive: true, createdAt: '' }];

  beforeEach(() => {
    vi.clearAllMocks();
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });
  });

  it('hiển thị đầy đủ thông tin cá nhân của khách hàng khi mở dialog', async () => {
    vi.mocked(clientApi.getById).mockResolvedValueOnce(mockClientDetail);

    render(
      <QueryClientProvider client={queryClient}>
        <ClientDetailDialog
          open={true}
          onOpenChange={vi.fn()}
          clientId="cli-test-01"
          companies={mockCompanies}
          departments={mockDepartments}
          contractors={mockContractors}
        />
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('Nguyễn Văn An')).toBeInTheDocument();
      expect(screen.getAllByText(/001099012345/).length).toBeGreaterThan(0);
      expect(screen.getAllByText('0912345678').length).toBeGreaterThan(0);
      expect(screen.getByText('an.nguyen@example.com')).toBeInTheDocument();
      expect(screen.getByText('Công ty Cổ phần Hải Phòng')).toBeInTheDocument();
      expect(screen.getByText('Phòng Công nghệ Thông tin')).toBeInTheDocument();
    });
  });

  it('chuyển đổi tab Phương tiện và hiển thị danh sách xe đăng ký', async () => {
    vi.mocked(clientApi.getById).mockResolvedValueOnce(mockClientDetail);

    render(
      <QueryClientProvider client={queryClient}>
        <ClientDetailDialog
          open={true}
          onOpenChange={vi.fn()}
          clientId="cli-test-01"
          companies={mockCompanies}
          departments={mockDepartments}
          contractors={mockContractors}
        />
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('Nguyễn Văn An')).toBeInTheDocument();
    });

    const vehicleTabButton = screen.getByRole('button', { name: /phương tiện/i });
    fireEvent.click(vehicleTabButton);

    await waitFor(() => {
      expect(screen.getByText('15A-999.88')).toBeInTheDocument();
      expect(screen.getByText('Ô tô')).toBeInTheDocument();
      expect(screen.getByText('Xe cá nhân')).toBeInTheDocument();
    });
  });

  it('chuyển đổi tab Thiết bị FaceID và hiển thị danh sách thiết bị', async () => {
    vi.mocked(clientApi.getById).mockResolvedValueOnce(mockClientDetail);

    render(
      <QueryClientProvider client={queryClient}>
        <ClientDetailDialog
          open={true}
          onOpenChange={vi.fn()}
          clientId="cli-test-01"
          companies={mockCompanies}
          departments={mockDepartments}
          contractors={mockContractors}
        />
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('Nguyễn Văn An')).toBeInTheDocument();
    });

    const faceIdTabButton = screen.getByRole('button', { name: /thiết bị faceid/i });
    fireEvent.click(faceIdTabButton);

    await waitFor(() => {
      expect(screen.getByText('FaceID Cổng Chính Làn Vào')).toBeInTheDocument();
      expect(screen.getByText('IP: 192.168.1.101')).toBeInTheDocument();
      expect(screen.getByText('Trực tuyến')).toBeInTheDocument();
    });
  });

  it('kích hoạt nút chỉnh sửa thì gọi callback onEdit và đóng dialog', async () => {
    vi.mocked(clientApi.getById).mockResolvedValueOnce(mockClientDetail);
    const mockOnEdit = vi.fn();
    const mockOnOpenChange = vi.fn();

    render(
      <QueryClientProvider client={queryClient}>
        <ClientDetailDialog
          open={true}
          onOpenChange={mockOnOpenChange}
          clientId="cli-test-01"
          companies={mockCompanies}
          departments={mockDepartments}
          contractors={mockContractors}
          onEdit={mockOnEdit}
        />
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('Nguyễn Văn An')).toBeInTheDocument();
    });

    const editBtn = screen.getByRole('button', { name: /chỉnh sửa/i });
    fireEvent.click(editBtn);

    expect(mockOnOpenChange).toHaveBeenCalledWith(false);
    expect(mockOnEdit).toHaveBeenCalledWith(expect.objectContaining({
      id: 'cli-test-01',
      name: 'Nguyễn Văn An',
    }));
  });
});
