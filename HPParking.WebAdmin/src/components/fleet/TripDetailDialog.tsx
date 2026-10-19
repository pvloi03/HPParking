import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  Truck,
  Route,
  Clock,
  Calendar,
  CreditCard,
  MapPin,
  CheckCircle2,
  AlertTriangle,
  ArrowRightCircle,
  ArrowLeftCircle,
  Eye,
  Camera,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { fleetDispatchApi } from '@/api/fleetDispatchApi';
import {
  TripStatus,
  TripStatusLabel,
  type FleetTripDto,
  type TripCheckpointDto,
} from '@/types/fleetDispatch';
import { formatImageUrl, hasImagePath } from '@/components/parkingSessions/EvidenceImageGrid';

function formatOverdueText(seconds: number): string {
  if (seconds <= 0) return 'Đúng hạn';
  const min = Math.floor(seconds / 60);
  const sec = Math.floor(seconds % 60);
  if (min > 0) {
    return `Quá ${min}p ${sec > 0 ? `${sec}s` : ''}`.trim();
  }
  return `Quá ${sec}s`;
}

export interface TripDetailDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  tripId: string | null;
  initialTrip?: FleetTripDto | null;
}

export function TripDetailDialog({
  open,
  onOpenChange,
  tripId,
  initialTrip,
}: TripDetailDialogProps) {
  const [previewCheckpoint, setPreviewCheckpoint] = useState<TripCheckpointDto | null>(null);

  const { data: tripData, isLoading } = useQuery({
    queryKey: ['fleet-trip-detail', tripId],
    queryFn: () => (tripId ? fleetDispatchApi.getById(tripId) : null),
    enabled: Boolean(open && tripId),
    initialData: initialTrip ?? undefined,
  });

  const trip = tripData || initialTrip;

  const getStatusBadge = (status?: TripStatus) => {
    switch (status) {
      case TripStatus.Idle:
        return (
          <Badge variant="outline" className="text-muted-foreground text-xs">
            {TripStatusLabel[TripStatus.Idle]}
          </Badge>
        );
      case TripStatus.InTransit:
        return (
          <Badge className="bg-blue-600 hover:bg-blue-600 text-white text-xs">
            {TripStatusLabel[TripStatus.InTransit]}
          </Badge>
        );
      case TripStatus.WorkingAtGate:
        return (
          <Badge className="bg-amber-600 hover:bg-amber-600 text-white text-xs">
            {TripStatusLabel[TripStatus.WorkingAtGate]}
          </Badge>
        );
      case TripStatus.OverdueTransit:
      case TripStatus.OverdueStay:
        return (
          <Badge className="bg-rose-600 hover:bg-rose-600 text-white text-xs animate-pulse">
            {TripStatusLabel[status]}
          </Badge>
        );
      case TripStatus.Completed:
        return (
          <Badge className="bg-emerald-600 hover:bg-emerald-600 text-white text-xs">
            {TripStatusLabel[TripStatus.Completed]}
          </Badge>
        );
      default:
        return null;
    }
  };

  const checkpoints: TripCheckpointDto[] = trip?.checkpoints || [];

  return (
    <>
      <Dialog open={open} onOpenChange={onOpenChange}>
        <DialogContent className="max-w-4xl max-h-[90vh] p-0 overflow-hidden flex flex-col bg-background text-foreground border-border shadow-2xl">
          {/* Header */}
          <DialogHeader className="p-4 sm:p-5 pb-3 border-b border-border bg-muted/20">
            <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2.5 pr-10 sm:pr-12">
              <div className="flex items-center gap-2.5 min-w-0">
                <div className="p-2 rounded-lg bg-blue-500/10 text-blue-600 dark:text-blue-400 shrink-0">
                  <Truck className="h-5 w-5" />
                </div>
                <div className="min-w-0">
                  <DialogTitle className="text-base sm:text-lg font-bold tracking-tight flex items-center gap-2 flex-wrap">
                    <span>Chi Tiết Hành Trình Phương Tiện Nội Bộ</span>
                    {trip && (
                      <span className="font-mono px-2 py-0.5 rounded bg-muted text-foreground text-sm font-semibold border border-border">
                        {trip.plateNumber}
                      </span>
                    )}
                  </DialogTitle>
                  <p className="text-xs text-muted-foreground mt-0.5 truncate sm:whitespace-normal">
                    Hồ sơ lưu trữ toàn bộ các mốc kiểm soát ra vào và tuân thủ lộ trình tuyến xe.
                  </p>
                </div>
              </div>
              <div className="shrink-0">{trip && getStatusBadge(trip.status)}</div>
            </div>
          </DialogHeader>

          {/* Content Body */}
          <div className="flex-1 overflow-y-auto p-4 sm:p-6 space-y-5">
            {isLoading && !trip ? (
              <div className="space-y-4">
                <Skeleton className="h-24 w-full rounded-lg" />
                <Skeleton className="h-48 w-full rounded-lg" />
              </div>
            ) : !trip ? (
              <div className="text-center py-10 text-muted-foreground text-sm">
                Không tìm thấy dữ liệu chuyến xe.
              </div>
            ) : (
              <>
                {/* Thông tin chung */}
                <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 bg-muted/30 p-3.5 rounded-lg border border-border">
                  <div>
                    <span className="text-[11px] text-muted-foreground flex items-center gap-1">
                      <Route className="h-3 w-3 text-blue-500" /> Tuyến điều vận
                    </span>
                    <span className="text-xs font-semibold text-foreground mt-0.5 block truncate">
                      {trip.routeName || 'Tuyến tự do'}
                    </span>
                  </div>

                  <div>
                    <span className="text-[11px] text-muted-foreground flex items-center gap-1">
                      <CreditCard className="h-3 w-3 text-emerald-500" /> Thẻ xe gắn kèm
                    </span>
                    <span className="text-xs font-mono font-medium text-foreground mt-0.5 block">
                      {trip.cardNumber || '---'}
                    </span>
                  </div>

                  <div>
                    <span className="text-[11px] text-muted-foreground flex items-center gap-1">
                      <MapPin className="h-3 w-3 text-amber-500" /> Cổng xuất phát
                    </span>
                    <span className="text-xs font-semibold text-foreground mt-0.5 block truncate">
                      {trip.originGateName || trip.originGateId || '---'}
                    </span>
                  </div>

                  <div>
                    <span className="text-[11px] text-muted-foreground flex items-center gap-1">
                      <MapPin className="h-3 w-3 text-purple-500" /> Cổng gần nhất
                    </span>
                    <span className="text-xs font-semibold text-foreground mt-0.5 block truncate">
                      {trip.currentGateName || trip.currentGateId || '---'}
                    </span>
                    {trip.nextGateName && trip.status !== TripStatus.Completed && (
                      <span className="text-[10px] text-blue-600 dark:text-blue-400 font-medium block truncate mt-0.5">
                        {trip.status === TripStatus.InTransit || trip.status === TripStatus.OverdueTransit
                          ? `➔ Đang đến: ${trip.nextGateName}`
                          : `➔ Điểm tiếp theo: ${trip.nextGateName}`}
                      </span>
                    )}
                  </div>

                  <div>
                    <span className="text-[11px] text-muted-foreground flex items-center gap-1">
                      <Calendar className="h-3 w-3 text-sky-500" /> Thời gian bắt đầu
                    </span>
                    <span className="text-xs font-medium text-foreground mt-0.5 block">
                      {new Date(trip.startTime).toLocaleString('vi-VN')}
                    </span>
                  </div>

                  <div>
                    <span className="text-[11px] text-muted-foreground flex items-center gap-1">
                      <Clock className="h-3 w-3 text-teal-500" /> Thời gian kết thúc
                    </span>
                    <span className="text-xs font-medium text-foreground mt-0.5 block">
                      {trip.endTime
                        ? new Date(trip.endTime).toLocaleString('vi-VN')
                        : 'Đang hoạt động'}
                    </span>
                  </div>

                  <div>
                    <span className="text-[11px] text-muted-foreground flex items-center gap-1">
                      <Clock className="h-3 w-3 text-rose-500" /> Hạn chót kế tiếp
                    </span>
                    <span className="text-xs font-medium text-foreground mt-0.5 block">
                      {trip.nextDeadline
                        ? new Date(trip.nextDeadline).toLocaleTimeString('vi-VN')
                        : '---'}
                    </span>
                  </div>

                  <div>
                    <span className="text-[11px] text-muted-foreground flex items-center gap-1">
                      <AlertTriangle className="h-3 w-3 text-amber-500" /> Tình trạng vi phạm
                    </span>
                    <span className="text-xs font-medium mt-0.5 block">
                      {trip.isOverdue ? (
                        <span className="text-rose-600 font-semibold flex items-center gap-1">
                          <AlertTriangle className="h-3.5 w-3.5" /> Quá giờ quy định
                        </span>
                      ) : (
                        <span className="text-emerald-600 font-semibold flex items-center gap-1">
                          <CheckCircle2 className="h-3.5 w-3.5" /> Đúng quy chuẩn
                        </span>
                      )}
                    </span>
                  </div>
                </div>

                {/* Timeline / Lịch sử mốc kiểm soát Checkpoints */}
                <div className="space-y-3">
                  <div className="flex items-center justify-between">
                    <h3 className="text-sm font-semibold tracking-tight text-foreground flex items-center gap-2">
                      <Clock className="h-4 w-4 text-blue-600" />
                      <span>Lịch Sử Di Chuyển & Điểm Kiểm Soát ({checkpoints.length} mốc)</span>
                    </h3>
                    <span className="text-[11px] text-muted-foreground">
                      Lưu trữ vĩnh viễn, không xóa
                    </span>
                  </div>

                  {checkpoints.length === 0 ? (
                    <div className="p-6 text-center rounded-lg border border-dashed border-border bg-muted/10 text-muted-foreground text-xs">
                      Chưa ghi nhận mốc kiểm soát nào trong chuyến này.
                    </div>
                  ) : (
                    <div className="border border-border rounded-lg overflow-hidden">
                      <table className="w-full text-left text-xs">
                        <thead className="bg-muted/50 border-b border-border text-[11px] text-muted-foreground uppercase font-semibold">
                          <tr>
                            <th className="py-2.5 px-3">Chặng</th>
                            <th className="py-2.5 px-3">Cổng kiểm soát</th>
                            <th className="py-2.5 px-3">Chiều</th>
                            <th className="py-2.5 px-3">Thời gian quẹt</th>
                            <th className="py-2.5 px-3">Biển số OCR</th>
                            <th className="py-2.5 px-3">Lộ trình</th>
                            <th className="py-2.5 px-3">Tiến độ SLA</th>
                            <th className="py-2.5 px-3">Ghi chú</th>
                            <th className="py-2.5 px-3 text-center">Ảnh</th>
                          </tr>
                        </thead>
                        <tbody className="divide-y divide-border">
                          {checkpoints.map((cp, idx) => {
                            const isCompliant = cp.isRouteCompliant !== false;
                            const isEntry =
                              cp.direction === 1 ||
                              cp.direction === 'In' ||
                              cp.direction === '1';
                            const hasOverview = hasImagePath(cp.overviewImagePath);
                            const hasPlate = hasImagePath(cp.plateImagePath);
                            const imageCount = (hasOverview ? 1 : 0) + (hasPlate ? 1 : 0);

                            return (
                              <tr
                                key={idx}
                                className={`hover:bg-muted/30 transition-colors ${!isCompliant ? 'bg-rose-500/5' : ''
                                  }`}
                              >
                                <td className="py-2 px-3 font-semibold text-foreground">
                                  #{cp.stepIndex}
                                </td>
                                <td className="py-2 px-3 font-medium text-foreground">
                                  {cp.gateName || cp.gateId}
                                </td>
                                <td className="py-2 px-3">
                                  {isEntry ? (
                                    <span className="inline-flex items-center gap-1 text-emerald-600 dark:text-emerald-400 font-medium">
                                      <ArrowRightCircle className="h-3.5 w-3.5" /> Vào cổng
                                    </span>
                                  ) : (
                                    <span className="inline-flex items-center gap-1 text-amber-600 dark:text-amber-400 font-medium">
                                      <ArrowLeftCircle className="h-3.5 w-3.5" /> Ra khỏi cổng
                                    </span>
                                  )}
                                </td>
                                <td className="py-2 px-3 text-muted-foreground whitespace-nowrap">
                                  {new Date(cp.timestamp).toLocaleString('vi-VN')}
                                </td>
                                <td className="py-2 px-3 font-mono font-semibold">
                                  {cp.plateDetected || '---'}
                                </td>
                                <td className="py-2 px-3">
                                  {isCompliant ? (
                                    <Badge className="bg-emerald-600/10 text-emerald-600 hover:bg-emerald-600/10 border-emerald-600/20 text-[10px] px-2 py-0">
                                      Đúng tuyến
                                    </Badge>
                                  ) : (
                                    <Badge className="bg-rose-600/15 text-rose-600 hover:bg-rose-600/15 border-rose-600/30 text-[10px] px-2 py-0 font-semibold animate-pulse">
                                      Lạc tuyến
                                    </Badge>
                                  )}
                                </td>
                                <td className="py-2 px-3">
                                  {cp.slaOverdue?.isOverdue ? (
                                    <Badge variant="destructive" className="text-[10px] px-2 py-0 gap-1 font-semibold">
                                      <AlertTriangle className="h-3 w-3" />
                                      {formatOverdueText(cp.slaOverdue.overdueSeconds)}
                                    </Badge>
                                  ) : (
                                    <Badge className="bg-emerald-600/10 text-emerald-600 hover:bg-emerald-600/10 border-emerald-600/20 text-[10px] px-2 py-0 font-normal">
                                      <CheckCircle2 className="h-3 w-3 mr-0.5" /> Đúng hạn
                                    </Badge>
                                  )}
                                </td>
                                <td className="py-2 px-3 text-muted-foreground max-w-[200px] truncate">
                                  {cp.note || '---'}
                                </td>
                                <td className="py-2 px-3 text-center">
                                  {imageCount > 0 ? (
                                    <button
                                      type="button"
                                      onClick={() => setPreviewCheckpoint(cp)}
                                      className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded bg-muted hover:bg-accent text-foreground text-[11px] font-medium border border-border cursor-pointer transition-colors"
                                      title="Xem ảnh camera mốc kiểm soát"
                                    >
                                      <Eye className="h-3 w-3 text-blue-500" />
                                      <span>Xem ảnh</span>
                                      <span className="text-[10px] font-semibold px-1.5 py-0.2 rounded bg-blue-500/10 text-blue-600 dark:text-blue-400">
                                        {imageCount}
                                      </span>
                                    </button>
                                  ) : (
                                    <span className="text-muted-foreground text-[11px]">---</span>
                                  )}
                                </td>
                              </tr>
                            );
                          })}
                        </tbody>
                      </table>
                    </div>
                  )}
                </div>
              </>
            )}
          </div>
        </DialogContent>
      </Dialog>

      {/* Modal xem ảnh bằng chứng điểm kiểm soát (Ảnh toàn cảnh & Ảnh biển số) */}
      {previewCheckpoint && (
        <Dialog open={Boolean(previewCheckpoint)} onOpenChange={() => setPreviewCheckpoint(null)}>
          <DialogContent className="max-w-4xl max-h-[90vh] p-0 overflow-hidden flex flex-col bg-background text-foreground border-border shadow-2xl">
            <DialogHeader className="p-4 sm:p-5 pb-3 border-b border-border bg-muted/20">
              <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2.5 pr-10 sm:pr-12">
                <div className="flex items-center gap-2.5 min-w-0">
                  <div className="p-2 rounded-lg bg-blue-500/10 text-blue-600 dark:text-blue-400 shrink-0">
                    <Camera className="h-5 w-5" />
                  </div>
                  <div className="min-w-0">
                    <DialogTitle className="text-base sm:text-lg font-bold tracking-tight flex items-center gap-2 flex-wrap">
                      <span>Ảnh Bằng Chứng Mốc Kiểm Soát #{previewCheckpoint.stepIndex}</span>
                      <span className="font-mono text-xs px-2 py-0.5 rounded bg-muted border border-border font-semibold">
                        {previewCheckpoint.gateName || previewCheckpoint.gateId}
                      </span>
                    </DialogTitle>
                    <p className="text-xs text-muted-foreground mt-0.5 truncate sm:whitespace-normal">
                      Thời điểm ghi nhận:{' '}
                      <span className="font-medium text-foreground">
                        {new Date(previewCheckpoint.timestamp).toLocaleString('vi-VN')}
                      </span>{' '}
                      —{' '}
                      {previewCheckpoint.direction === 1 ||
                        previewCheckpoint.direction === 'In' ||
                        previewCheckpoint.direction === '1' ? (
                        <span className="text-emerald-600 dark:text-emerald-400 font-medium">Vào cổng</span>
                      ) : (
                        <span className="text-amber-600 dark:text-amber-400 font-medium">Ra khỏi cổng</span>
                      )}
                    </p>
                  </div>
                </div>
                <div className="shrink-0">
                  {previewCheckpoint.plateDetected && (
                    <Badge variant="outline" className="font-mono text-xs px-2.5 py-1 bg-muted/50 border-border">
                      Biển số: {previewCheckpoint.plateDetected}
                    </Badge>
                  )}
                </div>
              </div>
            </DialogHeader>

            <div className="flex-1 overflow-y-auto p-4 sm:p-6">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {/* 1. Ảnh Toàn Cảnh / Xe & Tài Xế */}
                <div className="flex flex-col rounded-lg border border-border bg-muted/20 overflow-hidden">
                  <div className="px-3.5 py-2.5 bg-muted/40 border-b border-border flex items-center justify-between">
                    <span className="text-xs font-semibold text-foreground flex items-center gap-1.5">
                      <Camera className="h-3.5 w-3.5 text-blue-500" />
                      Ảnh Toàn Cảnh / Cabin Xe
                    </span>
                    {hasImagePath(previewCheckpoint.overviewImagePath) && (
                      <Badge variant="outline" className="text-[10px] text-emerald-600 border-emerald-500/30 bg-emerald-500/5">
                        Có ảnh
                      </Badge>
                    )}
                  </div>
                  <div className="p-3 flex items-center justify-center min-h-[220px] bg-black/5 dark:bg-black/20">
                    {hasImagePath(previewCheckpoint.overviewImagePath) ? (
                      <img
                        src={formatImageUrl(previewCheckpoint.overviewImagePath)}
                        alt={`Ảnh toàn cảnh chặng ${previewCheckpoint.stepIndex}`}
                        className="max-h-[320px] w-full rounded object-contain transition-transform hover:scale-[1.01]"
                        onError={(e) => {
                          (e.currentTarget as HTMLImageElement).src = '';
                        }}
                      />
                    ) : (
                      <div className="text-center py-10 text-muted-foreground text-xs">
                        <Camera className="h-8 w-8 mx-auto mb-2 opacity-30" />
                        Chưa có ảnh toàn cảnh cho mốc này
                      </div>
                    )}
                  </div>
                </div>

                {/* 2. Ảnh Nhận Dạng Biển Số */}
                <div className="flex flex-col rounded-lg border border-border bg-muted/20 overflow-hidden">
                  <div className="px-3.5 py-2.5 bg-muted/40 border-b border-border flex items-center justify-between">
                    <span className="text-xs font-semibold text-foreground flex items-center gap-1.5">
                      <Eye className="h-3.5 w-3.5 text-amber-500" />
                      Ảnh Nhận Dạng Biển Số (LPR)
                    </span>
                    {hasImagePath(previewCheckpoint.plateImagePath) && (
                      <Badge variant="outline" className="text-[10px] text-emerald-600 border-emerald-500/30 bg-emerald-500/5">
                        Có ảnh
                      </Badge>
                    )}
                  </div>
                  <div className="p-3 flex items-center justify-center min-h-[220px] bg-black/5 dark:bg-black/20">
                    {hasImagePath(previewCheckpoint.plateImagePath) ? (
                      <img
                        src={formatImageUrl(previewCheckpoint.plateImagePath)}
                        alt={`Ảnh biển số chặng ${previewCheckpoint.stepIndex}`}
                        className="max-h-[320px] w-full rounded object-contain transition-transform hover:scale-[1.01]"
                        onError={(e) => {
                          (e.currentTarget as HTMLImageElement).src = '';
                        }}
                      />
                    ) : (
                      <div className="text-center py-10 text-muted-foreground text-xs">
                        <Eye className="h-8 w-8 mx-auto mb-2 opacity-30" />
                        Chưa có ảnh biển số cho mốc này
                      </div>
                    )}
                  </div>
                </div>
              </div>
            </div>

            <div className="p-3 px-5 border-t border-border bg-muted/10 flex justify-end">
              <Button
                variant="outline"
                size="sm"
                onClick={() => setPreviewCheckpoint(null)}
                className="text-xs"
                aria-label="Đóng xem ảnh"
              >
                Đóng
              </Button>
            </div>
          </DialogContent>
        </Dialog>
      )}
    </>
  );
}
