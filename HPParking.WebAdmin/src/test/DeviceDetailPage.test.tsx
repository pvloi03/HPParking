import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { DeviceDetailPage } from '@/pages/DeviceDetailPage';
import { devicesApi, lanesApi } from '@/api/infrastructureApi';
import { DeviceType, LaneDirection } from '@/types/infrastructure';

vi.mock('@/api/infrastructureApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/infrastructureApi')>();
  return {
    ...actual,
    devicesApi: {
      getById: vi.fn(),
      update: vi.fn(),
      delete: vi.fn(),
      pingDeviceIp: vi.fn(),
    },
    lanesApi: {
      getPaged: vi.fn(),
    },
  };
});

describe('DeviceDetailPage Component', () => {
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
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });
  });

  const renderComponent = (deviceId = 'dev-101') => {
    return render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={[`/devices/${deviceId}`]}>
          <Routes>
            <Route path="/devices/:id" element={<DeviceDetailPage />} />
            <Route path="/devices" element={<div>Trang Danh Sách Thiết Bị</div>} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    );
  };

  it('hiển thị đầy đủ thông tin chi tiết thiết bị, mã, tên, trạng thái và endpoint mạng', async () => {
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
      expect(screen.getByText('Đang hoạt động bình thường')).toBeInTheDocument();
      expect(screen.getByText(/192.168.1.120:8000/i)).toBeInTheDocument();
      expect(screen.getByText('admin_cam')).toBeInTheDocument();
      expect(screen.getByText('Đã thiết lập mật khẩu')).toBeInTheDocument();
    });
  });

  it('hiển thị danh sách các làn xe đang sử dụng thiết bị này và vai trò tương ứng', async () => {
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

  it('thực hiện ping trực tiếp thiết bị khi bấm nút Kiểm tra kết nối', async () => {
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
      expect(screen.getByRole('button', { name: /Kiểm tra kết nối/i })).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: /Kiểm tra kết nối/i }));

    await waitFor(() => {
      expect(devicesApi.pingDeviceIp).toHaveBeenCalledWith('192.168.1.120');
      expect(screen.getByText('ONLINE')).toBeInTheDocument();
      expect(screen.getByText('14 ms')).toBeInTheDocument();
      expect(screen.getByText(/Thiết bị phản hồi tốt/i)).toBeInTheDocument();
    });
  });

  it('hiển thị thông báo khi thiết bị chưa được gán vào làn xe nào', async () => {
    vi.mocked(devicesApi.getById).mockResolvedValueOnce({
      ...mockDevice,
      id: 'dev-free',
    });
    vi.mocked(lanesApi.getPaged).mockResolvedValueOnce({
      items: mockLanes, // mockLanes chỉ có dev-101
      pagination: {
        pageIndex: 1,
        pageSize: 100,
        totalCount: 1,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    });

    renderComponent('dev-free');

    await waitFor(() => {
      expect(
        screen.getByText('Thiết bị chưa được gán vào làn xe nào')
      ).toBeInTheDocument();
    });
  });

  it('điều hướng quay lại danh sách khi bấm nút Quay lại', async () => {
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

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Quay lại/i })).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: /Quay lại/i }));

    await waitFor(() => {
      expect(screen.getByText('Trang Danh Sách Thiết Bị')).toBeInTheDocument();
    });
  });
});
