import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { GatesPage } from '@/pages/GatesPage';
import { gatesApi } from '@/api/infrastructureApi';
import { companiesApi } from '@/api/masterDataApi';

vi.mock('@/api/infrastructureApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/infrastructureApi')>();
  return {
    ...actual,
    gatesApi: {
      getPaged: vi.fn(),
      getById: vi.fn(),
      create: vi.fn(),
      update: vi.fn(),
      delete: vi.fn(),
      restore: vi.fn(),
    },
  };
});

vi.mock('@/api/masterDataApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/masterDataApi')>();
  return {
    ...actual,
    companiesApi: {
      getPaged: vi.fn(),
    },
  };
});

describe('GatesPage Component', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    vi.clearAllMocks();
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });
  });

  it('hiển thị danh sách cổng kiểm soát từ API', async () => {
    vi.mocked(companiesApi.getPaged).mockResolvedValueOnce({
      items: [
        {
          id: 'c1',
          code: 'HP_CORP',
          name: 'Hoàng Phát',
          isActive: true,
          createdAt: '',
        },
      ],
      pagination: {
        pageIndex: 1,
        pageSize: 100,
        totalCount: 1,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    });

    vi.mocked(gatesApi.getPaged).mockResolvedValueOnce({
      items: [
        {
          id: 'gate-1',
          code: 'GATE_CHINH_01',
          name: 'Cổng Chính Số 1',
          companyName: 'Hoàng Phát',
          machineCode: 'BOT_BV_01',
          isActive: true,
          createdAt: new Date().toISOString(),
        },
      ],
      pagination: {
        pageIndex: 1,
        pageSize: 10,
        totalCount: 1,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    });

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <GatesPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    expect(screen.getByText(/Quản Lý Cổng Kiểm Soát/i)).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getAllByText('GATE_CHINH_01').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Cổng Chính Số 1').length).toBeGreaterThan(0);
      expect(screen.getAllByText('BOT_BV_01').length).toBeGreaterThan(0);
    });
  });

  it('mở modal GateDetailDialog khi click nút Xem chi tiết của một cổng kiểm soát', async () => {
    const mockGate = {
      id: 'gate-2',
      code: 'GATE_PHU_02',
      name: 'Cổng Phụ Số 2',
      companyName: 'Hoàng Phát',
      machineCode: 'BOT_BV_02',
      isActive: true,
      createdAt: new Date().toISOString(),
    };

    vi.mocked(companiesApi.getPaged).mockResolvedValue({
      items: [{ id: 'c1', code: 'HP_CORP', name: 'Hoàng Phát', isActive: true, createdAt: '' }],
      pagination: { pageIndex: 1, pageSize: 100, totalCount: 1, totalPages: 1, hasPreviousPage: false, hasNextPage: false },
    });

    vi.mocked(gatesApi.getPaged).mockResolvedValue({
      items: [mockGate],
      pagination: {
        pageIndex: 1,
        pageSize: 10,
        totalCount: 1,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    });

    vi.mocked(gatesApi.getById).mockResolvedValue(mockGate);

    const { fireEvent } = await import('@testing-library/react');

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <GatesPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getAllByText('GATE_PHU_02').length).toBeGreaterThan(0);
    });

    const viewButtons = screen.getAllByRole('button', { name: /Xem chi tiết/i });
    expect(viewButtons.length).toBeGreaterThan(0);
    fireEvent.click(viewButtons[0]);

    await waitFor(() => {
      expect(gatesApi.getById).toHaveBeenCalledWith('gate-2');
    });
  });
});
