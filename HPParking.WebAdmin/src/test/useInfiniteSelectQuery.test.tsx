import { describe, it, expect, vi } from 'vitest';
import { renderHook, waitFor, act } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import React from 'react';
import { useInfiniteSelectQuery } from '@/hooks/useInfiniteSelectQuery';
import type { PagedResult } from '@/types/masterData';

interface TestItem {
  id: string;
  name: string;
}

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
      },
    },
  });
  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
}

describe('useInfiniteSelectQuery', () => {
  it('loads page 1 initially and reports totalCount', async () => {
    const mockFetch = vi.fn().mockResolvedValue({
      items: [
        { id: '1', name: 'Item 1' },
        { id: '2', name: 'Item 2' },
      ],
      pagination: {
        pageIndex: 1,
        pageSize: 2,
        totalCount: 4,
        totalPages: 2,
        hasNextPage: true,
        hasPreviousPage: false,
      },
    } as PagedResult<TestItem>);

    const { result } = renderHook(
      () =>
        useInfiniteSelectQuery<TestItem>({
          queryKey: ['test-items-1'],
          fetchFn: mockFetch,
          pageSize: 2,
        }),
      { wrapper: createWrapper() }
    );

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
      expect(result.current.items).toHaveLength(2);
    });

    expect(result.current.items[0]).toEqual({ id: '1', name: 'Item 1' });
    expect(result.current.totalCount).toBe(4);
    expect(result.current.hasNextPage).toBe(true);
    expect(mockFetch).toHaveBeenCalledWith(
      expect.objectContaining({
        pageIndex: 1,
        pageSize: 2,
      })
    );
  });

  it('fetches next page when fetchNextPage is invoked', async () => {
    const mockFetch = vi
      .fn()
      .mockResolvedValueOnce({
        items: [{ id: '1', name: 'Item 1' }],
        pagination: {
          pageIndex: 1,
          pageSize: 1,
          totalCount: 2,
          totalPages: 2,
          hasNextPage: true,
          hasPreviousPage: false,
        },
      } as PagedResult<TestItem>)
      .mockResolvedValueOnce({
        items: [{ id: '2', name: 'Item 2' }],
        pagination: {
          pageIndex: 2,
          pageSize: 1,
          totalCount: 2,
          totalPages: 2,
          hasNextPage: false,
          hasPreviousPage: true,
        },
      } as PagedResult<TestItem>);

    const { result } = renderHook(
      () =>
        useInfiniteSelectQuery<TestItem>({
          queryKey: ['test-items-2'],
          fetchFn: mockFetch,
          pageSize: 1,
        }),
      { wrapper: createWrapper() }
    );

    await waitFor(() => {
      expect(result.current.items).toHaveLength(1);
    });

    act(() => {
      result.current.fetchNextPage();
    });

    await waitFor(() => {
      expect(result.current.items).toHaveLength(2);
      expect(result.current.hasNextPage).toBe(false);
    });

    expect(result.current.items[1]).toEqual({ id: '2', name: 'Item 2' });
  });

  it('prepends pre-selected item using fetchById if not found in page 1', async () => {
    const mockFetch = vi.fn().mockResolvedValue({
      items: [{ id: '1', name: 'Item 1' }],
      pagination: {
        pageIndex: 1,
        pageSize: 1,
        totalCount: 10,
        totalPages: 10,
        hasNextPage: true,
        hasPreviousPage: false,
      },
    } as PagedResult<TestItem>);

    const mockFetchById = vi.fn().mockResolvedValue({
      id: '99',
      name: 'Preselected Item 99',
    });

    const { result } = renderHook(
      () =>
        useInfiniteSelectQuery<TestItem>({
          queryKey: ['test-items-3'],
          fetchFn: mockFetch,
          selectedId: '99',
          fetchById: mockFetchById,
          pageSize: 1,
        }),
      { wrapper: createWrapper() }
    );

    await waitFor(() => {
      expect(result.current.items.some((it) => it.id === '99')).toBe(true);
    });

    expect(mockFetchById).toHaveBeenCalledWith('99');
    expect(result.current.items[0]).toEqual({
      id: '99',
      name: 'Preselected Item 99',
    });
    expect(result.current.items).toHaveLength(2);
  });

  it('does not duplicate preselected item if it already exists in loaded pages', async () => {
    const mockFetch = vi.fn().mockResolvedValue({
      items: [
        { id: '1', name: 'Item 1' },
        { id: '2', name: 'Item 2' },
      ],
      pagination: {
        pageIndex: 1,
        pageSize: 2,
        totalCount: 2,
        totalPages: 1,
        hasNextPage: false,
        hasPreviousPage: false,
      },
    } as PagedResult<TestItem>);

    const mockFetchById = vi.fn();

    const { result } = renderHook(
      () =>
        useInfiniteSelectQuery<TestItem>({
          queryKey: ['test-items-4'],
          fetchFn: mockFetch,
          selectedId: '2',
          fetchById: mockFetchById,
          pageSize: 2,
        }),
      { wrapper: createWrapper() }
    );

    await waitFor(() => {
      expect(result.current.items).toHaveLength(2);
    });

    // fetchById should not be called because id 2 is already in page 1
    expect(mockFetchById).not.toHaveBeenCalled();
    expect(result.current.items.map((i) => i.id)).toEqual(['1', '2']);
  });

  it('debounces search input and sends search/keyword parameter to fetchFn', async () => {
    const mockFetch = vi.fn().mockResolvedValue({
      items: [{ id: '10', name: 'Matched Item' }],
      pagination: {
        pageIndex: 1,
        pageSize: 10,
        totalCount: 1,
        totalPages: 1,
        hasNextPage: false,
        hasPreviousPage: false,
      },
    } as PagedResult<TestItem>);

    const { result } = renderHook(
      () =>
        useInfiniteSelectQuery<TestItem>({
          queryKey: ['test-items-5'],
          fetchFn: mockFetch,
          debounceMs: 50,
        }),
      { wrapper: createWrapper() }
    );

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    act(() => {
      result.current.setSearch('xe cong');
    });

    await waitFor(() => {
      expect(result.current.debouncedSearch).toBe('xe cong');
    });

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledWith(
        expect.objectContaining({
          search: 'xe cong',
          keyword: 'xe cong',
        })
      );
    });
  });

  it('excludes non-matching initialSelectedItems when searching and restores them when search cleared', async () => {
    const initial = [{ id: 'pre-1', name: 'Preselected Old Item' }];
    const searchResult = [{ id: 's-1', name: 'Found New Item' }];

    const mockFetch = vi.fn().mockImplementation((params) => {
      if (params.search === 'new') {
        return Promise.resolve({
          items: searchResult,
          pagination: {
            pageIndex: 1,
            pageSize: 10,
            totalCount: 1,
            totalPages: 1,
            hasNextPage: false,
            hasPreviousPage: false,
          },
        });
      }
      return Promise.resolve({
        items: [{ id: '1', name: 'Default Item' }],
        pagination: {
          pageIndex: 1,
          pageSize: 10,
          totalCount: 1,
          totalPages: 1,
          hasNextPage: false,
          hasPreviousPage: false,
        },
      });
    });

    const { result } = renderHook(
      () =>
        useInfiniteSelectQuery<TestItem>({
          queryKey: ['test-items-search-filter'],
          fetchFn: mockFetch,
          selectedItems: initial,
          debounceMs: 50,
        }),
      { wrapper: createWrapper() }
    );

    await waitFor(() => {
      expect(result.current.items.map((i) => i.id)).toContain('pre-1');
    });

    // Bắt đầu tìm kiếm
    act(() => {
      result.current.setSearch('new');
    });

    await waitFor(() => {
      expect(result.current.debouncedSearch).toBe('new');
    });

    // Khi đang tìm kiếm: danh sách CHỈ chứa kết quả tìm kiếm (Found New Item), không có Preselected Old Item
    await waitFor(() => {
      expect(result.current.items.map((i) => i.id)).toEqual(['s-1']);
    });
    expect(result.current.items.map((i) => i.id)).not.toContain('pre-1');

    // Xóa tìm kiếm
    act(() => {
      result.current.setSearch('');
    });

    await waitFor(() => {
      expect(result.current.debouncedSearch).toBe('');
    });

    // Sau khi xóa tìm kiếm: preselected item lại được phục hồi
    await waitFor(() => {
      expect(result.current.items.map((i) => i.id)).toContain('pre-1');
    });
  });
});
