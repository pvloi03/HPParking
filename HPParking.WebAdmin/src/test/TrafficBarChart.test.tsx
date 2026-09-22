import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { TrafficBarChart } from '@/components/dashboard/TrafficBarChart';

describe('TrafficBarChart Component', () => {
  it('hiển thị tiêu đề biểu đồ và mô tả', () => {
    render(<TrafficBarChart isLoading={false} />);

    expect(screen.getByText('Thống Kê Số Lượt Xe Ra Vào')).toBeInTheDocument();
    expect(
      screen.getByText(/Lưu lượng xe quét thẻ và nhận diện biển số qua cổng theo thời gian/i)
    ).toBeInTheDocument();
  });

  it('hiển thị các nút chọn khoảng thời gian và cho phép chuyển đổi', () => {
    render(<TrafficBarChart isLoading={false} />);

    const todayBtn = screen.getByRole('button', { name: 'Hôm Nay' });
    const weekBtn = screen.getByRole('button', { name: '7 Ngày Qua' });

    expect(todayBtn).toBeInTheDocument();
    expect(weekBtn).toBeInTheDocument();

    // Ban đầu chọn Hôm Nay
    expect(todayBtn.className).toContain('font-semibold');

    // Chuyển sang 7 Ngày Qua
    fireEvent.click(weekBtn);
    expect(weekBtn.className).toContain('font-semibold');
  });

  it('hiển thị tổng số lượt xe vào và xe ra trong pill thống kê nhanh', () => {
    render(<TrafficBarChart isLoading={false} />);

    // Kiểm tra có hiển thị badge Vào và Ra
    expect(screen.getByText(/Vào:/i)).toBeInTheDocument();
    expect(screen.getByText(/Ra:/i)).toBeInTheDocument();
  });

  it('hiển thị skeleton khi isLoading là true', () => {
    const { container } = render(<TrafficBarChart isLoading={true} />);

    // Không render ChartContainer mà render Skeleton elements
    const skeletons = container.querySelectorAll('.animate-pulse');
    expect(skeletons.length).toBeGreaterThan(0);
  });
});
