import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { DistributionMatrixTable } from '@/components/dashboard/DistributionMatrixTable';
import type { DistributionStatisticsDto } from '@/types/statistics';

const mockDistributionData: DistributionStatisticsDto = {
  totalFilteredClients: 150,
  totalFilteredVehicles: 180,
  totalFilteredGates: 3,
  totalFilteredLanes: 8,
  items: [
    {
      companyId: 'comp-1',
      companyName: 'Công ty HPParking Solution',
      departmentId: 'dept-1',
      departmentName: 'Phòng Vận Hành',
      clientCount: 45,
      vehicleCount: 60,
      gateCount: 2,
      laneCount: 4,
    },
    {
      companyId: 'comp-2',
      companyName: 'Công ty TNHH Phú Xuân',
      departmentId: 'dept-2',
      departmentName: 'Ban Quản Lý Tòa Nhà',
      clientCount: 105,
      vehicleCount: 120,
      gateCount: 1,
      laneCount: 4,
    },
  ],
};

describe('DistributionMatrixTable Component', () => {
  it('hiển thị danh sách các công ty và phòng ban trong bảng phân bổ', () => {
    render(
      <DistributionMatrixTable data={mockDistributionData} isLoading={false} />
    );

    expect(
      screen.getByText('Ma Trận Phân Bổ Hạ Tầng & Đơn Vị')
    ).toBeInTheDocument();
    expect(screen.getAllByText('Công ty HPParking Solution').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Phòng Vận Hành').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Công ty TNHH Phú Xuân').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Ban Quản Lý Tòa Nhà').length).toBeGreaterThan(0);
  });

  it('hiển thị thông báo khi không có dữ liệu phân bổ', () => {
    render(
      <DistributionMatrixTable
        data={{
          totalFilteredClients: 0,
          totalFilteredVehicles: 0,
          totalFilteredGates: 0,
          totalFilteredLanes: 0,
          items: [],
        }}
        isLoading={false}
      />
    );

    expect(
      screen.getByText('Chưa có dữ liệu phân bổ theo đơn vị tổ chức.')
    ).toBeInTheDocument();
  });
});
