import { useState } from 'react';
import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query';
import { toast } from '@/hooks/use-toast';
import {
  Car,
  Bike,
  Download,
  Calendar,
  FileText,
  ShieldAlert,
  ArrowRightCircle,
  ArrowLeftCircle,
  UserCheck,
  XCircle,
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
import { inOrderNumberColumn } from './inOrderNumberColumn';
import { ParkingSessionDetailDialog } from '@/components/parkingSessions/ParkingSessionDetailDialog';
import { parkingSessionApi, extractErrorMessage } from '@/api/parkingSessionApi';
import { downloadBlob } from '@/utils/downloadBlob';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Switch } from '@/components/ui/switch';
import { usePermissions } from '@/hooks/usePermissions';
import {
  ParkingSessionStatus,
  SessionCloseReason,
  type ParkingSessionDto,
  type ForceCloseParkingSessionRequest,
} from '@/types/parkingSession';
import { VehicleType } from '@/types/vehicle';
import { LaneTargetType } from '@/types/infrastructure';
import { CardTargetType } from '@/types/card';
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
  const queryClient = useQueryClient();
  const { canWrite } = usePermissions();

  const [forceCloseTarget, setForceCloseTarget] = useState<ParkingSessionDto | null>(null);
  const [forceCloseReason, setForceCloseReason] = useState<SessionCloseReason>(SessionCloseReason.LostCard);
  const [forceCloseFee, setForceCloseFee] = useState<number>(0);
  const [forceCloseIsFeeCollected, setForceCloseIsFeeCollected] = useState<boolean>(true);
  const [forceCloseMarkLost, setForceCloseMarkLost] = useState<boolean>(true);
  const [forceCloseNote, setForceCloseNote] = useState<string>('');

  const forceCloseMutation = useMutation({
    mutationFn: (payload: { id: string; req: ForceCloseParkingSessionRequest }) =>
      parkingSessionApi.forceClose(payload.id, payload.req),
    onSuccess: () => {
      toast.success('Đã đóng cưỡng bức phiên đỗ xe thành công');
      setForceCloseTarget(null);
      void queryClient.invalidateQueries({ queryKey: ['sessions'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

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

  // TanStack Query v5: Tra cứu danh sách phiên vào/ra
  const { data, isLoading } = useQuery({
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
        keyword: searchQuery.trim() ? searchQuery.trim() : undefined,
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
        keyword: searchQuery.trim() ? searchQuery.trim() : undefined,
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
    inOrderNumberColumn,
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
      cell: (item) => (
        <div className="flex flex-col min-w-0 py-0.5 text-xs">
          <span className="font-medium text-foreground truncate max-w-[140px]">
            {item.personFullName || 'Khách vãng lai'}
          </span>
          {item.personCode && (
            <span className="text-[10px] text-muted-foreground font-mono truncate max-w-[140px]">
              {item.personCode}
            </span>
          )}
        </div>
      ),
    },
    {
      header: 'Thời Gian Vào',
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
      header: 'Thời Gian Ra',
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
      header: 'Tiền Phí',
      cell: (item) => {
        const fee = item.feeAmount ?? 0;
        const isActive = item.status === ParkingSessionStatus.Active || !item.outTime;
        const isFree =
          fee === 0 &&
          (item.cardTargetType === CardTargetType.Monthly ||
            item.cardTargetType === CardTargetType.VIP ||
            Boolean(item.personId) ||
            !isActive);

        if (isActive && !isFree) {
          return <span className="font-mono text-muted-foreground font-medium">---</span>;
        }

        if (fee === 0 || isFree) {
          return (
            <span className="font-medium text-emerald-600 dark:text-emerald-400 text-xs">
              Miễn phí
            </span>
          );
        }

        return (
          <div className="text-xs">
            <span className="font-mono font-semibold text-emerald-600 dark:text-emerald-400">
              {fee.toLocaleString('vi-VN')} đ
            </span>
            {item.isFeeCollected ? (
              <span className="text-[10px] text-emerald-600 block">Đã thu</span>
            ) : (
              <span className="text-[10px] text-amber-600 block">Chưa thu</span>
            )}
          </div>
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
          {canWrite && item.status === ParkingSessionStatus.Active && (
            <Button
              size="sm"
              variant="ghost"
              onClick={() => {
                setForceCloseTarget(item);
                setForceCloseReason(SessionCloseReason.LostCard);
                setForceCloseFee(item.feeAmount || 0);
                setForceCloseIsFeeCollected(true);
                setForceCloseMarkLost(true);
                setForceCloseNote('');
              }}
              className="h-7 px-1.5 text-xs text-rose-600 hover:text-rose-700 hover:bg-rose-50 dark:hover:bg-rose-950/30 cursor-pointer"
              title="Đóng cưỡng bức (Mất thẻ / Sự cố)"
            >
              <XCircle className="h-3.5 w-3.5 mr-0.5" />
              Đóng phiên
            </Button>
          )}
        </div>
      ),
    },
  ];

  const pedestrianColumns: ColumnDef<ParkingSessionDto>[] = [
    inOrderNumberColumn,
    {
      header: 'Mã định danh',
      cell: (item) => (
        <Badge variant="outline" className="border-slate-400 font-mono font-bold text-xs bg-slate-50 dark:bg-slate-900/40">
          {item.personCode || '--'}
        </Badge>
      ),
    },
    {
      header: 'Họ Và Tên',
      cell: (item) => (
        <div className="flex flex-col min-w-0 py-0.5 text-xs">
          <span className="font-semibold text-foreground truncate max-w-[160px]">
            {item.personFullName || 'Khách vãng lai'}
          </span>
          <span className="text-[10px] text-muted-foreground">
            {item.personId ? 'Nhân sự nội bộ' : 'Khách vãng lai'}
          </span>
        </div>
      ),
    },
    {
      header: 'Thời Gian Vào',
      cell: (item) => (
        <div className="text-xs space-y-0.5">
          <div className="font-mono text-foreground font-medium">
            {item.inTime ? new Date(item.inTime).toLocaleString('vi-VN') : '--'}
          </div>
          <div className="text-[11px] text-muted-foreground flex items-center gap-1">
            <ArrowRightCircle className="h-3 w-3 text-emerald-500 shrink-0" />
            <span className="truncate max-w-[130px]">{item.inLaneName || 'Cổng vào'}</span>
          </div>
        </div>
      ),
    },
    {
      header: 'Thời Gian Ra',
      cell: (item) => (
        <div className="text-xs space-y-0.5">
          <div className="font-mono text-foreground font-medium">
            {item.outTime ? new Date(item.outTime).toLocaleString('vi-VN') : '--'}
          </div>
          <div className="text-[11px] text-muted-foreground flex items-center gap-1">
            <ArrowLeftCircle className="h-3 w-3 text-indigo-500 shrink-0" />
            <span className="truncate max-w-[130px]">
              {item.outLaneName || (item.outTime ? 'Cổng ra' : '--')}
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
                Đã ra / Hoàn tất
              </Badge>
            );
          case ParkingSessionStatus.UnmatchedOut:
            return (
              <Badge className="bg-rose-600 hover:bg-rose-600 text-white font-semibold text-[11px] px-2 py-0.5 animate-pulse">
                Ra không có lượt vào
              </Badge>
            );
          case ParkingSessionStatus.Cancelled:
            return (
              <Badge variant="secondary" className="text-[11px]">
                Đã hủy bỏ
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
            onClick={handleExportExcel}
            disabled={isExporting}
            className="h-9 gap-1.5 text-xs cursor-pointer min-h-[36px]"
            title="Xuất dữ liệu ra Excel"
          >
            <Download className="h-3.5 w-3.5 text-emerald-600" />
            <span> {isExporting ? 'Đang xuất...' : 'Xuất Excel'}</span>
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
                <SelectItem value={String(ParkingSessionStatus.UnmatchedOut)}>
                  {isPedestrian ? 'Ra không có lượt vào' : 'Ra không vào / Lệch biển'}
                </SelectItem>
                <SelectItem value={String(ParkingSessionStatus.Cancelled)}>
                  Đã hủy bỏ
                </SelectItem>
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
        emptyDescription={
          isPedestrian
            ? 'Không tìm thấy dữ liệu lượt người vào ra phù hợp với bộ lọc đã chọn.'
            : 'Không tìm thấy phiên nào phù hợp với bộ lọc đã chọn.'
        }
      />

      {/* Dialog xem chi tiết phiên */}
      {selectedSessionId && (
        <ParkingSessionDetailDialog
          open={!!selectedSessionId}
          onOpenChange={(open) => {
            if (!open) setSelectedSessionId(null);
          }}
          sessionId={selectedSessionId}
          targetType={targetType}
        />
      )}

      {/* Modal Đóng cưỡng bức phiên đỗ xe */}
      {forceCloseTarget && (
        <Dialog
          open={Boolean(forceCloseTarget)}
          onOpenChange={(open) => !open && setForceCloseTarget(null)}
        >
          <DialogContent className="sm:max-w-md">
            <DialogHeader>
              <DialogTitle className="flex items-center gap-2 text-base font-bold text-rose-600">
                <ShieldAlert className="h-5 w-5" />
                <span>Đóng Cưỡng Bức Phiên Đỗ Xe</span>
              </DialogTitle>
              <DialogDescription className="text-xs">
                Áp dụng khi khách mất thẻ, sự cố barrier hoặc xe ra bất thường.
              </DialogDescription>
            </DialogHeader>

            <div className="space-y-3.5 py-1 text-xs">
              <div className="p-2.5 rounded-lg bg-muted/30 border space-y-1">
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Biển số:</span>
                  <span className="font-mono font-bold text-foreground">{forceCloseTarget.plateNumber}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Thời điểm vào:</span>
                  <span className="font-mono">{forceCloseTarget.inTime ? new Date(forceCloseTarget.inTime).toLocaleString('vi-VN') : '—'}</span>
                </div>
                {forceCloseTarget.cardNumber && (
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Mã thẻ:</span>
                    <span className="font-mono">{forceCloseTarget.cardNumber}</span>
                  </div>
                )}
              </div>

              <div className="space-y-1">
                <label className="text-xs font-semibold text-foreground">Lý do đóng cưỡng bức</label>
                <Select
                  value={String(forceCloseReason)}
                  onValueChange={(val) => setForceCloseReason(Number(val) as SessionCloseReason)}
                >
                  <SelectTrigger className="text-xs">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value={String(SessionCloseReason.LostCard)} className="text-xs">
                      Báo mất thẻ (Lost Card)
                    </SelectItem>
                    <SelectItem value={String(SessionCloseReason.AdminForceClose)} className="text-xs">
                      Admin cưỡng bức / Xử lý sự cố
                    </SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-1">
                <label className="text-xs font-semibold text-foreground">Số tiền phí thu (VNĐ)</label>
                <Input
                  type="number"
                  value={forceCloseFee}
                  onChange={(e) => setForceCloseFee(Number(e.target.value) || 0)}
                  className="text-xs font-mono"
                  min={0}
                  step={1000}
                />
              </div>

              <div className="flex items-center justify-between p-2.5 rounded-lg border bg-muted/20">
                <span className="text-xs font-medium">Đã thu tiền phí</span>
                <Switch
                  checked={forceCloseIsFeeCollected}
                  onCheckedChange={setForceCloseIsFeeCollected}
                />
              </div>

              {forceCloseReason === SessionCloseReason.LostCard && (
                <div className="flex items-center justify-between p-2.5 rounded-lg border bg-muted/20">
                  <span className="text-xs font-medium">Khóa và báo mất thẻ trong kho</span>
                  <Switch
                    checked={forceCloseMarkLost}
                    onCheckedChange={setForceCloseMarkLost}
                  />
                </div>
              )}

              <div className="space-y-1">
                <label className="text-xs font-semibold text-foreground">Ghi chú sự cố / giải trình</label>
                <Input
                  value={forceCloseNote}
                  onChange={(e) => setForceCloseNote(e.target.value)}
                  placeholder="VD: Khách làm rơi thẻ lúc mua sắm, đã thu phí phạt mất thẻ"
                  className="text-xs"
                />
              </div>
            </div>

            <DialogFooter className="pt-2">
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => setForceCloseTarget(null)}
                className="text-xs h-9 cursor-pointer"
              >
                Hủy bỏ
              </Button>
              <Button
                type="button"
                size="sm"
                disabled={forceCloseMutation.isPending}
                onClick={async () => {
                  if (forceCloseTarget) {
                    await forceCloseMutation.mutateAsync({
                      id: forceCloseTarget.id,
                      req: {
                        closeReason: forceCloseReason,
                        overrideFeeAmount: forceCloseFee,
                        isFeeCollected: forceCloseIsFeeCollected,
                        markCardAsLost: forceCloseMarkLost,
                        note: forceCloseNote || undefined,
                      },
                    });
                  }
                }}
                className="text-xs h-9 bg-rose-600 hover:bg-rose-700 text-white cursor-pointer"
              >
                {forceCloseMutation.isPending ? 'Đang xử lý...' : 'Xác nhận đóng phiên'}
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      )}
    </div>
  );
}
