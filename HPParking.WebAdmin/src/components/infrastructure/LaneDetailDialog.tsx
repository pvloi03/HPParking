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
import {
  Route,
  DoorOpen,
  Camera,
  Cpu,
  ScanFace,
  ArrowDownLeft,
  ArrowUpRight,
  ArrowLeftRight,
  Network,
  X,
} from 'lucide-react';
import { lanesApi } from '@/api/infrastructureApi';
import { LaneDirection, type DeviceSummaryDto } from '@/types/infrastructure';

interface LaneDetailDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  laneId: string | null;
}

export function LaneDetailDialog({
  open,
  onOpenChange,
  laneId,
}: LaneDetailDialogProps) {
  const { data: detail, isLoading } = useQuery({
    queryKey: ['lane-detail', laneId],
    queryFn: () => (laneId ? lanesApi.getDetail(laneId) : null),
    enabled: open && Boolean(laneId),
  });

  const getDirectionBadge = (dir?: number) => {
    switch (dir) {
      case LaneDirection.In:
        return (
          <Badge className="bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300 gap-1 text-xs">
            <ArrowDownLeft className="h-3 w-3" />
            <span>Làn Vào (In)</span>
          </Badge>
        );
      case LaneDirection.Out:
        return (
          <Badge className="bg-amber-100 text-amber-800 dark:bg-amber-950 dark:text-amber-300 gap-1 text-xs">
            <ArrowUpRight className="h-3 w-3" />
            <span>Làn Ra (Out)</span>
          </Badge>
        );
      case LaneDirection.Bidirectional:
        return (
          <Badge className="bg-blue-100 text-blue-800 dark:bg-blue-950 dark:text-blue-300 gap-1 text-xs">
            <ArrowLeftRight className="h-3 w-3" />
            <span>Hai Chiều (Bidirectional)</span>
          </Badge>
        );
      default:
        return null;
    }
  };

  const renderDeviceCard = (
    title: string,
    icon: React.ReactNode,
    device?: DeviceSummaryDto | null,
    extraInfo?: React.ReactNode
  ) => {
    if (!device) {
      return (
        <div className="p-3.5 rounded-xl border border-dashed border-border bg-card/40 flex flex-col items-center justify-center text-center">
          <div className="text-muted-foreground/60 mb-1">{icon}</div>
          <p className="text-xs font-semibold text-muted-foreground">{title}</p>
          <span className="text-[11px] text-muted-foreground/70 italic mt-0.5">
            Chưa cấu hình / Chưa gán thiết bị
          </span>
        </div>
      );
    }

    return (
      <div className="p-3.5 rounded-xl border border-border bg-card shadow-2xs space-y-2">
        <div className="flex items-start justify-between gap-2">
          <div className="flex items-center gap-2">
            <div className="p-1.5 rounded-lg bg-muted text-foreground">{icon}</div>
            <div>
              <span className="text-xs font-semibold text-foreground block">
                {title}
              </span>
              <span className="text-xs font-medium text-blue-600 dark:text-blue-400 block truncate">
                {device.name}
              </span>
            </div>
          </div>
          <Badge
            variant={device.isActive ? 'default' : 'secondary'}
            className={`text-[10px] shrink-0 ${
              device.isActive
                ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300'
                : 'bg-muted text-muted-foreground'
            }`}
          >
            {device.isActive ? 'Hoạt động' : 'Ngừng'}
          </Badge>
        </div>

        <div className="pt-2 border-t border-border/60 grid grid-cols-2 gap-2 text-[11px]">
          <div>
            <span className="text-muted-foreground block">Mã thiết bị:</span>
            <span className="font-mono font-medium text-foreground">{device.code}</span>
          </div>
          <div>
            <span className="text-muted-foreground block">Địa chỉ IP & Cổng:</span>
            <span className="font-mono font-medium text-foreground flex items-center gap-1">
              <Network className="h-3 w-3 text-muted-foreground" />
              {device.ipAddress}:{device.port}
            </span>
          </div>
        </div>

        {extraInfo && (
          <div className="pt-1.5 border-t border-border/40 text-[11px]">
            {extraInfo}
          </div>
        )}
      </div>
    );
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <div className="flex items-center gap-2 mb-1">
            <div className="p-2 rounded-lg bg-blue-100 dark:bg-blue-950 text-blue-600 dark:text-blue-400">
              <Route className="h-5 w-5" />
            </div>
            <DialogTitle className="text-base font-bold">
              Chi Tiết Cấu Hình Làn Xe &amp; Thiết Bị Ngoại Vi
            </DialogTitle>
          </div>
          <DialogDescription>
            Thông số kỹ thuật, cổng điều khiển và trạng thái kết nối 4 thiết bị ngoại vi của làn.
          </DialogDescription>
        </DialogHeader>

        {isLoading ? (
          <div className="space-y-4 py-3">
            <div className="grid grid-cols-2 gap-3">
              <Skeleton className="h-14 w-full" />
              <Skeleton className="h-14 w-full" />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <Skeleton className="h-28 w-full" />
              <Skeleton className="h-28 w-full" />
              <Skeleton className="h-28 w-full" />
              <Skeleton className="h-28 w-full" />
            </div>
          </div>
        ) : detail ? (
          <div className="space-y-4 py-1">
            {/* THÔNG TIN CHUNG LÀN XE */}
            <div className="p-4 rounded-xl bg-muted/40 border border-border space-y-2.5">
              <div className="flex flex-wrap items-center justify-between gap-2">
                <div>
                  <div className="flex items-center gap-2">
                    <span className="font-mono text-xs font-bold text-blue-600 dark:text-blue-400">
                      {detail.code}
                    </span>
                    <span className="text-foreground font-semibold text-sm">
                      {detail.name}
                    </span>
                  </div>
                  <div className="flex items-center gap-1.5 text-xs text-muted-foreground mt-0.5">
                    <DoorOpen className="h-3.5 w-3.5" />
                    <span>Cổng trực thuộc: <strong>{detail.gateName || detail.gate?.name || '—'}</strong></span>
                  </div>
                </div>
                <div className="flex items-center gap-2">
                  {getDirectionBadge(detail.direction)}
                  <Badge
                    variant={detail.isActive ? 'default' : 'secondary'}
                    className={
                      detail.isActive
                        ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300 text-xs'
                        : 'bg-muted text-muted-foreground text-xs'
                    }
                  >
                    {detail.isActive ? 'Đang hoạt động' : 'Ngừng hoạt động'}
                  </Badge>
                </div>
              </div>

              {/* Thông số Relay và Reader */}
              <div className="pt-2 border-t border-border flex items-center gap-6 text-xs text-muted-foreground">
                <div>
                  Rơ-le kích Barrier:{' '}
                  <strong className="text-foreground font-mono">Relay {detail.outputRelay}</strong>
                </div>
                <div>
                  Cổng nhận Reader:{' '}
                  <strong className="text-foreground font-mono">Reader {detail.inputReader}</strong>
                </div>
              </div>
            </div>

            {/* 4 THIẾT BỊ NGOẠI VI GÁN VÀO LÀN */}
            <div className="space-y-2">
              <h4 className="text-xs font-bold uppercase tracking-wider text-muted-foreground">
                4 Thiết Bị Ngoại Vi Liên Kết (ADR 0034)
              </h4>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                {renderDeviceCard(
                  'Camera Biển Số (LPR)',
                  <Camera className="h-4 w-4 text-blue-600" />,
                  detail.plateCamera
                )}
                {renderDeviceCard(
                  'Camera Toàn Cảnh (Overview)',
                  <Camera className="h-4 w-4 text-indigo-600" />,
                  detail.overviewCamera
                )}
                {renderDeviceCard(
                  'Bộ Điều Khiển Barrier (Controller)',
                  <Cpu className="h-4 w-4 text-amber-600" />,
                  detail.controller,
                  <div className="text-muted-foreground">
                    Điều khiển qua cổng <strong className="text-foreground">Relay {detail.outputRelay}</strong>, đọc thẻ <strong className="text-foreground">Reader {detail.inputReader}</strong>
                  </div>
                )}
                {renderDeviceCard(
                  'Nhận Diện Khuôn Mặt (FaceID)',
                  <ScanFace className="h-4 w-4 text-emerald-600" />,
                  detail.faceDevice
                )}
              </div>
            </div>
          </div>
        ) : (
          <div className="py-8 text-center text-xs text-muted-foreground">
            Không tìm thấy thông tin chi tiết của làn xe này.
          </div>
        )}

        <DialogFooter className="pt-3">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => onOpenChange(false)}
            className="text-xs h-9 cursor-pointer"
          >
            <X className="h-3.5 w-3.5 mr-1" />
            <span>Đóng</span>
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
