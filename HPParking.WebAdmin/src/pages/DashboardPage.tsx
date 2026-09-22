import { RefreshCw, History, AlertCircle } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { KpiCardGrid } from '@/components/dashboard/KpiCardGrid';
import { TrafficBarChart } from '@/components/dashboard/TrafficBarChart';
import {
  useDashboardKPIs,
  useRecentSessions,
} from '@/hooks/useDashboard';
import { ParkingSessionStatus } from '@/types/parkingSession';

function getVehicleTypeName(type?: number | string | null): string {
  if (type === 1 || type === 'Car') return 'Ô tô';
  if (type === 2 || type === 'Motorbike') return 'Xe máy';
  if (type === 3 || type === 'Bicycle') return 'Xe đạp';
  if (type === 4 || type === 'Other') return 'Khác';
  return 'Phương tiện';
}

function formatDuration(minutes?: number | null, inTime?: string): string {
  if (minutes != null && minutes > 0) {
    if (minutes < 1) return '< 1 phút';
    if (minutes < 60) return `${Math.round(minutes)} phút`;
    const hours = Math.floor(minutes / 60);
    const mins = Math.round(minutes % 60);
    if (hours < 24) return `${hours} giờ ${mins > 0 ? `${mins}p` : ''}`;
    const days = Math.floor(hours / 24);
    const remainHours = hours % 24;
    return `${days} ngày ${remainHours > 0 ? `${remainHours}h` : ''}`;
  }
  if (inTime) {
    const elapsedMinutes = Math.max(0, (Date.now() - new Date(inTime).getTime()) / 60000);
    if (elapsedMinutes < 1) return '< 1 phút';
    if (elapsedMinutes < 60) return `${Math.round(elapsedMinutes)} phút`;
    const hours = Math.floor(elapsedMinutes / 60);
    return `${hours} giờ`;
  }
  return '—';
}

