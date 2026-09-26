import {
  Search,
  Plus,
  Trash2,
  RotateCcw,
  Edit,
  FileText,
  ChevronLeft,
  ChevronRight,
  Inbox,
  Download,
  Upload,
  ShieldAlert,
} from 'lucide-react';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { Checkbox } from '@/components/ui/checkbox';
import { cn } from '@/lib/utils';
import type { PaginationMetadata } from '@/types/masterData';

export interface ColumnDef<T> {
  header: string;
  accessorKey?: keyof T;
  cell?: (item: T) => React.ReactNode;
  className?: string;
  /** Tiêu đề ngắn gọn hiển thị trên card di động */
  mobileLabel?: string;
  /** Ẩn cột này trên giao diện desktop nếu cần */
  hiddenOnDesktop?: boolean;
}

export interface DataTableActions<T> {
  onView?: (item: T) => void;
  onEdit?: (item: T) => void;
  onDelete?: (item: T) => void;
  onHardDelete?: (item: T) => void;
  onRestore?: (item: T) => void;
  canView?: (item: T) => boolean;
  canEdit?: (item: T) => boolean;
  canDelete?: (item: T) => boolean;
  canHardDelete?: (item: T) => boolean;
  canRestore?: (item: T) => boolean;
}

interface DataTableProps<T> {
  data: T[];
  columns: ColumnDef<T>[];
  pagination: PaginationMetadata;
  onPageChange: (pageIndex: number) => void;
  isLoading?: boolean;
  searchKeyword: string;
  onSearchChange: (keyword: string) => void;
  searchPlaceholder?: string;
  statusFilter?: boolean | 'all';
  onStatusFilterChange?: (status: boolean | 'all') => void;
  isTrashMode?: boolean;
  onTrashModeToggle?: () => void;
  onAddNew?: () => void;
  addNewLabel?: string;
  onImportExcel?: () => void;
  onExportExcel?: () => void;
  isExportingExcel?: boolean;
  actions?: DataTableActions<T>;
  extraFilters?: React.ReactNode;
  emptyTitle?: string;
  emptyDescription?: string;
  renderMobileCard?: (item: T, actionButtons: React.ReactNode) => React.ReactNode;
  selectable?: boolean;
  selectedRowIds?: string[];
  onSelectedRowIdsChange?: (ids: string[]) => void;
  bulkActions?: React.ReactNode;
}

