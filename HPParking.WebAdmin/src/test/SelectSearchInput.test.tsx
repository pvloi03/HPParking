import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import {
  Select,
  SelectTrigger,
  SelectValue,
  SelectContent,
  SelectItem,
} from '@/components/ui/select';
import { InfiniteSearchableSelect } from '@/components/ui/infinite-searchable-select';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

describe('Select Search Input & Filtering Tests', () => {
  it('SelectContent AutoSearch: Lọc chính xác danh sách item theo từ khóa', () => {
    render(
      <Select>
        <SelectTrigger>
          <SelectValue placeholder="Chọn cổng" />
        </SelectTrigger>
        <SelectContent>
          {Array.from({ length: 15 }, (_, i) => (
            <SelectItem key={i} value={`gate-${i}`}>
              Cổng số {i}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    );

    fireEvent.click(screen.getByRole('combobox'));
    const input = screen.getByPlaceholderText('Tìm theo mã hoặc tên...');
    expect(input).toBeDefined();

    // Gõ tìm kiếm "Cổng số 14"
    fireEvent.change(input, { target: { value: 'Cổng số 14' } });

    expect(screen.getByText('Cổng số 14')).toBeDefined();
    expect(screen.queryByText('Cổng số 0')).toBeNull();
    expect(screen.queryByText('Cổng số 1')).toBeNull();
  });

  it('InfiniteSearchableSelect: Khi tìm kiếm, chỉ hiển thị kết quả từ server, không hiển thị item preselected không khớp', async () => {
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    const mockClients = [
      { id: 'c1', name: 'Nguyễn Văn An' },
      { id: 'c2', name: 'Trần Thị Bích' },
      { id: 'c3', name: 'Lê Văn Cường' },
    ];

    const fetchFn = vi.fn().mockImplementation((params) => {
      const q = params.search || params.keyword;
      if (q) {
        const filtered = mockClients.filter((c) => c.name.includes(q));
        return Promise.resolve({
          items: filtered,
          totalCount: filtered.length,
          pageIndex: 1,
          pageSize: 20,
        });
      }
      return Promise.resolve({
        items: mockClients,
        totalCount: mockClients.length,
        pageIndex: 1,
        pageSize: 20,
      });
    });

    render(
      <QueryClientProvider client={queryClient}>
        <InfiniteSearchableSelect
          queryKey={['test-infinite-search']}
          fetchFn={fetchFn}
          selectedItems={mockClients}
          onValueChange={vi.fn()}
        />
      </QueryClientProvider>
    );

    fireEvent.click(screen.getByRole('combobox'));
    const input = screen.getByPlaceholderText(/tìm/i);

    // Gõ tìm kiếm "Bích"
    fireEvent.change(input, { target: { value: 'Bích' } });

    // Chờ debounce 300ms
    await new Promise((r) => setTimeout(r, 400));

    expect(screen.getByText('Trần Thị Bích')).toBeDefined();
    expect(screen.queryByText('Nguyễn Văn An')).toBeNull();
  });
});