export function DashboardPage() {
  const {
    data: kpiData,
    isLoading: isKpiLoading,
    isRefetching: isKpiRefetching,
    refetch: refetchKpis,
    error: kpiError,
  } = useDashboardKPIs();

  const {
    data: recentSessions,
    isLoading: isSessionsLoading,
    isRefetching: isSessionsRefetching,
    refetch: refetchSessions,
  } = useRecentSessions(5);

  const isRefreshing = isKpiRefetching || isSessionsRefetching;

  const handleRefresh = async () => {
    await Promise.all([refetchKpis(), refetchSessions()]);
  };

  return (
    <div className="space-y-6">
      {/* Top Control Bar: Title & Refresh */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-xl font-bold tracking-tight text-foreground">
            Tổng Quan Hoạt Động Bãi Xe
          </h1>
          <p className="text-xs text-muted-foreground mt-0.5">
            Dữ liệu giám sát tự động cập nhật thời gian thực từ API HPParking
          </p>
        </div>

        <Button
          variant="outline"
          size="sm"
          onClick={handleRefresh}
          disabled={isRefreshing}
          className="h-8 gap-1.5 text-xs cursor-pointer min-h-[36px]"
        >
          <RefreshCw
            className={`h-3.5 w-3.5 ${isRefreshing ? 'animate-spin' : ''}`}
          />
          <span className="hidden sm:inline">Làm mới</span>
        </Button>
      </div>

      {kpiError && (
        <div className="p-3.5 rounded-xl bg-amber-50 dark:bg-amber-950/40 border border-amber-200 dark:border-amber-800/60 text-amber-800 dark:text-amber-300 text-xs flex items-center gap-2.5">
          <AlertCircle className="h-4 w-4 shrink-0" />
          <span>
            Không thể kết nối đến dịch vụ thống kê. Đang hiển thị bộ nhớ đệm
            hoặc vui lòng kiểm tra kết nối API.
          </span>
        </div>
      )}

      {/* 6 Primary KPI Cards */}
      <KpiCardGrid data={kpiData} isLoading={isKpiLoading} />

      {/* Traffic Flow Bar Chart (shadcn/ui + Recharts) */}
      <TrafficBarChart isLoading={isKpiLoading || isRefreshing} />

      {/* Recent Parking Activity Table (Connected to API) */}
      <Card className="border-border/80 shadow-xs">
        <CardHeader className="pb-3">
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="text-sm font-bold flex items-center gap-2">
                <History className="h-4 w-4 text-blue-600 dark:text-blue-400" />
                Hoạt Động Ra Vào Gần Nhất
              </CardTitle>
              <CardDescription className="text-xs text-muted-foreground mt-0.5">
                Nhật ký phương tiện vừa quét thẻ / nhận diện biển số tại các làn
              </CardDescription>
            </div>
            <Button
              variant="ghost"
              size="sm"
              className="text-xs text-blue-600 dark:text-blue-400 hover:text-blue-700"
            >
              Xem tất cả lịch sử →
            </Button>
          </div>
        </CardHeader>
        <CardContent className="p-0">
          {isSessionsLoading ? (
            <div className="p-4 space-y-3">
              {Array.from({ length: 3 }).map((_, i) => (
                <div key={i} className="flex items-center gap-4">
                  <Skeleton className="h-4 w-24" />
                  <Skeleton className="h-4 w-28" />
                  <Skeleton className="h-4 w-32" />
                  <Skeleton className="h-4 w-24" />
                  <Skeleton className="h-4 w-16" />
                </div>
              ))}
            </div>
          ) : !recentSessions || recentSessions.length === 0 ? (
            <div className="py-12 text-center text-xs text-muted-foreground">
              Chưa ghi nhận lượt xe ra vào trong phiên làm việc hiện tại.
            </div>
          ) : (
            <>
              {/* Mobile Stacked Cards View (< 640px) */}
              <div className="block sm:hidden divide-y divide-border">
                {recentSessions.map((session) => (
                  <div
                    key={session.id}
                    className="p-4 space-y-2.5 hover:bg-muted/20 transition-colors"
                  >
                    <div className="flex items-center justify-between">
                      <span className="font-mono font-bold text-sm text-foreground">
                        {session.plateNumber}
                      </span>
                      {session.isPlateMismatch ? (
                        <Badge variant="warning" className="text-[10px]">
                          Lệch biển số
                        </Badge>
                      ) : session.status === ParkingSessionStatus.Active ? (
                        <Badge variant="success" className="text-[10px]">
                          Đang đỗ
                        </Badge>
                      ) : (
                        <Badge variant="secondary" className="text-[10px]">
                          Hoàn tất
                        </Badge>
                      )}
                    </div>

                    <div className="grid grid-cols-2 gap-2 text-xs text-muted-foreground">
                      <div>
                        <span className="text-slate-400 block text-[10px]">
                          Khách / Xe:
                        </span>
                        <span className="text-foreground font-medium">
                          {session.clientName || getVehicleTypeName(session.vehicleType)}
                        </span>
                      </div>
                      <div>
                        <span className="text-slate-400 block text-[10px]">
                          Làn vào:
                        </span>
                        <span className="text-foreground font-medium truncate block">
                          {session.inLaneName || session.laneInName || 'Cổng chính'}
                        </span>
                      </div>
                      <div>
                        <span className="text-slate-400 block text-[10px]">
                          Thời điểm vào:
                        </span>
                        <span className="font-mono">
                          {new Date(session.inTime).toLocaleTimeString('vi-VN')}
                        </span>
                      </div>
                      <div>
                        <span className="text-slate-400 block text-[10px]">
                          Thời lượng:
                        </span>
                        <span className="font-medium">
                          {formatDuration(session.durationMinutes, session.inTime)}
                        </span>
                      </div>
                    </div>

                    <div className="pt-1 flex items-center justify-end">
                      <button
                        type="button"
                        className="text-xs font-semibold text-blue-600 dark:text-blue-400 hover:underline cursor-pointer min-h-[36px] flex items-center"
                      >
                        Xem chi tiết ảnh đối soát →
                      </button>
                    </div>
                  </div>
                ))}
              </div>

              {/* Desktop & Tablet Table View (>= 640px) */}
              <div className="hidden sm:block overflow-x-auto">
                <table className="w-full text-left border-collapse text-xs">
                  <thead>
                    <tr className="border-y border-border bg-muted/40 text-[11px] uppercase tracking-wider text-muted-foreground font-semibold">
                      <th className="py-2.5 px-4">Biển Số Xe</th>
                      <th className="py-2.5 px-4">Khách Hàng / Loại Xe</th>
                      <th className="py-2.5 px-4">Làn Vào</th>
                      <th className="py-2.5 px-4">Thời Điểm Vào</th>
                      <th className="py-2.5 px-4">Thời Lượng</th>
                      <th className="py-2.5 px-4">Trạng Thái</th>
                      <th className="py-2.5 px-4 text-right">Hành Động</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border">
                    {recentSessions.map((session) => (
                      <tr
                        key={session.id}
                        className="hover:bg-muted/30 transition-colors"
                      >
                        <td className="py-3 px-4 font-mono font-bold text-foreground">
                          {session.plateNumber}
                        </td>
                        <td className="py-3 px-4 text-muted-foreground">
                          {session.clientName || getVehicleTypeName(session.vehicleType)}
                        </td>
                        <td className="py-3 px-4 text-muted-foreground">
                          {session.inLaneName || session.laneInName || 'Cổng chính'}
                        </td>
                        <td className="py-3 px-4 font-mono text-muted-foreground">
                          {new Date(session.inTime).toLocaleString('vi-VN')}
                        </td>
                        <td className="py-3 px-4 text-muted-foreground">
                          {formatDuration(session.durationMinutes, session.inTime)}
                        </td>
                        <td className="py-3 px-4">
                          {session.isPlateMismatch ? (
                            <Badge variant="warning" className="text-[10px]">
                              Lệch biển số
                            </Badge>
                          ) : session.status === ParkingSessionStatus.Active ? (
                            <Badge variant="success" className="text-[10px]">
                              Đang đỗ
                            </Badge>
                          ) : (
                            <Badge variant="secondary" className="text-[10px]">
                              Hoàn tất
                            </Badge>
                          )}
                        </td>
                        <td className="py-3 px-4 text-right">
                          <button
                            type="button"
                            className="text-xs font-semibold text-blue-600 dark:text-blue-400 hover:underline cursor-pointer"
                          >
                            Chi tiết ảnh
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

export default DashboardPage;
