import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import React from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { InfiniteSearchableSelect } from '@/components/ui/infinite-searchable-select';
import type { PagedResult } from '@/types/masterData';

interface MockItem {
  id: string;
  name: string;
  code: string;
}

function renderWithClient(ui: React.ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
      },
    },
  });
  return render(
    <QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>
  );
}

describe('InfiniteSearchableSelect', () => {
  it('renders trigger with placeholder initially and displays items when opened', async () => {
    const mockFetch = vi.fn().mockResolvedValue({
      items: [
        { id: 'item-1', name: 'Nguyễn Văn A', code: 'NV001' },
        { id: 'item-2', name: 'Trần Thị B', code: 'NV002' },
      ],
      pagination: {
        pageIndex: 1,
        pageSize: 20,
        totalCount: 2,
        totalPages: 1,
        hasNextPage: false,
        hasPreviousPage: false,
      },
    } as PagedResult<MockItem>);

    const handleValueChange = vi.fn();

    renderWithClient(
      <InfiniteSearchableSelect<MockItem>
        queryKey={['test-mock-items-1']}
        fetchFn={mockFetch}
        placeholder="-- Chọn nhân sự --"
        value=""
        onValueChange={handleValueChange}
      />
    );

    // Placeholder displayed on trigger
    expect(screen.getByText('-- Chọn nhân sự --')).toBeInTheDocument();

    // Click trigger to open dropdown
    const trigger = screen.getByRole('combobox');
    fireEvent.click(trigger);

    // Expect items to appear in dropdown
    await waitFor(() => {
      expect(screen.getByText('Nguyễn Văn A')).toBeInTheDocument();
      expect(screen.getByText('Trần Thị B')).toBeInTheDocument();
    });
  });

  it('displays pre-selected item label correctly on trigger', async () => {
    const mockFetch = vi.fn().mockResolvedValue({
      items: [
        { id: 'item-1', name: 'Xe 29A-12345', code: '29A-12345' },
      ],
      pagination: {
        pageIndex: 1,
        pageSize: 20,
        totalCount: 1,
        totalPages: 1,
        hasNextPage: false,
        hasPreviousPage: false,
      },
    } as PagedResult<MockItem>);

    renderWithClient(
      <InfiniteSearchableSelect<MockItem>
        queryKey={['test-mock-items-2']}
        fetchFn={mockFetch}
        value="item-1"
        onValueChange={() => {}}
      />
    );

    await waitFor(() => {
      expect(screen.getByText('Xe 29A-12345')).toBeInTheDocument();
    });
  });

  it('renders sentinel indicator when hasNextPage is true', async () => {
    const mockFetch = vi.fn().mockResolvedValue({
      items: [
        { id: 'item-1', name: 'Tuyến 1', code: 'T01' },
      ],
      pagination: {
        pageIndex: 1,
        pageSize: 1,
        totalCount: 5,
        totalPages: 5,
        hasNextPage: true,
        hasPreviousPage: false,
      },
    } as PagedResult<MockItem>);

    renderWithClient(
      <InfiniteSearchableSelect<MockItem>
        queryKey={['test-mock-items-3']}
        fetchFn={mockFetch}
        pageSize={1}
        value=""
        onValueChange={() => {}}
      />
    );

    const trigger = screen.getByRole('combobox');
    fireEvent.click(trigger);

    await waitFor(() => {
      expect(screen.getByText(/Cuộn xuống để tải thêm/i)).toBeInTheDocument();
    });
  });

  it('supports allowClear to reset value to empty string', async () => {
    const mockFetch = vi.fn().mockResolvedValue({
      items: [
        { id: 'item-1', name: 'Tuyến 1', code: 'T01' },
      ],
      pagination: {
        pageIndex: 1,
        pageSize: 1,
        totalCount: 1,
        totalPages: 1,
        hasNextPage: false,
        hasPreviousPage: false,
      },
    } as PagedResult<MockItem>);

    const handleValueChange = vi.fn();

    renderWithClient(
      <InfiniteSearchableSelect<MockItem>
        queryKey={['test-mock-items-4']}
        fetchFn={mockFetch}
        value="item-1"
        onValueChange={handleValueChange}
        allowClear={true}
        clearLabel="-- Bỏ chọn tuyến --"
      />
    );

    const trigger = screen.getByRole('combobox');
    fireEvent.click(trigger);

    await waitFor(() => {
      expect(screen.getByText('-- Bỏ chọn tuyến --')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByText('-- Bỏ chọn tuyến --'));
    expect(handleValueChange).toHaveBeenCalledWith('');
  });
});
