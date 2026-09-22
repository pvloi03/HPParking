import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { TrafficBarChart } from '@/components/dashboard/TrafficBarChart';
import type { DashboardFilterState } from '@/types/dashboard';

describe('TrafficBarChart Component', () => {
  const mockFilterDay: DashboardFilterState = {
    type: 'day',
    date: '2026-09-22',
    month: '2026-09',
    year: '2026',
    customFrom: '2026-09-16',
    customTo: '2026-09-22',
  };

  it('hiển thị tiêu đề biểu đồ và mô tả động theo ngày 24/7', () => {
    render(<TrafficBarChart isLoading={false} filter={mockFilterDay} />);

    expect(screen.getByText('Thống Kê Số Lượt Xe Ra Vào')).toBeInTheDocument();
    expect(
      screen.getByText(/Lưu lượng xe 24\/7 theo từng giờ ngày 22\/09\/2026/i)
    ).toBeInTheDocument();
  });

  it('hiển thị mô tả động khi bộ lọc chuyển sang tháng hoặc năm', () => {
    const mockFilterMonth: DashboardFilterState = {
      ...mockFilterDay,
      type: 'month',
    };
    const { rerender } = render(
      <TrafficBarChart isLoading={false} filter={mockFilterMonth} />
    );

    expect(
      screen.getByText(/Lưu lượng xe đầy đủ 30 ngày trong tháng 09\/2026/i)
    ).toBeInTheDocument();

    const mockFilterYear: DashboardFilterState = {
      ...mockFilterDay,
      type: 'year',
    };
    rerender(<TrafficBarChart isLoading={false} filter={mockFilterYear} />);

    expect(
      screen.getByText(/Lưu lượng xe qua 12 tháng năm 2026/i)
    ).toBeInTheDocument();
  });

  it('hiển thị tổng số lượt xe vào và xe ra trong pill thống kê nhanh', () => {
    render(<TrafficBarChart isLoading={false} filter={mockFilterDay} />);

    expect(screen.getByText(/Vào:/i)).toBeInTheDocument();
    expect(screen.getByText(/Ra:/i)).toBeInTheDocument();
  });

  it('hiển thị skeleton khi isLoading là true', () => {
    const { container } = render(<TrafficBarChart isLoading={true} filter={mockFilterDay} />);

    const skeletons = container.querySelectorAll('.animate-pulse');
    expect(skeletons.length).toBeGreaterThan(0);
  });
});
