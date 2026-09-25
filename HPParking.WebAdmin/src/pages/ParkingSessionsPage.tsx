import { useState } from 'react';
import { useQuery, keepPreviousData } from '@tanstack/react-query';
import { toast } from 'sonner';
import {
  Car,
  Bike,
  Search,
  Download,
  RotateCcw,
  Calendar,
  FileText,
  ShieldAlert,
  ArrowRightCircle,
  ArrowLeftCircle,
  User,
} from 'lucide-react';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Card } from '@/components/ui/card';
import { DataTable, type ColumnDef } from '@/components/common/DataTable';
import { ParkingSessionDetailDialog } from '@/components/parkingSessions/ParkingSessionDetailDialog';
import { parkingSessionApi, extractErrorMessage } from '@/api/parkingSessionApi';
import { downloadBlob } from '@/utils/downloadBlob';
import {
  ParkingSessionStatus,
  type ParkingSessionDto,
} from '@/types/parkingSession';
import { VehicleType } from '@/types/vehicle';

export function ParkingSessionsPage() {
  // State phân trang & bộ lọc
  const [pageIndex, setPageIndex] = useState(1);
  const pageSize = 15;
  const [plateNumber, setPlateNumber] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [vehicleTypeFilter, setVehicleTypeFilter] = useState<string>('');
  const [fromDate, setFromDate] = useState<string>('');
  const [toDate, setToDate] = useState<string>('');

  // State Dialog & Thao tác
  const [selectedSessionId, setSelectedSessionId] = useState<string | null>(null);
  const [isExporting, setIsExporting] = useState(false);

  // Bộ chọn ngày nhanh (Date Presets)
  const setDatePreset = (preset: 'today' | 'yesterday' | 'week' | 'month' | 'all') => {
    const now = new Date();
    const formatDate = (d: Date) => d.toISOString().slice(0, 10);

    if (preset === 'today') {
      const t = formatDate(now);
      setFromDate(t);
      setToDate(t);
    } else if (preset === 'yesterday') {
      const y = new Date(now);
      y.setDate(y.getDate() - 1);
      const t = formatDate(y);
      setFromDate(t);
      setToDate(t);
    } else if (preset === 'week') {
      const w = new Date(now);
      w.setDate(w.getDate() - 6);
      setFromDate(formatDate(w));
      setToDate(formatDate(now));
    } else if (preset === 'month') {
      const m = new Date(now.getFullYear(), now.getMonth(), 1);
      setFromDate(formatDate(m));
      setToDate(formatDate(now));
    } else if (preset === 'all') {
      setFromDate('');
      setToDate('');
    }
    setPageIndex(1);
  };

  const handleResetFilters = () => {
    setPlateNumber('');
    setStatusFilter('');
    setVehicleTypeFilter('');
    setFromDate('');
    setToDate('');
    setPageIndex(1);
  };

  // TanStack Query v5: Tra cứu danh sách phiên đỗ xe
  const { data, isLoading, refetch, isRefetching } = useQuery({
    queryKey: [
      'parking-sessions',
      pageIndex,
      pageSize,
      plateNumber,
      statusFilter,
      vehicleTypeFilter,
      fromDate,
      toDate,
    ],
    queryFn: () =>
      parkingSessionApi.getParkingSessions({
        pageIndex,
        pageSize,
        plateNumber: plateNumber.trim() || undefined,
        status: statusFilter ? (Number(statusFilter) as ParkingSessionStatus) : undefined,
        vehicleType: vehicleTypeFilter ? (Number(vehicleTypeFilter) as VehicleType) : undefined,
        fromDate: fromDate ? `${fromDate}T00:00:00` : undefined,
        toDate: toDate ? `${toDate}T23:59:59` : undefined,
      }),
    placeholderData: keepPreviousData,
  });

  const sessions = data?.items || [];
  const pagination = data?.pagination || {
    pageIndex: 1,
    pageSize: 15,
    totalCount: 0,
    totalPages: 1,
    hasPreviousPage: false,
    hasNextPage: false,
  };

  // Thao tác xuất Excel ClosedXML
  const handleExportExcel = async () => {
    if (pagination.totalCount === 0) {
      toast.warning('Không có bản ghi nào để xuất báo cáo trong khoảng thời gian đã chọn!');
      return;
    }

    try {
      setIsExporting(true);
      toast.info('Đang khởi tạo và xuất tệp Excel lịch sử đỗ xe...');

      const blob = await parkingSessionApi.exportParkingSessions({
        plateNumber: plateNumber.trim() || undefined,
        status: statusFilter ? (Number(statusFilter) as ParkingSessionStatus) : undefined,
        vehicleType: vehicleTypeFilter ? (Number(vehicleTypeFilter) as VehicleType) : undefined,
        fromDate: fromDate ? `${fromDate}T00:00:00` : undefined,
        toDate: toDate ? `${toDate}T23:59:59` : undefined,
      });

      const fileName = `lich_su_do_xe_${fromDate || 'tat_ca'}_${toDate || 'tat_ca'}.xlsx`;
      downloadBlob(blob, fileName);
      toast.success(`Xuất báo cáo Excel thành công! (${pagination.totalCount} bản ghi)`);
    } catch (err) {
      toast.error(extractErrorMessage(err));
    } finally {
      setIsExporting(false);
    }
  };


  // Định nghĩa các cột cho DataTable
  const columns: ColumnDef<ParkingSessionDto>[] = [
    {
      header: 'Biển Số Xe',
      cell: (item) => {
        const isMismatch = item.status === ParkingSessionStatus.UnmatchedOut;
        return (
          <div className="flex flex-col gap-1 py-0.5">
            <div className="flex items-center gap-1.5">
              <span className="font-extrabold font-mono text-sm tracking-wide text-foreground">
                {item.plateNumber}
              </span>
            </div>
            {/* Huy hiệu cảnh báo lệch biển số nổi bật */}
            {isMismatch && (
              <div className="flex items-center gap-1">
                <Badge className="bg-rose-600 hover:bg-rose-600 text-white font-bold text-[10px] px-1.5 py-0.2 shadow-2xs gap-0.5 animate-pulse">
                  <ShieldAlert className="h-3 w-3" />
                  Lệch biển số (Ra không vào)
                </Badge>
              </div>
            )}
          </div>
        );
      },
    },
    {
      header: 'Chủ Phương Tiện',
      cell: (item) => (
        <div className="flex items-center gap-1.5 text-xs">
          <User className="h-3.5 w-3.5 text-muted-foreground shrink-0" />
          <span className="font-medium text-foreground truncate max-w-[140px]">
            {(item as any).personFullName || 'Khách vãng lai'}
          </span>
        </div>
      ),
    },
    {
      header: 'Loại Xe',
      cell: (item) => {
        if (item.vehicleType === VehicleType.Car) {
          return (
            <Badge variant="outline" className="bg-indigo-50 dark:bg-indigo-950/40 text-indigo-700 dark:text-indigo-300 border-indigo-200 dark:border-indigo-800 gap-1 font-semibold text-[11px]">
              <Car className="h-3 w-3" /> Ô tô
            </Badge>
          );
        }
        if (item.vehicleType === VehicleType.Motorbike) {
          return (
            <Badge variant="outline" className="bg-amber-50 dark:bg-amber-950/40 text-amber-700 dark:text-amber-300 border-amber-200 dark:border-amber-800 gap-1 font-semibold text-[11px]">
              <Bike className="h-3 w-3" /> Xe máy
            </Badge>
          );
        }
        return (
          <Badge variant="secondary" className="text-[11px]">
            Khác
          </Badge>
        );
      },
    },
    {
      header: 'Lượt Vào (Check-In)',
      cell: (item) => (
        <div className="text-xs space-y-0.5">
          <div className="font-mono text-foreground font-medium">
            {item.inTime ? new Date(item.inTime).toLocaleString('vi-VN') : '--'}
          </div>
          <div className="text-muted-foreground text-[11px] flex items-center gap-1">
            <ArrowRightCircle className="h-3 w-3 text-cyan-500" />
            <span>{item.inLaneName || 'Làn Vào'}</span>
          </div>
        </div>
      ),
    },
    {
      header: 'Lượt Ra (Check-Out)',
      cell: (item) => (
        <div className="text-xs space-y-0.5">
          <div className="font-mono text-foreground font-medium">
            {item.outTime ? (
              new Date(item.outTime).toLocaleString('vi-VN')
            ) : (
              <span className="text-primary italic font-sans font-medium">Đang trong bãi</span>
            )}
          </div>
          <div className="text-muted-foreground text-[11px] flex items-center gap-1">
            <ArrowLeftCircle className="h-3 w-3 text-rose-500" />
            <span>{item.outLaneName || (item.outTime ? 'Làn Ra' : '--')}</span>
          </div>
        </div>
      ),
    },
    {
      header: 'Trạng Thái',
      cell: (item) => {
        switch (item.status) {
          case ParkingSessionStatus.Active:
            return (
              <Badge className="bg-blue-600 hover:bg-blue-600 text-white font-medium text-[11px] px-2 py-0.5">
                Đang trong bãi
              </Badge>
            );
          case ParkingSessionStatus.Completed:
            return (
              <Badge className="bg-emerald-600 hover:bg-emerald-600 text-white font-medium text-[11px] px-2 py-0.5">
                Đã hoàn thành
              </Badge>
            );
          case ParkingSessionStatus.UnmatchedOut:
            return (
              <Badge className="bg-rose-600 hover:bg-rose-600 text-white font-bold text-[11px] px-2 py-0.5 animate-pulse">
                Ra không vào
              </Badge>
            );
          case ParkingSessionStatus.Cancelled:
            return (
              <Badge variant="secondary" className="text-[11px]">
                Đã hủy
              </Badge>
            );
          default:
            return null;
        }
      },
    },
    {
      header: 'Thao Tác',
      className: 'text-right',
      cell: (item) => (
        <div className="flex items-center justify-end gap-1">
          <Button
            size="sm"
            variant="outline"
            onClick={() => setSelectedSessionId(item.id)}
            className="h-7 px-2.5 text-xs gap-1 cursor-pointer font-medium border-border hover:bg-muted"
            title="Xem chi tiết và 4 ảnh bằng chứng"
          >
            <FileText className="h-3.5 w-3.5 text-primary" />
            Chi tiết
          </Button>
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-4">
      {/* Header & Actions */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-bold tracking-tight text-foreground">
            Lịch Sử Xe Ra Vào
          </h1>
          <p className="text-xs text-muted-foreground mt-0.5">
            Tra cứu, xem lại bộ 4 ảnh bằng chứng và quản lý dữ liệu xe vào/ra trên toàn hệ thống
          </p>
        </div>

        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => refetch()}
            disabled={isRefetching}
            className="h-8 gap-1.5 text-xs font-medium cursor-pointer"
          >
            <RotateCcw className={`h-3.5 w-3.5 ${isRefetching ? 'animate-spin' : ''}`} />
            Làm mới
          </Button>
          <Button
            size="sm"
            onClick={handleExportExcel}
            disabled={isExporting}
            className="h-8 gap-1.5 text-xs font-semibold cursor-pointer shadow-xs bg-emerald-600 hover:bg-emerald-700 text-white"
          >
            <Download className="h-3.5 w-3.5" />
            {isExporting ? 'Đang xuất...' : 'Xuất Excel'}
          </Button>
        </div>
      </div>

      {/* Thanh Bộ Lọc & Bộ Chọn Ngày Nhanh (Date Presets) */}
      <Card className="p-3.5 shadow-2xs space-y-3 border-border bg-card">
        {/* Hàng 1: Các trường lọc đầu vào */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-2.5">
          {/* 1. Tìm theo biển số */}
          <div className="relative">
            <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
            <Input
              placeholder="Tìm theo biển số xe (VD: 30A-12345)..."
              value={plateNumber}
              onChange={(e) => {
                setPlateNumber(e.target.value);
                setPageIndex(1);
              }}
              className="pl-8.5 text-xs h-9"
            />
          </div>

          {/* 2. Lọc theo trạng thái */}
          <div>
            <select
              value={statusFilter}
              onChange={(e) => {
                setStatusFilter(e.target.value);
                setPageIndex(1);
              }}
              className="w-full h-9 rounded-lg border border-input bg-background px-3 text-xs focus:outline-none focus:ring-2 focus:ring-ring text-foreground cursor-pointer"
            >
              <option value="">-- Tất cả trạng thái --</option>
              <option value={ParkingSessionStatus.Active}>Đang trong bãi (Active)</option>
              <option value={ParkingSessionStatus.Completed}>Đã hoàn thành (Completed)</option>
              <option value={ParkingSessionStatus.UnmatchedOut}>Ra không vào / Lệch biển</option>
              <option value={ParkingSessionStatus.Cancelled}>Đã hủy bỏ (Cancelled)</option>
            </select>
          </div>

          {/* 3. Từ ngày */}
          <div className="flex items-center gap-1.5 bg-muted/40 border border-input rounded-lg px-2.5 py-1">
            <span className="text-[11px] font-semibold text-muted-foreground whitespace-nowrap">
              Từ:
            </span>
            <input
              type="date"
              value={fromDate}
              onChange={(e) => {
                setFromDate(e.target.value);
                setPageIndex(1);
              }}
              className="w-full bg-transparent text-xs text-foreground focus:outline-none cursor-pointer"
            />
          </div>

          {/* 4. Đến ngày */}
          <div className="flex items-center gap-1.5 bg-muted/40 border border-input rounded-lg px-2.5 py-1">
            <span className="text-[11px] font-semibold text-muted-foreground whitespace-nowrap">
              Đến:
            </span>
            <input
              type="date"
              value={toDate}
              onChange={(e) => {
                setToDate(e.target.value);
                setPageIndex(1);
              }}
              className="w-full bg-transparent text-xs text-foreground focus:outline-none cursor-pointer"
            />
          </div>
        </div>

        {/* Hàng 2: Bộ chọn nhanh khoảng thời gian (Presets) & Đặt lại */}
        <div className="flex flex-wrap items-center justify-between gap-2 pt-2 border-t border-border/60">
          <div className="flex flex-wrap items-center gap-1.5">
            <span className="text-[11px] font-semibold text-muted-foreground flex items-center gap-1 mr-1">
              <Calendar className="h-3.5 w-3.5 text-primary" />
              Xem nhanh:
            </span>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => setDatePreset('today')}
              className={`h-7 px-2.5 text-[11px] cursor-pointer rounded-md ${
                fromDate && fromDate === toDate && fromDate === new Date().toISOString().slice(0, 10)
                  ? 'bg-primary text-primary-foreground font-bold border-primary'
                  : 'text-muted-foreground'
              }`}
            >
              Hôm nay
            </Button>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => setDatePreset('yesterday')}
              className="h-7 px-2.5 text-[11px] cursor-pointer rounded-md text-muted-foreground"
            >
              Hôm qua
            </Button>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => setDatePreset('week')}
              className="h-7 px-2.5 text-[11px] cursor-pointer rounded-md text-muted-foreground"
            >
              7 ngày qua
            </Button>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => setDatePreset('month')}
              className="h-7 px-2.5 text-[11px] cursor-pointer rounded-md text-muted-foreground"
            >
              Tháng này
            </Button>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => setDatePreset('all')}
              className={`h-7 px-2.5 text-[11px] cursor-pointer rounded-md ${
                !fromDate && !toDate
                  ? 'bg-muted font-bold text-foreground'
                  : 'text-muted-foreground'
              }`}
            >
              Tất cả thời gian
            </Button>
          </div>

          <Button
            variant="ghost"
            size="sm"
            onClick={handleResetFilters}
            className="text-xs h-7 px-2 text-muted-foreground hover:text-foreground cursor-pointer"
          >
            Đặt lại bộ lọc
          </Button>
        </div>
      </Card>

      {/* Bảng dữ liệu phiên đỗ xe DataTable */}
      <DataTable
        data={sessions}
        columns={columns}
        pagination={pagination}
        onPageChange={setPageIndex}
        isLoading={isLoading}
        searchKeyword={plateNumber}
        onSearchChange={setPlateNumber}
        searchPlaceholder="Tìm nhanh biển số xe..."
        emptyTitle="Không có dữ liệu lượt xe"
        emptyDescription="Không tìm thấy phiên đỗ xe nào phù hợp với bộ lọc đã chọn."
      />

      {/* Modal Chi Tiết & Bộ 4 Ảnh Bằng Chứng */}
      <ParkingSessionDetailDialog
        open={Boolean(selectedSessionId)}
        onOpenChange={(open) => {
          if (!open) setSelectedSessionId(null);
        }}
        sessionId={selectedSessionId}
      />
    </div>
  );
}

export default ParkingSessionsPage;
