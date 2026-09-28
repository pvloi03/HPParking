import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { ActiveStatusBadge } from '@/components/common/ActiveStatusBadge';
import {
  Car,
  Bike,
  HelpCircle,
  User,
  Phone,
  Calendar,
  Clock,
  Edit,
  AlertCircle,
  History,
  ArrowDownLeft,
  ArrowUpRight,
  ShieldAlert,
} from 'lucide-react';
import { vehicleApi } from '@/api/vehicleApi';
import { clientApi } from '@/api/clientApi';
import { parkingSessionApi } from '@/api/parkingSessionApi';
import { VehicleType, type VehicleDto } from '@/types/vehicle';
import { ParkingSessionStatus } from '@/types/parkingSession';

export interface VehicleDetailDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  vehicleId: string | null;
  onEdit?: (vehicle: VehicleDto) => void;
}

export function VehicleDetailDialog({
  open,
  onOpenChange,
  vehicleId,
  onEdit,
}: VehicleDetailDialogProps) {
  const [activeTab, setActiveTab] = useState<'info' | 'history'>('info');

  // Fetch chi tiết phương tiện
  const {
    data: vehicle,
    isLoading: isLoadingVehicle,
    isError: isVehicleError,
  } = useQuery({
    queryKey: ['vehicle-detail', vehicleId],
    queryFn: () => (vehicleId ? vehicleApi.getById(vehicleId) : Promise.reject('No ID')),
    enabled: Boolean(open && vehicleId),
  });

  // Fetch thông tin chủ sở hữu nếu có ownerClientId
  const { data: ownerClient, isLoading: isLoadingOwner } = useQuery({
    queryKey: ['vehicle-owner', vehicle?.ownerClientId],
    queryFn: () =>
      vehicle?.ownerClientId ? clientApi.getById(vehicle.ownerClientId) : null,
    enabled: Boolean(open && vehicle?.ownerClientId),
  });

  // Fetch lịch sử 5 phiên gửi xe gần nhất của biển số này
  const { data: sessionHistoryData, isLoading: isLoadingHistory } = useQuery({
    queryKey: ['vehicle-sessions', vehicle?.plateNumber],
    queryFn: () =>
      vehicle?.plateNumber
        ? parkingSessionApi.getParkingSessions({
            plateNumber: vehicle.plateNumber,
            pageSize: 5,
          })
        : Promise.resolve({ items: [], pagination: { totalCount: 0, totalPages: 0, pageIndex: 1, pageSize: 5, hasNextPage: false, hasPreviousPage: false } }),
    enabled: Boolean(open && vehicle?.plateNumber && (activeTab === 'history' || activeTab === 'info')),
  });

  const recentSessions = sessionHistoryData?.items || [];

  const formatDate = (isoString?: string) => {
    if (!isoString) return '—';
    try {
      const d = new Date(isoString);
      return isNaN(d.getTime()) ? '—' : d.toLocaleString('vi-VN');
    } catch {
      return '—';
    }
  };

  const getVehicleTypeBadge = (type?: number) => {
    switch (type) {
      case VehicleType.Car:
        return (
          <Badge className="bg-blue-100 text-blue-800 dark:bg-blue-950 dark:text-blue-300 gap-1 text-xs">
            <Car className="h-3.5 w-3.5" />
            <span>Ô tô</span>
          </Badge>
        );
      case VehicleType.Motorbike:
        return (
          <Badge className="bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300 gap-1 text-xs">
            <Bike className="h-3.5 w-3.5" />
            <span>Xe máy</span>
          </Badge>
        );
      case VehicleType.Bicycle:
        return (
          <Badge className="bg-amber-100 text-amber-800 dark:bg-amber-950 dark:text-amber-300 gap-1 text-xs">
            <Bike className="h-3.5 w-3.5" />
            <span>Xe đạp</span>
          </Badge>
        );
      default:
        return (
          <Badge variant="outline" className="gap-1 text-xs">
            <HelpCircle className="h-3.5 w-3.5" />
            <span>Khác</span>
          </Badge>
        );
    }
  };

  const getSessionStatusBadge = (status?: number) => {
    switch (status) {
      case ParkingSessionStatus.Active:
        return (
          <Badge className="bg-emerald-500/10 text-emerald-600 border-emerald-500/20 text-[10px]">
            Đang đỗ trong bãi
          </Badge>
        );
      case ParkingSessionStatus.Completed:
        return (
          <Badge variant="outline" className="text-muted-foreground text-[10px]">
            Đã hoàn tất
          </Badge>
        );
      case ParkingSessionStatus.UnmatchedOut:
        return (
          <Badge className="bg-rose-500/10 text-rose-600 border-rose-500/20 text-[10px] gap-1">
            <ShieldAlert className="h-3 w-3" />
            <span>Lệch biển số</span>
          </Badge>
        );
      default:
        return (
          <Badge variant="secondary" className="text-[10px]">
            Đã hủy
          </Badge>
        );
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <div className="flex items-center gap-3">
            <div className="p-2.5 rounded-xl bg-blue-500/10 text-blue-600 dark:text-blue-400">
              <Car className="h-6 w-6" />
            </div>
            <div>
              <DialogTitle className="text-lg font-bold flex items-center gap-2">
                {isLoadingVehicle ? (
                  <Skeleton className="h-6 w-48" />
                ) : (
                  <>
                    <span>Chi Tiết Phương Tiện</span>
                    {vehicle && <ActiveStatusBadge isActive={vehicle.isActive} />}
                  </>
                )}
              </DialogTitle>
              <DialogDescription className="text-xs text-muted-foreground mt-0.5">
                Xem thông tin đăng ký, chủ sở hữu và lịch sử ra vào bãi đỗ xe
              </DialogDescription>
            </div>
          </div>
        </DialogHeader>

        {isLoadingVehicle ? (
          <div className="space-y-4 py-4">
            <Skeleton className="h-16 w-full rounded-xl" />
            <div className="grid grid-cols-2 gap-4">
              <Skeleton className="h-14 rounded-lg" />
              <Skeleton className="h-14 rounded-lg" />
            </div>
          </div>
        ) : isVehicleError || !vehicle ? (
          <div className="py-8 flex flex-col items-center justify-center text-center">
            <AlertCircle className="h-10 w-10 text-destructive mb-2 opacity-80" />
            <p className="text-sm font-semibold text-foreground">Không thể tải thông tin phương tiện</p>
            <p className="text-xs text-muted-foreground mt-1">
              Bản ghi có thể đã bị xóa hoặc không thể kết nối tới máy chủ.
            </p>
          </div>
        ) : (
          <div className="space-y-4 pt-2">
            {/* Card Biển Số Xe Nổi Bật */}
            <div className="p-4 rounded-xl border-2 border-border bg-gradient-to-r from-card to-muted/40 shadow-xs flex flex-col sm:flex-row sm:items-center justify-between gap-3">
              <div className="space-y-1">
                <span className="text-[11px] font-semibold text-muted-foreground uppercase tracking-wider block">
                  Biển số phương tiện
                </span>
                <div className="inline-block px-4 py-1.5 rounded-lg bg-background border-2 border-foreground/80 font-mono text-2xl font-black tracking-widest text-foreground shadow-2xs">
                  {vehicle.plateNumber}
                </div>
              </div>
              <div className="flex flex-col items-start sm:items-end gap-1.5">
                <span className="text-[11px] text-muted-foreground">Phân loại phương tiện:</span>
                {getVehicleTypeBadge(vehicle.type)}
              </div>
            </div>

            {/* Tabs điều hướng */}
            <div className="flex items-center gap-2 border-b border-border pb-2">
              <button
                type="button"
                onClick={() => setActiveTab('info')}
                className={`text-xs font-semibold px-3 py-1.5 rounded-lg transition-colors cursor-pointer ${
                  activeTab === 'info'
                    ? 'bg-primary text-primary-foreground'
                    : 'text-muted-foreground hover:bg-muted'
                }`}
              >
                Thông tin & Chủ sở hữu
              </button>
              <button
                type="button"
                onClick={() => setActiveTab('history')}
                className={`text-xs font-semibold px-3 py-1.5 rounded-lg transition-colors cursor-pointer flex items-center gap-1.5 ${
                  activeTab === 'history'
                    ? 'bg-primary text-primary-foreground'
                    : 'text-muted-foreground hover:bg-muted'
                }`}
              >
                <History className="h-3.5 w-3.5" />
                <span>Lịch sử ra vào ({recentSessions.length})</span>
              </button>
            </div>

            {/* Tab 1: Thông tin & Chủ xe */}
            {activeTab === 'info' && (
              <div className="space-y-4">
                {/* Khối Thông tin Chủ xe */}
                <div className="p-3.5 rounded-xl border border-border bg-muted/30 space-y-2">
                  <div className="flex items-center gap-1.5 text-xs font-semibold text-foreground">
                    <User className="h-4 w-4 text-blue-600 dark:text-blue-400" />
                    <span>Hồ sơ chủ sở hữu</span>
                  </div>

                  {isLoadingOwner ? (
                    <div className="space-y-1.5">
                      <Skeleton className="h-4 w-40" />
                      <Skeleton className="h-4 w-28" />
                    </div>
                  ) : ownerClient ? (
                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-1 text-xs">
                      <div className="p-2.5 rounded-lg bg-background border border-border">
                        <span className="text-[11px] text-muted-foreground block">Họ và tên chủ xe:</span>
                        <span className="text-sm font-semibold text-foreground mt-0.5 block">
                          {ownerClient.name}
                        </span>
                        <span className="text-[11px] text-muted-foreground font-mono">
                          Mã: {ownerClient.code}
                        </span>
                      </div>
                      <div className="p-2.5 rounded-lg bg-background border border-border">
                        <span className="text-[11px] text-muted-foreground flex items-center gap-1">
                          <Phone className="h-3 w-3" />
                          Số điện thoại:
                        </span>
                        {ownerClient.phoneNumber ? (
                          <a
                            href={`tel:${ownerClient.phoneNumber}`}
                            className="text-sm font-medium text-blue-600 dark:text-blue-400 hover:underline mt-0.5 block"
                          >
                            {ownerClient.phoneNumber}
                          </a>
                        ) : (
                          <span className="text-xs text-muted-foreground italic mt-0.5 block">
                            Chưa cập nhật
                          </span>
                        )}
                        <div className="mt-1">
                          <ActiveStatusBadge isActive={ownerClient.isActive} />
                        </div>
                      </div>
                    </div>
                  ) : (
                    <div className="p-3 rounded-lg border border-dashed border-border text-center">
                      <span className="text-xs text-muted-foreground italic">
                        Phương tiện chưa gán chủ sở hữu cụ thể (Xe vãng lai hoặc chưa liên kết khách hàng).
                      </span>
                    </div>
                  )}
                </div>

                {/* Các thuộc tính hệ thống */}
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  <div className="p-3 rounded-xl border border-border bg-card/60">
                    <span className="text-[11px] font-medium text-muted-foreground flex items-center gap-1">
                      <Calendar className="h-3 w-3 text-muted-foreground" />
                      Thời điểm đăng ký phương tiện
                    </span>
                    <span className="text-xs text-foreground mt-0.5 block">
                      {formatDate(vehicle.createdAt)}
                    </span>
                  </div>

                  <div className="p-3 rounded-xl border border-border bg-card/60">
                    <span className="text-[11px] font-medium text-muted-foreground flex items-center gap-1">
                      <Clock className="h-3 w-3 text-muted-foreground" />
                      Cập nhật lần cuối
                    </span>
                    <span className="text-xs text-foreground mt-0.5 block">
                      {formatDate(vehicle.updatedAt)}
                    </span>
                  </div>

                  {vehicle.note && (
                    <div className="p-3 rounded-xl border border-border bg-card/60 sm:col-span-2">
                      <span className="text-[11px] font-medium text-muted-foreground block">
                        Ghi chú phương tiện
                      </span>
                      <p className="text-xs text-foreground mt-0.5 whitespace-pre-wrap">
                        {vehicle.note}
                      </p>
                    </div>
                  )}
                </div>
              </div>
            )}

            {/* Tab 2: Lịch sử ra vào */}
            {activeTab === 'history' && (
              <div className="space-y-2">
                {isLoadingHistory ? (
                  <div className="space-y-2">
                    <Skeleton className="h-14 w-full rounded-lg" />
                    <Skeleton className="h-14 w-full rounded-lg" />
                  </div>
                ) : recentSessions.length === 0 ? (
                  <div className="py-6 text-center border border-dashed border-border rounded-xl">
                    <History className="h-8 w-8 text-muted-foreground mx-auto mb-1.5 opacity-40" />
                    <p className="text-xs text-muted-foreground">
                      Chưa ghi nhận lượt ra vào nào của biển số này
                    </p>
                  </div>
                ) : (
                  <div className="max-h-64 overflow-y-auto space-y-2 pr-1">
                    {recentSessions.map((session) => (
                      <div
                        key={session.id}
                        className="p-3 rounded-xl border border-border bg-card space-y-2 text-xs"
                      >
                        <div className="flex items-center justify-between">
                          <span className="font-semibold text-foreground">
                            Phiên: {formatDate(session.inTime || session.createdAt)}
                          </span>
                          {getSessionStatusBadge(session.status)}
                        </div>

                        <div className="grid grid-cols-2 gap-2 text-[11px] text-muted-foreground">
                          <div className="flex items-start gap-1.5">
                            <ArrowDownLeft className="h-3.5 w-3.5 text-emerald-600 shrink-0 mt-0.5" />
                            <div>
                              <span className="text-foreground font-medium block">Lượt vào:</span>
                              <span>{formatDate(session.inTime)}</span>
                              {session.inLaneName && (
                                <span className="block text-[10px] text-muted-foreground/80">
                                  Làn: {session.inLaneName}
                                </span>
                              )}
                            </div>
                          </div>

                          <div className="flex items-start gap-1.5">
                            <ArrowUpRight className="h-3.5 w-3.5 text-amber-600 shrink-0 mt-0.5" />
                            <div>
                              <span className="text-foreground font-medium block">Lượt ra:</span>
                              <span>{session.outTime ? formatDate(session.outTime) : '—'}</span>
                              {session.outLaneName && (
                                <span className="block text-[10px] text-muted-foreground/80">
                                  Làn: {session.outLaneName}
                                </span>
                              )}
                            </div>
                          </div>
                        </div>

                        {session.durationMinutes !== undefined && (
                          <div className="text-[11px] text-muted-foreground pt-1 border-t border-border/60 flex items-center justify-between">
                            <span>Thời lượng đỗ:</span>
                            <span className="font-semibold text-foreground">
                              {session.durationMinutes} phút
                            </span>
                          </div>
                        )}
                      </div>
                    ))}
                  </div>
                )}
              </div>
            )}
          </div>
        )}

        <DialogFooter className="flex items-center justify-end gap-2 pt-2 border-t border-border">
          {vehicle && onEdit && (
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => {
                onOpenChange(false);
                onEdit(vehicle);
              }}
              className="text-xs gap-1.5 text-blue-600 border-blue-200 hover:bg-blue-50 dark:hover:bg-blue-950/40 cursor-pointer"
            >
              <Edit className="h-3.5 w-3.5" />
              <span>Chỉnh sửa</span>
            </Button>
          )}
          <Button
            type="button"
            variant="default"
            size="sm"
            onClick={() => onOpenChange(false)}
            className="text-xs cursor-pointer"
          >
            Đóng
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
