import { useState, useEffect, useMemo, useCallback } from 'react';
import { useInfiniteQuery, useQuery } from '@tanstack/react-query';
import type { PagedResult } from '@/types/masterData';

export interface UseInfiniteSelectQueryOptions<T, TFilter = Record<string, any>> {
  /** Khóa truy vấn gốc cho TanStack Query (ví dụ: ['vehicles', 'select']) */
  queryKey: unknown[];
  /** Hàm gọi API trả về kết quả phân trang PagedResult<T> */
  fetchFn: (
    params: {
      pageIndex: number;
      pageSize: number;
      search?: string;
      keyword?: string;
    } & TFilter
  ) => Promise<PagedResult<T>>;
  /** Hàm tùy chọn để lấy chi tiết 1 item theo ID khi item đó chưa có trong trang 1 */
  fetchById?: (id: string | number) => Promise<T | null | undefined>;
  /** Kích thước mỗi trang (mặc định: 20) */
  pageSize?: number;
  /** Thời gian trễ debounce tìm kiếm tính bằng mili-giây (mặc định: 300ms) */
  debounceMs?: number;
  /** Từ khóa tìm kiếm điều khiển từ bên ngoài (nếu có) */
  search?: string;
  /** Callback thông báo khi từ khóa tìm kiếm thay đổi */
  onSearchChange?: (search: string) => void;
  /** ID của phần tử đang được chọn trước (dành cho single select) */
  selectedId?: string | number | null;
  /** Danh sách ID các phần tử đang được chọn (dành cho multi select) */
  selectedIds?: Array<string | number>;
  /** Các đối tượng phần tử đã chọn sẵn có từ trước (nếu caller đã có sẵn) */
  selectedItems?: T[];
  /** Hàm trích xuất ID duy nhất của một phần tử (mặc định lấy `id`, `code`, hoặc `value`) */
  getItemId?: (item: T) => string | number;
  /** Các bộ lọc bổ sung truyền vào API */
  filters?: TFilter;
  /** Có kích hoạt query hay không (mặc định: true) */
  enabled?: boolean;
}

export interface UseInfiniteSelectQueryResult<T> {
  /** Danh sách phẳng các phần tử đã nạp qua các trang (đã merge pre-selected và khử trùng lặp) */
  items: T[];
  /** Tổng số bản ghi trên server */
  totalCount: number;
  /** Đang tải trang đầu tiên */
  isLoading: boolean;
  /** Đang tải thêm trang tiếp theo khi cuộn */
  isFetchingNextPage: boolean;
  /** Còn trang tiếp theo để tải không */
  hasNextPage: boolean;
  /** Kích hoạt tải trang tiếp theo */
  fetchNextPage: () => void;
  /** Lỗi nếu có */
  error: unknown;
  /** Đang tìm kiếm hoặc refetch */
  isFetching: boolean;
  /** Từ khóa tìm kiếm hiện tại */
  search: string;
  /** Hàm cập nhật từ khóa tìm kiếm */
  setSearch: (value: string) => void;
  /** Từ khóa tìm kiếm sau debounce */
  debouncedSearch: string;
  /** Tải lại toàn bộ dữ liệu từ trang 1 */
  refetch: () => void;
}

const defaultGetItemId = (item: any): string | number => {
  if (!item || typeof item !== 'object') return String(item);
  return item.id ?? item.code ?? item.value ?? JSON.stringify(item);
};

const EMPTY_ARRAY: any[] = [];

