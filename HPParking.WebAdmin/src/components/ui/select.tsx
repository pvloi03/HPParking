import * as React from 'react';
import * as SelectPrimitive from '@radix-ui/react-select';
import { Check, ChevronDown, ChevronUp, Search } from 'lucide-react';
import { cn } from '@/lib/utils';
import {
  shouldShowSelectSearch,
  getSelectSearchField,
  normalizeVietnamese,
  DEFAULT_SELECT_SEARCH_THRESHOLD,
} from '@/lib/select-search';

const Select = SelectPrimitive.Root;
const SelectGroup = SelectPrimitive.Group;
const SelectValue = SelectPrimitive.Value;

const SelectTrigger = React.forwardRef<
  React.ComponentRef<typeof SelectPrimitive.Trigger>,
  React.ComponentPropsWithoutRef<typeof SelectPrimitive.Trigger>
>(({ className, children, ...props }, ref) => (
  <SelectPrimitive.Trigger
    ref={ref}
    className={cn(
      'flex h-9 w-full items-center justify-between rounded-lg border border-input bg-card px-3 py-2 text-xs shadow-2xs ring-offset-background placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 [&>span]:line-clamp-1 cursor-pointer transition-colors hover:border-accent-foreground/30',
      className
    )}
    {...props}
  >
    {children}
    <SelectPrimitive.Icon asChild>
      <ChevronDown className="h-4 w-4 opacity-50 shrink-0 ml-1" />
    </SelectPrimitive.Icon>
  </SelectPrimitive.Trigger>
));
SelectTrigger.displayName = SelectPrimitive.Trigger.displayName;

const SelectScrollUpButton = React.forwardRef<
  React.ComponentRef<typeof SelectPrimitive.ScrollUpButton>,
  React.ComponentPropsWithoutRef<typeof SelectPrimitive.ScrollUpButton>
>(({ className, ...props }, ref) => (
  <SelectPrimitive.ScrollUpButton
    ref={ref}
    className={cn(
      'flex cursor-default items-center justify-center py-1',
      className
    )}
    {...props}
  >
    <ChevronUp className="h-4 w-4" />
  </SelectPrimitive.ScrollUpButton>
));
SelectScrollUpButton.displayName = SelectPrimitive.ScrollUpButton.displayName;

const SelectScrollDownButton = React.forwardRef<
  React.ComponentRef<typeof SelectPrimitive.ScrollDownButton>,
  React.ComponentPropsWithoutRef<typeof SelectPrimitive.ScrollDownButton>
>(({ className, ...props }, ref) => (
  <SelectPrimitive.ScrollDownButton
    ref={ref}
    className={cn(
      'flex cursor-default items-center justify-center py-1',
      className
    )}
    {...props}
  >
    <ChevronDown className="h-4 w-4" />
  </SelectPrimitive.ScrollDownButton>
));
SelectScrollDownButton.displayName =
  SelectPrimitive.ScrollDownButton.displayName;

/** Trích xuất text từ ReactNode để lọc tìm kiếm */
function extractNodeText(node: React.ReactNode): string {
  if (node === null || node === undefined || typeof node === 'boolean') return '';
  if (typeof node === 'string' || typeof node === 'number') return String(node);
  if (Array.isArray(node)) return node.map(extractNodeText).join('');
  if (React.isValidElement(node) && node.props) {
    const p = node.props as any;
    let s = '';
    if (p['data-code']) s += ' ' + p['data-code'];
    if (p['data-search']) s += ' ' + p['data-search'];
    if (p.children) s += ' ' + extractNodeText(p.children);
    return s;
  }
  return '';
}

export interface SelectContentProps
  extends React.ComponentPropsWithoutRef<typeof SelectPrimitive.Content> {
  /**
   * Bật hoặc tắt tính năng tìm kiếm (mặc định 'auto': tự động bật nếu số lượng item > searchThreshold, mặc định 10)
   */
  searchable?: boolean | 'auto';
  /** Ngưỡng số phần tử kích hoạt ô tìm kiếm (mặc định là 10) */
  searchThreshold?: number;
  /** Tùy biến placeholder cho ô tìm kiếm */
  searchPlaceholder?: string;
  /** Mảng dữ liệu nguồn (tùy chọn) để tự động nhận diện trường tìm kiếm theo mã hoặc trường nhận diện cao nhất */
  items?: any[];
}

const SelectContent = React.forwardRef<
  React.ComponentRef<typeof SelectPrimitive.Content>,
  SelectContentProps
