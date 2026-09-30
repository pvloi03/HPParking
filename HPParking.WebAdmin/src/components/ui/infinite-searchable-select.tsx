import React, { useState, useMemo, useRef, useEffect } from 'react';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Input } from '@/components/ui/input';
import { Search, Loader2 } from 'lucide-react';
import { getSelectSearchField } from '@/lib/select-search';
import { useInfiniteSelectQuery } from '@/hooks/useInfiniteSelectQuery';
import { useIntersectionSentinel } from '@/hooks/useIntersectionSentinel';
import { cn } from '@/lib/utils';
import type { PagedResult } from '@/types/masterData';

const CLEAR_SENTINEL_VALUE = '__infinite_select_clear__';

export interface InfiniteSearchableSelectProps<T = any, TFilter = Record<string, any>> {
  /** Giá trị đang được chọn (ID hoặc value của item) */
  value?: string | number | null;
  /** Hàm callback khi người dùng chọn một phần tử */
  onValueChange: (value: string) => void;
  /** Placeholder hiển thị trên ô chọn khi chưa chọn gì */
  placeholder?: string;
  /** Placeholder cho ô tìm kiếm (nếu không truyền sẽ tự động sinh theo trường nhận diện mã/tên) */
  searchPlaceholder?: string;
  /** Vô hiệu hóa select */
  disabled?: boolean;
  /** ClassName bao bọc ngoài cùng */
  className?: string;
  /** ClassName cho nút SelectTrigger */
  triggerClassName?: string;
  /** ClassName cho khung SelectContent */
  contentClassName?: string;

  /** Khóa truy vấn cho TanStack Query (ví dụ: ['clients', 'select']) */
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
  /** Hàm lấy chi tiết một item theo ID khi item đó chưa có ở trang 1 */
  fetchById?: (id: string | number) => Promise<T | null | undefined>;
  /** Kích thước mỗi trang khi cuộn (mặc định: 20) */
  pageSize?: number;
  /** Các bộ lọc phụ truyền thêm vào API */
  filters?: TFilter;
  /** Hàm trích xuất giá trị `value` định danh (mặc định lấy `item.id`, `item.code`, hoặc `item.value`) */
  getValue?: (item: T) => string;
  /** Hàm trích xuất nhãn hiển thị đơn giản (mặc định lấy `name`, `title`, `label`, `plateNumber`, `code`...) */
  getLabel?: (item: T) => React.ReactNode;
  /** Hàm render tùy biến chi tiết cho từng dòng Item trong dropdown */
  renderItem?: (item: T) => React.ReactNode;
  /** Danh sách các đối tượng đã chọn sẵn từ trước (dành cho edit form khi caller có sẵn object) */
  selectedItems?: T[];

  /** Cho phép tùy chọn xóa/bỏ chọn */
  allowClear?: boolean;
  /** Nhãn cho tùy chọn bỏ chọn */
  clearLabel?: string;
  /** Thông báo khi không tìm thấy kết quả */
  emptyMessage?: string;
  /** ID cho phần tử HTML nếu cần */
  id?: string;
}

/**
 * Component Select nạp dữ liệu từng phần theo cuộn (Server-side Infinite Scroll):
 * - Tự động tải trang đầu tiên (20 items).
 * - Khi cuộn xuống gần đáy, IntersectionObserver tự động nạp tiếp trang tiếp theo.
 * - Ô tìm kiếm debounce 300ms gửi thẳng từ khóa lên server để tìm kiếm toàn bộ CSDL.
 * - Tự động bảo toàn và gộp item đã chọn (Pre-selected) lên đầu danh sách để SelectTrigger hiển thị đúng nhãn.
 */
