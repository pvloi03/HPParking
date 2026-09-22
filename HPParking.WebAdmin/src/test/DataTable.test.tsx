import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { DataTable, type ColumnDef } from '@/components/common/DataTable';

interface MockItem {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
}

const mockColumns: ColumnDef<MockItem>[] = [
  { header: 'Mã', accessorKey: 'code' },
  { header: 'Tên', accessorKey: 'name' },
];

const mockPagination = {
  pageIndex: 1,
  pageSize: 10,
  totalCount: 20,
  totalPages: 2,
  hasPreviousPage: false,
  hasNextPage: true,
};

describe('DataTable Component', () => {
  const defaultProps = {
    data: [
      { id: '1', code: 'C01', name: 'Công ty Alpha', isActive: true },
      { id: '2', code: 'C02', name: 'Công ty Beta', isActive: false },
    ],
    columns: mockColumns,
    pagination: mockPagination,
    onPageChange: vi.fn(),
    searchKeyword: '',
    onSearchChange: vi.fn(),
    statusFilter: 'all' as const,
    onStatusFilterChange: vi.fn(),
  };

  it('hiển thị đầy đủ tiêu đề cột và các dòng dữ liệu', () => {
    render(<DataTable {...defaultProps} />);

    // Kiểm tra headers (có thể xuất hiện ở desktop và mobile cards)
    expect(screen.getAllByText('Mã').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Tên').length).toBeGreaterThan(0);

    // Kiểm tra dữ liệu dòng
    expect(screen.getAllByText('Công ty Alpha').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Công ty Beta').length).toBeGreaterThan(0);
  });

  it('gọi onSearchChange khi người dùng nhập từ khóa tìm kiếm', () => {
    const onSearchChange = vi.fn();
    render(<DataTable {...defaultProps} onSearchChange={onSearchChange} />);

    const searchInput = screen.getByPlaceholderText(/tìm kiếm/i);
    fireEvent.change(searchInput, { target: { value: 'Alpha' } });

    expect(onSearchChange).toHaveBeenCalledWith('Alpha');
  });

  it('gọi onStatusFilterChange khi chọn lọc trạng thái', () => {
    const onStatusFilterChange = vi.fn();
    render(<DataTable {...defaultProps} onStatusFilterChange={onStatusFilterChange} />);

    const activeBtn = screen.getByRole('button', { name: /đang hoạt động/i });
    fireEvent.click(activeBtn);

    expect(onStatusFilterChange).toHaveBeenCalledWith(true);
  });

  it('hỗ trợ chuyển trang tiếp theo khi click nút Trang sau', () => {
    const onPageChange = vi.fn();
    render(<DataTable {...defaultProps} onPageChange={onPageChange} />);

    const nextBtn = screen.getByRole('button', { name: /trang sau/i });
    fireEvent.click(nextBtn);

    expect(onPageChange).toHaveBeenCalledWith(2);
  });

  it('hiển thị trạng thái rỗng khi không có bản ghi nào', () => {
    render(
      <DataTable
        {...defaultProps}
        data={[]}
        pagination={{ ...mockPagination, totalCount: 0, totalPages: 0, hasNextPage: false }}
        emptyTitle="Không có công ty nào"
      />
    );

    expect(screen.getByText('Không có công ty nào')).toBeInTheDocument();
  });

  it('kích hoạt các callback thao tác Sửa và Xóa khi click nút', () => {
    const onEdit = vi.fn();
    const onDelete = vi.fn();

    render(
      <DataTable
        {...defaultProps}
        actions={{ onEdit, onDelete }}
      />
    );

    const editBtns = screen.getAllByRole('button', { name: /chỉnh sửa/i });
    fireEvent.click(editBtns[0]);
    expect(onEdit).toHaveBeenCalledWith(defaultProps.data[0]);

    const deleteBtns = screen.getAllByRole('button', { name: /xóa/i });
    fireEvent.click(deleteBtns[0]);
    expect(onDelete).toHaveBeenCalledWith(defaultProps.data[0]);
  });
});
