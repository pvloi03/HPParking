import { useState, useEffect, useMemo, useCallback } from 'react';
import { useQuery } from '@tanstack/react-query';
import { toast } from 'sonner';
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
  Cpu,
  Camera,
  ScanFace,
  HelpCircle,
  Activity,
  RefreshCw,
  Loader2,
  Edit,
  ShieldCheck,
  ShieldAlert,
  Network,
  Clock,
  User,
  Copy,
  Check,
  Layers,
  DoorOpen,
  Calendar,
  AlertTriangle,
  Info,
  X,
} from 'lucide-react';
import { devicesApi, lanesApi, extractErrorMessage } from '@/api/infrastructureApi';
import {
  DeviceType,
  LaneDirection,
  type DeviceDto,
  type DevicePingResultDto,
} from '@/types/infrastructure';

export interface DeviceDetailDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  deviceId: string | null;
  onEdit?: (device: DeviceDto) => void;
}

export function DeviceDetailDialog({
  open,
  onOpenChange,
  deviceId,
  onEdit,
}: DeviceDetailDialogProps) {
  const [isCopied, setIsCopied] = useState(false);
  const [isPinging, setIsPinging] = useState(false);
  const [pingResult, setPingResult] = useState<DevicePingResultDto | null>(null);

  // Query: Thông tin chi tiết thiết bị
  const {
    data: device,
    isLoading: isDeviceLoading,
  } = useQuery({
    queryKey: ['device-detail', deviceId],
    queryFn: () => (deviceId ? devicesApi.getById(deviceId) : null),
    enabled: open && Boolean(deviceId),
  });

  // Query: Danh sách tất cả các làn xe để lọc ra các làn đang liên kết với thiết bị
  const { data: lanesData, isLoading: isLanesLoading } = useQuery({
    queryKey: ['lanes-all'],
    queryFn: () => lanesApi.getPaged({ pageSize: 100 }),
    enabled: open && Boolean(deviceId),
  });

  // Lọc các làn xe đang sử dụng thiết bị này
  const associatedLanes = useMemo(() => {
    if (!lanesData?.items || !device?.id) return [];
    return lanesData.items.filter(
      (l) =>
        l.overviewCameraDeviceId === device.id ||
        l.plateCameraDeviceId === device.id ||
        l.controllerDeviceId === device.id ||
        l.faceDeviceId === device.id
    );
  }, [lanesData, device?.id]);

  // Xử lý sao chép mã thiết bị
  const handleCopyCode = async () => {
    if (!device?.code) return;
    try {
      await navigator.clipboard.writeText(device.code);
      setIsCopied(true);
      toast.success(`Đã sao chép mã "${device.code}" vào bộ nhớ tạm.`);
      setTimeout(() => setIsCopied(false), 2000);
    } catch {
      toast.error('Không thể sao chép vào bộ nhớ tạm.');
    }
  };

  // Xử lý Ping kiểm tra kết nối thiết bị trực tiếp
  const handlePing = useCallback(
    async (showToast = false) => {
      if (!device?.ipAddress) return;

      setIsPinging(true);
      try {
        const res = await devicesApi.pingDeviceIp(device.ipAddress);
        setPingResult(res);
        if (showToast) {
          if (res.isAlive) {
            toast.success(
              `Kết nối thành công: ${res.ipAddress} (${res.roundtripTimeMs}ms via ${res.method})`
            );
          } else {
            toast.error(`Mất kết nối: ${res.message || 'Không có phản hồi từ thiết bị'}`);
          }
        }
      } catch (err) {
        setPingResult({
          ipAddress: device.ipAddress,
          isAlive: false,
          roundtripTimeMs: 2000,
          method: 'ERROR',
          message: extractErrorMessage(err),
          timestamp: new Date().toISOString(),
        });
        if (showToast) {
          toast.error(extractErrorMessage(err));
        }
      } finally {
        setIsPinging(false);
      }
    },
    [device?.ipAddress]
  );

  // Tự động ping ngay khi mở modal và đã nạp xong thông tin thiết bị
  useEffect(() => {
    if (open && device?.ipAddress) {
      void handlePing(false);
    } else if (!open) {
      setPingResult(null);
      setIsPinging(false);
    }
  }, [open, device?.id, device?.ipAddress, handlePing]);

  const getDeviceTypeBadge = (type: DeviceType) => {
    switch (type) {
      case DeviceType.Camera:
        return (
          <Badge className="bg-blue-100 text-blue-800 hover:bg-blue-100 dark:bg-blue-950 dark:text-blue-300 gap-1.5 text-xs font-medium py-0.5 px-2">
            <Camera className="h-3.5 w-3.5" />
            <span>Camera Giám Sát</span>
          </Badge>
        );
      case DeviceType.Controller:
        return (
          <Badge className="bg-amber-100 text-amber-800 hover:bg-amber-100 dark:bg-amber-950 dark:text-amber-300 gap-1.5 text-xs font-medium py-0.5 px-2">
            <Cpu className="h-3.5 w-3.5" />
            <span>Bộ Điều Khiển Barrier</span>
          </Badge>
        );
      case DeviceType.FaceId:
        return (
          <Badge className="bg-emerald-100 text-emerald-800 hover:bg-emerald-100 dark:bg-emerald-950 dark:text-emerald-300 gap-1.5 text-xs font-medium py-0.5 px-2">
            <ScanFace className="h-3.5 w-3.5" />
            <span>Nhận Diện Khuôn Mặt (FaceID)</span>
          </Badge>
        );
      default:
        return (
          <Badge className="bg-muted text-muted-foreground gap-1.5 text-xs font-medium py-0.5 px-2">
            <HelpCircle className="h-3.5 w-3.5" />
            <span>Thiết Bị Khác</span>
          </Badge>
        );
    }
  };

  const getLaneDirectionBadge = (dir: LaneDirection) => {
    switch (dir) {
      case LaneDirection.In:
        return (
          <Badge variant="outline" className="text-[10px] font-medium text-blue-600 dark:text-blue-400 border-blue-200 dark:border-blue-900">
            Làn Vào
          </Badge>
        );
      case LaneDirection.Out:
        return (
          <Badge variant="outline" className="text-[10px] font-medium text-amber-600 dark:text-amber-400 border-amber-200 dark:border-amber-900">
            Làn Ra
          </Badge>
        );
      case LaneDirection.Bidirectional:
        return (
          <Badge variant="outline" className="text-[10px] font-medium text-purple-600 dark:text-purple-400 border-purple-200 dark:border-purple-900">
            Hai Chiều
          </Badge>
        );
      default:
        return null;
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <div className="flex items-center gap-2 mb-1">
            <div className="p-2 rounded-lg bg-blue-100 dark:bg-blue-950 text-blue-600 dark:text-blue-400">
              <Cpu className="h-5 w-5" />
            </div>
            <DialogTitle className="text-base font-bold">
              Chi Tiết Thiết Bị Ngoại Vi &amp; Kết Nối Mạng
            </DialogTitle>
          </div>
          <DialogDescription>
            Thông số kỹ thuật, cổng kết nối, trạng thái kiểm tra trực tiếp và các làn xe liên kết.
          </DialogDescription>
        </DialogHeader>

        {isDeviceLoading ? (
          <div className="space-y-4 py-3">
            <Skeleton className="h-20 w-full rounded-xl" />
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <Skeleton className="h-32 w-full rounded-xl" />
              <Skeleton className="h-32 w-full rounded-xl" />
            </div>
            <Skeleton className="h-28 w-full rounded-xl" />
          </div>
        ) : device ? (
          <div className="space-y-4 py-1">
            {/* BANNER THÔNG TIN CHUNG THIẾT BỊ */}
            <div className="p-4 rounded-xl bg-muted/40 border border-border space-y-2.5">
              <div className="flex flex-wrap items-center justify-between gap-2">
                <div className="space-y-1">
                  <div className="flex items-center gap-2">
                    <span className="font-mono text-sm font-bold text-foreground bg-muted/80 px-2 py-0.5 rounded">
                      {device.code}
                    </span>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={handleCopyCode}
                      className="h-6 w-6 p-0 text-muted-foreground hover:text-foreground cursor-pointer"
                      title="Sao chép mã thiết bị"
                    >
                      {isCopied ? (
                        <Check className="h-3 w-3 text-emerald-600" />
                      ) : (
                        <Copy className="h-3 w-3" />
                      )}
                    </Button>
                  </div>
                  <h3 className="text-sm font-semibold text-foreground">
                    {device.name}
                  </h3>
                </div>

                <div className="flex items-center gap-2 flex-wrap">
                  {getDeviceTypeBadge(device.type)}
                  <Badge
                    variant={device.isActive ? 'default' : 'secondary'}
                    className={`text-xs font-semibold py-0.5 px-2 ${
                      device.isActive
                        ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300'
                        : 'bg-muted text-muted-foreground'
                    }`}
                  >
                    {device.isActive ? 'Đang hoạt động' : 'Ngừng hoạt động'}
                  </Badge>
                </div>
              </div>
            </div>

            {/* GRID 2 CỘT: CHẨN ĐOÁN MẠNG & THÔNG SỐ XÁC THỰC */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              {/* Card 1: Chẩn đoán kết nối mạng */}
              <div className="p-3.5 rounded-xl border border-border bg-card shadow-2xs space-y-2.5">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-1.5 text-xs font-semibold text-foreground">
                    <Network className="h-4 w-4 text-emerald-600" />
                    <span>Địa chỉ kết nối (Endpoint)</span>
                  </div>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => void handlePing(true)}
                    disabled={isPinging || !device.ipAddress}
                    className="h-6 px-1.5 text-xs text-muted-foreground hover:text-foreground cursor-pointer"
                    title="Kiểm tra lại kết nối"
                  >
                    <RefreshCw className={`h-3 w-3 ${isPinging ? 'animate-spin' : ''}`} />
                  </Button>
                </div>

                <div className="p-2.5 rounded-lg bg-muted/30 border border-border/70 flex items-center justify-between">
                  <span className="font-mono text-xs font-bold text-foreground">
                    {device.ipAddress}:{device.port}
                  </span>
                  <Badge variant="outline" className="text-[10px] font-mono">
                    Port {device.port}
                  </Badge>
                </div>

                {/* Kết quả Ping */}
                <div>
                  {isPinging ? (
                    <div className="p-2.5 rounded-lg border border-blue-200 dark:border-blue-900 bg-blue-50/50 dark:bg-blue-950/30 text-center space-y-1 animate-pulse">
                      <Loader2 className="h-4 w-4 animate-spin text-blue-600 mx-auto" />
                      <p className="text-[11px] font-medium text-blue-700 dark:text-blue-300">
                        Đang gửi gói tin kiểm tra...
                      </p>
                    </div>
                  ) : pingResult ? (
                    <div
                      className={`p-2.5 rounded-lg border space-y-1 text-xs ${
                        pingResult.isAlive
                          ? 'border-emerald-200 dark:border-emerald-900 bg-emerald-50/40 dark:bg-emerald-950/30'
                          : 'border-rose-200 dark:border-rose-900 bg-rose-50/40 dark:bg-rose-950/30'
                      }`}
                    >
                      <div className="flex items-center justify-between">
                        <Badge
                          className={`gap-1 text-[10px] font-semibold ${
                            pingResult.isAlive
                              ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300'
                              : 'bg-rose-100 text-rose-800 dark:bg-rose-950 dark:text-rose-300'
                          }`}
                        >
                          {pingResult.isAlive ? (
                            <>
                              <span className="relative flex h-1.5 w-1.5">
                                <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75" />
                                <span className="relative inline-flex rounded-full h-1.5 w-1.5 bg-emerald-500" />
                              </span>
                              <span>ONLINE</span>
                            </>
                          ) : (
                            <>
                              <span className="h-1.5 w-1.5 rounded-full bg-rose-500" />
                              <span>OFFLINE</span>
                            </>
                          )}
                        </Badge>
                        {pingResult.isAlive && (
                          <span className="font-mono text-xs font-bold text-emerald-700 dark:text-emerald-400">
                            {pingResult.roundtripTimeMs} ms
                          </span>
                        )}
                      </div>
                      <p className="text-[11px] text-muted-foreground truncate">
                        {pingResult.message || (pingResult.isAlive ? 'Phản hồi tốt' : 'Không có phản hồi')}
                      </p>
                    </div>
                  ) : (
                    <div className="p-2 rounded-lg border border-dashed border-border bg-muted/10 flex items-center justify-between text-xs">
                      <span className="text-[11px] text-muted-foreground">Chưa kiểm tra phiên này</span>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => void handlePing(true)}
                        className="h-6 text-[11px] gap-1 px-2 cursor-pointer"
                      >
                        <Activity className="h-3 w-3 text-blue-500" />
                        <span>Thử lại</span>
                      </Button>
                    </div>
                  )}
                </div>
              </div>

              {/* Card 2: Xác thực & Bảo mật */}
              <div className="p-3.5 rounded-xl border border-border bg-card shadow-2xs space-y-2.5">
                <div className="flex items-center gap-1.5 text-xs font-semibold text-foreground">
                  <ShieldCheck className="h-4 w-4 text-purple-600" />
                  <span>Xác Thực &amp; Bảo Mật</span>
                </div>

                <div className="space-y-2 text-xs">
                  <div className="flex items-center justify-between py-1 border-b border-border/50">
                    <span className="text-muted-foreground">Tài khoản (User):</span>
                    <span className="font-mono font-medium text-foreground flex items-center gap-1">
                      <User className="h-3 w-3 text-muted-foreground" />
                      {device.userName || <span className="text-muted-foreground italic">—</span>}
                    </span>
                  </div>

                  <div className="flex items-center justify-between py-1 border-b border-border/50">
                    <span className="text-muted-foreground">Mật khẩu:</span>
                    <div>
                      {device.hasPassword ? (
                        <Badge
                          variant="outline"
                          className="gap-1 text-[10px] text-emerald-700 dark:text-emerald-400 border-emerald-300 dark:border-emerald-800 bg-emerald-50/50 dark:bg-emerald-950/30"
                        >
                          <ShieldCheck className="h-3 w-3" />
                          <span>Đã thiết lập mật khẩu</span>
                        </Badge>
                      ) : (
                        <Badge
                          variant="outline"
                          className="gap-1 text-[10px] text-amber-700 dark:text-amber-400 border-amber-300 dark:border-amber-800 bg-amber-50/50 dark:bg-amber-950/30"
                        >
                          <ShieldAlert className="h-3 w-3" />
                          <span>Chưa cài đặt mật khẩu</span>
                        </Badge>
                      )}
                    </div>
                  </div>

                  <div className="flex items-center justify-between py-1 text-[11px]">
                    <span className="text-muted-foreground flex items-center gap-1">
                      <Calendar className="h-3 w-3" />
                      <span>Ngày khởi tạo:</span>
                    </span>
                    <span className="text-foreground">
                      {device.createdAt ? new Date(device.createdAt).toLocaleDateString('vi-VN') : '—'}
                    </span>
                  </div>

                  {device.updatedAt && (
                    <div className="flex items-center justify-between py-1 text-[11px]">
                      <span className="text-muted-foreground flex items-center gap-1">
                        <Clock className="h-3 w-3" />
                        <span>Cập nhật gần nhất:</span>
                      </span>
                      <span className="text-foreground">
                        {new Date(device.updatedAt).toLocaleDateString('vi-VN')}
                      </span>
                    </div>
                  )}
                </div>
              </div>
            </div>

            {/* PHÂN BỔ & RÀNG BUỘC LÀN XE (LANE TOPOLOGY) */}
            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-1.5 text-xs font-bold uppercase tracking-wider text-muted-foreground">
                  <Layers className="h-3.5 w-3.5 text-indigo-500" />
                  <span>Phân Bổ &amp; Ràng Buộc Làn Xe</span>
                </div>
                <Badge variant="outline" className="text-xs font-semibold">
                  {associatedLanes.length} làn đang dùng
                </Badge>
              </div>

              {isLanesLoading ? (
                <div className="space-y-2">
                  <Skeleton className="h-12 w-full rounded-lg" />
                </div>
              ) : associatedLanes.length === 0 ? (
                <div className="p-3.5 rounded-xl border border-dashed border-border bg-muted/20 text-center space-y-1">
                  <Info className="h-4 w-4 text-muted-foreground mx-auto" />
                  <p className="text-xs font-medium text-foreground">
                    Thiết bị chưa được gán vào làn xe nào
                  </p>
                  <p className="text-[11px] text-muted-foreground max-w-md mx-auto">
                    Thiết bị có thể chỉnh sửa cấu hình mạng hoặc chuyển vào thùng rác mà không gây xung đột toàn vẹn tham chiếu (ADR 0030/0031).
                  </p>
                </div>
              ) : (
                <div className="space-y-2">
                  <div className="p-2.5 rounded-lg bg-amber-50/60 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-900 text-[11px] text-amber-800 dark:text-amber-300 flex items-start gap-2">
                    <AlertTriangle className="h-4 w-4 shrink-0 text-amber-600 mt-0.5" />
                    <span>
                      <strong>Ràng buộc toàn vẹn:</strong> Đang liên kết với {associatedLanes.length} làn xe. Vui lòng gỡ liên kết khỏi cấu hình làn trước khi xóa thiết bị.
                    </span>
                  </div>

                  <div className="divide-y divide-border/60 rounded-xl border border-border overflow-hidden">
                    {associatedLanes.map((lane) => {
                      const roles: string[] = [];
                      if (lane.plateCameraDeviceId === device.id) roles.push('Camera Biển Số (LPR)');
                      if (lane.overviewCameraDeviceId === device.id) roles.push('Camera Toàn Cảnh');
                      if (lane.controllerDeviceId === device.id) roles.push('Bộ Điều Khiển Barrier');
                      if (lane.faceDeviceId === device.id) roles.push('Đầu Đọc Khuôn Mặt (FaceID)');

                      return (
                        <div
                          key={lane.id}
                          className="p-2.5 bg-card hover:bg-muted/30 transition-colors flex flex-col sm:flex-row sm:items-center sm:justify-between gap-1.5 text-xs"
                        >
                          <div className="space-y-0.5">
                            <div className="flex items-center gap-1.5">
                              <span className="font-semibold text-foreground">{lane.name}</span>
                              <span className="font-mono text-[11px] text-muted-foreground">({lane.code})</span>
                              {getLaneDirectionBadge(lane.direction)}
                            </div>
                            {lane.gateName && (
                              <div className="flex items-center gap-1 text-[11px] text-muted-foreground">
                                <DoorOpen className="h-3 w-3" />
                                <span>Thuộc cổng: {lane.gateName}</span>
                              </div>
                            )}
                          </div>

                          <div className="flex items-center gap-1.5 flex-wrap">
                            {roles.map((role, idx) => (
                              <Badge
                                key={idx}
                                variant="secondary"
                                className="text-[10px] font-medium bg-blue-50 text-blue-700 dark:bg-blue-950 dark:text-blue-300 border border-blue-200 dark:border-blue-900"
                              >
                                {role}
                              </Badge>
                            ))}
                          </div>
                        </div>
                      );
                    })}
                  </div>
                </div>
              )}
            </div>
          </div>
        ) : (
          <div className="py-8 text-center text-xs text-muted-foreground">
            Không tìm thấy thông tin chi tiết của thiết bị này.
          </div>
        )}

        <DialogFooter className="pt-3 gap-2 flex-wrap">
          {device && onEdit && (
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => onEdit(device)}
              className="text-xs h-9 cursor-pointer gap-1.5"
            >
              <Edit className="h-3.5 w-3.5 text-muted-foreground" />
              <span>Chỉnh sửa</span>
            </Button>
          )}

          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => onOpenChange(false)}
            className="text-xs h-9 cursor-pointer gap-1"
          >
            <X className="h-3.5 w-3.5" />
            <span>Đóng</span>
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
