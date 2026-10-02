import { useState } from 'react';
import { useQuery, keepPreviousData } from '@tanstack/react-query';
import { toast } from '@/hooks/use-toast';
import {
  Car,
  Bike,
  Download,
  RotateCcw,
  Calendar,
  FileText,
  ShieldAlert,
  ArrowRightCircle,
  ArrowLeftCircle,
  User,
  UserCheck,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Card } from '@/components/ui/card';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { DataTable, type ColumnDef } from '@/components/common/DataTable';
import { ParkingSessionDetailDialog } from '@/components/parkingSessions/ParkingSessionDetailDialog';
import { formatImageUrl, hasImagePath } from '@/components/parkingSessions/EvidenceImageGrid';
import { parkingSessionApi, extractErrorMessage } from '@/api/parkingSessionApi';
import { downloadBlob } from '@/utils/downloadBlob';
import {
  ParkingSessionStatus,
  type ParkingSessionDto,
} from '@/types/parkingSession';
import { VehicleType } from '@/types/vehicle';
import { LaneTargetType } from '@/types/infrastructure';
import { DEFAULT_PAGE_SIZE } from '@/types/masterData';

export interface SessionsManagerProps {
  targetType: LaneTargetType;
}

export function SessionsManager({ targetType }: SessionsManagerProps) {
  const isPedestrian = targetType === LaneTargetType.Pedestrian;

  // State phân trang & bộ lọc
  const [pageIndex, setPageIndex] = useState(1);
  const pageSize = DEFAULT_PAGE_SIZE;
  const [searchQuery, setSearchQuery] = useState('');
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
    setSearchQuery('');
    setStatusFilter('');
    setVehicleTypeFilter('');
    setFromDate('');
    setToDate('');
    setPageIndex(1);
  };

  // TanStack Query v5: Tra cứu danh sách phiên vào/ra
  const { data, isLoading, refetch, isRefetching } = useQuery({
    queryKey: [
      'sessions',
      targetType,
      pageIndex,
      pageSize,
      searchQuery,
      statusFilter,
      vehicleTypeFilter,
      fromDate,
      toDate,
    ],
    queryFn: () =>
      parkingSessionApi.getParkingSessions({
        pageIndex,
        pageSize,
        plateNumber: !isPedestrian && searchQuery.trim() ? searchQuery.trim() : undefined,
        targetType,
        status: statusFilter ? (Number(statusFilter) as ParkingSessionStatus) : undefined,
        vehicleType: !isPedestrian && vehicleTypeFilter ? (Number(vehicleTypeFilter) as VehicleType) : undefined,
        fromDate: fromDate ? `${fromDate}T00:00:00` : undefined,
        toDate: toDate ? `${toDate}T23:59:59` : undefined,
      }),
    placeholderData: keepPreviousData,
  });

  const sessions = data?.items || [];
  const totalCount = data?.pagination?.totalCount ?? 0;

  // Thao tác xuất Excel ClosedXML
  const handleExportExcel = async () => {
    if (totalCount === 0) {
      toast.warning('Không có bản ghi nào để xuất báo cáo trong khoảng thời gian đã chọn!');
      return;
    }

    try {
      setIsExporting(true);
      const targetLabel = isPedestrian ? 'người vào ra' : 'phương tiện vào ra';
      toast.info(`Đang khởi tạo và xuất tệp Excel lịch sử ${targetLabel}...`);

      const blob = await parkingSessionApi.exportParkingSessions({
        plateNumber: !isPedestrian && searchQuery.trim() ? searchQuery.trim() : undefined,
        targetType,
        status: statusFilter ? (Number(statusFilter) as ParkingSessionStatus) : undefined,
        vehicleType: !isPedestrian && vehicleTypeFilter ? (Number(vehicleTypeFilter) as VehicleType) : undefined,
        fromDate: fromDate ? `${fromDate}T00:00:00` : undefined,
        toDate: toDate ? `${toDate}T23:59:59` : undefined,
      });

      const prefix = isPedestrian ? 'lich_su_nguoi_vao_ra' : 'lich_su_phuong_tien_vao_ra';
      const fileName = `${prefix}_${fromDate || 'tat_ca'}_${toDate || 'tat_ca'}.xlsx`;
      downloadBlob(blob, fileName);
      toast.success(`Xuất báo cáo Excel thành công! (${totalCount} bản ghi)`);
    } catch (err) {
      toast.error(extractErrorMessage(err));
    } finally {
      setIsExporting(false);
    }
  };

  // Cấu hình cột bảng riêng biệt cho Phương tiện vs Người đi bộ
  const vehicleColumns: ColumnDef<ParkingSessionDto>[] = [
    {
      header: 'Biển Số Xe',
      cell: (item) => {
        const isMismatch = item.status === ParkingSessionStatus.UnmatchedOut;
        return (
          <div className="flex flex-col gap-1 py-0.5">
            <div className="flex items-center gap-1.5">
              <Badge variant="outline" className="border-slate-400 font-mono font-bold text-xs">
                {item.plateNumber || '--'}
              </Badge>
            </div>
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
            {item.vehicleType ? 'Khác' : '--'}
          </Badge>
        );
      },
    },
    {
      header: 'Chủ Phương Tiện',
      cell: (item) => {
        const faceUrl = item.inFaceImagePath || item.personAvatar;
        return (
          <div className="flex items-center gap-2 py-0.5 text-xs">
            {hasImagePath(faceUrl) ? (
              <img
                src={formatImageUrl(faceUrl)}
                alt={item.personFullName || 'Chủ xe'}
                className="h-7 w-7 rounded-full object-cover border border-slate-200 dark:border-slate-700 shadow-2xs shrink-0"
              />
            ) : (
              <div className="h-7 w-7 rounded-full bg-slate-100 dark:bg-slate-800 flex items-center justify-center text-slate-500 border border-slate-200 dark:border-slate-700 shrink-0">
                <User className="h-3.5 w-3.5 text-muted-foreground shrink-0" />
              </div>
            )}
            <div className="flex flex-col min-w-0">
              <span className="font-medium text-foreground truncate max-w-[130px]">
                {item.personFullName || 'Khách vãng lai'}
              </span>
              {item.personCode && (
                <span className="text-[10px] text-muted-foreground font-mono truncate max-w-[130px]">
                  {item.personCode}
                </span>
              )}
            </div>
          </div>
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
          <div className="text-[11px] text-muted-foreground flex items-center gap-1">
            <ArrowRightCircle className="h-3 w-3 text-emerald-500 shrink-0" />
            <span className="truncate max-w-[110px]">{item.inLaneName || 'Cổng vào'}</span>
          </div>
        </div>
      ),
    },
    {
      header: 'Lượt Ra (Check-Out)',
      cell: (item) => (
        <div className="text-xs space-y-0.5">
          <div className="font-mono text-foreground font-medium">
            {item.outTime ? new Date(item.outTime).toLocaleString('vi-VN') : '--'}
          </div>
          <div className="text-[11px] text-muted-foreground flex items-center gap-1">
            <ArrowLeftCircle className="h-3 w-3 text-indigo-500 shrink-0" />
            <span className="truncate max-w-[110px]">
              {item.outLaneName || (item.outTime ? 'Cổng ra' : '--')}
            </span>
          </div>
        </div>
      ),
    },
    {
      header: 'Thời Lượng',
      cell: (item) => {
        if (!item.inTime) return <span className="text-muted-foreground text-xs">--</span>;
        if (!item.outTime) {
          return (
            <Badge variant="outline" className="border-amber-400 bg-amber-50 dark:bg-amber-950/40 text-amber-700 dark:text-amber-300 text-[10px] font-mono">
              Đang gửi
            </Badge>
          );
        }
        const diffMs = new Date(item.outTime).getTime() - new Date(item.inTime).getTime();
        if (diffMs <= 0) return <span className="text-muted-foreground text-xs">0 phút</span>;
        const totalMinutes = Math.floor(diffMs / (1000 * 60));
        const hours = Math.floor(totalMinutes / 60);
        const minutes = totalMinutes % 60;
        return (
          <span className="text-xs font-mono text-foreground font-medium">
            {hours > 0 ? `${hours}h ${minutes}p` : `${minutes} phút`}
          </span>
        );
      },
    },
    {
      header: 'Trạng Thái',
      cell: (item) => {
        switch (item.status) {
          case ParkingSessionStatus.Active:
            return (
              <Badge className="bg-sky-600 hover:bg-sky-600 text-white font-medium text-[11px] px-2 py-0.5">
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
      className: 'w-max',
      cell: (item) => (
        <div className="flex items-center justify-start gap-1">
          <Button
            size="sm"
            onClick={() => setSelectedSessionId(item.id)}
            className="border-none bg-transparent shadow-none hover:bg-transparent h-7 p-0 text-xs gap-1 cursor-pointer font-medium text-slate-500 hover:text-primary"
            title="Xem chi tiết"
          >
            <FileText className="h-3.5 w-3.5" />
            Xem chi tiết
          </Button>
        </div>
      ),
    },
  ];

  const pedestrianColumns: ColumnDef<ParkingSessionDto>[] = [
    {
      header: 'Mã Nhân Sự / CCCD',
      cell: (item) => (
        <Badge variant="outline" className="border-slate-400 font-mono font-bold text-xs bg-slate-50 dark:bg-slate-900/40">
          {item.personCode || '--'}
        </Badge>
      ),
    },
    {
      header: 'Họ Và Tên',
      cell: (item) => {
        const faceUrl = item.inFaceImagePath || item.personAvatar;
        return (
          <div className="flex items-center gap-2 py-0.5 text-xs">
            {hasImagePath(faceUrl) ? (
              <img
                src={formatImageUrl(faceUrl)}
                alt={item.personFullName || 'Người đi bộ'}
                className="h-8 w-8 rounded-full object-cover border border-slate-200 dark:border-slate-700 shadow-2xs shrink-0"
              />
            ) : (
              <div className="h-8 w-8 rounded-full bg-slate-100 dark:bg-slate-800 flex items-center justify-center text-slate-500 border border-slate-200 dark:border-slate-700 shrink-0">
                <User className="h-4 w-4 text-muted-foreground shrink-0" />
              </div>
            )}
            <div className="flex flex-col min-w-0">
              <span className="font-semibold text-foreground truncate max-w-[150px]">
                {item.personFullName || 'Khách vãng lai'}
              </span>
              <span className="text-[10px] text-muted-foreground">
                {item.personId ? 'Nhân sự nội bộ' : 'Khách vãng lai'}
              </span>
            </div>
          </div>
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
          <div className="text-[11px] text-muted-foreground flex items-center gap-1">
            <ArrowRightCircle className="h-3 w-3 text-emerald-500 shrink-0" />
            <span className="truncate max-w-[130px]">{item.inLaneName || 'Cửa xoay vào'}</span>
          </div>
        </div>
      ),
    },
    {
      header: 'Lượt Ra (Check-Out)',
      cell: (item) => (
        <div className="text-xs space-y-0.5">
          <div className="font-mono text-foreground font-medium">
            {item.outTime ? new Date(item.outTime).toLocaleString('vi-VN') : '--'}
          </div>
          <div className="text-[11px] text-muted-foreground flex items-center gap-1">
            <ArrowLeftCircle className="h-3 w-3 text-indigo-500 shrink-0" />
            <span className="truncate max-w-[130px]">
              {item.outLaneName || (item.outTime ? 'Cửa xoay ra' : '--')}
            </span>
          </div>
        </div>
      ),
    },
    {
      header: 'Thời Lượng Bên Trong',
      cell: (item) => {
        if (!item.inTime) return <span className="text-muted-foreground text-xs">--</span>;
        if (!item.outTime) {
          return (
            <Badge variant="outline" className="border-emerald-400 bg-emerald-50 dark:bg-emerald-950/40 text-emerald-700 dark:text-emerald-300 text-[10px] font-mono">
              Đang bên trong
            </Badge>
          );
        }
        const diffMs = new Date(item.outTime).getTime() - new Date(item.inTime).getTime();
        if (diffMs <= 0) return <span className="text-muted-foreground text-xs">0 phút</span>;
        const totalMinutes = Math.floor(diffMs / (1000 * 60));
        const hours = Math.floor(totalMinutes / 60);
        const minutes = totalMinutes % 60;
        return (
          <span className="text-xs font-mono text-foreground font-medium">
            {hours > 0 ? `${hours}h ${minutes}p` : `${minutes} phút`}
          </span>
        );
      },
    },
    {
      header: 'Trạng Thái',
      cell: (item) => {
        switch (item.status) {
          case ParkingSessionStatus.Active:
            return (
              <Badge className="bg-sky-600 hover:bg-sky-600 text-white font-medium text-[11px] px-2 py-0.5">
                Đang bên trong
              </Badge>
            );
          case ParkingSessionStatus.Completed:
            return (
              <Badge className="bg-emerald-600 hover:bg-emerald-600 text-white font-medium text-[11px] px-2 py-0.5">
                Đã hoàn thành
              </Badge>
            );
          default:
            return (
              <Badge variant="secondary" className="text-[11px]">
                Đã kết thúc
              </Badge>
            );
        }
      },
    },
    {
      header: 'Thao Tác',
      className: 'w-max',
      cell: (item) => (
        <div className="flex items-center justify-start gap-1">
          <Button
            size="sm"
            onClick={() => setSelectedSessionId(item.id)}
            className="border-none bg-transparent shadow-none hover:bg-transparent h-7 p-0 text-xs gap-1 cursor-pointer font-medium text-slate-500 hover:text-primary"
            title="Xem chi tiết"
          >
            <FileText className="h-3.5 w-3.5" />
            Xem chi tiết
          </Button>
        </div>
      ),
    },
  ];

  const columns = isPedestrian ? pedestrianColumns : vehicleColumns;

  return (
    <div className="space-y-4">
      {/* Header & Actions */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div className="flex items-center gap-3">
          <div className="h-10 w-10 rounded-xl bg-primary/10 text-primary flex items-center justify-center shrink-0">
            {isPedestrian ? <UserCheck className="h-5 w-5" /> : <Car className="h-5 w-5" />}
          </div>
          <div>
            <h1 className="text-xl font-bold tracking-tight text-foreground">
              {isPedestrian ? 'Lịch Sử Người Vào Ra' : 'Lịch Sử Phương Tiện Vào Ra'}
            </h1>
            <p className="text-xs text-muted-foreground mt-0.5">
              {isPedestrian
                ? 'Theo dõi lượt ra vào qua cổng xoay/lối đi bộ và xác thực FaceID'
                : 'Theo dõi, đối chiếu và quản lý toàn bộ lượt xe cơ giới ra vào cổng'}
            </p>
          </div>
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
        <div className={`grid grid-cols-1 sm:grid-cols-2 ${isPedestrian ? 'lg:grid-cols-3' : 'lg:grid-cols-4'} gap-2.5`}>

          {/* 2. Lọc theo loại xe (chỉ hiển thị cho phương tiện) */}
          {!isPedestrian && (
            <div className="min-w-[140px]">
              <Select
                value={vehicleTypeFilter || 'all'}
                onValueChange={(val) => {
                  setVehicleTypeFilter(val === 'all' ? '' : val);
                  setPageIndex(1);
                }}
              >
                <SelectTrigger aria-label="Lọc theo loại xe" className="w-full h-9 text-xs">
                  <SelectValue placeholder="Tất cả loại xe" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">-- Tất cả loại xe --</SelectItem>
                  <SelectItem value={String(VehicleType.Car)}>🚗 Ô tô</SelectItem>
                  <SelectItem value={String(VehicleType.Motorbike)}>🏍️ Xe máy</SelectItem>
                  <SelectItem value={String(VehicleType.Bicycle)}>🚲 Xe đạp</SelectItem>
                </SelectContent>
              </Select>
            </div>
          )}

          {/* 3. Lọc theo trạng thái */}
          <div className="min-w-[150px]">
            <Select
              value={statusFilter || 'all'}
              onValueChange={(val) => {
                setStatusFilter(val === 'all' ? '' : val);
                setPageIndex(1);
              }}
            >
              <SelectTrigger aria-label="Lọc theo trạng thái" className="w-full h-9 text-xs">
                <SelectValue placeholder="Tất cả trạng thái" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">-- Tất cả trạng thái --</SelectItem>
                <SelectItem value={String(ParkingSessionStatus.Active)}>
                  {isPedestrian ? 'Đang bên trong' : 'Đang trong bãi'}
                </SelectItem>
                <SelectItem value={String(ParkingSessionStatus.Completed)}>
                  {isPedestrian ? 'Đã ra / Hoàn tất' : 'Đã hoàn thành'}
                </SelectItem>
                {!isPedestrian && (
                  <>
                    <SelectItem value={String(ParkingSessionStatus.UnmatchedOut)}>
                      Ra không vào / Lệch biển
                    </SelectItem>
                    <SelectItem value={String(ParkingSessionStatus.Cancelled)}>
                      Đã hủy bỏ (Cancelled)
                    </SelectItem>
                  </>
                )}
              </SelectContent>
            </Select>
          </div>

          {/* 4. Từ ngày */}
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
              className="w-full bg-transparent border-0 text-xs focus:outline-none text-foreground cursor-pointer"
            />
          </div>

          {/* 5. Đến ngày */}
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
              className="w-full bg-transparent border-0 text-xs focus:outline-none text-foreground cursor-pointer"
            />
          </div>
        </div>

        {/* Hàng 2: Bộ chọn mốc thời gian nhanh (Date Presets) */}
        <div className="flex items-center justify-between gap-2 flex-wrap pt-2 border-t border-border/60">
          <div className="flex items-center gap-1.5 flex-wrap">
            <span className="text-[11px] text-muted-foreground font-medium flex items-center gap-1 mr-1">
              <Calendar className="h-3.5 w-3.5" />
              Xem nhanh:
            </span>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setDatePreset('today')}
              className="h-7 px-2.5 text-[11px] cursor-pointer"
            >
              Hôm nay
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setDatePreset('yesterday')}
              className="h-7 px-2.5 text-[11px] cursor-pointer"
            >
              Hôm qua
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setDatePreset('week')}
              className="h-7 px-2.5 text-[11px] cursor-pointer"
            >
              7 ngày qua
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setDatePreset('month')}
              className="h-7 px-2.5 text-[11px] cursor-pointer"
            >
              Tháng này
            </Button>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setDatePreset('all')}
              className="h-7 px-2.5 text-[11px] text-muted-foreground hover:text-foreground cursor-pointer"
            >
              Tất cả thời gian
            </Button>
          </div>

          {(searchQuery || statusFilter || vehicleTypeFilter || fromDate || toDate) && (
            <Button
              variant="ghost"
              size="sm"
              onClick={handleResetFilters}
              className="h-7 px-2 text-[11px] text-destructive hover:text-destructive hover:bg-destructive/10 cursor-pointer"
            >
              Xóa bộ lọc
            </Button>
          )}
        </div>
      </Card>

      {/* Bảng Dữ Liệu Lịch Sử */}
      <DataTable
        data={sessions}
        columns={columns}
        pagination={data?.pagination}
        onPageChange={setPageIndex}
        isLoading={isLoading}
        searchKeyword={searchQuery}
        onSearchChange={(val) => {
          setSearchQuery(val);
          setPageIndex(1);
        }}
        searchPlaceholder={isPedestrian ? 'Tìm theo họ tên, mã nhân sự, mã thẻ...' : 'Tìm theo biển số xe...'}
        emptyTitle={isPedestrian ? 'Không có dữ liệu người vào ra' : 'Không có dữ liệu lượt xe'}
        emptyDescription="Không tìm thấy phiên nào phù hợp với bộ lọc đã chọn."
      />

      {/* Dialog xem chi tiết phiên */}
      {selectedSessionId && (
        <ParkingSessionDetailDialog
          open={!!selectedSessionId}
          onOpenChange={(open) => {
            if (!open) setSelectedSessionId(null);
          }}
          sessionId={selectedSessionId}
        />
      )}
    </div>
  );
}
