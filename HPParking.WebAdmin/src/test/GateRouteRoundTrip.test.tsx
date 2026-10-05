import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import React from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { GateRouteFormDialog } from '@/components/routes/GateRouteFormDialog';
import { GateRoutesPage } from '@/pages/GateRoutesPage';
import { gatesApi } from '@/api/infrastructureApi';
import { vehicleApi } from '@/api/vehicleApi';
import { gateRouteApi } from '@/api/gateRouteApi';
import type { GateRouteDto } from '@/types/gateRoute';
import type { GateDto } from '@/types/infrastructure';

vi.mock('@/api/infrastructureApi', () => ({
  gatesApi: {
    getPaged: vi.fn(),
  },
}));

vi.mock('@/api/vehicleApi', () => ({
  vehicleApi: {
    getPaged: vi.fn(),
  },
}));

vi.mock('@/api/gateRouteApi', () => ({
  gateRouteApi: {
    getAll: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    delete: vi.fn(),
  },
}));

const createMockGate = (id: string, name: string, code: string): GateDto => ({
  id,
  name,
  code,
  isActive: true,
  createdAt: '2026-01-01T00:00:00Z',
});

const createMockRoute = (overrides: Partial<GateRouteDto> = {}): GateRouteDto => ({
  id: 'route-mock',
  routeCode: 'ROUTE-MOCK',
  routeName: 'Tuyến Mock',
  description: '',
  gateSteps: [],
  isClosedLoop: true,
  alertEmails: [],
  isActive: true,
  createdAt: '2026-01-01T00:00:00Z',
  ...overrides,
});

function renderWithClient(ui: React.ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
      },
    },
  });
  return render(
    <QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>
  );
}

