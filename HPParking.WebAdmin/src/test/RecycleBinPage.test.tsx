import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { RecycleBinPage } from '@/pages/RecycleBinPage';
import { useAuthStore } from '@/stores/authStore';
import { UserRole } from '@/types/user';
import { clientApi } from '@/api/clientApi';
import { vehicleApi } from '@/api/vehicleApi';
import { usersApi } from '@/api/userApi';

vi.mock('@/api/clientApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/clientApi')>();
  return {
    ...actual,
    clientApi: {
      getPaged: vi.fn(),
      delete: vi.fn(),
      restore: vi.fn(),
    },
  };
});

vi.mock('@/api/vehicleApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/vehicleApi')>();
  return {
    ...actual,
    vehicleApi: {
      getPaged: vi.fn(),
      delete: vi.fn(),
      restore: vi.fn(),
    },
  };
});

vi.mock('@/api/masterDataApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/masterDataApi')>();
  return {
    ...actual,
    companiesApi: { getPaged: vi.fn(), delete: vi.fn(), restore: vi.fn() },
    departmentsApi: { getPaged: vi.fn(), delete: vi.fn(), restore: vi.fn() },
    contractorsApi: { getPaged: vi.fn(), delete: vi.fn(), restore: vi.fn() },
  };
});

vi.mock('@/api/infrastructureApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/infrastructureApi')>();
  return {
    ...actual,
    gatesApi: { getPaged: vi.fn(), delete: vi.fn(), restore: vi.fn() },
    lanesApi: { getPaged: vi.fn(), delete: vi.fn(), restore: vi.fn() },
    devicesApi: { getPaged: vi.fn(), delete: vi.fn(), restore: vi.fn() },
  };
});

vi.mock('@/api/userApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/userApi')>();
  return {
    ...actual,
    usersApi: { getPaged: vi.fn(), delete: vi.fn(), restore: vi.fn() },
  };
});

describe('RecycleBinPage Component', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    vi.clearAllMocks();
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });
    useAuthStore.setState({
      user: {
        id: 'u-admin',
        username: 'admin',
        fullName: 'Admin User',
        role: UserRole.Admin,
        isActive: true,
      },
      isAuthenticated: true,
      isInitialized: true,
    });
  });

  const renderComponent = () =>
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <RecycleBinPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

  it('hiển thị tiêu đề Thùng Rác Hệ Thống và đầy đủ 8 tab đối tượng lọc', async () => {
    vi.mocked(clientApi.getPaged).mockResolvedValue({
      items: [],
      pagination: {
        pageIndex: 1,
        pageSize: 15,
        totalCount: 0,
        totalPages: 0,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    });

    renderComponent();

    expect(screen.getByText('Thùng Rác Hệ Thống')).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: /Tất cả/i })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: /Khách hàng/i })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: /Phương tiện/i })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: /Công ty/i })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: /Phòng ban/i })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: /Nhà thầu/i })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: /Cổng bãi xe/i })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: /Làn xe/i })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: /Thiết bị/i })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: /Tài khoản/i })).toBeInTheDocument();
  });

  it('ẩn hoàn toàn tab Tài khoản khi người dùng có vai trò Manager', async () => {
    useAuthStore.setState({
      user: {
        id: 'u-mgr',
        username: 'manager',
        fullName: 'Quản Lý Bãi Xe',
        role: UserRole.Manager,
        isActive: true,
      },
      isAuthenticated: true,
      isInitialized: true,
    });

    renderComponent();

    expect(screen.getByRole('tab', { name: /Tất cả/i })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: /Khách hàng/i })).toBeInTheDocument();
    expect(screen.queryByRole('tab', { name: /Tài khoản/i })).not.toBeInTheDocument();
    expect(usersApi.getPaged).not.toHaveBeenCalled();
  });

  it('gọi getPaged với onlyDeleted=true và hiển thị các bản ghi bị xóa kèm nút Khôi phục & Xóa vĩnh viễn', async () => {
    vi.mocked(clientApi.getPaged).mockResolvedValue({
      items: [
        {
          id: 'c-deleted-1',
          code: 'KH_DEL_01',
          name: 'Nguyễn Văn Đã Xóa',
          birthDay: '',
          address: 'Hải Phòng',
          type: 0,
          avatar: '',
          gender: 1,
          phoneNumber: '0988776655',
          isActive: false,
          expired: { enable: false, startDay: '', endDay: '' },
          createdAt: '',
        },
      ],
      pagination: {
        pageIndex: 1,
        pageSize: 15,
        totalCount: 1,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    });

    renderComponent();

    await waitFor(() => {
      expect(clientApi.getPaged).toHaveBeenCalledWith(
        expect.objectContaining({ onlyDeleted: true })
      );
    });

    await waitFor(() => {
      expect(screen.getAllByText('Nguyễn Văn Đã Xóa').length).toBeGreaterThan(0);
      expect(screen.getAllByText('KH_DEL_01').length).toBeGreaterThan(0);
      expect(screen.getAllByText('0988776655').length).toBeGreaterThan(0);
    });

    // Xác nhận có các nút Khôi phục và Xóa vĩnh viễn
    expect(screen.getAllByRole('button', { name: /Khôi phục/i }).length).toBeGreaterThan(0);
    expect(screen.getAllByRole('button', { name: /Xóa vĩnh viễn/i }).length).toBeGreaterThan(0);
  });

  it('cho phép chuyển tab sang Phương tiện và gọi vehicleApi với onlyDeleted=true', async () => {
    vi.mocked(clientApi.getPaged).mockResolvedValue({
      items: [],
      pagination: {
        pageIndex: 1,
        pageSize: 15,
        totalCount: 0,
        totalPages: 0,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    });

    vi.mocked(vehicleApi.getPaged).mockResolvedValue({
      items: [
        {
          id: 'v-del-1',
          plateNumber: '15A-999.99',
          type: 1,
          isActive: false,
          ownerClientId: undefined,
          note: 'Xe đã thanh lý',
          createdAt: '',
        },
      ],
      pagination: {
        pageIndex: 1,
        pageSize: 15,
        totalCount: 1,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    });

    renderComponent();

    const vehicleTab = screen.getByRole('tab', { name: /Phương tiện/i });
    fireEvent.click(vehicleTab);

    await waitFor(() => {
      expect(vehicleApi.getPaged).toHaveBeenCalledWith(
        expect.objectContaining({ onlyDeleted: true })
      );
    });

    await waitFor(() => {
      expect(screen.getAllByText('15A-999.99').length).toBeGreaterThan(0);
    });
  });

  it('hiển thị số lượng bản ghi bị xóa sau label và hiển thị 99+ khi số lượng từ 100 trở lên', async () => {
    vi.mocked(clientApi.getPaged).mockResolvedValue({
      items: [],
      pagination: {
        pageIndex: 1,
        pageSize: 1,
        totalCount: 150,
        totalPages: 150,
        hasPreviousPage: false,
        hasNextPage: true,
      },
    });

    vi.mocked(vehicleApi.getPaged).mockResolvedValue({
      items: [],
      pagination: {
        pageIndex: 1,
        pageSize: 1,
        totalCount: 25,
        totalPages: 25,
        hasPreviousPage: false,
        hasNextPage: true,
      },
    });

    renderComponent();

    await waitFor(() => {
      // clientApi có 150 >= 100 và 'Tất cả' cũng >= 100 -> có nhiều hơn 1 badge '99+'
      expect(screen.getAllByText('99+').length).toBeGreaterThanOrEqual(2);
      // vehicleApi có 25 -> hiển thị '25'
      expect(screen.getByText('25')).toBeInTheDocument();
    });
  });
});
