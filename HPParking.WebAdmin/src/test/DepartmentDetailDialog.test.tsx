import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { DepartmentDetailDialog } from '@/components/organizations/DepartmentDetailDialog';
import { departmentsApi } from '@/api/masterDataApi';
import { clientApi } from '@/api/clientApi';

vi.mock('@/api/masterDataApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/masterDataApi')>();
  return {
    ...actual,
    departmentsApi: {
      getById: vi.fn(),
    },
  };
});

vi.mock('@/api/clientApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/clientApi')>();
  return {
    ...actual,
    clientApi: {
      getPaged: vi.fn(),
    },
  };
});

describe('DepartmentDetailDialog Component', () => {
  let queryClient: QueryClient;

  const mockDepartment = {
    id: 'dept-101',
    companyId: 'comp-1',
    companyName: 'Tập đoàn Hải Phòng Holdings',
    code: 'TECH_DEPT',
    name: 'Phòng Kỹ Thuật & Vận Hành',
    managerName: 'Trần Văn Trưởng Phòng',
    phoneNumber: '0901234567',
    email: 'tech@hpholdings.vn',
    isActive: true,
    createdAt: '2026-02-01T08:00:00Z',
    updatedAt: '2026-09-01T10:00:00Z',
  };

  const mockMembers = [
    {
      id: 'client-1',
      code: 'EMP_001',
      name: 'Lê Văn Kỹ Sư',
      phoneNumber: '0988111222',
      isActive: true,
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
      },
    });

    vi.mocked(departmentsApi.getById).mockResolvedValue(mockDepartment);
    vi.mocked(clientApi.getPaged).mockResolvedValue({
      items: mockMembers as unknown as import('@/types/client').ClientDto[],
      pagination: {
        pageIndex: 1,
        pageSize: 20,
        totalCount: 1,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    });
  });

  it('hiển thị đầy đủ thông tin phòng ban, công ty chủ quản và người quản lý', async () => {
    render(
      <QueryClientProvider client={queryClient}>
        <DepartmentDetailDialog
          open={true}
          onOpenChange={vi.fn()}
          departmentId="dept-101"
        />
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('Phòng Kỹ Thuật & Vận Hành')).toBeInTheDocument();
      expect(screen.getByText('TECH_DEPT')).toBeInTheDocument();
      expect(screen.getByText('Tập đoàn Hải Phòng Holdings')).toBeInTheDocument();
      expect(screen.getByText('Trần Văn Trưởng Phòng')).toBeInTheDocument();
      expect(screen.getByText('0901234567')).toBeInTheDocument();
      expect(screen.getByText('tech@hpholdings.vn')).toBeInTheDocument();
    });
  });

  it('chuyển tab xem danh sách nhân sự trực thuộc phòng ban', async () => {
    render(
      <QueryClientProvider client={queryClient}>
        <DepartmentDetailDialog
          open={true}
          onOpenChange={vi.fn()}
          departmentId="dept-101"
        />
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('Phòng Kỹ Thuật & Vận Hành')).toBeInTheDocument();
    });

    const membersTab = screen.getByRole('button', { name: /Nhân sự trực thuộc/i });
    fireEvent.click(membersTab);

    await waitFor(() => {
      expect(screen.getByText('Lê Văn Kỹ Sư')).toBeInTheDocument();
      expect(screen.getByText('EMP_001')).toBeInTheDocument();
      expect(screen.getByText(/0988111222/i)).toBeInTheDocument();
    });
  });

  it('gọi callback onEdit khi click nút Chỉnh sửa', async () => {
    const handleEdit = vi.fn();
    const handleOpenChange = vi.fn();

    render(
      <QueryClientProvider client={queryClient}>
        <DepartmentDetailDialog
          open={true}
          onOpenChange={handleOpenChange}
          departmentId="dept-101"
          onEdit={handleEdit}
        />
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('Phòng Kỹ Thuật & Vận Hành')).toBeInTheDocument();
    });

    const editBtn = screen.getByRole('button', { name: /Chỉnh sửa/i });
    fireEvent.click(editBtn);

    expect(handleOpenChange).toHaveBeenCalledWith(false);
    expect(handleEdit).toHaveBeenCalledWith(mockDepartment);
  });
});
