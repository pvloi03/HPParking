import React from 'react';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Input } from '@/components/ui/input';
import { Search } from 'lucide-react';
import { useSelectSearch, DEFAULT_SELECT_SEARCH_THRESHOLD } from '@/lib/select-search';
import { cn } from '@/lib/utils';

export interface SearchableSelectProps<T = any> {
  /** Danh sách dữ liệu nguồn */
  items: T[];
  /** Giá trị đang được chọn (value của item) */
  value?: string;
  /** Hàm callback khi người dùng chọn một phần tử */
  onValueChange: (value: string) => void;
  /** Placeholder hiển thị trên ô chọn khi chưa chọn gì */
  placeholder?: string;
  /** Placeholder cho ô tìm kiếm (nếu không truyền sẽ tự động tạo theo trường nhận diện) */
  searchPlaceholder?: string;
  /** Chỉ định trường cụ thể để tìm kiếm (nếu không truyền sẽ tự động nhận diện: mã -> tên...) */
  searchField?: keyof T & string;
  /** Hàm trích xuất giá trị `value` định danh (mặc định lấy `item.id` hoặc `item.value`) */
  getValue?: (item: T) => string;
  /** Hàm trích xuất nhãn hiển thị đơn giản (mặc định lấy `name`, `title`, `label`...) */
  getLabel?: (item: T) => React.ReactNode;
  /** Hàm render tùy biến chi tiết cho từng dòng Item trong dropdown */
  renderItem?: (item: T) => React.ReactNode;
  /** Vô hiệu hóa select */
  disabled?: boolean;
  /** ClassName bao bọc ngoài cùng */
  className?: string;
  /** ClassName cho nút SelectTrigger */
  triggerClassName?: string;
  /** ClassName cho khung SelectContent */
  contentClassName?: string;
  /** Ngưỡng hiển thị ô tìm kiếm (mặc định là 10 phần tử) */
  threshold?: number;
  /** Cho phép tùy chọn xóa/bỏ chọn */
  allowClear?: boolean;
  /** Nhãn cho tùy chọn bỏ chọn */
  clearLabel?: string;
  /** Giá trị khi bỏ chọn (mặc định là chuỗi rỗng '') */
  clearValue?: string;
  /** Thông báo khi không tìm thấy kết quả */
  emptyMessage?: string;
}

/**
 * Component Select dùng chung thông minh:
 * - Tự động đếm số lượng phần tử: nếu > 10 sẽ hiển thị ô tìm kiếm
 * - Tự động nhận diện trường tìm kiếm theo "MÃ" (code, plateNumber, cardNumber...).
 * - Nếu không có mã, tự động tìm theo trường có độ nhận diện lớn nhất ngoài ID (name, fullName, title...).
 */
export function SearchableSelect<T = any>({
  items = [],
  value,
  onValueChange,
  placeholder = '-- Chọn giá trị --',
  searchPlaceholder,
  searchField,
  getValue,
  getLabel,
  renderItem,
  disabled = false,
  className,
  triggerClassName,
  contentClassName,
  threshold = DEFAULT_SELECT_SEARCH_THRESHOLD,
  allowClear = false,
  clearLabel = '-- Chưa chọn (Bỏ chọn) --',
  clearValue = '',
  emptyMessage = 'Không tìm thấy kết quả phù hợp',
}: SearchableSelectProps<T>) {
  const {
    search,
    setSearch,
    filteredItems,
    showSearch,
    placeholder: autoSearchPlaceholder,
  } = useSelectSearch(items, {
    threshold,
    searchField,
    customPlaceholder: searchPlaceholder,
  });

  // Trích xuất value
  const resolveValue = (item: any): string => {
    if (getValue) return getValue(item);
    if (item && typeof item === 'object') {
      return String(item.id ?? item.value ?? item.code ?? '');
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

  return (
    <div className={cn('relative w-full', className)}>
      <Select
        value={value ?? ''}
        onValueChange={(val) => {
          onValueChange(val);
          // Reset tìm kiếm khi chọn xong
          setSearch('');
        }}
        disabled={disabled}
      >
        <SelectTrigger className={cn('text-xs h-9 cursor-pointer', triggerClassName)}>
          <SelectValue placeholder={placeholder} />
        </SelectTrigger>

        <SelectContent searchable={false} className={cn('max-h-60', contentClassName)}>
          {/* Ô TÌM KIẾM: Tự động hiển thị khi danh sách > threshold (mặc định 10 phần tử) */}
          {showSearch && (
            <div className="p-1.5 border-b border-border sticky top-0 bg-popover z-10">
              <div className="relative">
                <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted-foreground pointer-events-none" />
                <Input
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  placeholder={autoSearchPlaceholder}
                  className="pl-8 h-7 text-xs bg-background"
                  autoFocus
                  onClick={(e) => e.stopPropagation()}
                  onPointerDown={(e) => e.stopPropagation()}
                  onMouseDown={(e) => e.stopPropagation()}
                  onKeyDown={(e) => e.stopPropagation()}
                />
              </div>
            </div>
          )}

          {/* Tùy chọn bỏ chọn (nếu bật allowClear) */}
          {allowClear && (
            <SelectItem value={clearValue} className="text-xs text-muted-foreground">
              {clearLabel}
            </SelectItem>
          )}

          {/* Trạng thái không có kết quả */}
          {filteredItems.length === 0 ? (
            <div className="py-4 text-center text-xs text-muted-foreground">
              {emptyMessage}
            </div>
          ) : (
            filteredItems.map((item, idx) => {
              const itemVal = resolveValue(item);
              return (
                <SelectItem key={`${itemVal}-${idx}`} value={itemVal} className="text-xs py-1.5">
                  {renderItem ? renderItem(item) : resolveLabel(item)}
                </SelectItem>
              );
            })
          )}
        </SelectContent>
      </Select>
    </div>
  );
}
