import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { Users, AlertTriangle } from 'lucide-react';
import { KpiCard } from '@/components/dashboard/KpiCard';
import type { KpiCardItem } from '@/types/dashboard';

describe('KpiCard Component', () => {
  const mockItem: KpiCardItem = {
    title: 'Tổng Nhân Sự',
    value: '1.250',
    subtext: 'Đang hoạt động',
    icon: Users,
    iconColor: 'text-blue-600',
    iconBg: 'bg-blue-50',
  };

  it('hiển thị đầy đủ thông tin title, value, subtext và icon', () => {
    render(<KpiCard item={mockItem} />);

    expect(screen.getByText('Tổng Nhân Sự')).toBeInTheDocument();
    expect(screen.getByText('1.250')).toBeInTheDocument();
    expect(screen.getByText('Đang hoạt động')).toBeInTheDocument();
  });

  it('áp dụng class cảnh báo khi isWarning là true', () => {
    const warningItem: KpiCardItem = {
      ...mockItem,
      title: 'Cảnh Báo Sức Chứa',
      value: '95%',
      subtext: 'Sắp đầy',
      icon: AlertTriangle,
      isWarning: true,
    };

    const { container } = render(<KpiCard item={warningItem} />);

    expect(screen.getByText('Cảnh Báo Sức Chứa')).toBeInTheDocument();
    expect(screen.getByText('95%')).toBeInTheDocument();
    expect(screen.getByText('Sắp đầy')).toHaveClass('text-red-600');
    expect(container.querySelector('.border-red-300')).toBeInTheDocument();
  });

  it('hiển thị Skeleton khi isLoading là true', () => {
    const { container } = render(<KpiCard item={mockItem} isLoading={true} />);

    expect(screen.getByText('Tổng Nhân Sự')).toBeInTheDocument();
    expect(screen.queryByText('1.250')).not.toBeInTheDocument();
    expect(container.querySelectorAll('.animate-pulse').length).toBeGreaterThan(0);
  });
});
