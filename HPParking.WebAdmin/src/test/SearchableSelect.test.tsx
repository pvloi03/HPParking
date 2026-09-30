import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { SearchableSelect } from '@/components/ui/searchable-select';

describe('SearchableSelect Component', () => {
  it('không hiển thị ô tìm kiếm khi danh sách <= 10 phần tử', () => {
    const smallList = [
      { id: '1', code: 'A1', name: 'Mục 1' },
      { id: '2', code: 'A2', name: 'Mục 2' },
      { id: '3', code: 'A3', name: 'Mục 3' },
    ];

    render(
      <SearchableSelect
        items={smallList}
        value=""
        onValueChange={vi.fn()}
        placeholder="Chọn mục"
      />
    );

    // Mở dropdown
    const trigger = screen.getByRole('combobox');
    fireEvent.click(trigger);

    // Ô input tìm kiếm không xuất hiện vì <= 10 phần tử
    const searchInput = screen.queryByPlaceholderText(/tìm theo/i);
    expect(searchInput).toBeNull();
  });

  it('tự động hiển thị ô tìm kiếm với placeholder theo MÃ khi danh sách > 10 phần tử có trường code', () => {
    const longList = Array.from({ length: 15 }, (_, i) => ({
      id: `id-${i}`,
      code: `CODE-${i}`,
      name: `Tên ${i}`,
    }));

    render(
      <SearchableSelect
        items={longList}
        value=""
        onValueChange={vi.fn()}
        placeholder="Chọn danh sách"
      />
    );

    // Mở dropdown
    const trigger = screen.getByRole('combobox');
    fireEvent.click(trigger);

    // Tự động nhận diện trường "code" và hiển thị "Tìm theo mã..."
    const searchInput = screen.getByPlaceholderText('Tìm theo mã...');
    expect(searchInput).toBeDefined();

    // Thử gõ tìm kiếm
    fireEvent.change(searchInput, { target: { value: 'CODE-12' } });

    // Mục khớp CODE-12 hiển thị
    expect(screen.getByText('Tên 12')).toBeDefined();
    // Mục không khớp bị ẩn
    expect(screen.queryByText('Tên 1')).toBeNull();
  });

  it('tự động hiển thị placeholder theo BIỂN SỐ khi đối tượng có plateNumber', () => {
    const vehicles = Array.from({ length: 12 }, (_, i) => ({
      id: `v-${i}`,
      plateNumber: `30A-${1000 + i}`,
      type: 'Xe bán tải',
    }));

    render(
      <SearchableSelect
        items={vehicles}
        value=""
        onValueChange={vi.fn()}
      />
    );

    fireEvent.click(screen.getByRole('combobox'));
    expect(screen.getByPlaceholderText('Tìm theo biển số...')).toBeDefined();
  });

  it('tự động hiển thị placeholder theo TÊN khi đối tượng không có mã nhưng có trường name', () => {
    const companies = Array.from({ length: 12 }, (_, i) => ({
      id: `comp-${i}`,
      name: `Công ty ${i}`,
      address: `Địa chỉ ${i}`,
    }));

    render(
      <SearchableSelect
        items={companies}
        value=""
        onValueChange={vi.fn()}
      />
    );

    fireEvent.click(screen.getByRole('combobox'));
    expect(screen.getByPlaceholderText('Tìm theo tên...')).toBeDefined();
  });

  it('gọi onValueChange khi click chọn một phần tử', () => {
    const onSelect = vi.fn();
    const items = [
      { id: 'item-1', code: 'C1', name: 'Phần tử 1' },
      { id: 'item-2', code: 'C2', name: 'Phần tử 2' },
    ];

    render(
      <SearchableSelect
        items={items}
        value=""
        onValueChange={onSelect}
      />
    );

    fireEvent.click(screen.getByRole('combobox'));
    fireEvent.click(screen.getByText('Phần tử 2'));

    expect(onSelect).toHaveBeenCalledWith('item-2');
  });
});
