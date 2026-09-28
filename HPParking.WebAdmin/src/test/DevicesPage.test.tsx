import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { DevicesPage } from '@/pages/DevicesPage';
import { devicesApi } from '@/api/infrastructureApi';
import { DeviceType } from '@/types/infrastructure';
import { useDevicePingStore } from '@/stores/devicePingStore';

vi.mock('@/api/infrastructureApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/infrastructureApi')>();
  return {
    ...actual,
    devicesApi: {
      getPaged: vi.fn(),
      getById: vi.fn(),
      create: vi.fn(),
      update: vi.fn(),
      delete: vi.fn(),
      restore: vi.fn(),
      pingDeviceIp: vi.fn(),
      pingBatchDeviceIps: vi.fn(),
    },
  };
});

describe('DevicesPage Component', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    vi.clearAllMocks();
    useDevicePingStore.getState().clearAllRecords();
    useDevicePingStore.getState().setAutoPingEnabled(true);
    useDevicePingStore.getState().setBatchPinging(false);
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });
  });

  it('hiển thị danh sách thiết bị và loại thiết bị từ API', async () => {
    vi.mocked(devicesApi.getPaged).mockResolvedValueOnce({
      items: [
        {
          id: 'dev-1',
          code: 'CAM_LPR_01',
          name: 'Camera Biển Số Cổng 1',
          type: DeviceType.Camera,
          ipAddress: '192.168.1.50',
          port: 80,
          userName: 'admin',
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
          <DevicesPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    expect(
      screen.getByText(/Quản Lý Thiết Bị Ngoại Vi/i)
    ).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getAllByText('CAM_LPR_01').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Camera Biển Số Cổng 1').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Camera Giám Sát').length).toBeGreaterThan(0);
      expect(screen.getByText(/Ping tất cả/i)).toBeInTheDocument();
      expect(screen.getAllByTitle(/Kiểm tra kết nối tới/i).length).toBeGreaterThan(0);
    });
  });

  it('thực hiện ping đơn lẻ khi bấm nút Ping trên từng hàng thiết bị', async () => {
    vi.mocked(devicesApi.getPaged).mockResolvedValueOnce({
      items: [
        {
          id: 'dev-1',
          code: 'CAM_LPR_01',
          name: 'Camera Biển Số Cổng 1',
          type: DeviceType.Camera,
          ipAddress: '192.168.1.50',
          port: 80,
          userName: 'admin',
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

    vi.mocked(devicesApi.pingDeviceIp).mockResolvedValueOnce({
      ipAddress: '192.168.1.50',
      isAlive: true,
      roundtripTimeMs: 18,
      method: 'ICMP',
      message: 'Thiết bị phản hồi tốt (18ms)',
      timestamp: new Date().toISOString(),
    });

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <DevicesPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getAllByTitle(/Kiểm tra kết nối tới/i).length).toBeGreaterThan(0);
    });

    fireEvent.click(screen.getAllByTitle(/Kiểm tra kết nối tới/i)[0]);

    await waitFor(() => {
      expect(devicesApi.pingDeviceIp).toHaveBeenCalledWith('192.168.1.50');
      expect(screen.getAllByText(/Online/i).length).toBeGreaterThan(0);
      expect(screen.getAllByText(/\(18ms\)/i).length).toBeGreaterThan(0);
    });
  });

  it('thực hiện ping tất cả thiết bị đồng thời khi bấm nút Ping tất cả', async () => {
    vi.mocked(devicesApi.getPaged).mockResolvedValueOnce({
      items: [
        {
          id: 'dev-1',
          code: 'CAM_01',
          name: 'Camera Cổng 1',
          type: DeviceType.Camera,
          ipAddress: '192.168.1.50',
          port: 80,
          userName: 'admin',
          isActive: true,
          createdAt: new Date().toISOString(),
        },
        {
          id: 'dev-2',
          code: 'BARRIER_01',
          name: 'Barrier Cổng 1',
          type: DeviceType.Controller,
          ipAddress: '192.168.1.60',
          port: 8000,
          userName: 'admin',
          isActive: true,
          createdAt: new Date().toISOString(),
        },
      ],
      pagination: {
        pageIndex: 1,
        pageSize: 10,
        totalCount: 2,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    });

    vi.mocked(devicesApi.pingBatchDeviceIps).mockResolvedValueOnce([
      {
        ipAddress: '192.168.1.50',
        isAlive: true,
        roundtripTimeMs: 12,
        method: 'ICMP',
        message: 'Online 12ms',
        timestamp: new Date().toISOString(),
      },
      {
        ipAddress: '192.168.1.60',
        isAlive: false,
        roundtripTimeMs: 2000,
        method: 'NONE',
        message: 'Timeout',
        timestamp: new Date().toISOString(),
      },
    ]);

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <DevicesPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    await waitFor(() => {
      const btn = screen.getByTitle(/Kiểm tra kết nối song song toàn bộ/i);
      expect(btn).not.toBeDisabled();
    });

    fireEvent.click(screen.getByTitle(/Kiểm tra kết nối song song toàn bộ/i));

    await waitFor(() => {
      expect(devicesApi.pingBatchDeviceIps).toHaveBeenCalledWith([
        '192.168.1.50',
        '192.168.1.60',
      ]);
      expect(screen.getAllByText(/Online/i).length).toBeGreaterThan(0);
      expect(screen.getAllByText(/Offline/i).length).toBeGreaterThan(0);
    });
  });

  it('mở modal DeviceDetailDialog khi bấm vào mã hoặc tên thiết bị', async () => {
    vi.mocked(devicesApi.getPaged).mockResolvedValueOnce({
      items: [
        {
          id: 'dev-1',
          code: 'CAM_LPR_01',
          name: 'Camera Biển Số Cổng 1',
          type: DeviceType.Camera,
          ipAddress: '192.168.1.50',
          port: 80,
          userName: 'admin',
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

    vi.mocked(devicesApi.getById).mockResolvedValueOnce({
      id: 'dev-1',
      code: 'CAM_LPR_01',
      name: 'Camera Biển Số Cổng 1',
      type: DeviceType.Camera,
      ipAddress: '192.168.1.50',
      port: 80,
      userName: 'admin',
      isActive: true,
      createdAt: new Date().toISOString(),
    });

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <DevicesPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getAllByText('CAM_LPR_01').length).toBeGreaterThan(0);
    });

    // Bấm vào mã thiết bị CAM_LPR_01
    const codeBtn = screen.getAllByRole('button', { name: 'CAM_LPR_01' })[0];
    fireEvent.click(codeBtn);

    await waitFor(() => {
      expect(
        screen.getByText(/Chi Tiết Thiết Bị Ngoại Vi & Kết Nối Mạng/i)
      ).toBeInTheDocument();
      expect(screen.getByText('Địa chỉ kết nối (Endpoint)')).toBeInTheDocument();
    });
  });

  it('bật/tắt công tắc Tự động ping (15s) và cập nhật trạng thái trong useDevicePingStore', async () => {
    vi.mocked(devicesApi.getPaged).mockResolvedValueOnce({
      items: [],
      pagination: {
        pageIndex: 1,
        pageSize: 10,
        totalCount: 0,
        totalPages: 0,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    });

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <DevicesPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    const toggleBtn = screen.getByRole('button', { name: /Tự động ping \(15s\)/i });
    expect(toggleBtn).toBeInTheDocument();
    expect(useDevicePingStore.getState().isAutoPingEnabled).toBe(true);

    // Bấm để tắt
    fireEvent.click(toggleBtn);
    expect(useDevicePingStore.getState().isAutoPingEnabled).toBe(false);

    // Bấm để bật lại
    fireEvent.click(toggleBtn);
    expect(useDevicePingStore.getState().isAutoPingEnabled).toBe(true);
  });

  it('khôi phục trạng thái kết nối từ useDevicePingStore ngay khi render mà không cần bấm nút', async () => {
    // Nạp sẵn dữ liệu ping đã lưu trong store
    useDevicePingStore.getState().setPingResult('dev-1', {
      ipAddress: '192.168.1.50',
      isAlive: true,
      roundtripTimeMs: 12,
      method: 'ICMP',
      message: 'Online 12ms',
      timestamp: new Date().toISOString(),
    });

    vi.mocked(devicesApi.getPaged).mockResolvedValueOnce({
      items: [
        {
          id: 'dev-1',
          code: 'CAM_LPR_01',
          name: 'Camera Biển Số Cổng 1',
          type: DeviceType.Camera,
          ipAddress: '192.168.1.50',
          port: 80,
          userName: 'admin',
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
          <DevicesPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    // Kiểm tra thấy hiển thị trạng thái Online và độ trễ 12ms ngay lập tức
    await waitFor(() => {
      expect(screen.getAllByText('Online').length).toBeGreaterThan(0);
      expect(screen.getAllByText('(12ms)').length).toBeGreaterThan(0);
    });
  });

  it('hiển thị badge "Chờ kiểm tra" khi bản ghi ping đã quá hạn TTL 5 phút', async () => {
    // Nạp bản ghi quá hạn 6 phút
    const sixMinutesAgo = Date.now() - 6 * 60 * 1000;
    useDevicePingStore.setState({
      records: {
        'dev-1': {
          result: {
            ipAddress: '192.168.1.50',
            isAlive: true,
            roundtripTimeMs: 12,
            method: 'ICMP',
            message: 'Online 12ms',
            timestamp: new Date(sixMinutesAgo).toISOString(),
          },
          lastPingedAt: sixMinutesAgo,
        },
      },
    });

    vi.mocked(devicesApi.getPaged).mockResolvedValueOnce({
      items: [
        {
          id: 'dev-1',
          code: 'CAM_LPR_01',
          name: 'Camera Biển Số Cổng 1',
          type: DeviceType.Camera,
          ipAddress: '192.168.1.50',
          port: 80,
          userName: 'admin',
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
          <DevicesPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    // Kiểm tra thấy hiển thị badge "Chờ kiểm tra"
    await waitFor(() => {
      expect(screen.getAllByText('Chờ kiểm tra').length).toBeGreaterThan(0);
    });
  });
});