export function DataTable<T extends { id: string; isActive?: boolean }>({
  data,
  columns,
  pagination,
  onPageChange,
  isLoading = false,
  searchKeyword,
  onSearchChange,
  searchPlaceholder = 'Tìm kiếm từ khóa...',
  statusFilter = 'all',
  onStatusFilterChange,
  isTrashMode = false,
  onTrashModeToggle,
  onAddNew,
  addNewLabel = 'Thêm mới',
  onImportExcel,
  onExportExcel,
  isExportingExcel = false,
  actions,
  extraFilters,
  emptyTitle = 'Không tìm thấy dữ liệu',
  emptyDescription = 'Không có bản ghi nào phù hợp với bộ lọc hiện tại.',
  renderMobileCard,
  selectable = false,
  selectedRowIds = [],
  onSelectedRowIdsChange,
  bulkActions,
}: DataTableProps<T>) {
  const hasActions = Boolean(actions?.onView || actions?.onEdit || actions?.onDelete || actions?.onHardDelete || actions?.onRestore);

  // Logic chọn tất cả / chọn một phần checkbox
  const isAllSelected = data.length > 0 && data.every((item) => selectedRowIds.includes(item.id));
  const isIndeterminate = !isAllSelected && data.some((item) => selectedRowIds.includes(item.id));

  const handleToggleAll = (checked: boolean) => {
    if (!onSelectedRowIdsChange) return;
    if (checked) {
      const pageIds = data.map((d) => d.id);
      const merged = Array.from(new Set([...selectedRowIds, ...pageIds]));
      onSelectedRowIdsChange(merged);
    } else {
      const pageIdSet = new Set(data.map((d) => d.id));
      const remaining = selectedRowIds.filter((id) => !pageIdSet.has(id));
      onSelectedRowIdsChange(remaining);
    }
  };

  const handleToggleRow = (id: string, checked: boolean) => {
    if (!onSelectedRowIdsChange) return;
    if (checked) {
      onSelectedRowIdsChange([...selectedRowIds, id]);
    } else {
      onSelectedRowIdsChange(selectedRowIds.filter((item) => item !== id));
    }
  };

  // Render các nút hành động cho một dòng/thẻ
  const renderActionButtons = (item: T) => {
    if (!hasActions) return null;

    if (isTrashMode) {
      return (
        <div className="flex items-center gap-1.5 justify-end">
          {actions?.onRestore && (actions.canRestore ? actions.canRestore(item) : true) && (
            <Button
              variant="outline"
              size="sm"
              onClick={() => actions.onRestore?.(item)}
              className="h-8 px-2.5 text-xs text-blue-600 hover:text-blue-700 hover:bg-blue-50 dark:hover:bg-blue-950/50 border-blue-200 dark:border-blue-900 cursor-pointer min-h-[36px]"
              title="Khôi phục bản ghi"
              aria-label="Khôi phục bản ghi"
            >
              <RotateCcw className="h-3.5 w-3.5 mr-1 text-blue-600" />
              <span>Khôi phục</span>
            </Button>
          )}
          {actions?.onHardDelete && (actions.canHardDelete ? actions.canHardDelete(item) : true) && (
            <Button
              variant="ghost"
              size="sm"
              onClick={() => actions.onHardDelete?.(item)}
              className="h-8 px-2 text-xs text-destructive hover:bg-destructive/10 cursor-pointer min-h-[36px]"
              title="Xóa vĩnh viễn khỏi CSDL (Xóa cứng)"
              aria-label="Xóa vĩnh viễn"
            >
              <Trash2 className="h-3.5 w-3.5 mr-1 text-destructive" />
              <span>Xóa vĩnh viễn</span>
            </Button>
          )}
        </div>
      );
    }

    return (
      <div className="flex items-center gap-1 justify-end">
        {actions?.onView && (actions.canView ? actions.canView(item) : true) && (
          <Button
            variant="ghost"
            size="sm"
            onClick={() => actions.onView?.(item)}
            className="h-8 w-8 p-0 text-muted-foreground hover:text-blue-600 hover:bg-blue-50 dark:hover:bg-blue-950/50 cursor-pointer min-h-[36px] min-w-[36px]"
            title="Xem chi tiết"
            aria-label="Xem chi tiết"
          >
            <FileText className="h-4 w-4" />
          </Button>
        )}
        {actions?.onEdit && (actions.canEdit ? actions.canEdit(item) : true) && (
          <Button
            variant="ghost"
            size="sm"
            onClick={() => actions.onEdit?.(item)}
            className="h-8 w-8 p-0 text-muted-foreground hover:text-foreground hover:bg-accent cursor-pointer min-h-[36px] min-w-[36px]"
            title="Chỉnh sửa thông tin"
            aria-label="Chỉnh sửa"
          >
            <Edit className="h-4 w-4" />
          </Button>
        )}
        {actions?.onDelete && (actions.canDelete ? actions.canDelete(item) : true) && (
          <Button
            variant="ghost"
            size="sm"
            onClick={() => actions.onDelete?.(item)}
            className="h-8 w-8 p-0 text-muted-foreground hover:text-amber-600 hover:bg-amber-500/10 cursor-pointer min-h-[36px] min-w-[36px]"
            title="Xóa vào thùng rác (Xóa mềm)"
            aria-label="Xóa mềm"
          >
            <Trash2 className="h-4 w-4 text-amber-600/80 hover:text-amber-600" />
          </Button>
        )}
        {actions?.onHardDelete && (actions.canHardDelete ? actions.canHardDelete(item) : true) && (
          <Button
            variant="ghost"
            size="sm"
            onClick={() => actions.onHardDelete?.(item)}
            className="h-8 w-8 p-0 text-muted-foreground hover:text-destructive hover:bg-destructive/10 cursor-pointer min-h-[36px] min-w-[36px]"
            title="Xóa vĩnh viễn khỏi CSDL (Xóa cứng)"
            aria-label="Xóa cứng"
          >
            <ShieldAlert className="h-4 w-4 text-destructive/80 hover:text-destructive" />
          </Button>
        )}
      </div>
    );
  };

  return (
    <div className="space-y-3.5">
      {/* TOOLBAR CHIA 2 DÒNG HOÀN TOÀN TÁCH BIỆT */}
      <div className="p-3.5 rounded-xl border border-border bg-card shadow-xs space-y-3">
        {/* DÒNG 1: TẤT CẢ BỘ LỌC TÌM KIẾM, ĐƠN VỊ VÀ TRẠNG THÁI */}
        <div className="flex flex-wrap items-center gap-2.5 w-full">
          {/* Ô tìm kiếm */}
          <div className="relative flex-1 min-w-[220px] max-w-sm">
            <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted-foreground pointer-events-none" />
            <Input
              value={searchKeyword}
              onChange={(e) => onSearchChange(e.target.value)}
              placeholder={searchPlaceholder}
              className="pl-8 h-9 text-xs bg-background"
            />
          </div>

          {/* Bộ lọc bổ sung (Công ty, Phòng ban, v.v.) */}
          {extraFilters}

          {/* Bộ lọc trạng thái hoạt động */}
          {onStatusFilterChange && !isTrashMode && (
            <div className="flex items-center rounded-lg border border-border bg-background p-0.5 text-xs shadow-2xs">
              <button
                type="button"
                onClick={() => onStatusFilterChange('all')}
                className={cn(
                  'px-2.5 py-1 rounded-md transition-colors cursor-pointer text-xs font-medium',
                  statusFilter === 'all'
                    ? 'bg-muted text-foreground font-semibold shadow-2xs'
                    : 'text-muted-foreground hover:text-foreground'
                )}
              >
                Tất cả
              </button>
              <button
                type="button"
                onClick={() => onStatusFilterChange(true)}
                className={cn(
                  'px-2.5 py-1 rounded-md transition-colors cursor-pointer text-xs font-medium',
                  statusFilter === true
                    ? 'bg-emerald-600 text-white font-semibold shadow-2xs'
                    : 'text-muted-foreground hover:text-foreground'
                )}
              >
                Đang hoạt động
              </button>
              <button
                type="button"
                onClick={() => onStatusFilterChange(false)}
                className={cn(
                  'px-2.5 py-1 rounded-md transition-colors cursor-pointer text-xs font-medium',
                  statusFilter === false
                    ? 'bg-amber-600 text-white font-semibold shadow-2xs'
                    : 'text-muted-foreground hover:text-foreground'
                )}
              >
                Ngừng
              </button>
            </div>
          )}
        </div>

        {/* DÒNG 2: THAO TÁC HÀNG LOẠT (BÊN TRÁI) & CÁC NÚT HÀNH ĐỘNG CHÍNH (BÊN PHẢI) */}
        <div className="flex flex-wrap items-center justify-between gap-2.5 w-full pt-2 border-t border-border/60">
          {/* Bên trái: Bulk Actions khi chọn Checkbox hoặc đếm số lượng */}
          <div className="flex items-center gap-2 flex-wrap min-h-[36px]">
            {selectable && selectedRowIds.length > 0 ? (
              <div className="flex items-center gap-2 bg-blue-50/90 dark:bg-blue-950/50 border border-blue-200 dark:border-blue-900 px-3 py-1 rounded-lg">
                <span className="text-xs font-semibold text-blue-700 dark:text-blue-300">
                  Đã chọn {selectedRowIds.length} mục
                </span>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => onSelectedRowIdsChange?.([])}
                  className="h-6 px-1.5 text-[11px] text-muted-foreground hover:text-foreground cursor-pointer"
                >
                  Bỏ chọn
                </Button>
                {bulkActions}
              </div>
            ) : (
              <span className="text-xs text-muted-foreground font-medium">
                Tổng cộng: <strong className="text-foreground font-semibold">{pagination.totalCount}</strong> bản ghi
              </span>
            )}
          </div>

          {/* Bên phải: Nhóm nút hành động chính */}
          <div className="flex items-center gap-2 shrink-0">
            {onExportExcel && !isTrashMode && (
              <Button
                variant="outline"
                size="sm"
                onClick={onExportExcel}
                disabled={isExportingExcel}
                className="h-9 gap-1.5 text-xs cursor-pointer min-h-[36px]"
                title="Xuất dữ liệu ra Excel"
              >
                <Download className="h-3.5 w-3.5 text-emerald-600" />
                <span>Xuất Excel</span>
              </Button>
            )}

            {onImportExcel && !isTrashMode && (
              <Button
                variant="outline"
                size="sm"
                onClick={onImportExcel}
                className="h-9 gap-1.5 text-xs cursor-pointer min-h-[36px]"
                title="Nhập dữ liệu từ tệp Excel"
              >
                <Upload className="h-3.5 w-3.5 text-emerald-600" />
                <span>Nhập Excel</span>
              </Button>
            )}

            {onTrashModeToggle && (
              <Button
                variant={isTrashMode ? 'secondary' : 'outline'}
                size="sm"
                onClick={onTrashModeToggle}
                className={cn(
                  'h-9 gap-1.5 text-xs cursor-pointer min-h-[36px]',
                  isTrashMode &&
                  'bg-amber-100 text-amber-900 hover:bg-amber-200 dark:bg-amber-950 dark:text-amber-200 font-semibold border-amber-300'
                )}
              >
                <Trash2 className="h-3.5 w-3.5" />
                <span>{isTrashMode ? 'Thùng rác (Đang xem)' : 'Thùng rác'}</span>
              </Button>
            )}

            {onAddNew && !isTrashMode && (
              <Button
                size="sm"
                onClick={onAddNew}
                className="h-9 gap-1.5 text-xs bg-blue-600 hover:bg-blue-700 text-white font-medium cursor-pointer shadow-xs min-h-[36px]"
              >
                <Plus className="h-4 w-4" />
                <span>{addNewLabel}</span>
              </Button>
            )}
          </div>
        </div>
      </div>

      {/* CHẾ ĐỘ THÙNG RÁC BANNER */}
      {isTrashMode && (
        <div className="flex items-center justify-between p-3 rounded-xl bg-amber-50 dark:bg-amber-950/40 border border-amber-200 dark:border-amber-900 text-amber-800 dark:text-amber-300 text-xs">
          <div className="flex items-center gap-2">
            <Trash2 className="h-4 w-4 shrink-0 text-amber-600" />
            <span>
              Đang hiển thị danh sách trong <strong>Thùng rác</strong>. Bạn có thể khôi phục bản ghi bất kỳ lúc nào.
            </span>
          </div>
          {onTrashModeToggle && (
            <Button
              variant="ghost"
              size="sm"
              onClick={onTrashModeToggle}
              className="h-7 text-xs font-semibold text-amber-900 dark:text-amber-200 hover:bg-amber-200/60 dark:hover:bg-amber-900/60 cursor-pointer"
            >
              Quay lại danh sách chính
            </Button>
          )}
        </div>
      )}

      {/* DATA VIEW (HYBRID RESPONSIVE: DESKTOP TABLE & MOBILE CARDS) */}
      {isLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 5 }).map((_, i) => (
            <div
              key={i}
              className="p-4 rounded-xl border border-border bg-card flex items-center justify-between"
            >
              <div className="space-y-2 flex-1">
                <Skeleton className="h-4 w-1/3" />
                <Skeleton className="h-3 w-1/2" />
              </div>
              <Skeleton className="h-8 w-20" />
            </div>
          ))}
        </div>
      ) : data.length === 0 ? (
        <div className="flex flex-col items-center justify-center p-12 text-center rounded-2xl border border-dashed border-border bg-card/50">
          <div className="h-12 w-12 rounded-full bg-muted flex items-center justify-center text-muted-foreground mb-3">
            <Inbox className="h-6 w-6" />
          </div>
          <h3 className="text-sm font-semibold text-foreground mb-1">{emptyTitle}</h3>
          <p className="text-xs text-muted-foreground max-w-sm mb-4">{emptyDescription}</p>
          {(searchKeyword || statusFilter !== 'all') && (
            <Button
              variant="outline"
              size="sm"
              onClick={() => {
                onSearchChange('');
                onStatusFilterChange?.('all');
              }}
              className="text-xs h-8 cursor-pointer"
            >
              Xóa bộ lọc
            </Button>
          )}
        </div>
      ) : (
        <>
          {/* DESKTOP / TABLET TABLE (>= 640px) */}
          <div className="hidden sm:block rounded-xl border border-border bg-card overflow-hidden shadow-2xs">
            <div className="overflow-x-auto">
              <table className="w-full text-left text-xs">
                <thead className="border-b border-border bg-muted/40 font-semibold text-muted-foreground">
                  <tr>
                    {selectable && (
                      <th className="w-10 px-3 py-3 text-center">
                        <Checkbox
                          checked={isAllSelected}
                          indeterminate={isIndeterminate}
                          onCheckedChange={handleToggleAll}
                          aria-label="Chọn tất cả trang này"
                        />
                      </th>
                    )}
                    {columns
                      .filter((col) => !col.hiddenOnDesktop)
                      .map((col, idx) => (
                        <th key={idx} className={cn('px-4 py-3', col.className)}>
                          {col.header}
                        </th>
                      ))}
                    {hasActions && (
                      <th className="px-4 py-3 text-right">Thao tác</th>
                    )}
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {data.map((item) => (
                    <tr
                      key={item.id}
                      className={cn(
                        'hover:bg-muted/30 transition-colors group',
                        selectable && selectedRowIds.includes(item.id) && 'bg-blue-50/40 dark:bg-blue-950/20'
                      )}
                    >
                      {selectable && (
                        <td
                          className="w-10 px-3 py-3 text-center"
                          onClick={(e) => e.stopPropagation()}
                        >
                          <Checkbox
                            checked={selectedRowIds.includes(item.id)}
                            onCheckedChange={(chk) => handleToggleRow(item.id, Boolean(chk))}
                            aria-label={`Chọn dòng ${item.id}`}
                          />
                        </td>
                      )}
                      {columns
                        .filter((col) => !col.hiddenOnDesktop)
                        .map((col, idx) => (
                          <td key={idx} className={cn('px-4 py-3 text-foreground', col.className)}>
                            {col.cell
                              ? col.cell(item)
                              : col.accessorKey
                                ? String(item[col.accessorKey] ?? '—')
                                : '—'}
                          </td>
                        ))}
                      {hasActions && (
                        <td className="px-4 py-3 text-right">
                          {renderActionButtons(item)}
                        </td>
                      )}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          {/* MOBILE STACKED CARDS (< 640px) */}
          <div className="sm:hidden space-y-3">
            {data.map((item) => {
              if (renderMobileCard) {
                return (
                  <div key={item.id}>
                    {renderMobileCard(item, renderActionButtons(item))}
                  </div>
                );
              }

              // Fallback default mobile card
              return (
                <div
                  key={item.id}
                  className={cn(
                    'p-4 rounded-xl border border-border bg-card shadow-2xs space-y-2.5',
                    selectable && selectedRowIds.includes(item.id) && 'border-blue-500/50 bg-blue-50/20 dark:bg-blue-950/20'
                  )}
                >
                  <div className="flex items-start justify-between gap-2">
                    <div className="flex items-center gap-2.5 min-w-0 flex-1">
                      {selectable && (
                        <Checkbox
                          checked={selectedRowIds.includes(item.id)}
                          onCheckedChange={(chk) => handleToggleRow(item.id, Boolean(chk))}
                          aria-label={`Chọn dòng ${item.id}`}
                        />
                      )}
                      <div className="min-w-0 flex-1">
                        {columns[0] && (
                          <div className="font-semibold text-sm text-foreground truncate">
                            {columns[0].cell
                              ? columns[0].cell(item)
                              : columns[0].accessorKey
                                ? String(item[columns[0].accessorKey] ?? '—')
                                : '—'}
                          </div>
                        )}
                        {columns[1] && (
                          <div className="text-xs text-muted-foreground truncate">
                            {columns[1].cell
                              ? columns[1].cell(item)
                              : columns[1].accessorKey
                                ? String(item[columns[1].accessorKey] ?? '—')
                                : '—'}
                          </div>
                        )}
                      </div>
                    </div>
                    {item.isActive !== undefined && (
                      <Badge
                        variant={item.isActive ? 'default' : 'secondary'}
                        className={cn(
                          'text-[10px] shrink-0 font-medium',
                          item.isActive
                            ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300'
                            : 'bg-muted text-muted-foreground'
                        )}
                      >
                        {item.isActive ? 'Hoạt động' : 'Ngừng'}
                      </Badge>
                    )}
                  </div>

                  {/* Other fields */}
                  <div className="grid grid-cols-2 gap-2 text-xs pt-2 border-t border-border/60">
                    {columns.slice(2).map((col, idx) => (
                      <div key={idx} className="min-w-0">
                        <span className="text-[11px] text-muted-foreground block">
                          {col.mobileLabel || col.header}:
                        </span>
                        <span className="text-foreground truncate font-medium block">
                          {col.cell
                            ? col.cell(item)
                            : col.accessorKey
                              ? String(item[col.accessorKey] ?? '—')
                              : '—'}
                        </span>
                      </div>
                    ))}
                  </div>

                  {/* Actions row */}
                  {hasActions && (
                    <div className="pt-2 border-t border-border/60 flex items-center justify-end gap-1">
                      {renderActionButtons(item)}
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </>
      )}

      {/* PAGINATION CONTROLS */}
      {pagination.totalPages > 1 && (
        <div className="flex flex-col sm:flex-row items-center justify-between gap-3 pt-2 text-xs text-muted-foreground">
          <div>
            Hiển thị trang <strong>{pagination.pageIndex}</strong> /{' '}
            <strong>{pagination.totalPages}</strong> (Tổng cộng{' '}
            <strong>{pagination.totalCount}</strong> bản ghi)
          </div>

          <div className="flex items-center gap-1.5">
            <Button
              variant="outline"
              size="sm"
              onClick={() => onPageChange(pagination.pageIndex - 1)}
              disabled={!pagination.hasPreviousPage || isLoading}
              className="h-8 px-2.5 text-xs gap-1 cursor-pointer disabled:cursor-not-allowed min-h-[36px]"
              aria-label="Trang trước"
            >
              <ChevronLeft className="h-3.5 w-3.5" />
              <span>Trang trước</span>
            </Button>
            <div className="px-2 font-medium text-foreground">
              {pagination.pageIndex}
            </div>
            <Button
              variant="outline"
              size="sm"
              onClick={() => onPageChange(pagination.pageIndex + 1)}
              disabled={!pagination.hasNextPage || isLoading}
              className="h-8 px-2.5 text-xs gap-1 cursor-pointer disabled:cursor-not-allowed min-h-[36px]"
              aria-label="Trang sau"
            >
              <span>Trang sau</span>
              <ChevronRight className="h-3.5 w-3.5" />
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
