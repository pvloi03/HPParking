import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { CompanyDetailDialog } from '@/components/organizations/CompanyDetailDialog';
import { companiesApi, departmentsApi } from '@/api/masterDataApi';
import { gatesApi } from '@/api/infrastructureApi';

vi.mock('@/api/masterDataApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/masterDataApi')>();
  return {
    ...actual,
    companiesApi: {
      getById: vi.fn(),
    },
    departmentsApi: {
      getPaged: vi.fn(),
    },
  };
});

vi.mock('@/api/infrastructureApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/infrastructureApi')>();
  return {
    ...actual,
    gatesApi: {
      getPaged: vi.fn(),
    },
  };
});

describe('CompanyDetailDialog Component', () => {
  let queryClient: QueryClient;

  const mockCompany = {
    id: 'comp-101',
    code: 'HP_CORP',
    name: 'Tập đoàn Hải Phòng Holdings',
    phoneNumber: '02253888999',
    email: 'contact@hpholdings.vn',
    isActive: true,
    createdAt: '2026-01-01T08:00:00Z',
    updatedAt: '2026-09-01T10:00:00Z',
  };

  const mockDepartments = [
    {
      id: 'dept-1',
      code: 'HR_DEPT',
      name: 'Phòng Hành Chính Nhân Sự',
      managerName: 'Nguyễn Văn Quản Lý',
      isActive: true,
      createdAt: '2026-01-01T08:00:00Z',
    },
  ];

  const mockGates = [
    {
      id: 'gate-1',
      code: 'GATE_NORTH',
      name: 'Cổng Bắc Số 1',
      machineCode: 'GATE_01_PC',
      isActive: true,
      createdAt: '2026-01-01T08:00:00Z',
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
      },
    });

    vi.mocked(companiesApi.getById).mockResolvedValue(mockCompany);
    vi.mocked(departmentsApi.getPaged).mockResolvedValue({
      items: mockDepartments,
      pagination: {
        pageIndex: 1,
        pageSize: 50,
        totalCount: 1,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    });
    vi.mocked(gatesApi.getPaged).mockResolvedValue({
      items: mockGates,
      pagination: {
        pageIndex: 1,
        pageSize: 50,
        totalCount: 1,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    });
  });

  it('hiển thị đầy đủ thông tin chi tiết của công ty khi mở dialog', async () => {
    render(
      <QueryClientProvider client={queryClient}>
        <CompanyDetailDialog
          open={true}
          onOpenChange={vi.fn()}
          companyId="comp-101"
        />
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('Tập đoàn Hải Phòng Holdings')).toBeInTheDocument();
      expect(screen.getByText('HP_CORP')).toBeInTheDocument();
      expect(screen.getByText('02253888999')).toBeInTheDocument();
      expect(screen.getByText('contact@hpholdings.vn')).toBeInTheDocument();
      expect(screen.getAllByText('Đã kích hoạt').length).toBeGreaterThan(0);
    });
  });

  it('chuyển tab xem danh sách phòng ban và cổng trực thuộc', async () => {
    render(
      <QueryClientProvider client={queryClient}>
        <CompanyDetailDialog
          open={true}
          onOpenChange={vi.fn()}
          companyId="comp-101"
        />
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('Tập đoàn Hải Phòng Holdings')).toBeInTheDocument();
    });

    // Chuyển sang tab Phòng ban
    const deptsTab = screen.getByRole('button', { name: /Phòng ban/i });
    fireEvent.click(deptsTab);

    await waitFor(() => {
      expect(screen.getByText('Phòng Hành Chính Nhân Sự')).toBeInTheDocument();
      expect(screen.getByText('HR_DEPT')).toBeInTheDocument();
    });

    // Chuyển sang tab Cổng kiểm soát
    const gatesTab = screen.getByRole('button', { name: /Cổng kiểm soát/i });
    fireEvent.click(gatesTab);

    await waitFor(() => {
      expect(screen.getByText('Cổng Bắc Số 1')).toBeInTheDocument();
      expect(screen.getByText('GATE_NORTH')).toBeInTheDocument();
      expect(screen.getByText(/Máy trạm: GATE_01_PC/i)).toBeInTheDocument();
    });
  });

  it('gọi callback onEdit khi click nút Chỉnh sửa trong dialog', async () => {
    const handleEdit = vi.fn();
    const handleOpenChange = vi.fn();

    render(
      <QueryClientProvider client={queryClient}>
        <CompanyDetailDialog
          open={true}
          onOpenChange={handleOpenChange}
          companyId="comp-101"
          onEdit={handleEdit}
        />
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('Tập đoàn Hải Phòng Holdings')).toBeInTheDocument();
    });

    const editBtn = screen.getByRole('button', { name: /Chỉnh sửa/i });
    fireEvent.click(editBtn);

    expect(handleOpenChange).toHaveBeenCalledWith(false);
    expect(handleEdit).toHaveBeenCalledWith(mockCompany);
  });
});
