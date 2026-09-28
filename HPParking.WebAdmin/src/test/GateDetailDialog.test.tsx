import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { GateDetailDialog } from '@/components/infrastructure/GateDetailDialog';
import { gatesApi, lanesApi } from '@/api/infrastructureApi';
import { LaneDirection } from '@/types/infrastructure';

vi.mock('@/api/infrastructureApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/infrastructureApi')>();
  return {
    ...actual,
    gatesApi: {
      getById: vi.fn(),
    },
    lanesApi: {
      getPaged: vi.fn(),
    },
  };
});

describe('GateDetailDialog Component', () => {
  let queryClient: QueryClient;

  const mockGate = {
    id: 'gate-101',
    code: 'CONG_NAM_01',
    name: 'Cổng Nam Xe Hơi',
    companyId: 'comp-1',
    companyName: 'Tập đoàn Hải Phòng Holdings',
    machineCode: 'GATE_STATION_SOUTH_01',
    isActive: true,
    createdAt: '2026-04-01T08:00:00Z',
    updatedAt: '2026-09-01T10:00:00Z',
  };

  const mockLanes = [
    {
      id: 'lane-1',
      code: 'LAN_VAO_01',
      name: 'Làn Vào Xe Ô Tô 1',
      direction: LaneDirection.In,
      outputRelay: 1,
      inputReader: 2,
      isActive: true,
      createdAt: '2026-04-01T08:00:00Z',
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
      },
    });

    vi.mocked(gatesApi.getById).mockResolvedValue(mockGate);
    vi.mocked(lanesApi.getPaged).mockResolvedValue({
      items: mockLanes,
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

  it('hiển thị thông tin cổng kiểm soát, mã máy trạm và công ty quản lý', async () => {
    render(
      <QueryClientProvider client={queryClient}>
        <GateDetailDialog
          open={true}
          onOpenChange={vi.fn()}
          gateId="gate-101"
        />
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('Cổng Nam Xe Hơi')).toBeInTheDocument();
      expect(screen.getByText('CONG_NAM_01')).toBeInTheDocument();
      expect(screen.getByText('GATE_STATION_SOUTH_01')).toBeInTheDocument();
      expect(screen.getByText('Tập đoàn Hải Phòng Holdings')).toBeInTheDocument();
      expect(screen.getAllByText('Đã kích hoạt').length).toBeGreaterThan(0);
    });
  });

  it('chuyển tab xem danh sách các làn xe thuộc cổng', async () => {
    render(
      <QueryClientProvider client={queryClient}>
        <GateDetailDialog
          open={true}
          onOpenChange={vi.fn()}
          gateId="gate-101"
        />
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('Cổng Nam Xe Hơi')).toBeInTheDocument();
    });

    const lanesTab = screen.getByRole('button', { name: /Danh sách làn xe/i });
    fireEvent.click(lanesTab);

    await waitFor(() => {
      expect(screen.getByText('Làn Vào Xe Ô Tô 1')).toBeInTheDocument();
      expect(screen.getByText('LAN_VAO_01')).toBeInTheDocument();
      expect(screen.getByText(/Relay: 1 \| Reader: 2/i)).toBeInTheDocument();
    });
  });

  it('gọi callback onEdit khi click nút Chỉnh sửa trong dialog', async () => {
    const handleEdit = vi.fn();
    const handleOpenChange = vi.fn();

    render(
      <QueryClientProvider client={queryClient}>
        <GateDetailDialog
          open={true}
          onOpenChange={handleOpenChange}
          gateId="gate-101"
          onEdit={handleEdit}
        />
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('Cổng Nam Xe Hơi')).toBeInTheDocument();
    });

    const editBtn = screen.getByRole('button', { name: /Chỉnh sửa/i });
    fireEvent.click(editBtn);

    expect(handleOpenChange).toHaveBeenCalledWith(false);
    expect(handleEdit).toHaveBeenCalledWith(mockGate);
  });
});
