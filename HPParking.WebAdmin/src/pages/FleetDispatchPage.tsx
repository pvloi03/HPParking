import { useState, useEffect } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Truck,
  Clock,
  AlertTriangle,
  MapPin,
  RefreshCw,
  Search,
  Route,
  ShieldAlert,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { fleetDispatchApi } from '@/api/fleetDispatchApi';
import {
  TripStatus,
  TripStatusLabel,
  type FleetTripDto,
} from '@/types/fleetDispatch';

function formatRemainingSeconds(seconds: number): { text: string; isOverdue: boolean } {
  const isOverdue = seconds < 0;
  const absSec = Math.abs(seconds);
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
  const [keyword, setKeyword] = useState('');
  const [, setTick] = useState(0);

  // Live timer update every 1 second
  useEffect(() => {
    const timer = setInterval(() => {
      setTick((t) => t + 1);
    }, 1000);
    return () => clearInterval(timer);
  }, []);

  // Fetch active trips with 10s polling
  const { data: activeTrips = [], isLoading, isRefetching } = useQuery<FleetTripDto[]>({
    queryKey: ['activeTrips'],
    queryFn: () => fleetDispatchApi.getActiveTrips(),
    refetchInterval: 10000,
  });

  // Filtered trips
  const filteredTrips = activeTrips.filter((t: FleetTripDto) => {
    if (!keyword.trim()) return true;
    const q = keyword.toLowerCase().trim();
    return (
      (t.plateNumber && t.plateNumber.toLowerCase().includes(q)) ||
      (t.cardNumber && t.cardNumber.includes(q)) ||
      (t.originGateName && t.originGateName.toLowerCase().includes(q)) ||
      (t.currentGateName && t.currentGateName.toLowerCase().includes(q)) ||
      (t.routeName && t.routeName.toLowerCase().includes(q))
    );
  });

  // KPI counts
  const totalActive = activeTrips.length;
  const inTransitCount = activeTrips.filter((t: FleetTripDto) => t.status === TripStatus.InTransit).length;
  const workingCount = activeTrips.filter((t: FleetTripDto) => t.status === TripStatus.WorkingAtGate).length;
  const violatedCount = activeTrips.filter(
    (t: FleetTripDto) =>
      t.isOverdue ||
      t.status === TripStatus.OverdueTransit ||
      t.status === TripStatus.OverdueStay
  ).length;

  return (
    <div className="space-y-4 p-4 lg:p-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <div>
          <h1 className="text-xl font-bold tracking-tight text-foreground flex items-center gap-2">
            <Truck className="h-6 w-6 text-amber-600" />
            Điều Vận Xe Công Vụ Thời Gian Thực
          </h1>
          <p className="text-xs text-muted-foreground mt-0.5">
            Giám sát trực tiếp các chuyến xe dùng chung di chuyển giữa các nhà máy và tự động cảnh báo vi phạm SLA.
          </p>
        </div>
        <Button
          onClick={() => queryClient.invalidateQueries({ queryKey: ['activeTrips'] })}
          variant="outline"
          size="sm"
          disabled={isLoading || isRefetching}
          className="text-xs h-9 gap-1.5 cursor-pointer self-start sm:self-auto"
        >
          <RefreshCw className={`h-3.5 w-3.5 ${isRefetching ? 'animate-spin' : ''}`} />
          <span>Làm mới</span>
        </Button>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
        <div className="p-3 rounded-lg border border-border bg-card shadow-xs">
          <span className="text-[11px] text-muted-foreground font-medium block">Tổng chuyến đang chạy</span>
          <div className="text-2xl font-bold text-foreground mt-1 flex items-center justify-between">
            <span>{totalActive}</span>
            <Truck className="h-5 w-5 text-blue-500/70" />
          </div>
        </div>

        <div className="p-3 rounded-lg border border-border bg-card shadow-xs">
          <span className="text-[11px] text-muted-foreground font-medium block">Đang di chuyển</span>
          <div className="text-2xl font-bold text-blue-600 mt-1 flex items-center justify-between">
            <span>{inTransitCount}</span>
            <Route className="h-5 w-5 text-blue-500/70" />
          </div>
        </div>

        <div className="p-3 rounded-lg border border-border bg-card shadow-xs">
          <span className="text-[11px] text-muted-foreground font-medium block">Đang làm việc tại điểm</span>
          <div className="text-2xl font-bold text-emerald-600 mt-1 flex items-center justify-between">
            <span>{workingCount}</span>
            <MapPin className="h-5 w-5 text-emerald-500/70" />
          </div>
        </div>

        <div className="p-3 rounded-lg border border-red-200 dark:border-red-900/50 bg-red-50/40 dark:bg-red-950/20 shadow-xs">
          <span className="text-[11px] text-red-600 dark:text-red-400 font-medium block">Vi phạm SLA / Quá giờ</span>
          <div className="text-2xl font-bold text-red-600 dark:text-red-400 mt-1 flex items-center justify-between">
            <span>{violatedCount}</span>
            <ShieldAlert className="h-5 w-5 text-red-500" />
          </div>
        </div>
      </div>

      {/* Search Bar */}
      <div className="flex items-center gap-2">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted-foreground" />
          <Input
            value={keyword}
            onChange={(e) => setKeyword(e.target.value)}
            placeholder="Tìm biển số, thẻ, cổng, tuyến..."
            className="pl-8 text-xs h-9"
          />
        </div>
      </div>

      {/* Trips List */}
      {isLoading ? (
        <div className="py-12 text-center text-xs text-muted-foreground">Đang tải dữ liệu chuyến xe...</div>
      ) : filteredTrips.length === 0 ? (
        <div className="py-12 text-center border rounded-lg bg-card text-muted-foreground space-y-2">
          <Truck className="h-8 w-8 mx-auto text-muted-foreground/60" />
          <p className="text-sm font-medium">Hiện không có xe công vụ nào đang thực hiện chuyến</p>
          <p className="text-xs">Khi xe quẹt thẻ xuất phát tại cổng, chuyến xe sẽ tự động hiển thị tại đây.</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-3">
          {filteredTrips.map((trip: FleetTripDto) => {
            const countdown = formatRemainingSeconds(trip.remainingSeconds);
            const isWorking = trip.status === TripStatus.WorkingAtGate || trip.status === TripStatus.OverdueStay;
            const isTripViolated =
              trip.isOverdue ||
              trip.status === TripStatus.OverdueTransit ||
              trip.status === TripStatus.OverdueStay;

            return (
              <div
                key={trip.id}
                className={`rounded-lg border p-4 bg-card shadow-xs space-y-3 transition-all ${
                  isTripViolated
                    ? 'border-red-400 dark:border-red-800 bg-red-50/10'
                    : 'border-border hover:border-blue-300 dark:hover:border-blue-900'
                }`}
              >
                {/* Header Card */}
                <div className="flex items-start justify-between gap-2 border-b border-border/60 pb-2.5">
                  <div className="space-y-0.5">
                    <div className="flex items-center gap-2">
                      <span className="font-mono font-bold text-sm text-foreground uppercase tracking-wide">
                        {trip.plateNumber}
                      </span>
                      <span className="px-1.5 py-0.5 rounded text-[10px] font-mono bg-muted text-muted-foreground border">
                        Thẻ: {trip.cardNumber}
                      </span>
                    </div>
                    <span className="text-[11px] text-muted-foreground block">
                      Tuyến: {trip.routeName || 'Tự do (Free-roam SLA: 15p)'}
                    </span>
                  </div>

                  <div className="flex flex-col items-end gap-1">
                    <Badge
                      variant="outline"
                      className={`text-[10px] ${
                        trip.status === TripStatus.InTransit
                          ? 'border-blue-500 text-blue-600 bg-blue-50 dark:bg-blue-950/40'
                          : trip.status === TripStatus.WorkingAtGate
                          ? 'border-emerald-500 text-emerald-600 bg-emerald-50 dark:bg-emerald-950/40'
                          : 'border-red-500 text-red-600 bg-red-50 dark:bg-red-950/40'
                      }`}
                    >
                      {TripStatusLabel[trip.status]}
                    </Badge>
                    {isTripViolated && (
                      <Badge variant="destructive" className="text-[10px] gap-1 px-1.5 py-0">
                        <AlertTriangle className="h-3 w-3" />
                        SLA Overdue
                      </Badge>
                    )}
                  </div>
                </div>

                {/* Hành Trình & Chặng */}
                <div className="rounded-md bg-muted/40 p-2.5 space-y-2 text-xs">
                  <div className="flex items-center justify-between text-muted-foreground text-[11px]">
                    <span className="flex items-center gap-1">
                      <MapPin className="h-3 w-3 text-blue-500" />
                      Xuất phát: <strong>{trip.originGateName || 'Cổng xuất phát'}</strong>
                    </span>
                    <span>Chặng {trip.currentStepIndex}</span>
                  </div>

                  <div className="flex items-center gap-1.5 font-medium text-foreground">
                    <span className="truncate">
                      {isWorking ? 'Đang dừng tại:' : 'Đã rời:'}{' '}
                      <span className="text-blue-600 dark:text-blue-400">
                        {trip.currentGateName || trip.originGateName || 'Cổng'}
                      </span>
                    </span>
                  </div>
                </div>

                {/* SLA Countdown Timer */}
                <div className="flex items-center justify-between rounded-md border p-2 text-xs">
                  <div className="flex items-center gap-1.5">
                    <Clock className="h-4 w-4 text-muted-foreground" />
                    <div>
                      <span className="text-[10px] text-muted-foreground block">Thời gian SLA:</span>
                      <span
                        className={`font-mono font-bold ${
                          isTripViolated
                            ? 'text-red-600 dark:text-red-400 animate-pulse'
                            : 'text-foreground'
                        }`}
                      >
                        {countdown.text}
                      </span>
                    </div>
                  </div>

                  {trip.nextDeadline && (
                    <span className="text-[10px] text-muted-foreground">
                      Hạn chót: {new Date(trip.nextDeadline).toLocaleTimeString('vi-VN')}
                    </span>
                  )}
                </div>

                {/* Violation reason or alert status if any */}
                {isTripViolated && (
                  <p className="text-[11px] text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-950/30 p-2 rounded border border-red-200 dark:border-red-900/40">
                    <strong>Cảnh báo:</strong> Xe đã vượt quá thời gian SLA cho phép.
                    {trip.isAlertSent ? ' (Đã gửi email cảnh báo tới ban quản lý kèm ảnh).' : ''}
                  </p>
                )}
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
