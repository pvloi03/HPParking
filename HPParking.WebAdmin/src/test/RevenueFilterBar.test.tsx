import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { RevenueFilterBar } from '@/components/revenue/RevenueFilterBar';

describe('RevenueFilterBar Component', () => {
  it('hiển thị đầy đủ các nút preset và 2 ô nhập ngày', () => {
    const onPresetChange = vi.fn();
    const onFromDateChange = vi.fn();
    const onToDateChange = vi.fn();

    render(
      <RevenueFilterBar
        preset="today"
        fromDate="2026-10-29"
        toDate="2026-10-29"
        onPresetChange={onPresetChange}
        onFromDateChange={onFromDateChange}
        onToDateChange={onToDateChange}
      />
    );

    expect(screen.getByRole('button', { name: 'Hôm nay' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: '7 ngày qua' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Tháng này' })).toBeInTheDocument();

    const fromInput = screen.getByLabelText('Từ ngày');
    const toInput = screen.getByLabelText('Đến ngày');

    expect(fromInput).toHaveValue('2026-10-29');
    expect(toInput).toHaveValue('2026-10-29');

    fireEvent.click(screen.getByRole('button', { name: '7 ngày qua' }));
    expect(onPresetChange).toHaveBeenCalledWith('last7days');

    fireEvent.change(fromInput, { target: { value: '2026-10-01' } });
    expect(onFromDateChange).toHaveBeenCalledWith('2026-10-01');

    fireEvent.change(toInput, { target: { value: '2026-10-31' } });
    expect(onToDateChange).toHaveBeenCalledWith('2026-10-31');
  });
});
