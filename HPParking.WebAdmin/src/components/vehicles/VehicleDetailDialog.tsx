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
  MapPin,
  Mail,
  CreditCard,
  Route,
  Truck,
} from 'lucide-react';
import { vehicleApi } from '@/api/vehicleApi';
import { clientApi } from '@/api/clientApi';
import { gateRouteApi } from '@/api/gateRouteApi';
import { VehicleType, type VehicleDto } from '@/types/vehicle';
import { ClientType } from '@/types/client';
import { formatDateTimeVi } from '@/utils/formatters';

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

  // Fetch thông tin tuyến điều vận nếu xe có assignedRouteId
  const { data: assignedRoute, isLoading: isLoadingRoute } = useQuery({
    queryKey: ['vehicle-route', vehicle?.assignedRouteId],
    queryFn: () =>
      vehicle?.assignedRouteId ? gateRouteApi.getById(vehicle.assignedRouteId) : null,
    enabled: Boolean(open && vehicle?.assignedRouteId),
  });

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

  const getClientTypeLabel = (type?: number) => {
    switch (type) {
      case ClientType.Employee:
        return 'Cán bộ nhân viên';
      case ClientType.Contractor:
        return 'Nhân sự nhà thầu';
      case ClientType.Visitor:
        return 'Khách vãng lai';
      case ClientType.VIP:
        return 'Khách VIP';
      case ClientType.Guest:
        return 'Khách đến thăm';
      default:
        return 'Nhân sự nội bộ';
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <div className="flex items-center gap-3 pr-10 sm:pr-12">
            <div className="p-2.5 rounded-xl bg-blue-500/10 text-blue-600 dark:text-blue-400 shrink-0">
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
                Xem thông tin chi tiết phương tiện và hồ sơ chủ sở hữu
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
                <div className="flex items-center gap-1.5">
                  <span className="text-[11px] text-muted-foreground">Phân loại xe:</span>
                  {getVehicleTypeBadge(vehicle.type)}
                </div>
                <div>
                  {vehicle.isShared ? (
                    <Badge className="bg-amber-100 text-amber-800 border border-amber-300 dark:bg-amber-950 dark:text-amber-300 text-[11px] font-medium">
                      Phương tiện dùng chung
                    </Badge>
                  ) : (
                    <Badge variant="outline" className="border-blue-300 text-blue-700 dark:text-blue-300 bg-blue-50/50 dark:bg-blue-950/30 text-[11px]">
                      Phương tiện cá nhân
                    </Badge>
                  )}
                </div>
              </div>
            </div>

            {/* Chi tiết thông tin & Chủ xe */}
            <div className="space-y-4">
                {/* 1. Trường hợp Xe cá nhân: Hiển thị đầy đủ thông tin hồ sơ chủ sở hữu */}
                {!vehicle.isShared && (
                  <div className="p-3.5 rounded-xl border border-border bg-muted/30 space-y-2.5">
                    <div className="flex items-center justify-between text-xs font-semibold text-foreground border-b border-border/60 pb-2">
                      <div className="flex items-center gap-1.5">
                        <User className="h-4 w-4 text-blue-600 dark:text-blue-400" />
                        <span>Hồ sơ chủ sở hữu phương tiện</span>
                      </div>
                      {ownerClient && (
                        <Badge variant="outline" className="text-[10px]">
                          {getClientTypeLabel(ownerClient.type)}
                        </Badge>
                      )}
                    </div>

                    {isLoadingOwner ? (
                      <div className="space-y-2 pt-1">
                        <Skeleton className="h-5 w-48" />
                        <Skeleton className="h-5 w-36" />
                        <Skeleton className="h-5 w-full" />
                      </div>
                    ) : ownerClient ? (
                      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-1 text-xs">
                        {/* Tên & Mã */}
                        <div className="p-2.5 rounded-lg bg-background border border-border space-y-1">
                          <span className="text-[11px] text-muted-foreground block">Họ và tên chủ xe:</span>
                          <span className="text-sm font-semibold text-foreground block">
                            {ownerClient.name}
                          </span>
                          <span className="text-[11px] text-muted-foreground font-mono block">
                            Mã nhân sự: {ownerClient.code}
                          </span>
                        </div>

                        {/* Số điện thoại & Trạng thái */}
                        <div className="p-2.5 rounded-lg bg-background border border-border space-y-1">
                          <span className="text-[11px] text-muted-foreground flex items-center gap-1">
                            <Phone className="h-3 w-3" />
                            Số điện thoại liên hệ:
                          </span>
                          {ownerClient.phoneNumber ? (
                            <a
                              href={`tel:${ownerClient.phoneNumber}`}
                              className="text-sm font-semibold text-blue-600 dark:text-blue-400 hover:underline block"
                            >
                              {ownerClient.phoneNumber}
                            </a>
                          ) : (
                            <span className="text-xs text-muted-foreground italic block">
                              Chưa cập nhật
                            </span>
                          )}
                          <div className="pt-0.5">
                            <ActiveStatusBadge isActive={ownerClient.isActive} />
                          </div>
                        </div>

                        {/* Địa chỉ đầy đủ (Thông tin chi tiết nhất) */}
                        <div className="p-2.5 rounded-lg bg-background border border-border sm:col-span-2 space-y-1">
                          <span className="text-[11px] text-muted-foreground flex items-center gap-1">
                            <MapPin className="h-3.5 w-3.5 text-rose-500" />
                            Địa chỉ thường trú / liên hệ:
                          </span>
                          <span className="text-xs font-medium text-foreground block">
                            {ownerClient.address || (
                              <span className="text-muted-foreground italic font-normal">Chưa cập nhật địa chỉ</span>
                            )}
                          </span>
                        </div>

                        {/* Email & Thẻ định danh nếu có */}
                        {(ownerClient.email || ownerClient.cardCode) && (
                          <div className="p-2.5 rounded-lg bg-background border border-border sm:col-span-2 grid grid-cols-1 sm:grid-cols-2 gap-2">
                            {ownerClient.email && (
                              <div className="space-y-0.5">
                                <span className="text-[11px] text-muted-foreground flex items-center gap-1">
                                  <Mail className="h-3 w-3" /> Email:
                                </span>
                                <span className="text-xs text-foreground font-mono">{ownerClient.email}</span>
                              </div>
                            )}
                            {ownerClient.cardCode && (
                              <div className="space-y-0.5">
                                <span className="text-[11px] text-muted-foreground flex items-center gap-1">
                                  <CreditCard className="h-3 w-3 text-indigo-500" /> Thẻ nhân sự:
                                </span>
                                <span className="text-xs font-mono font-bold text-foreground">
                                  {ownerClient.cardCode}
                                </span>
                              </div>
                            )}
                          </div>
                        )}
                      </div>
                    ) : (
                      <div className="p-3 rounded-lg border border-dashed border-border text-center">
                        <span className="text-xs text-muted-foreground italic">
                          Phương tiện chưa gán chủ sở hữu cụ thể (Xe vãng lai hoặc chưa liên kết nhân sự).
                        </span>
                      </div>
                    )}
                  </div>
                )}

                {/* 2. Trường hợp Phương tiện nội bộ / Dùng chung: Hiển thị Tuyến điều vận & Thẻ xe */}
                {vehicle.isShared && (
                  <div className="p-3.5 rounded-xl border border-border bg-amber-50/50 dark:bg-amber-950/20 space-y-2.5">
                    <div className="flex items-center justify-between text-xs font-semibold text-foreground border-b border-amber-200/60 dark:border-amber-800/60 pb-2">
                      <div className="flex items-center gap-1.5 text-amber-800 dark:text-amber-300">
                        <Truck className="h-4 w-4" />
                        <span>Cấu hình điều vận phương tiện nội bộ</span>
                      </div>
                      <Badge className="bg-amber-100 text-amber-800 border border-amber-300 dark:bg-amber-950 dark:text-amber-300 text-[10px]">
                        Xe dùng chung
                      </Badge>
                    </div>

                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-1 text-xs">
                      {/* Tuyến điều vận */}
                      <div className="p-2.5 rounded-lg bg-background border border-border space-y-1">
                        <span className="text-[11px] text-muted-foreground flex items-center gap-1">
                          <Route className="h-3.5 w-3.5 text-blue-500" />
                          Tuyến đường điều vận:
                        </span>
                        <span className="text-sm font-semibold text-foreground block">
                          {isLoadingRoute ? 'Đang tải...' : assignedRoute?.routeName || 'Tuyến điều vận mặc định'}
                        </span>
                        {assignedRoute?.routeCode && (
                          <span className="text-[11px] text-muted-foreground font-mono block">
                            Mã tuyến: {assignedRoute.routeCode}
                          </span>
                        )}
                      </div>

                      {/* Thẻ xe định danh */}
                      <div className="p-2.5 rounded-lg bg-background border border-border space-y-1">
                        <span className="text-[11px] text-muted-foreground flex items-center gap-1">
                          <CreditCard className="h-3.5 w-3.5 text-indigo-500" />
                          Thẻ xe RFID gắn kèm:
                        </span>
                        <span className="text-sm font-mono font-bold text-foreground block">
                          {vehicle.cardCode || (
                            <span className="text-xs font-normal text-muted-foreground italic">
                              Chưa gán thẻ xe
                            </span>
                          )}
                        </span>
                        <span className="text-[10px] text-muted-foreground block">
                          Dùng quẹt thẻ tại cổng kiểm soát điều vận
                        </span>
                      </div>
                    </div>
                  </div>
                )}

                {/* 3. Các thuộc tính hệ thống */}
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  {vehicle.cardCode && !vehicle.isShared && (
                    <div className="p-3 rounded-xl border border-border bg-card/60 sm:col-span-2">
                      <span className="text-[11px] font-medium text-muted-foreground flex items-center gap-1">
                        <CreditCard className="h-3 w-3 text-indigo-500" />
                        Thẻ xe định danh
                      </span>
                      <span className="text-xs font-mono font-bold text-foreground mt-0.5 block">
                        {vehicle.cardCode}
                      </span>
                    </div>
                  )}

                  <div className="p-3 rounded-xl border border-border bg-card/60">
                    <span className="text-[11px] font-medium text-muted-foreground flex items-center gap-1">
                      <Calendar className="h-3 w-3 text-muted-foreground" />
                      Thời điểm đăng ký phương tiện
                    </span>
                    <span className="text-xs text-foreground mt-0.5 block">
                      {formatDateTimeVi(vehicle.createdAt)}
                    </span>
                  </div>

                  <div className="p-3 rounded-xl border border-border bg-card/60">
                    <span className="text-[11px] font-medium text-muted-foreground flex items-center gap-1">
                      <Clock className="h-3 w-3 text-muted-foreground" />
                      Cập nhật lần cuối
                    </span>
                    <span className="text-xs text-foreground mt-0.5 block">
                      {formatDateTimeVi(vehicle.updatedAt)}
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
            </div>
        )}

        <DialogFooter className="pt-2">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => onOpenChange(false)}
            className="text-xs h-9 cursor-pointer"
          >
            Đóng
          </Button>
          {onEdit && vehicle && (
            <Button
              type="button"
              size="sm"
              onClick={() => {
                onOpenChange(false);
                onEdit(vehicle);
              }}
              className="text-xs h-9 bg-blue-600 hover:bg-blue-700 text-white cursor-pointer gap-1.5"
            >
              <Edit className="h-3.5 w-3.5" />
              <span>Chỉnh sửa thông tin</span>
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