export function useInfiniteSelectQuery<T, TFilter = Record<string, any>>({
  queryKey,
  fetchFn,
  fetchById,
  pageSize = 20,
  debounceMs = 300,
  search: externalSearch,
  onSearchChange,
  selectedId,
  selectedIds: _selectedIds,
  selectedItems: initialSelectedItems = EMPTY_ARRAY,
  getItemId = defaultGetItemId,
  filters,
  enabled = true,
}: UseInfiniteSelectQueryOptions<T, TFilter>): UseInfiniteSelectQueryResult<T> {
  // Quản lý từ khóa tìm kiếm
  const [internalSearch, setInternalSearch] = useState(externalSearch ?? '');

  useEffect(() => {
    if (externalSearch !== undefined) {
      setInternalSearch(externalSearch);
    }
  }, [externalSearch]);

  const search = externalSearch !== undefined ? externalSearch : internalSearch;

  const setSearch = useCallback(
    (value: string) => {
      setInternalSearch(value);
      onSearchChange?.(value);
    },
    [onSearchChange]
  );

  // Debounce từ khóa tìm kiếm
  const [debouncedSearch, setDebouncedSearch] = useState(search);

  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(search);
    }, debounceMs);
    return () => clearTimeout(timer);
  }, [search, debounceMs]);

  // Infinite query gọi API theo trang
  const infiniteQuery = useInfiniteQuery({
    queryKey: [...queryKey, debouncedSearch, filters, pageSize],
    queryFn: async ({ pageParam = 1 }) => {
      const queryParams = {
        pageIndex: pageParam as number,
        pageSize,
        search: debouncedSearch ? debouncedSearch.trim() : undefined,
        keyword: debouncedSearch ? debouncedSearch.trim() : undefined,
        ...(filters as any),
      };
      return fetchFn(queryParams);
    },
    initialPageParam: 1,
    getNextPageParam: (lastPage) => {
      if (lastPage?.pagination) {
        return lastPage.pagination.hasNextPage
          ? lastPage.pagination.pageIndex + 1
          : undefined;
      }
      if (typeof (lastPage as any)?.hasNextPage === 'boolean') {
        return (lastPage as any).hasNextPage
          ? ((lastPage as any).pageIndex ?? 1) + 1
          : undefined;
      }
      return undefined;
    },
    enabled,
  });

  // Gom các items từ tất cả các trang đã tải
  const pagedItems = useMemo(() => {
    if (!infiniteQuery.data?.pages) return EMPTY_ARRAY;
    return infiniteQuery.data.pages.flatMap((page) => page.items ?? []);
  }, [infiniteQuery.data?.pages]);

  // Kiểm tra xem selectedId đã có trong danh sách trang hay chưa
  const isSelectedIdLoaded = useMemo(() => {
    if (!selectedId) return true;
    return pagedItems.some(
      (item) => String(getItemId(item)) === String(selectedId)
    );
  }, [pagedItems, selectedId, getItemId]);

  // Nếu selectedId chưa có và caller truyền fetchById -> Nạp riêng item đó (chỉ kích hoạt sau khi trang 1 nạp xong và xác nhận không có)
  const isInitialLoading = infiniteQuery.isLoading;
  const shouldFetchById = Boolean(
    enabled && selectedId && fetchById && !isInitialLoading && !isSelectedIdLoaded
  );

  const { data: fetchedItem } = useQuery({
    queryKey: ['single-item', queryKey[0], selectedId],
    queryFn: async () => {
      if (!selectedId || !fetchById) return null;
      const res = await fetchById(selectedId);
      return res ?? null;
    },
    enabled: shouldFetchById,
    staleTime: 5 * 60 * 1000,
  });

  // Kết hợp và khử trùng lặp các items (pre-selected items đưa lên trước nếu chưa có)
  const items = useMemo(() => {
    const itemMap = new Map<string | number, T>();

    // 1. Nếu có preselected items từ props
    for (const item of initialSelectedItems) {
      if (item) {
        itemMap.set(getItemId(item), item);
      }
    }

    // 2. Nếu có item fetch riêng theo selectedId
    if (fetchedItem) {
      itemMap.set(getItemId(fetchedItem), fetchedItem);
    }

    // 3. Đưa các items nạp từ server vào map
    for (const item of pagedItems) {
      if (item) {
        itemMap.set(getItemId(item), item);
      }
    }

    return Array.from(itemMap.values());
  }, [initialSelectedItems, fetchedItem, pagedItems, getItemId]);

  // Lấy tổng số bản ghi từ metadata trang đầu tiên
  const totalCount = useMemo(() => {
    const firstPage = infiniteQuery.data?.pages?.[0];
    if (firstPage?.pagination?.totalCount !== undefined) {
      return firstPage.pagination.totalCount;
    }
    if ((firstPage as any)?.totalCount !== undefined) {
      return (firstPage as any).totalCount;
    }
    return items.length;
  }, [infiniteQuery.data?.pages, items.length]);

  return {
    items,
    totalCount,
    isLoading: infiniteQuery.isLoading,
    isFetchingNextPage: infiniteQuery.isFetchingNextPage,
    hasNextPage: Boolean(infiniteQuery.hasNextPage),
    fetchNextPage: infiniteQuery.fetchNextPage,
    error: infiniteQuery.error,
    isFetching: infiniteQuery.isFetching,
    search,
    setSearch,
    debouncedSearch,
    refetch: infiniteQuery.refetch,
  };
}
