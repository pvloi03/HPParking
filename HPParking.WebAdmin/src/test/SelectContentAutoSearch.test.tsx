import { describe, it, expect } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import {
  Select,
  SelectTrigger,
  SelectValue,
  SelectContent,
  SelectItem,
} from '@/components/ui/select';

describe('SelectContent Auto-Search Feature', () => {
  it('không hiển thị ô tìm kiếm nếu số lượng SelectItem <= 10', () => {
    render(
      <Select>
        <SelectTrigger>
          <SelectValue placeholder="Chọn" />
        </SelectTrigger>
        <SelectContent>
          {Array.from({ length: 5 }, (_, i) => (
            <SelectItem key={i} value={`val-${i}`}>
              Mục {i}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    );

    fireEvent.click(screen.getByRole('combobox'));
    expect(screen.queryByPlaceholderText(/tìm/i)).toBeNull();
  });

  it('tự động hiển thị ô tìm kiếm khi số lượng SelectItem > 10', () => {
    render(
      <Select>
        <SelectTrigger>
          <SelectValue placeholder="Chọn cổng" />
        </SelectTrigger>
        <SelectContent>
          {Array.from({ length: 15 }, (_, i) => (
            <SelectItem key={i} value={`gate-${i}`}>
              Cổng số {i} (GATE-0{i})
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    );

    fireEvent.click(screen.getByRole('combobox'));
    const input = screen.getByPlaceholderText('Tìm theo mã hoặc tên...');
    expect(input).toBeDefined();

    // Thử gõ tìm kiếm "GATE-012"
    fireEvent.change(input, { target: { value: 'GATE-012' } });

    expect(screen.getByText('Cổng số 12 (GATE-012)')).toBeDefined();
    expect(screen.queryByText('Cổng số 1 (GATE-01)')).toBeNull();
  });

  it('sử dụng placeholder thông minh theo mã khi truyền props items vào SelectContent', () => {
    const clients = Array.from({ length: 12 }, (_, i) => ({
      id: `c-${i}`,
      code: `KH-${100 + i}`,
      name: `Khách hàng ${i}`,
    }));

    render(
      <Select>
        <SelectTrigger>
          <SelectValue placeholder="Chọn khách" />
        </SelectTrigger>
        <SelectContent items={clients}>
          {clients.map((c) => (
            <SelectItem key={c.id} value={c.id}>
              {c.name} ({c.code})
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    );

    fireEvent.click(screen.getByRole('combobox'));
    expect(screen.getByPlaceholderText('Tìm theo mã...')).toBeDefined();
  });
});
