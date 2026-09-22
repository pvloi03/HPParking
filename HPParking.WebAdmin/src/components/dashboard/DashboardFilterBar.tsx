import { Calendar, RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/button';
import type { DashboardFilterState, DashboardFilterType } from '@/types/dashboard';

interface DashboardFilterBarProps {
  filter: DashboardFilterState;
  onFilterChange: (newFilter: DashboardFilterState) => void;
  onRefresh: () => void;
  isRefreshing?: boolean;
}

export function DashboardFilterBar({
  filter,
  onFilterChange,
  onRefresh,
  isRefreshing = false,
}: DashboardFilterBarProps) {
  const handleTypeChange = (type: DashboardFilterType) => {
    onFilterChange({
      ...filter,
      type,
    });
  };

  const currentYear = new Date().getFullYear();
  const availableYears = [currentYear - 2, currentYear - 1, currentYear, currentYear + 1];

  return (
    <div className="flex flex-col items-start sm:items-end gap-2">
      {/* Hàng 1: 4 Chế độ lọc dùng chung */}
      <div className="flex items-center rounded-lg border border-border bg-card p-1 text-xs shadow-xs">
        <button
          type="button"
          onClick={() => handleTypeChange('day')}
          className={`px-3 py-1.5 rounded-md font-medium transition-colors cursor-pointer ${
            filter.type === 'day'
              ? 'bg-blue-600 text-white shadow-xs font-semibold'
              : 'text-muted-foreground hover:text-foreground'
          }`}
        >
          Ngày
        </button>
        <button
          type="button"
          onClick={() => handleTypeChange('month')}
          className={`px-3 py-1.5 rounded-md font-medium transition-colors cursor-pointer ${
            filter.type === 'month'
              ? 'bg-blue-600 text-white shadow-xs font-semibold'
              : 'text-muted-foreground hover:text-foreground'
          }`}
        >
          Tháng
        </button>
        <button
          type="button"
          onClick={() => handleTypeChange('year')}
          className={`px-3 py-1.5 rounded-md font-medium transition-colors cursor-pointer ${
            filter.type === 'year'
              ? 'bg-blue-600 text-white shadow-xs font-semibold'
              : 'text-muted-foreground hover:text-foreground'
          }`}
        >
          Năm
        </button>
        <button
          type="button"
          onClick={() => handleTypeChange('custom')}
          className={`px-3 py-1.5 rounded-md font-medium transition-colors cursor-pointer ${
            filter.type === 'custom'
              ? 'bg-blue-600 text-white shadow-xs font-semibold'
              : 'text-muted-foreground hover:text-foreground'
          }`}
        >
          Tùy chọn
        </button>
      </div>

      {/* Hàng 2: Bộ điều khiển chi tiết (input date / month / year / range) + Nút làm mới (chỉ icon) nằm sau */}
      <div className="flex items-center gap-2 justify-start sm:justify-end">
        {filter.type === 'day' && (
          <div className="relative flex items-center">
            <Calendar className="absolute left-2.5 h-3.5 w-3.5 text-muted-foreground pointer-events-none" />
            <input
              type="date"
              aria-label="Chọn ngày xem báo cáo"
              value={filter.date}
              onChange={(e) => onFilterChange({ ...filter, date: e.target.value })}
              className="h-8 pl-8 pr-3 text-xs rounded-lg border border-border bg-card text-foreground shadow-2xs focus:outline-none focus:ring-1 focus:ring-blue-500 cursor-pointer"
            />
          </div>
        )}

        {filter.type === 'month' && (
          <div className="relative flex items-center">
            <Calendar className="absolute left-2.5 h-3.5 w-3.5 text-muted-foreground pointer-events-none" />
            <input
              type="month"
              aria-label="Chọn tháng xem báo cáo"
              value={filter.month}
              onChange={(e) => onFilterChange({ ...filter, month: e.target.value })}
              className="h-8 pl-8 pr-3 text-xs rounded-lg border border-border bg-card text-foreground shadow-2xs focus:outline-none focus:ring-1 focus:ring-blue-500 cursor-pointer"
            />
          </div>
        )}

        {filter.type === 'year' && (
          <div className="relative flex items-center">
            <Calendar className="absolute left-2.5 h-3.5 w-3.5 text-muted-foreground pointer-events-none" />
            <select
              aria-label="Chọn năm xem báo cáo"
              value={filter.year}
              onChange={(e) => onFilterChange({ ...filter, year: e.target.value })}
              className="h-8 pl-8 pr-4 text-xs rounded-lg border border-border bg-card text-foreground shadow-2xs focus:outline-none focus:ring-1 focus:ring-blue-500 cursor-pointer"
            >
              {availableYears.map((y) => (
                <option key={y} value={String(y)}>
                  Năm {y}
                </option>
              ))}
            </select>
          </div>
        )}

        {filter.type === 'custom' && (
          <div className="flex items-center gap-1.5 text-xs bg-card border border-border rounded-lg px-2.5 py-1 shadow-2xs">
            <input
              type="date"
              aria-label="Từ ngày"
              value={filter.customFrom}
              onChange={(e) => onFilterChange({ ...filter, customFrom: e.target.value })}
              className="h-6 px-1 text-xs rounded border-0 bg-transparent text-foreground focus:outline-none focus:ring-1 focus:ring-blue-500 cursor-pointer"
            />
            <span className="text-muted-foreground font-medium text-[11px]">đến</span>
            <input
              type="date"
              aria-label="Đến ngày"
              value={filter.customTo}
              onChange={(e) => onFilterChange({ ...filter, customTo: e.target.value })}
              className="h-6 px-1 text-xs rounded border-0 bg-transparent text-foreground focus:outline-none focus:ring-1 focus:ring-blue-500 cursor-pointer"
            />
          </div>
        )}

        {/* Nút Làm mới đồng bộ (chỉ icon, nằm sau input date) */}
        <Button
          variant="outline"
          size="sm"
          onClick={onRefresh}
          disabled={isRefreshing}
          aria-label="Làm mới dữ liệu"
          title="Làm mới dữ liệu"
          className="h-8 w-8 p-0 cursor-pointer min-h-[32px] shadow-2xs"
        >
          <RefreshCw className={`h-3.5 w-3.5 ${isRefreshing ? 'animate-spin' : ''}`} />
        </Button>
      </div>
    </div>
  );
}
