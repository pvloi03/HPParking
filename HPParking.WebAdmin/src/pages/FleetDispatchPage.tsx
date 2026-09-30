import { useState, useEffect } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Truck,
  Clock,
  AlertTriangle,
  MapPin,
  RefreshCw,
  Route,
  ShieldAlert,
  RotateCcw,
  Eye,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { DataTable, type ColumnDef } from '@/components/common/DataTable';
import { TripDetailDialog } from '@/components/fleet/TripDetailDialog';
import { fleetDispatchApi } from '@/api/fleetDispatchApi';
import {
  TripStatus,
  TripStatusLabel,
  type FleetTripDto,
} from '@/types/fleetDispatch';
import { DEFAULT_PAGE_SIZE } from '@/types/masterData';

function formatRemainingSeconds(seconds: number): { text: string; isOverdue: boolean } {
  const isOverdue = seconds < 0;
  const absSec = Math.floor(Math.abs(seconds));
  const minutes = Math.floor(absSec / 60);
  const sec = absSec % 60;

  const formatted = `${minutes.toString().padStart(2, '0')}:${sec.toString().padStart(2, '0')}`;
  return {
    text: isOverdue ? `Quá giờ ${formatted}` : `Còn ${formatted}`,
    isOverdue,
  };
}

export function FleetDispatchPage() {
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState<'active' | 'history'>('active');
  const [, setTick] = useState(0);

  // Search & Filters cho Chuyến đang chạy
  const [activeKeyword, setActiveKeyword] = useState('');

  // Search, Filters & Pagination cho Lịch sử chuyến xe
  const [historyPageIndex, setHistoryPageIndex] = useState(1);
  const [historyKeyword, setHistoryKeyword] = useState('');
  const [historyStatusFilter, setHistoryStatusFilter] = useState<string>('all');

  // Dialog chi tiết
  const [selectedTrip, setSelectedTrip] = useState<FleetTripDto | null>(null);
  const [isDetailOpen, setIsDetailOpen] = useState(false);

  // Live timer tick 1s
  useEffect(() => {
    const timer = setInterval(() => {
      setTick((t) => t + 1);
    }, 1000);
    return () => clearInterval(timer);
  }, []);

  // 1. Fetch chuyến đang chạy (Polling 10s)
  const {
    data: activeTrips = [],
    isLoading: isLoadingActive,
    isRefetching: isRefetchingActive,
  } = useQuery<FleetTripDto[]>({
    queryKey: ['activeTrips'],
    queryFn: () => fleetDispatchApi.getActiveTrips(),
    refetchInterval: 10000,
  });

  // 2. Fetch lịch sử chuyến xe (Phân trang server)
  const {
    data: historyPaged,
    isLoading: isLoadingHistory,
    isRefetching: isRefetchingHistory,
  } = useQuery({
    queryKey: [
      'tripHistory',
      historyPageIndex,
      DEFAULT_PAGE_SIZE,
      historyKeyword,
      historyStatusFilter,
    ],
    queryFn: () =>
      fleetDispatchApi.getTripHistory({
        pageIndex: historyPageIndex,
        pageSize: DEFAULT_PAGE_SIZE,
        status:
          historyStatusFilter !== 'all'
            ? (Number(historyStatusFilter) as TripStatus)
            : undefined,
      }),
    enabled: activeTab === 'history',
  });

  const handleRefresh = () => {
    if (activeTab === 'active') {
      queryClient.invalidateQueries({ queryKey: ['activeTrips'] });
    } else {
      queryClient.invalidateQueries({ queryKey: ['tripHistory'] });
    }
  };

  const handleResetHistoryFilters = () => {
    setHistoryKeyword('');
    setHistoryStatusFilter('all');
    setHistoryPageIndex(1);
  };

  // KPI counts
  const totalActive = activeTrips.length;
  const inTransitCount = activeTrips.filter((t) => t.status === TripStatus.InTransit).length;
  const workingCount = activeTrips.filter((t) => t.status === TripStatus.WorkingAtGate).length;
  const violatedCount = activeTrips.filter(
    (t) =>
      t.isOverdue ||
      t.status === TripStatus.OverdueTransit ||
      t.status === TripStatus.OverdueStay
  ).length;

  // Lọc client-side cho Chuyến đang chạy
  const filteredActiveTrips = activeTrips.filter((t: FleetTripDto) => {
    if (!activeKeyword.trim()) return true;
    const q = activeKeyword.toLowerCase().trim();
    return (
      (t.plateNumber && t.plateNumber.toLowerCase().includes(q)) ||
      (t.cardNumber && t.cardNumber.includes(q)) ||
      (t.originGateName && t.originGateName.toLowerCase().includes(q)) ||
      (t.currentGateName && t.currentGateName.toLowerCase().includes(q)) ||
      (t.routeName && t.routeName.toLowerCase().includes(q))
    );
  });

  // Cột cho Bảng Chuyến Đang Chạy
  const activeColumns: ColumnDef<FleetTripDto>[] = [
    {
      header: 'Biển số & Thẻ',
      cell: (trip) => (
        <div className="space-y-0.5">
          <span className="font-mono font-bold text-xs text-foreground uppercase tracking-wide block">
            {trip.plateNumber}
          </span>
          <span className="text-[10px] font-mono text-muted-foreground bg-muted px-1.5 py-0.2 rounded border inline-block">
            Thẻ: {trip.cardNumber}
          </span>
        </div>
      ),
    },
    {
      header: 'Cổng xuất phát',
      cell: (trip) => (
        <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
          <MapPin className="h-3.5 w-3.5 text-blue-500 shrink-0" />
          <span className="truncate">{trip.originGateName || trip.originGateId || '---'}</span>
        </div>
      ),
    },
    {
      header: 'Vị trí hiện tại',
      cell: (trip) => {
        const isTransit =
          trip.status === TripStatus.InTransit ||
          trip.status === TripStatus.OverdueTransit;

        return (
          <div className="space-y-0.5">
            <div className="flex items-center gap-1.5 text-xs font-medium">
              {isTransit ? (
                <>
                  <Route className="h-3.5 w-3.5 text-blue-500 shrink-0" />
                  <span className="text-blue-600 dark:text-blue-400 font-semibold">Đang trên đường</span>
                </>
              ) : (
                <>
                  <MapPin className="h-3.5 w-3.5 text-emerald-500 shrink-0" />
                  <span className="text-emerald-600 dark:text-emerald-400 font-semibold truncate">
                    Dừng tại: {trip.currentGateName || trip.currentGateId || '---'}
                  </span>
                </>
              )}
            </div>
            {isTransit && (
              <span className="text-[11px] text-muted-foreground block truncate">
                (Đã rời {trip.currentGateName || trip.originGateName || 'cổng xuất phát'})
              </span>
            )}
          </div>
        );
      },
    },
    {
      header: 'Trạng thái',
      cell: (trip) => {
        const isViolated =
          trip.isOverdue ||
          trip.status === TripStatus.OverdueTransit ||
          trip.status === TripStatus.OverdueStay;

        const isInTransit =
          trip.status === TripStatus.InTransit ||
          trip.status === TripStatus.OverdueTransit;
        const isWorking =
          trip.status === TripStatus.WorkingAtGate ||
          trip.status === TripStatus.OverdueStay;

        return (
          <div className="flex flex-col gap-1 items-start">
            <Badge
              variant="outline"
              className={`text-[10px] ${
                isInTransit
                  ? 'border-blue-500 text-blue-600 bg-blue-50 dark:bg-blue-950/40'
                  : isWorking
                  ? 'border-emerald-500 text-emerald-600 bg-emerald-50 dark:bg-emerald-950/40'
                  : 'border-muted-foreground/40 text-muted-foreground'
              }`}
            >
              {TripStatusLabel[trip.status] || 'Đang di chuyển'}
            </Badge>
            {isViolated && (
              <Badge variant="destructive" className="text-[9px] gap-1 px-1 py-0">
                <AlertTriangle className="h-2.5 w-2.5" />
                Quá hạn SLA
              </Badge>
            )}
          </div>
        );
      },
    },
    {
      header: 'SLA Thời gian',
      cell: (trip) => {
        const countdown = formatRemainingSeconds(trip.remainingSeconds);
        return (
          <div className="space-y-0.5">
            <span
              className={`font-mono text-xs font-bold block ${
                countdown.isOverdue ? 'text-red-600 dark:text-red-400 animate-pulse' : 'text-foreground'
              }`}
            >
              {countdown.text}
            </span>
            {trip.nextDeadline && (
              <span className="text-[10px] text-muted-foreground block">
                Hạn: {new Date(trip.nextDeadline).toLocaleTimeString('vi-VN')}
              </span>
            )}
          </div>
        );
      },
    },
    {
      header: 'Bắt đầu',
      cell: (trip) => (
        <span className="text-xs text-muted-foreground whitespace-nowrap">
          {new Date(trip.startTime).toLocaleTimeString('vi-VN')}{' '}
          <span className="text-[10px] block">{new Date(trip.startTime).toLocaleDateString('vi-VN')}</span>
        </span>
      ),
    },
    {
      header: 'Thao tác',
      cell: (trip) => (
        <Button
          variant="outline"
          size="sm"
          onClick={() => {
            setSelectedTrip(trip);
            setIsDetailOpen(true);
          }}
          className="text-[11px] h-7 gap-1 px-2 cursor-pointer"
        >
          <Eye className="h-3 w-3 text-blue-600" />
          <span>Chi tiết</span>
        </Button>
      ),
    },
  ];

  // Cột cho Bảng Lịch Sử Chuyến Xe
  const historyColumns: ColumnDef<FleetTripDto>[] = [
    {
      header: 'Biển số & Thẻ',
      cell: (trip) => (
        <div className="space-y-0.5">
          <span className="font-mono font-bold text-xs text-foreground uppercase tracking-wide block">
            {trip.plateNumber}
          </span>
          <span className="text-[10px] font-mono text-muted-foreground bg-muted px-1.5 py-0.2 rounded border inline-block">
            {trip.cardNumber}
          </span>
        </div>
      ),
    },
    {
      header: 'Cổng xuất phát',
      cell: (trip) => (
        <span className="text-xs text-muted-foreground">
          {trip.originGateName || trip.originGateId || '---'}
        </span>
      ),
    },
    {
      header: 'Thời gian bắt đầu',
      cell: (trip) => (
        <span className="text-xs text-muted-foreground whitespace-nowrap">
          {new Date(trip.startTime).toLocaleString('vi-VN')}
        </span>
      ),
    },
    {
      header: 'Thời gian kết thúc',
      cell: (trip) => (
        <span className="text-xs text-muted-foreground whitespace-nowrap">
          {trip.endTime ? new Date(trip.endTime).toLocaleString('vi-VN') : '---'}
        </span>
      ),
    },
    {
      header: 'Trạng thái',
      cell: (trip) => (
        <Badge
          className={
            trip.status === TripStatus.Completed
              ? 'bg-emerald-600 hover:bg-emerald-600 text-white text-[10px]'
              : trip.status === TripStatus.InTransit
              ? 'bg-blue-600 hover:bg-blue-600 text-white text-[10px]'
              : 'bg-muted text-muted-foreground text-[10px]'
          }
        >
          {TripStatusLabel[trip.status]}
        </Badge>
      ),
    },
    {
      header: 'Thao tác',
      cell: (trip) => (
        <Button
          variant="outline"
          size="sm"
          onClick={() => {
            setSelectedTrip(trip);
            setIsDetailOpen(true);
          }}
          className="text-[11px] h-7 gap-1 px-2 cursor-pointer"
        >
          <Eye className="h-3 w-3 text-blue-600" />
          <span>Xem hành trình</span>
        </Button>
      ),
    },
  ];

  return (
    <div className="space-y-4 p-4 lg:p-6">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <div>
          <h1 className="text-xl font-bold tracking-tight text-foreground flex items-center gap-2">
            <Truck className="h-6 w-6 text-amber-600" />
            Điều Vận Phương Tiện Nội Bộ Thời Gian Thực
          </h1>
          <p className="text-xs text-muted-foreground mt-0.5">
            Giám sát trực tiếp các chuyến phương tiện nội bộ giữa các nhà máy, tự động phát hiện vi phạm SLA và tra cứu hồ sơ lịch sử hành trình.
          </p>
        </div>
        <Button
          onClick={handleRefresh}
          variant="outline"
          size="sm"
          disabled={isLoadingActive || isRefetchingActive || isLoadingHistory || isRefetchingHistory}
          className="text-xs h-9 gap-1.5 cursor-pointer self-start sm:self-auto"
        >
          <RefreshCw
            className={`h-3.5 w-3.5 ${
              isRefetchingActive || isRefetchingHistory ? 'animate-spin' : ''
            }`}
          />
          <span>Làm mới</span>
        </Button>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
        <div className="p-3.5 rounded-lg border border-border bg-card shadow-xs">
          <span className="text-[11px] text-muted-foreground font-medium block">
            Tổng chuyến đang chạy
          </span>
          <div className="text-2xl font-bold text-foreground mt-1 flex items-center justify-between">
            <span>{totalActive}</span>
            <Truck className="h-5 w-5 text-blue-500/70" />
          </div>
        </div>

        <div className="p-3.5 rounded-lg border border-border bg-card shadow-xs">
          <span className="text-[11px] text-muted-foreground font-medium block">
            Đang di chuyển
          </span>
          <div className="text-2xl font-bold text-blue-600 mt-1 flex items-center justify-between">
            <span>{inTransitCount}</span>
            <Route className="h-5 w-5 text-blue-500/70" />
          </div>
        </div>

        <div className="p-3.5 rounded-lg border border-border bg-card shadow-xs">
          <span className="text-[11px] text-muted-foreground font-medium block">
            Đang dừng đỗ / Làm việc
          </span>
          <div className="text-2xl font-bold text-emerald-600 mt-1 flex items-center justify-between">
            <span>{workingCount}</span>
            <MapPin className="h-5 w-5 text-emerald-500/70" />
          </div>
        </div>

        <div
          className={`p-3.5 rounded-lg border shadow-xs transition-colors ${
            violatedCount > 0
              ? 'border-red-300 dark:border-red-900/60 bg-red-50/50 dark:bg-red-950/20'
              : 'border-border bg-card'
          }`}
        >
          <span
            className={`text-[11px] font-medium block ${
              violatedCount > 0 ? 'text-red-600 dark:text-red-400' : 'text-muted-foreground'
            }`}
          >
            Vi phạm SLA / Quá giờ
          </span>
          <div
            className={`text-2xl font-bold mt-1 flex items-center justify-between ${
              violatedCount > 0 ? 'text-red-600 dark:text-red-400' : 'text-foreground'
            }`}
          >
            <span>{violatedCount}</span>
            <ShieldAlert
              className={`h-5 w-5 ${
                violatedCount > 0 ? 'text-red-500 animate-pulse' : 'text-muted-foreground/60'
              }`}
            />
          </div>
        </div>
      </div>

      {/* Tabs chuyển đổi: Chuyến đang chạy vs Lịch sử chuyến xe */}
      <div className="flex items-center justify-between border-b border-border pb-2">
        <div className="flex items-center gap-1.5 bg-muted/40 p-1 rounded-lg border border-border">
          <button
            type="button"
            onClick={() => setActiveTab('active')}
            className={`text-xs px-3.5 py-1.5 rounded-md font-medium transition-colors cursor-pointer flex items-center gap-1.5 ${
              activeTab === 'active'
                ? 'bg-background text-foreground shadow-xs font-semibold'
                : 'text-muted-foreground hover:text-foreground'
            }`}
          >
            <Truck className="h-3.5 w-3.5" />
            <span>Chuyến đang chạy</span>
            <Badge className="ml-1 text-[10px] px-1.5 py-0 bg-blue-600 text-white hover:bg-blue-600">
              {totalActive}
            </Badge>
          </button>

          <button
            type="button"
            onClick={() => setActiveTab('history')}
            className={`text-xs px-3.5 py-1.5 rounded-md font-medium transition-colors cursor-pointer flex items-center gap-1.5 ${
              activeTab === 'history'
                ? 'bg-background text-foreground shadow-xs font-semibold'
                : 'text-muted-foreground hover:text-foreground'
            }`}
          >
            <Clock className="h-3.5 w-3.5" />
            <span>Lịch sử chuyến xe</span>
          </button>
        </div>
      </div>

      {/* NỘI DUNG TAB 1: CHUYẾN ĐANG CHẠY */}
      {activeTab === 'active' && (
        <DataTable<FleetTripDto>
          data={filteredActiveTrips}
          columns={activeColumns}
          searchKeyword={activeKeyword}
          onSearchChange={setActiveKeyword}
          searchPlaceholder="Tìm biển số, mã thẻ, cổng..."
          onPageChange={() => {}}
          isLoading={isLoadingActive}
          emptyTitle="Không có chuyến phương tiện nội bộ đang chạy"
          emptyDescription="Khi phương tiện quẹt thẻ xuất phát tại cổng, chuyến xe sẽ tự động hiển thị tại đây."
        />
      )}

      {/* NỘI DUNG TAB 2: LỊCH SỬ CHUYẾN XE */}
      {activeTab === 'history' && (
        <DataTable<FleetTripDto>
          data={
            historyPaged?.items?.filter((t) =>
              historyKeyword.trim()
                ? t.plateNumber?.toLowerCase().includes(historyKeyword.toLowerCase().trim())
                : true
            ) || []
          }
          columns={historyColumns}
          searchKeyword={historyKeyword}
          onSearchChange={(kw) => {
            setHistoryKeyword(kw);
            setHistoryPageIndex(1);
          }}
          searchPlaceholder="Lọc biển số xe..."
          pagination={historyPaged?.pagination}
          onPageChange={(page) => setHistoryPageIndex(page)}
          isLoading={isLoadingHistory}
          emptyTitle="Không có hồ sơ lịch sử chuyến xe nào"
          emptyDescription="Không tìm thấy chuyến xe nào phù hợp với bộ lọc hiện tại."
          extraFilters={
            <div className="flex items-center gap-2">
              <div className="w-[180px]">
                <Select
                  value={historyStatusFilter}
                  onValueChange={(val) => {
                    setHistoryStatusFilter(val);
                    setHistoryPageIndex(1);
                  }}
                >
                  <SelectTrigger className="text-xs h-9">
                    <SelectValue placeholder="Trạng thái chuyến" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">Tất cả trạng thái</SelectItem>
                    <SelectItem value={String(TripStatus.Completed)}>Đã hoàn tất</SelectItem>
                    <SelectItem value={String(TripStatus.InTransit)}>Đang di chuyển</SelectItem>
                    <SelectItem value={String(TripStatus.WorkingAtGate)}>Đang làm việc</SelectItem>
                    <SelectItem value={String(TripStatus.OverdueTransit)}>Quá hạn di chuyển</SelectItem>
                    <SelectItem value={String(TripStatus.OverdueStay)}>Quá hạn dừng đỗ</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <Button
                variant="outline"
                size="sm"
                onClick={handleResetHistoryFilters}
                className="text-xs h-9 gap-1 text-muted-foreground hover:text-foreground cursor-pointer"
              >
                <RotateCcw className="h-3.5 w-3.5" />
                <span>Đặt lại</span>
              </Button>
            </div>
          }
        />
      )}

      {/* Dialog Chi Tiết Chuyến Xe & Mốc Kiểm Soát Checkpoints */}
      <TripDetailDialog
        open={isDetailOpen}
        onOpenChange={setIsDetailOpen}
        tripId={selectedTrip?.id || null}
        initialTrip={selectedTrip}
      />
    </div>
  );
}
