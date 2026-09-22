import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { DashboardFilterBar } from '@/components/dashboard/DashboardFilterBar';
import type { DashboardFilterState } from '@/types/dashboard';

describe('DashboardFilterBar Component', () => {
  const initialFilter: DashboardFilterState = {
    type: 'day',
    date: '2026-09-22',
    month: '2026-09',
    year: '2026',
    customFrom: '2026-09-16',
    customTo: '2026-09-22',
  };

  it('hiển thị đầy đủ 4 chế độ lọc: Ngày, Tháng, Năm, Tùy chọn', () => {
    render(
      <DashboardFilterBar
        filter={initialFilter}
        onFilterChange={vi.fn()}
        onRefresh={vi.fn()}
      />
    );

    expect(screen.getByRole('button', { name: 'Ngày' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Tháng' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Năm' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Tùy chọn' })).toBeInTheDocument();
  });

  it('kích hoạt onFilterChange khi chuyển đổi chế độ lọc', () => {
    const handleFilterChange = vi.fn();
    render(
      <DashboardFilterBar
        filter={initialFilter}
        onFilterChange={handleFilterChange}
        onRefresh={vi.fn()}
      />
    );

    fireEvent.click(screen.getByRole('button', { name: 'Tháng' }));
    expect(handleFilterChange).toHaveBeenCalledWith(
      expect.objectContaining({ type: 'month' })
    );

    fireEvent.click(screen.getByRole('button', { name: 'Năm' }));
    expect(handleFilterChange).toHaveBeenCalledWith(
      expect.objectContaining({ type: 'year' })
    );

    fireEvent.click(screen.getByRole('button', { name: 'Tùy chọn' }));
    expect(handleFilterChange).toHaveBeenCalledWith(
      expect.objectContaining({ type: 'custom' })
    );
  });

  it('hiển thị các input ngày tương ứng theo chế độ', () => {
    const { rerender } = render(
      <DashboardFilterBar
        filter={initialFilter}
        onFilterChange={vi.fn()}
        onRefresh={vi.fn()}
      />
    );

    expect(screen.getByLabelText('Chọn ngày xem báo cáo')).toBeInTheDocument();

    rerender(
      <DashboardFilterBar
        filter={{ ...initialFilter, type: 'custom' }}
        onFilterChange={vi.fn()}
        onRefresh={vi.fn()}
      />
    );

    expect(screen.getByLabelText('Từ ngày')).toBeInTheDocument();
    expect(screen.getByLabelText('Đến ngày')).toBeInTheDocument();
  });

  it('gọi hàm onRefresh khi bấm nút Làm mới', () => {
    const handleRefresh = vi.fn();
    render(
      <DashboardFilterBar
        filter={initialFilter}
        onFilterChange={vi.fn()}
        onRefresh={handleRefresh}
      />
    );

    const refreshBtn = screen.getByRole('button', { name: /làm mới/i });
    fireEvent.click(refreshBtn);
    expect(handleRefresh).toHaveBeenCalledTimes(1);
  });
});
