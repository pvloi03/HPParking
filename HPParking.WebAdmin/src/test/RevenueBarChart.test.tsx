import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { RevenueBarChart } from '@/components/revenue/RevenueBarChart';

describe('RevenueBarChart Component', () => {
  it('hiển thị tiêu đề biểu đồ và tổng thu', () => {
    render(
      <RevenueBarChart
        timeline={[
          {
            date: '2026-10-29',
            revenue: 500000,
            paidCount: 10,
            completedCount: 12,
            averageFee: 50000,
          },
        ]}
        totalRevenue={500000}
        fromDate="2026-10-29"
        toDate="2026-10-29"
        isLoading={false}
      />
    );

    expect(screen.getByText('Biểu Đồ Doanh Thu Theo Ngày')).toBeInTheDocument();
    expect(screen.getByText(/500\.000 đ/)).toBeInTheDocument();
  });

  it('hiển thị trạng thái rỗng khi timeline không có dữ liệu', () => {
    render(
      <RevenueBarChart
        timeline={[]}
        totalRevenue={0}
        fromDate="2026-10-29"
        toDate="2026-10-29"
        isLoading={false}
      />
    );

    expect(
      screen.getByText('Không có dữ liệu trong khoảng thời gian đã chọn')
    ).toBeInTheDocument();
  });
});
