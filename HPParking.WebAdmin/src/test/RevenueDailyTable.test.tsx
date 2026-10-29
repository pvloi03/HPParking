import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { RevenueDailyTable } from '@/components/revenue/RevenueDailyTable';
import type { RevenueReportDto } from '@/types/statistics';

describe('RevenueDailyTable Component', () => {
  const mockData: RevenueReportDto = {
    totalRevenue: 1500000,
    paidSessionsCount: 30,
    completedSessionsCount: 40,
    averageFeePerSession: 50000,
    paidRatio: 75.0,
    timeline: [
      {
        date: '2026-10-28',
        revenue: 600000,
        paidCount: 12,
        completedCount: 15,
        averageFee: 50000,
      },
      {
        date: '2026-10-29',
        revenue: 900000,
        paidCount: 18,
        completedCount: 25,
        averageFee: 50000,
      },
    ],
  };

  it('hiển thị danh sách các ngày trong timeline', () => {
    render(<RevenueDailyTable data={mockData} isLoading={false} />);

    expect(screen.getByText('28/10/2026')).toBeInTheDocument();
    expect(screen.getByText('29/10/2026')).toBeInTheDocument();
    expect(screen.getByText('600.000 đ')).toBeInTheDocument();
    expect(screen.getByText('900.000 đ')).toBeInTheDocument();
  });

  it('hiển thị thông báo không có dữ liệu khi timeline rỗng', () => {
    const emptyData: RevenueReportDto = {
      ...mockData,
      timeline: [],
    };
    render(<RevenueDailyTable data={emptyData} isLoading={false} />);

    expect(
      screen.getByText('Không có dữ liệu trong khoảng thời gian đã chọn')
    ).toBeInTheDocument();
  });
});
