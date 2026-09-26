import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { DeviceDetailDialog } from '@/components/infrastructure/DeviceDetailDialog';
import { devicesApi, lanesApi } from '@/api/infrastructureApi';
import { DeviceType, LaneDirection } from '@/types/infrastructure';

vi.mock('@/api/infrastructureApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/infrastructureApi')>();
  return {
    ...actual,
    devicesApi: {
      getById: vi.fn(),
      pingDeviceIp: vi.fn(),
    },
    lanesApi: {
      getPaged: vi.fn(),
    },
  };
});

import { useDevicePingStore } from '@/stores/devicePingStore';

describe('DeviceDetailDialog Component', () => {
  let queryClient: QueryClient;

  const mockDevice = {
    id: 'dev-101',
    code: 'CAM_LPR_IN_01',
    name: 'Camera Biển Số Cổng Chính Vào',
    type: DeviceType.Camera,
    ipAddress: '192.168.1.120',
    port: 8000,
    userName: 'admin_cam',
    hasPassword: true,
    isActive: true,
    createdAt: '2026-09-01T08:00:00Z',
    updatedAt: '2026-09-20T10:30:00Z',
  };

  const mockLanes = [
    {
      id: 'lane-1',
      code: 'LANE_IN_01',
      name: 'Làn Vào Xe Máy 01',
      gateId: 'gate-1',
      gateName: 'Cổng Chính Phía Nam',
      direction: LaneDirection.In,
      plateCameraDeviceId: 'dev-101',
      overviewCameraDeviceId: 'dev-102',
      controllerDeviceId: 'dev-103',
      outputRelay: 1,
      inputReader: 1,
      isActive: true,
      createdAt: '2026-09-01T08:00:00Z',
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    useDevicePingStore.getState().clearAllRecords();
    localStorage.clear();
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });
  });

  const renderComponent = (props: {
    deviceId?: string | null;
    open?: boolean;
    onEdit?: () => void;
  } = {}) => {
    return render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <DeviceDetailDialog
            open={props.open ?? true}
            onOpenChange={vi.fn()}
            deviceId={props.deviceId ?? 'dev-101'}
            onEdit={props.onEdit}
          />
        </MemoryRouter>
      </QueryClientProvider>
    );
  };

  it('hiển thị đầy đủ thông tin chi tiết thiết bị, mã, tên, trạng thái và endpoint', async () => {
    vi.mocked(devicesApi.getById).mockResolvedValueOnce(mockDevice);
    vi.mocked(lanesApi.getPaged).mockResolvedValueOnce({
      items: mockLanes,
      pagination: {
        pageIndex: 1,
        pageSize: 100,
        totalCount: 1,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    });

    renderComponent();

    await waitFor(() => {
      expect(screen.getAllByText('Camera Biển Số Cổng Chính Vào').length).toBeGreaterThan(0);
      expect(screen.getAllByText('CAM_LPR_IN_01').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Camera Giám Sát').length).toBeGreaterThan(0);
      expect(screen.getByText('Đang hoạt động')).toBeInTheDocument();
      expect(screen.getByText(/192.168.1.120:8000/i)).toBeInTheDocument();
      expect(screen.getByText('admin_cam')).toBeInTheDocument();
      expect(screen.getByText('Đã thiết lập mật khẩu')).toBeInTheDocument();
    });
  });

  it('hiển thị các làn xe liên kết và vai trò ngoại vi', async () => {
    vi.mocked(devicesApi.getById).mockResolvedValueOnce(mockDevice);
    vi.mocked(lanesApi.getPaged).mockResolvedValueOnce({
      items: mockLanes,
      pagination: {
        pageIndex: 1,
        pageSize: 100,
        totalCount: 1,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    });

    renderComponent();

    await waitFor(() => {
      expect(screen.getByText('Làn Vào Xe Máy 01')).toBeInTheDocument();
      expect(screen.getByText('(LANE_IN_01)')).toBeInTheDocument();
      expect(screen.getByText('Camera Biển Số (LPR)')).toBeInTheDocument();
      expect(screen.getByText(/Thuộc cổng: Cổng Chính Phía Nam/i)).toBeInTheDocument();
    });
  });

  it('tự động ping kiểm tra kết nối khi mở dialog chi tiết thiết bị', async () => {
    vi.mocked(devicesApi.getById).mockResolvedValueOnce(mockDevice);
    vi.mocked(lanesApi.getPaged).mockResolvedValueOnce({
      items: mockLanes,
      pagination: {
        pageIndex: 1,
        pageSize: 100,
        totalCount: 1,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    });

    vi.mocked(devicesApi.pingDeviceIp).mockResolvedValueOnce({
      ipAddress: '192.168.1.120',
      isAlive: true,
      roundtripTimeMs: 14,
      method: 'ICMP',
      message: 'Thiết bị phản hồi tốt (14ms)',
      timestamp: new Date().toISOString(),
    });

    renderComponent();

    await waitFor(() => {
      expect(devicesApi.pingDeviceIp).toHaveBeenCalledWith('192.168.1.120');
      expect(screen.getByText('ONLINE')).toBeInTheDocument();
      expect(screen.getByText('14 ms')).toBeInTheDocument();
    });

    // Không hiển thị lối tắt nhật ký kiểm toán và nút kiểm tra kết nối ở footer
    expect(screen.queryByText(/nhật ký kiểm toán/i)).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /^kiểm tra kết nối$/i })).not.toBeInTheDocument();
  });

  it('hiển thị thông báo khi thiết bị chưa được gán vào làn xe nào', async () => {
    vi.mocked(devicesApi.getById).mockResolvedValueOnce({
      ...mockDevice,
      id: 'dev-free',
    });
    vi.mocked(lanesApi.getPaged).mockResolvedValueOnce({
      items: mockLanes,
      pagination: {
        pageIndex: 1,
        pageSize: 100,
        totalCount: 1,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    });

    renderComponent({ deviceId: 'dev-free' });

    await waitFor(() => {
      expect(
        screen.getByText('Thiết bị chưa được gán vào làn xe nào')
      ).toBeInTheDocument();
    });
  });

  it('kích hoạt onEdit khi bấm nút Chỉnh sửa', async () => {
    vi.mocked(devicesApi.getById).mockResolvedValueOnce(mockDevice);
    vi.mocked(lanesApi.getPaged).mockResolvedValueOnce({
      items: [],
      pagination: {
        pageIndex: 1,
        pageSize: 100,
        totalCount: 0,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    });

    const handleEdit = vi.fn();
    renderComponent({ onEdit: handleEdit });

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Chỉnh sửa/i })).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: /Chỉnh sửa/i }));

    expect(handleEdit).toHaveBeenCalledWith(mockDevice);
  });

  it('hiển thị ngay lập tức trạng thái từ store cache khi mở dialog', async () => {
    // Lưu trước kết quả ping vào store (giả lập đã lưu từ trước khi F5)
    useDevicePingStore.getState().setPingResult('dev-101', {
      ipAddress: '192.168.1.120',
      isAlive: true,
      roundtripTimeMs: 25,
      method: 'TCP_SOCKET',
      message: 'Socket open port 8000 (25ms)',
      timestamp: new Date().toISOString(),
    });

    vi.mocked(devicesApi.getById).mockResolvedValueOnce(mockDevice);
    vi.mocked(lanesApi.getPaged).mockResolvedValueOnce({
      items: [],
      pagination: {
        pageIndex: 1,
        pageSize: 100,
        totalCount: 0,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    });

    renderComponent();

    // Phải hiển thị ngay ONLINE và 25 ms từ cache trước khi bất kỳ ping mới nào phản hồi
    await waitFor(() => {
      expect(screen.getByText('ONLINE')).toBeInTheDocument();
      expect(screen.getByText('25 ms')).toBeInTheDocument();
    });
  });
});
