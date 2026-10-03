import { DEFAULT_PAGE_SIZE } from '@/constants/pagination';
import type { PagedResult } from '@/types/masterData';

/**
 * Tạo kết quả phân trang rỗng chuẩn mực cho fallback của TanStack Query và trạng thái khởi tạo.
 */
export function createEmptyPagedResult<T>(pageSize: number = DEFAULT_PAGE_SIZE): PagedResult<T> {
  return {
    items: [],
    pagination: {
      pageIndex: 1,
      pageSize,
      totalCount: 0,
      totalPages: 0,
      hasPreviousPage: false,
      hasNextPage: false,
    },
  };
}