>(
  (
    {
      className,
      children,
      position = 'popper',
      searchable = 'auto',
      searchThreshold = DEFAULT_SELECT_SEARCH_THRESHOLD,
      searchPlaceholder,
      items,
      ...props
    },
    ref
  ) => {
    const [searchQuery, setSearchQuery] = React.useState('');

    // Reset tìm kiếm khi popover đóng (unmount)
    React.useEffect(() => {
      return () => setSearchQuery('');
    }, []);

    // Phân tích danh sách children thành mảng
    const childrenArray = React.useMemo(() => React.Children.toArray(children), [children]);
    const totalCount = items?.length ?? childrenArray.length;

    // Kiểm tra điều kiện > 10 phần tử
    const isSearchActive =
      searchable !== false &&
      (searchable === true || shouldShowSelectSearch(totalCount, searchThreshold));

    // Xác định placeholder thông minh (theo mã hoặc trường nhận diện lớn nhất)
    const computedPlaceholder = React.useMemo(() => {
      if (searchPlaceholder) return searchPlaceholder;
      if (items && items.length > 0) {
        return getSelectSearchField(items).placeholder;
      }
      return 'Tìm theo mã hoặc tên...';
    }, [searchPlaceholder, items]);

    // Lọc children khi người dùng nhập từ khóa
    const renderedChildren = React.useMemo(() => {
      if (!isSearchActive || !searchQuery.trim()) {
        return children;
      }
      const q = normalizeVietnamese(searchQuery);

      const filterElement = (node: React.ReactNode): React.ReactNode | null => {
        if (!React.isValidElement(node)) return node;

        // Giữ lại Separator nếu cần
        if (node.type === SelectSeparator) return node;

        // Nếu là SelectGroup hoặc Fragment, lọc các con bên trong nó
        if (node.type === SelectGroup || node.type === React.Fragment) {
          const rawChildren = React.Children.toArray((node.props as any).children);
          const filteredSub = rawChildren
            .map((c) => filterElement(c))
            .filter((c): c is React.ReactNode => c !== null);

          // Kiểm tra xem trong sub-children có item nào (không tính label hoặc separator)
          const hasActionableItems = filteredSub.some(
            (c) =>
              React.isValidElement(c) &&
              c.type !== SelectLabel &&
              c.type !== SelectSeparator
          );
          if (!hasActionableItems) return null;

          return React.cloneElement(node, {}, filteredSub);
        }

        // Nếu là SelectLabel
        if (node.type === SelectLabel) return node;

        // Xử lý SelectItem thông thường
        const childProps = node.props as any;
        const text = normalizeVietnamese(extractNodeText(node));
        const val = normalizeVietnamese(String(childProps?.value ?? ''));
        if (text.includes(q) || val.includes(q)) {
          return node;
        }
        return null;
      };

      const filtered = childrenArray
        .map((child) => filterElement(child))
        .filter((c): c is React.ReactNode => c !== null);

      if (filtered.length === 0) {
        return (
          <div className="py-4 text-center text-xs text-muted-foreground">
            Không tìm thấy kết quả phù hợp
          </div>
        );
      }

      return filtered;
    }, [children, childrenArray, isSearchActive, searchQuery]);

    const searchInputRef = React.useRef<HTMLInputElement>(null);
    const isInputFocusedRef = React.useRef(false);

    React.useEffect(() => {
      if (isSearchActive) {
        const timer = setTimeout(() => {
          if (searchInputRef.current) {
            const active = document.activeElement;
            const isRadixTarget =
              !active ||
              active === document.body ||
              active.getAttribute('role') === 'listbox' ||
              active.hasAttribute('data-radix-select-viewport');

            if (isInputFocusedRef.current || searchQuery || isRadixTarget) {
              searchInputRef.current.focus({ preventScroll: true });
              isInputFocusedRef.current = true;
            }
          }
        }, 10);
        return () => clearTimeout(timer);
      }
    }, [renderedChildren, isSearchActive, searchQuery]);

    return (
      <SelectPrimitive.Portal>
        <SelectPrimitive.Content
          ref={ref}
          className={cn(
            'relative z-50 max-h-96 min-w-[8rem] overflow-hidden rounded-xl border border-border bg-popover text-popover-foreground shadow-lg data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-95 data-[side=bottom]:slide-in-from-top-2 data-[side=left]:slide-in-from-right-2 data-[side=right]:slide-in-from-left-2 data-[side=top]:slide-in-from-bottom-2',
            position === 'popper' &&
            'data-[side=bottom]:translate-y-1 data-[side=left]:-translate-x-1 data-[side=right]:translate-x-1 data-[side=top]:-translate-y-1',
            className
          )}
          position={position}
          onCloseAutoFocus={(e) => {
            if (isSearchActive) {
              e.preventDefault();
              searchInputRef.current?.focus({ preventScroll: true });
            }
            props.onCloseAutoFocus?.(e);
          }}
          {...props}
        >
          {/* Ô TÌM KIẾM TỰ ĐỘNG KHI DANH SÁCH > 10 PHẦN TỬ */}
          {isSearchActive && (
            <div className="p-1.5 border-b border-border sticky top-0 bg-popover z-10">
              <div className="relative">
                <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted-foreground pointer-events-none" />
                <input
                  ref={searchInputRef}
                  type="text"
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  placeholder={computedPlaceholder}
                  className="flex h-7 w-full rounded-md border border-input bg-background pl-8 pr-2 text-xs ring-offset-background placeholder:text-muted-foreground focus:outline-none focus:ring-1 focus:ring-ring"
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
                        if (searchInputRef.current) {
                          searchInputRef.current.focus({ preventScroll: true });
                          isInputFocusedRef.current = true;
                        }
                      });
                      return;
                    }
                    isInputFocusedRef.current = false;
                  }}
                />
              </div>
            </div>
          )}

          <SelectScrollUpButton />
          <SelectPrimitive.Viewport
            className={cn(
              'p-1',
              position === 'popper' &&
              'h-[var(--radix-select-trigger-height)] w-full min-w-[var(--radix-select-trigger-width)]'
            )}
          >
            {renderedChildren}
          </SelectPrimitive.Viewport>
          <SelectScrollDownButton />
        </SelectPrimitive.Content>
      </SelectPrimitive.Portal>
    );
  }
);
SelectContent.displayName = SelectPrimitive.Content.displayName;