export function InfiniteSearchableSelect<T = any, TFilter = Record<string, any>>({
  value,
  onValueChange,
  placeholder = '-- Chọn giá trị --',
  searchPlaceholder,
  disabled = false,
  className,
  triggerClassName,
  contentClassName,
  queryKey,
  fetchFn,
  fetchById,
  pageSize = 20,
  filters,
  getValue,
  getLabel,
  renderItem,
  selectedItems,
  allowClear = false,
  clearLabel = '-- Chưa chọn (Bỏ chọn) --',
  emptyMessage = 'Không tìm thấy kết quả phù hợp',
  id,
}: InfiniteSearchableSelectProps<T, TFilter>) {
  const [isOpen, setIsOpen] = useState(false);

  // Hook nạp dữ liệu server-side infinite scroll
  const {
    items,
    isLoading,
    isFetchingNextPage,
    hasNextPage,
    fetchNextPage,
    search,
    setSearch,
    isFetching,
    totalCount,
  } = useInfiniteSelectQuery<T, TFilter>({
    queryKey,
    fetchFn,
    fetchById,
    pageSize,
    selectedId: value,
    selectedItems,
    getItemId: getValue,
    filters,
    enabled: true,
  });

  // Observer theo dõi phần tử sentinel ở đáy danh sách
  const { sentinelRef } = useIntersectionSentinel({
    enabled: isOpen && hasNextPage && !isFetchingNextPage,
    onIntersect: fetchNextPage,
    rootMargin: '60px',
  });

  // Trích xuất value
  const resolveValue = (item: any): string => {
    if (getValue) return getValue(item);
    if (item && typeof item === 'object') {
      return String(item.id ?? item.code ?? item.value ?? '');
    }
    return String(item ?? '');
  };

  // Trích xuất label
  const resolveLabel = (item: any): React.ReactNode => {
    if (getLabel) return getLabel(item);
    if (item && typeof item === 'object') {
      return (
        item.name ??
        item.fullName ??
        item.title ??
        item.label ??
        item.plateNumber ??
        item.cardNumber ??
        item.code ??
        String(item.id ?? '')
      );
    }
    return String(item ?? '');
  };

  // Xác định placeholder cho ô tìm kiếm
  const computedSearchPlaceholder = useMemo(() => {
    if (searchPlaceholder) return searchPlaceholder;
    if (items.length > 0) {
      return getSelectSearchField(items).placeholder;
    }
    return 'Nhập từ khóa tìm kiếm...';
  }, [searchPlaceholder, items]);

  const inputRef = useRef<HTMLInputElement>(null);
  const isInputFocusedRef = useRef(false);

  // Giữ vững focus trên ô tìm kiếm khi API trả về kết quả mới hoặc dropdown mở
  useEffect(() => {
    if (isOpen) {
      const timer = setTimeout(() => {
        if (inputRef.current) {
          const active = document.activeElement;
          const isRadixTarget =
            !active ||
            active === document.body ||
            active.getAttribute('role') === 'listbox' ||
            active.hasAttribute('data-radix-select-viewport');

          if (isInputFocusedRef.current || search || isRadixTarget) {
            inputRef.current.focus({ preventScroll: true });
            isInputFocusedRef.current = true;
          }
        }
      }, 10);
      return () => clearTimeout(timer);
    } else {
      isInputFocusedRef.current = false;
    }
  }, [items, isOpen, isFetching, search]);

  const currentValue = value !== null && value !== undefined ? String(value) : '';

  return (
    <div className={cn('relative w-full', className)} id={id}>
      <Select
        value={currentValue}
        onValueChange={(val) => {
          if (val === CLEAR_SENTINEL_VALUE) {
            onValueChange('');
          } else {
            onValueChange(val);
          }
          setSearch('');
        }}
        open={isOpen}
        onOpenChange={(open) => {
          setIsOpen(open);
          if (!open) {
            setSearch('');
          }
        }}
        disabled={disabled}
      >
        <SelectTrigger className={cn('text-xs h-9 cursor-pointer', triggerClassName)}>
          <SelectValue placeholder={placeholder} />
        </SelectTrigger>

        <SelectContent
          searchable={false}
          className={cn('max-h-72 overflow-y-auto', contentClassName)}
        >
          {/* Ô TÌM KIẾM TRỰC TIẾP LÊN SERVER */}
          <div className="p-1.5 border-b border-border sticky top-0 bg-popover z-10 shadow-sm">
            <div className="relative">
              <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted-foreground pointer-events-none" />
              <Input
                ref={inputRef}
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder={computedSearchPlaceholder}
                className="pl-8 pr-8 h-7 text-xs bg-background"
                autoFocus
                onClick={(e) => e.stopPropagation()}
                onPointerDown={(e) => e.stopPropagation()}
                onMouseDown={(e) => e.stopPropagation()}
                onKeyDown={(e) => e.stopPropagation()}
                onFocus={() => {
                  isInputFocusedRef.current = true;
                }}
                onBlur={(e) => {
                  const related = e.relatedTarget as HTMLElement | null;
                  if (
                    related &&
                    (related.getAttribute('role') === 'listbox' ||
                      related.hasAttribute('data-radix-select-viewport'))
                  ) {
                    requestAnimationFrame(() => {
                      if (isOpen && inputRef.current) {
                        inputRef.current.focus({ preventScroll: true });
                        isInputFocusedRef.current = true;
                      }
                    });
                    return;
                  }
                  isInputFocusedRef.current = false;
                }}
              />
              {isFetching && !isFetchingNextPage && (
                <Loader2 className="absolute right-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted-foreground animate-spin pointer-events-none" />
              )}
            </div>
          </div>

          {/* Tùy chọn bỏ chọn (nếu bật allowClear) */}
          {allowClear && (
            <SelectItem value={CLEAR_SENTINEL_VALUE} className="text-xs text-muted-foreground italic">
              {clearLabel}
            </SelectItem>
          )}

          {/* TRẠNG THÁI NẠP BAN ĐẦU */}
          {isLoading && (
            <div className="py-6 flex flex-col items-center justify-center gap-2 text-xs text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin text-primary" />
              <span>Đang tải dữ liệu...</span>
            </div>
          )}

          {/* TRẠNG THÁI KHÔNG TÌM THẤY KẾT QUẢ */}
          {!isLoading && items.length === 0 && (
            <div className="py-4 text-center text-xs text-muted-foreground">
              {emptyMessage}
            </div>
          )}

          {/* DANH SÁCH CÁC MỤC ĐÃ NẠP */}
          {items.map((item, idx) => {
            const itemVal = resolveValue(item);
            return (
              <SelectItem key={`${itemVal}-${idx}`} value={itemVal} className="text-xs py-1.5">
                {renderItem ? renderItem(item) : resolveLabel(item)}
              </SelectItem>
            );
          })}

          {/* PHẦN TỬ SENTINEL KÍCH HOẠT NẠP THÊM TRANG TIẾP THEO KHI CUỘN ĐẾN */}
          {hasNextPage && (
            <div
              ref={sentinelRef}
              className="py-2.5 flex items-center justify-center text-xs text-muted-foreground border-t border-border/40"
            >
              {isFetchingNextPage ? (
                <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
                  <Loader2 className="h-3.5 w-3.5 animate-spin text-primary" />
                  <span>Đang tải thêm...</span>
                </div>
              ) : (
                <span className="text-[11px] text-muted-foreground/70">
                  Cuộn xuống để tải thêm ({items.length}/{totalCount})
                </span>
              )}
            </div>
          )}

          {/* THÔNG BÁO ĐÃ NẠP HẾT */}
          {!hasNextPage && items.length > 0 && !isLoading && (
            <div className="py-1 text-center text-[10px] text-muted-foreground/50 border-t border-border/20">
              Đã hiển thị toàn bộ ({items.length} mục)
            </div>
          )}
        </SelectContent>
      </Select>
    </div>
  );
}
