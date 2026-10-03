import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ActiveStatusBadge } from '@/components/common/ActiveStatusBadge';

describe('ActiveStatusBadge Component', () => {
  it('hiển thị nhãn "Đã kích hoạt" khi isActive là true', () => {
    render(<ActiveStatusBadge isActive={true} />);
    expect(screen.getByText('Đã kích hoạt')).toBeInTheDocument();
  });

  it('hiển thị nhãn "Chưa kích hoạt" khi isActive là false', () => {
    render(<ActiveStatusBadge isActive={false} />);
    expect(screen.getByText('Chưa kích hoạt')).toBeInTheDocument();
  });

  it('hiển thị nhãn "Chưa kích hoạt" khi isActive là null hoặc undefined', () => {
    render(<ActiveStatusBadge isActive={null} />);
    expect(screen.getByText('Chưa kích hoạt')).toBeInTheDocument();
  });
});
