import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { KpiCardGrid } from '@/components/dashboard/KpiCardGrid';
import type { DashboardStatisticsDto } from '@/types/statistics';

const mockKpiData: DashboardStatisticsDto = {
  totalClients: 250,
  activeClients: 230,
  clientsByType: { Resident: 200, Guest: 50 },
  clientsWithFaceId: 210,
  faceIdSyncRatePercentage: 84.0,
  totalVehicles: 320,
  activeVehicles: 300,
  vehiclesByType: { Car: 100, Motorcycle: 220 },
  activeParkingSessions: 85,
  totalGates: 3,
  totalLanes: 8,
  activeLanes: 8,
};

describe('KpiCardGrid Component', () => {
  it('hiển thị đầy đủ 6 chỉ số KPIs khi có dữ liệu', () => {
    render(<KpiCardGrid data={mockKpiData} isLoading={false} />);

    expect(screen.getByText('Khách Hàng')).toBeInTheDocument();
    expect(screen.getByText('250')).toBeInTheDocument();

    expect(screen.getByText('Phương Tiện')).toBeInTheDocument();
    expect(screen.getByText('320')).toBeInTheDocument();

    expect(screen.getByText('Xe Đang Đỗ')).toBeInTheDocument();
    expect(screen.getByText('85')).toBeInTheDocument();

    expect(screen.getByText('Cổng Kiểm Soát')).toBeInTheDocument();
    expect(screen.getByText('3')).toBeInTheDocument();

    expect(screen.getByText('Làn Vận Hành')).toBeInTheDocument();
    expect(screen.getByText('8 / 8')).toBeInTheDocument();

    expect(screen.getByText('Đồng Bộ FaceID')).toBeInTheDocument();
    expect(screen.getByText('84.0%')).toBeInTheDocument();
  });

  it('hiển thị cảnh báo dung lượng khi số lượng xe đang đỗ cao', () => {
    const highCapacityData = {
      ...mockKpiData,
      activeParkingSessions: 195,
    };
    render(<KpiCardGrid data={highCapacityData} isLoading={false} />);

    expect(
      screen.getByText(/Cảnh báo: Dung lượng sắp đầy/i)
    ).toBeInTheDocument();
  });
});
