import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ContractorDetailDialog } from '@/components/organizations/ContractorDetailDialog';
import { contractorsApi } from '@/api/masterDataApi';
import { clientApi } from '@/api/clientApi';

vi.mock('@/api/masterDataApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/masterDataApi')>();
  return {
    ...actual,
    contractorsApi: {
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

describe('ContractorDetailDialog Component', () => {
  let queryClient: QueryClient;

  const mockContractor = {
    id: 'contractor-101',
    code: 'NHA_THAU_XD_1',
    name: 'Công Ty Cổ Phần Xây Dựng Số 1 Hải Phòng',
    contactPerson: 'Nguyễn Văn Đốc Công',
    phoneNumber: '0977888999',
    email: 'xaydung1@hpcont.vn',
    isActive: true,
    createdAt: '2026-03-01T08:00:00Z',
    updatedAt: '2026-09-01T10:00:00Z',
  };

  const mockWorkers = [
    {
      id: 'worker-1',
      code: 'CN_001',
      name: 'Vũ Văn Thợ',
      phoneNumber: '0912999888',
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

    vi.mocked(contractorsApi.getById).mockResolvedValue(mockContractor);
    vi.mocked(clientApi.getPaged).mockResolvedValue({
      items: mockWorkers as unknown as import('@/types/client').ClientDto[],
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

  it('hiển thị thông tin nhà thầu, người liên hệ đại diện và trạng thái', async () => {
    render(
      <QueryClientProvider client={queryClient}>
        <ContractorDetailDialog
          open={true}
          onOpenChange={vi.fn()}
          contractorId="contractor-101"
        />
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('Công Ty Cổ Phần Xây Dựng Số 1 Hải Phòng')).toBeInTheDocument();
      expect(screen.getByText('NHA_THAU_XD_1')).toBeInTheDocument();
      expect(screen.getByText('Nguyễn Văn Đốc Công')).toBeInTheDocument();
      expect(screen.getByText('0977888999')).toBeInTheDocument();
      expect(screen.getByText('xaydung1@hpcont.vn')).toBeInTheDocument();
      expect(screen.getAllByText('Đã kích hoạt').length).toBeGreaterThan(0);
    });
  });

  it('chuyển tab xem danh sách nhân sự nhà thầu', async () => {
    render(
      <QueryClientProvider client={queryClient}>
        <ContractorDetailDialog
          open={true}
          onOpenChange={vi.fn()}
          contractorId="contractor-101"
        />
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('Công Ty Cổ Phần Xây Dựng Số 1 Hải Phòng')).toBeInTheDocument();
    });

    const workersTab = screen.getByRole('button', { name: /Nhân sự nhà thầu/i });
    fireEvent.click(workersTab);

    await waitFor(() => {
      expect(screen.getByText('Vũ Văn Thợ')).toBeInTheDocument();
      expect(screen.getByText('CN_001')).toBeInTheDocument();
      expect(screen.getByText(/0912999888/i)).toBeInTheDocument();
    });
  });

  it('gọi callback onEdit khi click nút Chỉnh sửa trong dialog', async () => {
    const handleEdit = vi.fn();
    const handleOpenChange = vi.fn();

    render(
      <QueryClientProvider client={queryClient}>
        <ContractorDetailDialog
          open={true}
          onOpenChange={handleOpenChange}
          contractorId="contractor-101"
          onEdit={handleEdit}
        />
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('Công Ty Cổ Phần Xây Dựng Số 1 Hải Phòng')).toBeInTheDocument();
    });

    const editBtn = screen.getByRole('button', { name: /Chỉnh sửa/i });
    fireEvent.click(editBtn);

    expect(handleOpenChange).toHaveBeenCalledWith(false);
    expect(handleEdit).toHaveBeenCalledWith(mockContractor);
  });
});
