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
import { EvidenceImageGrid, hasImagePath, formatImageUrl } from './EvidenceImageGrid';
import { parkingSessionApi } from '@/api/parkingSessionApi';
import { ParkingSessionStatus } from '@/types/parkingSession';
import { VehicleType } from '@/types/vehicle';
import { LaneTargetType } from '@/types/infrastructure';

export interface ParkingSessionDetailDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  sessionId: string | null;
  targetType?: LaneTargetType;
}

export function ParkingSessionDetailDialog({
  open,
  onOpenChange,
  sessionId,
  targetType,
}: ParkingSessionDetailDialogProps) {
  const [activeTab, setActiveTab] = useState<'all' | 'slider' | 'details'>('all');

  const { data: session, isLoading } = useQuery({
    queryKey: ['parking-session-detail', sessionId],
    queryFn: () => (sessionId ? parkingSessionApi.getSessionById(sessionId) : null),
    enabled: Boolean(open && sessionId),
  });

  const isPedestrian =
    targetType === LaneTargetType.Pedestrian ||
    session?.targetType === LaneTargetType.Pedestrian ||
    Boolean(session && session.targetType !== LaneTargetType.Vehicle && !session?.vehicleType);

  const getStatusBadge = (status?: ParkingSessionStatus) => {
    switch (status) {
      case ParkingSessionStatus.Active:
        return (
          <Badge className="bg-blue-600 hover:bg-blue-600 text-white font-medium text-xs px-2.5 py-0.5 shadow-xs">
            {isPedestrian ? 'Đang bên trong' : 'Đang trong bãi'}
          </Badge>
        );
      case ParkingSessionStatus.Completed:
        return (
          <Badge className="bg-emerald-600 hover:bg-emerald-600 text-white font-medium text-xs px-2.5 py-0.5 shadow-xs">
            {isPedestrian ? 'Đã ra / Hoàn tất' : 'Đã hoàn thành'}
          </Badge>
        );
      case ParkingSessionStatus.UnmatchedOut:
        return (
          <Badge className="bg-rose-600 hover:bg-rose-600 text-white font-semibold text-xs px-2.5 py-0.5 shadow-xs animate-pulse">
            {isPedestrian ? 'Ra không có lượt vào' : 'Ra không vào / Lệch biển'}
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
          <div className="flex flex-col gap-2.5 pr-8">
            <DialogTitle className="flex items-center gap-2.5 text-base sm:text-lg font-bold">
              <div className="h-8 w-8 rounded-lg bg-primary/10 text-primary flex items-center justify-center shrink-0">
                {isPedestrian ? <User className="h-4 w-4" /> : <Car className="h-4 w-4" />}
              </div>
              <div className="flex items-center gap-2 flex-wrap">
                <span className="font-bold tracking-wide">
                  {isPedestrian
                    ? `Chi Tiết Lượt Người Vào Ra: ${session?.personFullName || session?.personCode || 'Khách'}`
                    : `Chi Tiết Lượt Xe: ${session?.plateNumber || 'Đang tải...'}`}
                </span>
                <span className="text-xs text-muted-foreground font-normal">
                  {isPedestrian
                    ? `(${session?.personCode ? `Mã: ${session.personCode}` : 'Khách / Nhân sự'})`
                    : `(${session?.personFullName || 'Khách vãng lai'} • ${session ? getVehicleTypeName(session.vehicleType) : ''})`}
                </span>
              </div>
            </DialogTitle>

            {/* Badges trạng thái & thời lượng cho xuống hàng */}
            <div className="flex items-center gap-2 flex-wrap sm:pl-10">
              {session && getStatusBadge(session.status)}
              {session?.inTime && (
                <Badge
                  variant="outline"
                  className="bg-background border-border text-foreground font-mono text-[11px] px-2.5 py-0.5 flex items-center gap-1 shadow-2xs"
                >
                  <Timer className="h-3 w-3 text-primary" />
                  {session.durationFormatted || '--'}
                  {isActive && (isPedestrian ? ' (đang bên trong)' : ' (đang đỗ)')}
                </Badge>
              )}
            </div>
          </div>

          {/* View Switcher Tabs */}
          <div className="flex items-center gap-1 mt-2.5 pt-2 border-t border-border/60">
            <button
              type="button"
              onClick={() => setActiveTab('all')}
              className={`text-xs px-3 py-1 rounded-md font-medium transition-colors cursor-pointer flex items-center gap-1.5 ${activeTab === 'all'
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
              className={`text-xs px-3 py-1 rounded-md font-medium transition-colors cursor-pointer flex items-center gap-1.5 ${activeTab === 'slider'
                  ? 'bg-blue-600 text-white shadow-xs'
                  : 'text-muted-foreground hover:bg-muted'
                }`}
            >
              <Eye className="h-3.5 w-3.5" />
              {isPedestrian ? 'Slide Ảnh (4)' : 'Slide Ảnh (6)'}
            </button>
            <button
              type="button"
              onClick={() => setActiveTab('details')}
              className={`text-xs px-3 py-1 rounded-md font-medium transition-colors cursor-pointer flex items-center gap-1.5 ${activeTab === 'details'
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
                      {isPedestrian
                        ? 'CẢNH BÁO AN NINH: LƯỢT RA KHÔNG TÌM THẤY LƯỢT VÀO HỢP LỆ'
                        : 'CẢNH BÁO AN NINH: PHIÊN ĐỖ XE LỆCH BIỂN SỐ / KHÔNG KHỚP LƯỢT VÀO'}
                    </h4>
                    <p className="mt-1 leading-relaxed text-rose-700 dark:text-rose-300">
                      {isPedestrian
                        ? 'Hệ thống ghi nhận lượt ra nhưng không tìm thấy lượt vào hợp lệ tương ứng của người này. Đề nghị nhân viên an ninh kiểm tra ảnh camera/FaceID để xác minh!'
                        : 'Hệ thống phát hiện sai lệch biển số giữa lượt vào và lượt ra, hoặc phương tiện rời bãi không tìm thấy phiên vào hợp lệ tương ứng. Đề nghị nhân viên an ninh kiểm tra kỹ 4 ảnh bằng chứng và liên hệ chủ phương tiện để xác minh!'}
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
                  inFaceImagePath={session.inFaceImagePath}
                  outFaceImagePath={session.outFaceImagePath}
                  inLaneName={session.inLaneName}
                  outLaneName={session.outLaneName}
                  inTime={session.inTime}
                  outTime={session.outTime}
                  isActiveSession={isActive}
                  targetType={session.targetType}
                  personAvatar={session.personAvatar}
                />
              )}

              {/* PHẦN 2: 4 CARDS THÔNG SỐ CHI TIẾT (HIỂN THỊ Ở TAB ALL VÀ DETAILS) */}
              {(activeTab === 'all' || activeTab === 'details') && (
                <div className="space-y-3">
                  <div className="flex items-center gap-1.5 text-xs font-bold text-slate-800 dark:text-slate-200">
                    <Info className="h-4 w-4 text-blue-600 dark:text-blue-400" />
                    <span>
                      {isPedestrian
                        ? 'THÔNG TIN CHI TIẾT ĐẦY ĐỦ LƯỢT NGƯỜI VÀO RA'
                        : 'THÔNG TIN CHI TIẾT ĐẦY ĐỦ CỦA LƯỢT XE'}
                    </span>
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-2 gap-3 text-xs">
                    {/* Card 1: Người Vào Ra / Phương Tiện */}
                    <div className="p-3.5 rounded-xl border border-border bg-card space-y-2.5 shadow-2xs">
                      <div className="flex items-center gap-1.5 font-bold text-primary border-b border-border/60 pb-1.5">
                        {isPedestrian ? <User className="h-4 w-4" /> : <Car className="h-4 w-4" />}
                        <span>{isPedestrian ? 'Thông Tin Người Vào Ra' : 'Phương Tiện & Chủ Xe'}</span>
                      </div>
                      <div className="grid grid-cols-2 gap-2 pt-0.5">
                        {isPedestrian ? (
                          <>
                            <div>
                              <span className="text-muted-foreground block text-[11px]">Đối tượng:</span>
                              <span className="font-semibold text-foreground">
                                {session.personId ? 'Nhân sự nội bộ' : 'Khách / Vãng lai'}
                              </span>
                            </div>
                            <div>
                              <span className="text-muted-foreground block text-[11px]">Mã định danh/CCCD:</span>
                              <span className="font-extrabold text-sm text-foreground font-mono">
                                {session.personCode || '--'}
                              </span>
                            </div>
                          </>
                        ) : (
                          <>
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
                          </>
                        )}
                        <div className="col-span-2 sm:col-span-1">
                          <span className="text-muted-foreground block text-[11px]">
                            {isPedestrian ? 'Họ và tên:' : 'Họ tên chủ xe:'}
                          </span>
                          <div className="flex items-center gap-2 mt-0.5">
                            {hasImagePath(session.inFaceImagePath || session.personAvatar) ? (
                              <img
                                src={formatImageUrl(session.inFaceImagePath || session.personAvatar)}
                                alt={session.personFullName || (isPedestrian ? 'Người vào ra' : 'Chủ xe')}
                                className="h-8 w-8 rounded-full object-cover border border-slate-300 dark:border-slate-700 shadow-2xs shrink-0"
                              />
                            ) : (
                              <div className="h-8 w-8 rounded-full bg-slate-100 dark:bg-slate-800 flex items-center justify-center text-slate-500 border border-slate-200 dark:border-slate-700 shrink-0">
                                <User className="h-4 w-4" />
                              </div>
                            )}
                            <span className="font-semibold text-foreground truncate">
                              {session.personFullName || 'Khách vãng lai'}
                            </span>
                          </div>
                        </div>
                        <div>
                          <span className="text-muted-foreground block text-[11px]">Số điện thoại:</span>
                          <span className="font-mono text-foreground flex items-center gap-1 mt-1">
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
                          <span className="text-muted-foreground block text-[11px]">
                            {isPedestrian ? 'Thời gian lưu lại:' : 'Tổng thời gian gửi:'}
                          </span>
                          <span className="font-bold text-foreground">
                            {session.durationFormatted ||
                              (session.durationMinutes
                                ? `${session.durationMinutes} phút`
                                : '--')}
                          </span>
                        </div>
                        <div>
                          <span className="text-muted-foreground block text-[11px]">
                            {isPedestrian ? 'Mã lượt ghi nhận (ID):' : 'Mã phiên (ID):'}
                          </span>
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
                          <span className="text-muted-foreground text-[11px]">
                            {isPedestrian ? 'Cổng / Lối vào:' : 'Làn kiểm soát vào:'}
                          </span>
                          <span className="font-semibold text-foreground">
                            {session.inLaneName || (isPedestrian ? 'Cổng vào' : 'Làn Vào Số 1')}
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
                            {isPedestrian ? (
                              hasImagePath(session.inFaceImagePath || session.personAvatar) ? (
                                <span className="text-emerald-600 dark:text-emerald-400 flex items-center gap-0.5">
                                  <CheckCircle2 className="h-3 w-3" /> Khuôn mặt
                                </span>
                              ) : (
                                <span className="text-muted-foreground flex items-center gap-0.5">
                                  <AlertCircle className="h-3 w-3" /> Thiếu khuôn mặt
                                </span>
                              )
                            ) : (
                              hasImagePath(session.inPlateImagePath) ? (
                                <span className="text-emerald-600 dark:text-emerald-400 flex items-center gap-0.5">
                                  <CheckCircle2 className="h-3 w-3" /> Biển số
                                </span>
                              ) : (
                                <span className="text-muted-foreground flex items-center gap-0.5">
                                  <AlertCircle className="h-3 w-3" /> Thiếu biển số
                                </span>
                              )
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
                              <span className="text-primary italic font-medium">
                                {isPedestrian ? 'Đang bên trong' : 'Đang gửi trong bãi'}
                              </span>
                            )}
                          </span>
                        </div>
                        <div className="flex items-center justify-between">
                          <span className="text-muted-foreground text-[11px]">
                            {isPedestrian ? 'Cổng / Lối ra:' : 'Làn kiểm soát ra:'}
                          </span>
                          <span className="font-semibold text-foreground">
                            {session.outLaneName || (session.outTime ? (isPedestrian ? 'Cổng ra' : 'Làn Ra Số 1') : '--')}
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
                            {isPedestrian ? (
                              hasImagePath(session.outFaceImagePath || session.personAvatar) ? (
                                <span className="text-emerald-600 dark:text-emerald-400 flex items-center gap-0.5">
                                  <CheckCircle2 className="h-3 w-3" /> Khuôn mặt
                                </span>
                              ) : (
                                <span className="text-muted-foreground flex items-center gap-0.5">
                                  <AlertCircle className="h-3 w-3" /> Chưa có
                                </span>
                              )
                            ) : (
                              hasImagePath(session.outPlateImagePath) ? (
                                <span className="text-emerald-600 dark:text-emerald-400 flex items-center gap-0.5">
                                  <CheckCircle2 className="h-3 w-3" /> Biển số
                                </span>
                              ) : (
                                <span className="text-muted-foreground flex items-center gap-0.5">
                                  <AlertCircle className="h-3 w-3" /> Chưa có
                                </span>
                              )
                            )}
                          </div>
                        </div>
                      </div>
                    </div>

                    {/* Card 5: Ghi Chú */}
                    <div className="col-span-1 md:col-span-2 p-3.5 rounded-xl border border-border bg-card space-y-2 shadow-2xs">
                      <div className="flex items-center gap-1.5 font-bold text-amber-600 dark:text-amber-400 border-b border-border/60 pb-1.5">
                        <FileText className="h-4 w-4" />
                        <span>Ghi Chú</span>
                      </div>
                      <div className="pt-0.5">
                        {session.note ? (
                          <p className="text-xs text-foreground font-medium whitespace-pre-wrap leading-relaxed">
                            {session.note}
                          </p>
                        ) : (
                          <span className="text-muted-foreground italic text-xs">
                            {isPedestrian
                              ? '— Không có ghi chú nào cho lượt vào ra này —'
                              : '— Không có ghi chú nào cho phiên đỗ xe này —'}
                          </span>
                        )}
                      </div>
                    </div>
                  </div>
                </div>
              )}
            </>
          ) : (
            <div className="p-8 text-center text-muted-foreground">
              {isPedestrian
                ? 'Không tìm thấy thông tin lượt người vào ra.'
                : 'Không tìm thấy thông tin phiên đỗ xe.'}
            </div>
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
}
