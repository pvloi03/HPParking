import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  Car,
  ShieldAlert,
  Timer,
  Clock,
  ArrowRightCircle,
  ArrowLeftCircle,
  User,
  Phone,
  Layers,
  Eye,
  FileText,
  Info,
  CheckCircle2,
  AlertCircle,
} from 'lucide-react';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { EvidenceImageGrid, hasImagePath } from './EvidenceImageGrid';
import { parkingSessionApi } from '@/api/parkingSessionApi';
import { ParkingSessionStatus } from '@/types/parkingSession';
import { VehicleType } from '@/types/vehicle';

export interface ParkingSessionDetailDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  sessionId: string | null;
}

export function ParkingSessionDetailDialog({
  open,
  onOpenChange,
  sessionId,
}: ParkingSessionDetailDialogProps) {
  const [activeTab, setActiveTab] = useState<'all' | 'slider' | 'details'>('all');

  const { data: session, isLoading } = useQuery({
    queryKey: ['parking-session-detail', sessionId],
    queryFn: () => (sessionId ? parkingSessionApi.getSessionById(sessionId) : null),
    enabled: Boolean(open && sessionId),
  });

  const getStatusBadge = (status?: ParkingSessionStatus) => {
    switch (status) {
      case ParkingSessionStatus.Active:
        return (
          <Badge className="bg-blue-600 hover:bg-blue-600 text-white font-medium text-xs px-2.5 py-0.5 shadow-xs">
            Đang trong bãi
          </Badge>
        );
      case ParkingSessionStatus.Completed:
        return (
          <Badge className="bg-emerald-600 hover:bg-emerald-600 text-white font-medium text-xs px-2.5 py-0.5 shadow-xs">
            Đã hoàn thành
          </Badge>
        );
      case ParkingSessionStatus.UnmatchedOut:
        return (
          <Badge className="bg-rose-600 hover:bg-rose-600 text-white font-semibold text-xs px-2.5 py-0.5 shadow-xs animate-pulse">
            Ra không vào / Lệch biển
          </Badge>
        );
      case ParkingSessionStatus.Cancelled:
        return (
          <Badge variant="secondary" className="text-xs px-2.5 py-0.5">
            Đã hủy bỏ
          </Badge>
        );
      default:
        return null;
    }
  };

  const getVehicleTypeName = (type?: VehicleType) => {
    switch (type) {
      case VehicleType.Car:
        return 'Ô tô';
      case VehicleType.Motorbike:
        return 'Xe máy';
      default:
        return 'Khác';
    }
  };

  const isMismatch = session?.status === ParkingSessionStatus.UnmatchedOut;
  const isActive = session?.status === ParkingSessionStatus.Active;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-3xl max-h-[90vh] p-0 overflow-hidden flex flex-col bg-background text-foreground border-border shadow-2xl">
        {/* Header Dialog */}
        <DialogHeader className="p-4 sm:p-5 pb-3 border-b border-border bg-muted/20">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pr-10">
            <DialogTitle className="flex items-center gap-2.5 text-base sm:text-lg font-bold">
              <div className="h-8 w-8 rounded-lg bg-primary/10 text-primary flex items-center justify-center shrink-0">
                <Car className="h-4.5 w-4.5" />
              </div>
              <div className="truncate">
                <span className="font-bold tracking-wide">
                  Chi Tiết Lượt Xe: {session?.plateNumber || 'Đang tải...'}
                </span>
                <span className="text-xs text-muted-foreground ml-2 font-normal hidden sm:inline">
                  ({session?.personFullName || 'Khách vãng lai'} • {session ? getVehicleTypeName(session.vehicleType) : ''})
                </span>
              </div>
            </DialogTitle>

            {/* Badges trạng thái & thời lượng */}
            <div className="flex items-center gap-1.5 shrink-0">
              {session && getStatusBadge(session.status)}
              {session?.inTime && (
                <Badge
                  variant="outline"
                  className="bg-background border-border text-foreground font-mono text-[11px] px-2 py-0.5 flex items-center gap-1 shadow-2xs"
                >
                  <Timer className="h-3 w-3 text-primary" />
                  {session.durationFormatted || '--'}
                  {isActive && ' (đang đỗ)'}
                </Badge>
              )}
            </div>
          </div>

          {/* View Switcher Tabs (Chuẩn PhuXuan) */}
          <div className="flex items-center gap-1 mt-2.5 pt-2 border-t border-border/60">
            <button
              type="button"
              onClick={() => setActiveTab('all')}
              className={`text-xs px-3 py-1 rounded-md font-medium transition-colors cursor-pointer flex items-center gap-1.5 ${
                activeTab === 'all'
                  ? 'bg-blue-600 text-white shadow-xs'
                  : 'text-muted-foreground hover:bg-muted'
              }`}
            >
              <Layers className="h-3.5 w-3.5" />
              Tổng Quan (Ảnh & Thông Tin)
            </button>
            <button
              type="button"
              onClick={() => setActiveTab('slider')}
              className={`text-xs px-3 py-1 rounded-md font-medium transition-colors cursor-pointer flex items-center gap-1.5 ${
                activeTab === 'slider'
                  ? 'bg-blue-600 text-white shadow-xs'
                  : 'text-muted-foreground hover:bg-muted'
              }`}
            >
              <Eye className="h-3.5 w-3.5" />
              Slide Ảnh (4)
            </button>
            <button
              type="button"
              onClick={() => setActiveTab('details')}
              className={`text-xs px-3 py-1 rounded-md font-medium transition-colors cursor-pointer flex items-center gap-1.5 ${
                activeTab === 'details'
                  ? 'bg-blue-600 text-white shadow-xs'
                  : 'text-muted-foreground hover:bg-muted'
              }`}
            >
              <FileText className="h-3.5 w-3.5" />
              Bảng Thông Số Chi Tiết
            </button>
          </div>
        </DialogHeader>

        {/* Nội dung Dialog */}
        <div className="flex-1 overflow-y-auto p-4 sm:p-5 space-y-4">
          {isLoading ? (
            <div className="space-y-3">
              <Skeleton className="h-44 w-full rounded-xl" />
              <div className="grid grid-cols-2 gap-3">
                <Skeleton className="h-32 rounded-xl" />
                <Skeleton className="h-32 rounded-xl" />
              </div>
            </div>
          ) : session ? (
            <>
              {/* BANNER CẢNH BÁO AN NINH ĐỎ RỰC KHI LỆCH BIỂN SỐ */}
              {isMismatch && (
                <div className="p-3.5 rounded-xl bg-rose-50 dark:bg-rose-950/40 border border-rose-200 dark:border-rose-800 text-rose-800 dark:text-rose-300 text-xs flex items-start gap-3 shadow-xs animate-in fade-in">
                  <ShieldAlert className="h-5 w-5 text-rose-600 shrink-0 mt-0.5" />
                  <div>
                    <h4 className="font-bold text-sm text-rose-900 dark:text-rose-200 flex items-center gap-1.5">
                      CẢNH BÁO AN NINH: PHIÊN ĐỖ XE LỆCH BIỂN SỐ / KHÔNG KHỚP LƯỢT VÀO
                    </h4>
                    <p className="mt-1 leading-relaxed text-rose-700 dark:text-rose-300">
                      Hệ thống phát hiện sai lệch biển số giữa lượt vào và lượt ra, hoặc phương tiện
                      rời bãi không tìm thấy phiên vào hợp lệ tương ứng. Đề nghị nhân viên an ninh
                      kiểm tra kỹ 4 ảnh bằng chứng và liên hệ chủ phương tiện để xác minh!
                    </p>
                  </div>
                </div>
              )}

              {/* PHẦN 1: BỘ 4 ẢNH BẰNG CHỨNG SLIDE (HIỂN THỊ Ở TAB ALL VÀ SLIDER) */}
              {(activeTab === 'all' || activeTab === 'slider') && (
                <EvidenceImageGrid
                  plateNumber={session.plateNumber}
                  inOverviewImagePath={session.inOverviewImagePath}
                  inPlateImagePath={session.inPlateImagePath}
                  outOverviewImagePath={session.outOverviewImagePath}
                  outPlateImagePath={session.outPlateImagePath}
                  inLaneName={session.inLaneName}
                  outLaneName={session.outLaneName}
                  inTime={session.inTime}
                  outTime={session.outTime}
                  isActiveSession={isActive}
                />
              )}

              {/* PHẦN 2: 4 CARDS THÔNG SỐ CHI TIẾT (HIỂN THỊ Ở TAB ALL VÀ DETAILS) */}
              {(activeTab === 'all' || activeTab === 'details') && (
                <div className="space-y-3">
                  <div className="flex items-center gap-1.5 text-xs font-bold text-slate-800 dark:text-slate-200">
                    <Info className="h-4 w-4 text-blue-600 dark:text-blue-400" />
                    <span>THÔNG TIN CHI TIẾT ĐẦY ĐỦ CỦA LƯỢT XE</span>
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-2 gap-3 text-xs">
                    {/* Card 1: Phương Tiện & Chủ Xe */}
                    <div className="p-3.5 rounded-xl border border-border bg-card space-y-2.5 shadow-2xs">
                      <div className="flex items-center gap-1.5 font-bold text-primary border-b border-border/60 pb-1.5">
                        <Car className="h-4 w-4" />
                        <span>Phương Tiện & Chủ Xe</span>
                      </div>
                      <div className="grid grid-cols-2 gap-2 pt-0.5">
                        <div>
                          <span className="text-muted-foreground block text-[11px]">Biển số xe:</span>
                          <span className="font-extrabold text-sm text-foreground font-mono">
                            {session.plateNumber}
                          </span>
                        </div>
                        <div>
                          <span className="text-muted-foreground block text-[11px]">Loại phương tiện:</span>
                          <span className="font-semibold text-foreground">
                            {getVehicleTypeName(session.vehicleType)}
                          </span>
                        </div>
                        <div>
                          <span className="text-muted-foreground block text-[11px]">Họ tên chủ xe:</span>
                          <span className="font-semibold text-foreground flex items-center gap-1">
                            <User className="h-3 w-3 text-muted-foreground" />
                            {session.personFullName || 'Khách vãng lai'}
                          </span>
                        </div>
                        <div>
                          <span className="text-muted-foreground block text-[11px]">Số điện thoại:</span>
                          <span className="font-mono text-foreground flex items-center gap-1">
                            <Phone className="h-3 w-3 text-muted-foreground" />
                            {session.personPhoneNumber || '---'}
                          </span>
                        </div>
                        {session.personCode && (
                          <div>
                            <span className="text-muted-foreground block text-[11px]">Mã định danh:</span>
                            <span className="font-mono text-foreground font-semibold">
                              {session.personCode}
                            </span>
                          </div>
                        )}
                      </div>
                    </div>

                    {/* Card 2: Thời Lượng & Trạng Thái */}
                    <div className="p-3.5 rounded-xl border border-border bg-card space-y-2.5 shadow-2xs">
                      <div className="flex items-center gap-1.5 font-bold text-emerald-600 dark:text-emerald-400 border-b border-border/60 pb-1.5">
                        <Clock className="h-4 w-4" />
                        <span>Thời Lượng & Trạng Thái</span>
                      </div>
                      <div className="grid grid-cols-2 gap-2 pt-0.5">
                        <div>
                          <span className="text-muted-foreground block text-[11px]">Trạng thái hệ thống:</span>
                          <div className="mt-0.5">{getStatusBadge(session.status)}</div>
                        </div>
                        <div>
                          <span className="text-muted-foreground block text-[11px]">Tổng thời gian gửi:</span>
                          <span className="font-bold text-foreground">
                            {session.durationFormatted ||
                              (session.durationMinutes
                                ? `${session.durationMinutes} phút`
                                : '--')}
                          </span>
                        </div>
                        <div>
                          <span className="text-muted-foreground block text-[11px]">Mã phiên (ID):</span>
                          <span
                            className="font-mono text-[10px] text-muted-foreground truncate block"
                            title={session.id}
                          >
                            {session.id}
                          </span>
                        </div>
                        <div>
                          <span className="text-muted-foreground block text-[11px]">Thời điểm tạo:</span>
                          <span className="text-muted-foreground font-mono text-[11px]">
                            {session.createdAt ? new Date(session.createdAt).toLocaleDateString('vi-VN') : '--'}
                          </span>
                        </div>
                      </div>
                    </div>

                    {/* Card 3: Chi Tiết Lượt Vào (Check-In) */}
                    <div className="p-3.5 rounded-xl border border-border bg-card space-y-2 shadow-2xs">
                      <div className="flex items-center gap-1.5 font-bold text-cyan-600 dark:text-cyan-400 border-b border-border/60 pb-1.5">
                        <ArrowRightCircle className="h-4 w-4 text-cyan-500" />
                        <span>Chi Tiết Lượt Vào (Check-In)</span>
                      </div>
                      <div className="space-y-1.5 pt-0.5">
                        <div className="flex items-center justify-between">
                          <span className="text-muted-foreground text-[11px]">Thời gian vào chính xác:</span>
                          <span className="font-semibold text-foreground font-mono">
                            {session.inTime ? new Date(session.inTime).toLocaleString('vi-VN') : '--'}
                          </span>
                        </div>
                        <div className="flex items-center justify-between">
                          <span className="text-muted-foreground text-[11px]">Làn kiểm soát vào:</span>
                          <span className="font-semibold text-foreground">
                            {session.inLaneName || 'Làn Vào Số 1'}
                          </span>
                        </div>
                        <div className="flex items-center justify-between text-[11px]">
                          <span className="text-muted-foreground">Hình ảnh ghi nhận vào:</span>
                          <div className="flex items-center gap-1">
                            {hasImagePath(session.inOverviewImagePath) ? (
                              <span className="text-emerald-600 dark:text-emerald-400 flex items-center gap-0.5">
                                <CheckCircle2 className="h-3 w-3" /> Toàn cảnh
                              </span>
                            ) : (
                              <span className="text-muted-foreground flex items-center gap-0.5">
                                <AlertCircle className="h-3 w-3" /> Thiếu toàn cảnh
                              </span>
                            )}
                            <span className="text-muted-foreground/40">•</span>
                            {hasImagePath(session.inPlateImagePath) ? (
                              <span className="text-emerald-600 dark:text-emerald-400 flex items-center gap-0.5">
                                <CheckCircle2 className="h-3 w-3" /> Biển số
                              </span>
                            ) : (
                              <span className="text-muted-foreground flex items-center gap-0.5">
                                <AlertCircle className="h-3 w-3" /> Thiếu biển số
                              </span>
                            )}
                          </div>
                        </div>
                      </div>
                    </div>

                    {/* Card 4: Chi Tiết Lượt Ra (Check-Out) */}
                    <div className="p-3.5 rounded-xl border border-border bg-card space-y-2 shadow-2xs">
                      <div className="flex items-center gap-1.5 font-bold text-rose-600 dark:text-rose-400 border-b border-border/60 pb-1.5">
                        <ArrowLeftCircle className="h-4 w-4 text-rose-500" />
                        <span>Chi Tiết Lượt Ra (Check-Out)</span>
                      </div>
                      <div className="space-y-1.5 pt-0.5">
                        <div className="flex items-center justify-between">
                          <span className="text-muted-foreground text-[11px]">Thời gian ra chính xác:</span>
                          <span className="font-semibold text-foreground font-mono">
                            {session.outTime ? (
                              new Date(session.outTime).toLocaleString('vi-VN')
                            ) : (
                              <span className="text-primary italic">Đang gửi trong bãi</span>
                            )}
                          </span>
                        </div>
                        <div className="flex items-center justify-between">
                          <span className="text-muted-foreground text-[11px]">Làn kiểm soát ra:</span>
                          <span className="font-semibold text-foreground">
                            {session.outLaneName || (session.outTime ? 'Làn Ra Số 1' : '--')}
                          </span>
                        </div>
                        <div className="flex items-center justify-between text-[11px]">
                          <span className="text-muted-foreground">Hình ảnh ghi nhận ra:</span>
                          <div className="flex items-center gap-1">
                            {hasImagePath(session.outOverviewImagePath) ? (
                              <span className="text-emerald-600 dark:text-emerald-400 flex items-center gap-0.5">
                                <CheckCircle2 className="h-3 w-3" /> Toàn cảnh
                              </span>
                            ) : (
                              <span className="text-muted-foreground flex items-center gap-0.5">
                                <AlertCircle className="h-3 w-3" /> Chưa có
                              </span>
                            )}
                            <span className="text-muted-foreground/40">•</span>
                            {hasImagePath(session.outPlateImagePath) ? (
                              <span className="text-emerald-600 dark:text-emerald-400 flex items-center gap-0.5">
                                <CheckCircle2 className="h-3 w-3" /> Biển số
                              </span>
                            ) : (
                              <span className="text-muted-foreground flex items-center gap-0.5">
                                <AlertCircle className="h-3 w-3" /> Chưa có
                              </span>
                            )}
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              )}
            </>
          ) : (
            <div className="p-8 text-center text-muted-foreground">
              Không tìm thấy thông tin phiên đỗ xe.
            </div>
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
}
