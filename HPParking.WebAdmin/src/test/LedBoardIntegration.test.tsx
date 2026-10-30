import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { DeviceFormDialog } from '@/components/infrastructure/DeviceFormDialog';
import { LaneDetailDialog } from '@/components/infrastructure/LaneDetailDialog';
import { LaneFormDialog } from '@/components/infrastructure/LaneFormDialog';
import { lanesApi } from '@/api/infrastructureApi';
import { DeviceType, LaneDirection } from '@/types/infrastructure';

vi.mock('@/api/infrastructureApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/infrastructureApi')>();
  return {
    ...actual,
    lanesApi: {
      getDetail: vi.fn(),
      getPaged: vi.fn(),
      create: vi.fn(),
      update: vi.fn(),
    },
    gatesApi: {
      getPaged: vi.fn(),
      getById: vi.fn(),
    },
    devicesApi: {
      getPaged: vi.fn(),
      getById: vi.fn(),
    },
  };
});

describe('LedBoard WebAdmin UI Integration', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    vi.clearAllMocks();
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });
  });

  it('DeviceFormDialog renders LedBoard option without hardcoded port auto-fill', async () => {
    const handleSubmit = vi.fn();
    render(
      <DeviceFormDialog
        open={true}
        onOpenChange={vi.fn()}
        onSubmit={handleSubmit}
      />
    );

    // Open select dropdown for DeviceType
    const selectTrigger = screen.getByRole('combobox');
    fireEvent.click(selectTrigger);

    // Assert "Bảng LED (TCP)" is available in options
    const ledOption = await screen.findByRole('option', { name: /Bảng LED \(TCP\)/i });
    expect(ledOption).toBeInTheDocument();

    // Select "Bảng LED (TCP)"
    fireEvent.click(ledOption);

    // Assert port input is not force auto-filled with hardcoded 10000
    const portInput = screen.getByPlaceholderText('80') as HTMLInputElement;
    expect(portInput.value).not.toBe('10000');
  });

  it('LaneDetailDialog displays LedBoard device card when present', async () => {
    vi.mocked(lanesApi.getDetail).mockResolvedValueOnce({
      id: 'lane-101',
      code: 'LANE_OUT_01',
      name: 'Làn Ra Xe Con 1',
      direction: LaneDirection.Out,
      outputRelay: 1,
      inputReader: 1,
      isActive: true,
      createdAt: '2026-10-30T00:00:00Z',
      ledBoard: {
        id: 'led-1',
        code: 'LED_OUT_01',
        name: 'Bảng LED P10 Làn Ra',
        type: DeviceType.LedBoard,
        ipAddress: '192.168.1.250',
        port: 10000,
        isActive: true,
      },
    });

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <LaneDetailDialog open={true} onOpenChange={vi.fn()} laneId="lane-101" />
        </MemoryRouter>
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('Bảng LED P10 Làn Ra')).toBeInTheDocument();
      expect(screen.getByText('192.168.1.250:10000')).toBeInTheDocument();
      expect(screen.getByText('Bảng LED Hiển Thị (TCP)')).toBeInTheDocument();
    });
  });

  it('LaneFormDialog renders LedBoard select field and options', async () => {
    const devices = [
      {
        id: 'led-1',
        code: 'LED_P10_01',
        name: 'LED P10 Đơn Sắc Đỏ',
        type: DeviceType.LedBoard,
        ipAddress: '192.168.1.250',
        port: 10000,
        isActive: true,
        createdAt: '2026-10-30T00:00:00Z',
      },
    ];

    render(
      <QueryClientProvider client={queryClient}>
        <LaneFormDialog
          open={true}
          onOpenChange={vi.fn()}
          gates={[]}
          devices={devices}
          onSubmit={vi.fn()}
        />
      </QueryClientProvider>
    );

    expect(screen.getByText('Bảng LED Hiển Thị (TCP)')).toBeInTheDocument();
    expect(screen.getAllByText('-- Không dùng / Chưa gán --').length).toBeGreaterThan(0);
  });

  it('DeviceDetailDialog displays associated lane with role Bảng LED Hiển Thị (TCP)', async () => {
    const { DeviceDetailDialog } = await import('@/components/infrastructure/DeviceDetailDialog');
    const { devicesApi } = await import('@/api/infrastructureApi');

    vi.mocked(devicesApi.getById).mockResolvedValueOnce({
      id: 'led-dev-1',
      code: 'LED_01',
      name: 'Bảng LED Cổng Chính',
      type: DeviceType.LedBoard,
      ipAddress: '192.168.1.250',
      port: 10000,
      isActive: true,
      createdAt: '2026-10-30T00:00:00Z',
    });

    vi.mocked(lanesApi.getPaged).mockResolvedValueOnce({
      items: [
        {
          id: 'lane-101',
          code: 'LANE_OUT_01',
          name: 'Làn Ra 1',
          direction: LaneDirection.Out,
          outputRelay: 1,
          inputReader: 1,
          isActive: true,
          ledBoardDeviceId: 'led-dev-1',
          createdAt: '2026-10-30T00:00:00Z',
        },
      ],
      pagination: {
        totalCount: 1,
        pageIndex: 1,
        pageSize: 100,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    });

    render(
      <QueryClientProvider client={queryClient}>
        <DeviceDetailDialog open={true} onOpenChange={vi.fn()} deviceId="led-dev-1" />
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('Làn Ra 1')).toBeInTheDocument();
      expect(screen.getByText('Bảng LED Hiển Thị (TCP)')).toBeInTheDocument();
    });
  });

  it('getDeviceTypeBadge renders correct badge for LedBoard', async () => {
    const { getDeviceTypeBadge } = await import('@/components/infrastructure/deviceBadges');
    render(getDeviceTypeBadge(DeviceType.LedBoard));
    expect(screen.getByText('Bảng LED (TCP)')).toBeInTheDocument();
  });
});
