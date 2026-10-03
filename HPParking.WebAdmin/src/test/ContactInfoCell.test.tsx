import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ContactInfoCell } from '@/components/common/ContactInfoCell';

describe('ContactInfoCell Component', () => {
  it('hiển thị cả số điện thoại và email kèm icon', () => {
    render(<ContactInfoCell phoneNumber="0901234567" email="test@example.com" />);
    expect(screen.getByText('0901234567')).toBeInTheDocument();
    expect(screen.getByText('test@example.com')).toBeInTheDocument();
  });

  it('hiển thị chỉ số điện thoại khi email rỗng', () => {
    render(<ContactInfoCell phoneNumber="0901234567" email={null} />);
    expect(screen.getByText('0901234567')).toBeInTheDocument();
    expect(screen.queryByText('@')).not.toBeInTheDocument();
  });

  it('hiển thị chỉ email khi số điện thoại rỗng', () => {
    render(<ContactInfoCell phoneNumber="" email="test@example.com" />);
    expect(screen.getByText('test@example.com')).toBeInTheDocument();
  });

  it('hiển thị fallback "—" khi cả hai đều rỗng', () => {
    render(<ContactInfoCell phoneNumber="" email="" />);
    expect(screen.getByText('—')).toBeInTheDocument();
  });
});