const SelectLabel = React.forwardRef<
  React.ComponentRef<typeof SelectPrimitive.Label>,
  React.ComponentPropsWithoutRef<typeof SelectPrimitive.Label>
>(({ className, ...props }, ref) => (
  <SelectPrimitive.Label
    ref={ref}
    className={cn('py-1.5 pl-8 pr-2 text-xs font-semibold text-muted-foreground', className)}
    {...props}
  />
));
SelectLabel.displayName = SelectPrimitive.Label.displayName;

const SelectItem = React.forwardRef<
  React.ComponentRef<typeof SelectPrimitive.Item>,
  React.ComponentPropsWithoutRef<typeof SelectPrimitive.Item>
>(({ className, children, ...props }, ref) => (
  <SelectPrimitive.Item
    ref={ref}
    className={cn(
      'relative flex w-full cursor-pointer select-none items-center rounded-lg py-1.5 pl-8 pr-2 text-xs outline-none focus:bg-accent focus:text-accent-foreground data-[disabled]:pointer-events-none data-[disabled]:opacity-50 transition-colors',
      className
    )}
    {...props}
  >
    <span className="absolute left-2 flex h-3.5 w-3.5 items-center justify-center">
      <SelectPrimitive.ItemIndicator>
        <Check className="h-3.5 w-3.5 text-blue-600 dark:text-blue-400" />
      </SelectPrimitive.ItemIndicator>
    </span>

    <SelectPrimitive.ItemText>{children}</SelectPrimitive.ItemText>
  </SelectPrimitive.Item>
));
SelectItem.displayName = SelectPrimitive.Item.displayName;

const SelectSeparator = React.forwardRef<
  React.ComponentRef<typeof SelectPrimitive.Separator>,
  React.ComponentPropsWithoutRef<typeof SelectPrimitive.Separator>
>(({ className, ...props }, ref) => (
  <SelectPrimitive.Separator
    ref={ref}
    className={cn('-mx-1 my-1 h-px bg-muted', className)}
    {...props}
  />
));
SelectSeparator.displayName = SelectPrimitive.Separator.displayName;

/**
 * Ô tìm kiếm độc lập có thể chèn thủ công vào SelectContent khi cần tùy biến layout
 */
export const SelectSearchInput = React.forwardRef<
  HTMLInputElement,
  React.InputHTMLAttributes<HTMLInputElement>
>(({ className, placeholder = 'Tìm kiếm...', ...props }, ref) => (
  <div className="p-1.5 border-b border-border sticky top-0 bg-popover z-10">
    <div className="relative">
      <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted-foreground pointer-events-none" />
      <input
        ref={ref}
        type="text"
        placeholder={placeholder}
        className={cn(
          'flex h-7 w-full rounded-md border border-input bg-background pl-8 pr-2 text-xs ring-offset-background placeholder:text-muted-foreground focus:outline-none focus:ring-1 focus:ring-ring',
          className
        )}
        onClick={(e) => e.stopPropagation()}
        onKeyDown={(e) => e.stopPropagation()}
        {...props}
      />
    </div>
  </div>
));
SelectSearchInput.displayName = 'SelectSearchInput';

export {
  Select,
  SelectGroup,
  SelectValue,
  SelectTrigger,
  SelectContent,
  SelectLabel,
  SelectItem,
  SelectSeparator,
  SelectScrollUpButton,
  SelectScrollDownButton,
};