describe('GateRoute Round-Trip UI & Validation Tests', () => {
  beforeEach(() => {
    vi.clearAllMocks();

    vi.mocked(gatesApi.getPaged).mockResolvedValue({
      items: [
        createMockGate('gate-1', 'Cổng Nhà Máy 1', 'NM01'),
        createMockGate('gate-2', 'Cổng Nhà Máy 2', 'NM02'),
      ],
      pagination: {
        pageIndex: 1,
        pageSize: 200,
        totalCount: 2,
        totalPages: 1,
        hasNextPage: false,
        hasPreviousPage: false,
      },
    });

    vi.mocked(vehicleApi.getPaged).mockResolvedValue({
      items: [],
      pagination: {
        pageIndex: 1,
        pageSize: 20,
        totalCount: 0,
        totalPages: 1,
        hasNextPage: false,
        hasPreviousPage: false,
      },
    });
  });

  it('renders Chặng 1 with Origin & Return label, return travel time, and disabled delete button', async () => {
    renderWithClient(
      <GateRouteFormDialog
        open={true}
        onOpenChange={() => {}}
        onSubmit={vi.fn()}
      />
    );

    // Kiểm tra nhãn Chặng 1
    expect(screen.getByText(/📍 Chặng 1: Điểm Xuất Phát & Quay Về/i)).toBeInTheDocument();
    expect(screen.getAllByText(/Cổng xuất phát & quay về/i).length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText(/⏱️ Thời gian quay về \(phút\)/i)).toBeInTheDocument();
    expect(screen.getByText(/Thời gian xe từ chặng cuối quay về lại cổng này/i)).toBeInTheDocument();

    // Chặng 1 không có ô "Tối đa lưu lại"
    // Chỉ có Chặng 2 trở đi mới có "Tối đa lưu lại"
    expect(screen.getAllByText(/Tối đa lưu lại/i)).toHaveLength(1);

    // Kiểm tra nhãn Chặng 2 (Điểm đến 1)
    expect(screen.getByText(/🏁 Chặng 2: Điểm Đến 1/i)).toBeInTheDocument();
    expect(screen.getByText(/⏱️ Tối đa di chuyển đến cổng này \(phút\)/i)).toBeInTheDocument();
    expect(screen.getByText(/↩ Quay về Cổng xuất phát & quay về \(Chặng 1\)/i)).toBeInTheDocument();

    // Nút xóa: Chặng 1 bị vô hiệu hóa (disabled), Chặng 2 có nút xóa khả dụng
    const disabledDeleteButton = screen.getByTitle(/Không thể xóa chặng xuất phát & quay về/i);
    expect(disabledDeleteButton).toBeDisabled();

    const deleteButtons = screen.queryAllByTitle(/Xóa chặng này/i);
    expect(deleteButtons).toHaveLength(1);
  });

  it('validates minimum 2 steps for fixed routes and prevents submission when deleted down to 1', async () => {
    const handleSubmit = vi.fn();
    renderWithClient(
      <GateRouteFormDialog
        open={true}
        onOpenChange={() => {}}
        onSubmit={handleSubmit}
        initialData={createMockRoute({
          id: 'route-1',
          routeCode: 'ROUTE-01',
          routeName: 'Tuyến 1 Chặng',
          isClosedLoop: true,
          isActive: true,
          gateSteps: [
            { gateId: 'gate-1', stepIndex: 1, maxTravelMinutes: 15, maxStayMinutes: 0 },
            { gateId: 'gate-2', stepIndex: 2, maxTravelMinutes: 20, maxStayMinutes: 30 },
          ],
        })}
      />
    );

    // Xóa Chặng 2 để chỉ còn 1 chặng
    const deleteButtons = screen.queryAllByTitle(/Xóa chặng này/i);
    expect(deleteButtons).toHaveLength(1);
    fireEvent.click(deleteButtons[0]);

    // Click submit
    const submitBtn = screen.getByRole('button', { name: /Cập nhật/i });
    fireEvent.click(submitBtn);

    // Báo lỗi validation tối thiểu 2 chặng
    await waitFor(() => {
      expect(screen.getByText(/Tuyến cố định phải có tối thiểu 2 chặng/i)).toBeInTheDocument();
    });
    expect(handleSubmit).not.toHaveBeenCalled();
  });

  it('validates and prevents submission when consecutive steps have the same gate', async () => {
    const handleSubmit = vi.fn();
    renderWithClient(
      <GateRouteFormDialog
        open={true}
        onOpenChange={() => {}}
        onSubmit={handleSubmit}
        initialData={createMockRoute({
          id: 'route-dup',
          routeCode: 'ROUTE-DUP',
          routeName: 'Tuyến Trùng Cổng',
          isClosedLoop: true,
          isActive: true,
          gateSteps: [
            { gateId: 'gate-1', stepIndex: 1, maxTravelMinutes: 15, maxStayMinutes: 0 },
            { gateId: 'gate-1', stepIndex: 2, maxTravelMinutes: 20, maxStayMinutes: 30 },
          ],
        })}
      />
    );

    const submitBtn = screen.getByRole('button', { name: /Cập nhật/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText(/Hai chặng cổng liền kề không được chọn cùng một cổng kiểm soát/i)).toBeInTheDocument();
    });
    expect(handleSubmit).not.toHaveBeenCalled();
  });

  it('allows adding more destination steps with delete buttons', async () => {
    renderWithClient(
      <GateRouteFormDialog
        open={true}
        onOpenChange={() => {}}
        onSubmit={vi.fn()}
      />
    );

    const addStepBtn = screen.getByRole('button', { name: /Thêm chặng/i });
    fireEvent.click(addStepBtn);

    // Sau khi thêm, có thêm Chặng 3 (Điểm Đến 2)
    await waitFor(() => {
      expect(screen.getByText(/🏁 Chặng 3: Điểm Đến 2/i)).toBeInTheDocument();
    });

    const deleteButtons = screen.queryAllByTitle(/Xóa chặng này/i);
    expect(deleteButtons).toHaveLength(2); // Chặng 2 và Chặng 3
  });

  it('GateRoutesPage displays round-trip summary with Origin & Return and closed-loop return indicator', async () => {
    vi.mocked(gateRouteApi.getAll).mockResolvedValue([
      createMockRoute({
        id: 'route-fixed-2',
        routeCode: 'ROUTE-01',
        routeName: 'Tuyến NM1 - NM2',
        isDefault: false,
        isActive: true,
        gateSteps: [
          {
            gateId: 'gate-1',
            gateName: 'Cổng NM1',
            stepIndex: 1,
            maxTravelMinutes: 15,
            maxStayMinutes: 0,
          },
          {
            gateId: 'gate-2',
            gateName: 'Cổng NM2',
            stepIndex: 2,
            maxTravelMinutes: 20,
            maxStayMinutes: 30,
          },
        ],
      }),
    ]);

    renderWithClient(<GateRoutesPage />);

    await waitFor(() => {
      expect(screen.getByText('Tuyến NM1 - NM2')).toBeInTheDocument();
    });

    // Tiêu đề lộ trình
    expect(screen.getByText(/Lộ trình khứ hồi \(2 chặng\):/i)).toBeInTheDocument();

    // Chặng 1: Xuất phát & Quay về
    expect(screen.getByText(/📍 Xuất phát: Cổng NM1/i)).toBeInTheDocument();
    expect(screen.getByText(/Quay về: 15 phút/i)).toBeInTheDocument();

    // Chặng 2: Điểm đến
    expect(screen.getByText(/🏁 Điểm 1: Cổng NM2/i)).toBeInTheDocument();
    expect(screen.getByText(/SLA: 20p di chuyển/i)).toBeInTheDocument();
    expect(screen.getByText(/30p dừng đỗ/i)).toBeInTheDocument();

    // Mũi tên uốn cong khép kín quay lại Cổng NM1
    expect(screen.getByText(/↩ Về lại Cổng NM1 \(15p\)/i)).toBeInTheDocument();
  });

  it('GateRoutesPage displays 3-step route summary and handles missing gate snapshot gracefully', async () => {
    vi.mocked(gateRouteApi.getAll).mockResolvedValue([
      createMockRoute({
        id: 'route-fixed-3',
        routeCode: 'ROUTE-03',
        routeName: 'Tuyến 3 Nhà Máy',
        isDefault: false,
        isActive: true,
        gateSteps: [
          {
            gateId: 'gate-1',
            gateName: '',
            gateCode: 'NM01',
            stepIndex: 1,
            maxTravelMinutes: 10,
            maxStayMinutes: 0,
          },
          {
            gateId: 'gate-2',
            gateName: 'Cổng Kho',
            stepIndex: 2,
            maxTravelMinutes: 25,
            maxStayMinutes: 35,
          },
          {
            gateId: '',
            gateName: '',
            gateCode: '',
            stepIndex: 3,
            maxTravelMinutes: 30,
            maxStayMinutes: 40,
          },
        ],
      }),
    ]);

    renderWithClient(<GateRoutesPage />);

    await waitFor(() => {
      expect(screen.getByText('Tuyến 3 Nhà Máy')).toBeInTheDocument();
    });

    expect(screen.getByText(/Lộ trình khứ hồi \(3 chặng\):/i)).toBeInTheDocument();
    // Chặng 1 uses gateCode fallback
    expect(screen.getByText(/📍 Xuất phát: Cổng NM01/i)).toBeInTheDocument();
    // Chặng 2 uses gateName
    expect(screen.getByText(/🏁 Điểm 1: Cổng Kho/i)).toBeInTheDocument();
    // Chặng 3 handles undefined gateId safely
    expect(screen.getByText(/🏁 Điểm 2: Cổng không xác định/i)).toBeInTheDocument();
    // Closed-loop return text uses fallback
    expect(screen.getByText(/↩ Về lại Cổng NM01 \(10p\)/i)).toBeInTheDocument();
  });
});
