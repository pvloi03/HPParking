import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { RevenueKpiGrid } from '@/components/revenue/RevenueKpiGrid';
import type { RevenueReportDto } from '@/types/statistics';

describe('RevenueKpiGrid Component', () => {
  const mockData: RevenueReportDto = {
    totalRevenue: 2500000,
    paidSessionsCount: 50,
    completedSessionsCount: 60,
    averageFeePerSession: 50000,
    paidRatio: 83.3,
    timeline: [],
  };

  it('hiển thị 4 thẻ KPI với dữ liệu format chính xác', () => {
    render(<RevenueKpiGrid data={mockData} isLoading={false} />);

    expect(screen.getByText('Tổng Doanh Thu')).toBeInTheDocument();
    expect(screen.getByText('2.500.000 đ')).toBeInTheDocument();

    expect(screen.getByText('Lượt Thu Phí')).toBeInTheDocument();
    expect(screen.getByText('50 lượt')).toBeInTheDocument();

    expect(screen.getByText('Doanh Thu TB / Lượt')).toBeInTheDocument();
    expect(screen.getByText('50.000 đ/lượt')).toBeInTheDocument();

    expect(screen.getByText('Tỷ Lệ Thu Phí')).toBeInTheDocument();
    expect(screen.getByText('83.3%')).toBeInTheDocument();
    expect(screen.getByText(/50 \/ 60 lượt hoàn thành/i)).toBeInTheDocument();
  });
});
