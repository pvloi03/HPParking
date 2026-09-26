import { useState, useMemo } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import {
  Cpu,
  Camera,
  ScanFace,
  HelpCircle,
  Activity,
  RefreshCw,
  Loader2,
  ArrowLeft,
  Edit,
  Trash2,
  ShieldCheck,
  ShieldAlert,
  Network,
  ExternalLink,
  Clock,
  User,
  Copy,
  Check,
  Layers,
  DoorOpen,
  Calendar,
  AlertTriangle,
  Info,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { ConfirmDialog } from '@/components/common/ConfirmDialog';
import { DeviceFormDialog } from '@/components/infrastructure/DeviceFormDialog';
import { usePermissions } from '@/hooks/usePermissions';
import { devicesApi, lanesApi, extractErrorMessage } from '@/api/infrastructureApi';
import {
  DeviceType,
  LaneDirection,
  type DevicePingResultDto,
  type UpdateDeviceRequest,
} from '@/types/infrastructure';

export function DeviceDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { canWrite, isAdmin } = usePermissions();

  const [isCopied, setIsCopied] = useState(false);
  const [isEditOpen, setIsEditOpen] = useState(false);
  const [isDeleteOpen, setIsDeleteOpen] = useState(false);

  // State Ping trực tiếp trên trang chi tiết
  const [isPinging, setIsPinging] = useState(false);
  const [pingResult, setPingResult] = useState<DevicePingResultDto | null>(null);

  // Query: Thông tin chi tiết thiết bị
  const {
    data: device,
    isLoading: isDeviceLoading,
    error: deviceError,
  } = useQuery({
    queryKey: ['device', id],
    queryFn: () => (id ? devicesApi.getById(id) : Promise.reject('Thiếu Id thiết bị')),
    enabled: Boolean(id),
  });

  // Query: Danh sách tất cả các làn xe để lọc ra các làn đang liên kết với thiết bị
  const { data: lanesData, isLoading: isLanesLoading } = useQuery({
    queryKey: ['lanes-all'],
    queryFn: () => lanesApi.getPaged({ pageSize: 100 }),
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

  // Mutation: Cập nhật thiết bị
  const updateMutation = useMutation({
    mutationFn: (payload: UpdateDeviceRequest) => {
      if (!id) throw new Error('Thiếu Id thiết bị');
      return devicesApi.update(id, payload);
    },
    onSuccess: (updated) => {
      toast.success(`Đã cập nhật thông tin thiết bị "${updated.name}" thành công.`);
      setIsEditOpen(false);
      void queryClient.invalidateQueries({ queryKey: ['device', id] });
      void queryClient.invalidateQueries({ queryKey: ['devices'] });
      void queryClient.invalidateQueries({ queryKey: ['lanes'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Mutation: Xóa mềm thiết bị
  const deleteMutation = useMutation({
    mutationFn: () => {
      if (!id) throw new Error('Thiếu Id thiết bị');
      return devicesApi.delete(id, false);
    },
    onSuccess: () => {
      toast.success('Đã chuyển thiết bị vào thùng rác thành công.');
      setIsDeleteOpen(false);
      void queryClient.invalidateQueries({ queryKey: ['devices'] });
      navigate('/devices');
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Xử lý copy mã thiết bị
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

  // Xử lý Ping trực tiếp thiết bị
  const handlePing = async () => {
    if (!device?.ipAddress) {
      toast.error('Thiết bị chưa được cấu hình địa chỉ IP.');
      return;
    }

    setIsPinging(true);
    try {
      const res = await devicesApi.pingDeviceIp(device.ipAddress);
      setPingResult(res);
      if (res.isAlive) {
        toast.success(
          `Kết nối thành công: ${res.ipAddress} (${res.roundtripTimeMs}ms via ${res.method})`
        );
      } else {
        toast.error(`Mất kết nối: ${res.message || 'Không có phản hồi từ thiết bị'}`);
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
      toast.error(extractErrorMessage(err));
    } finally {
      setIsPinging(false);
    }
  };

  const getDeviceTypeBadge = (type: DeviceType) => {
    switch (type) {
      case DeviceType.Camera:
        return (
          <Badge className="bg-blue-100 text-blue-800 hover:bg-blue-100 dark:bg-blue-950 dark:text-blue-300 gap-1.5 text-xs font-medium py-1 px-2.5">
            <Camera className="h-3.5 w-3.5" />
            <span>Camera Giám Sát</span>
          </Badge>
        );
      case DeviceType.Controller:
        return (
          <Badge className="bg-amber-100 text-amber-800 hover:bg-amber-100 dark:bg-amber-950 dark:text-amber-300 gap-1.5 text-xs font-medium py-1 px-2.5">
            <Cpu className="h-3.5 w-3.5" />
            <span>Bộ Điều Khiển Barrier</span>
          </Badge>
        );
      case DeviceType.FaceId:
        return (
          <Badge className="bg-emerald-100 text-emerald-800 hover:bg-emerald-100 dark:bg-emerald-950 dark:text-emerald-300 gap-1.5 text-xs font-medium py-1 px-2.5">
            <ScanFace className="h-3.5 w-3.5" />
            <span>Nhận Diện Khuôn Mặt (FaceID)</span>
          </Badge>
        );
      default:
        return (
          <Badge className="bg-muted text-muted-foreground gap-1.5 text-xs font-medium py-1 px-2.5">
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
          <Badge variant="outline" className="text-[11px] font-medium text-blue-600 dark:text-blue-400 border-blue-200 dark:border-blue-900">
            Làn Vào
          </Badge>
        );
      case LaneDirection.Out:
        return (
          <Badge variant="outline" className="text-[11px] font-medium text-amber-600 dark:text-amber-400 border-amber-200 dark:border-amber-900">
            Làn Ra
          </Badge>
        );
      case LaneDirection.Bidirectional:
        return (
          <Badge variant="outline" className="text-[11px] font-medium text-purple-600 dark:text-purple-400 border-purple-200 dark:border-purple-900">
            Hai Chiều
          </Badge>
        );
      default:
        return null;
    }
  };

  if (isDeviceLoading) {
    return (
      <div className="space-y-6">
        <div className="flex items-center gap-3">
          <Skeleton className="h-9 w-24" />
          <Skeleton className="h-7 w-48" />
        </div>
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="lg:col-span-2 space-y-6">
            <Skeleton className="h-64 rounded-xl" />
            <Skeleton className="h-48 rounded-xl" />
          </div>
          <div className="space-y-6">
            <Skeleton className="h-56 rounded-xl" />
            <Skeleton className="h-40 rounded-xl" />
          </div>
        </div>
      </div>
    );
  }

  if (deviceError || !device) {
    return (
      <div className="flex flex-col items-center justify-center p-12 text-center rounded-2xl border border-dashed border-border bg-card/50 space-y-3">
        <div className="h-12 w-12 rounded-full bg-destructive/10 text-destructive flex items-center justify-center">
          <AlertTriangle className="h-6 w-6" />
        </div>
        <h3 className="text-base font-semibold text-foreground">Không tìm thấy thông tin thiết bị</h3>
        <p className="text-xs text-muted-foreground max-w-sm">
          Thiết bị này có thể không tồn tại hoặc đã bị xóa vĩnh viễn khỏi hệ thống cơ sở dữ liệu.
        </p>
        <Button
          variant="outline"
          size="sm"
          onClick={() => navigate('/devices')}
          className="mt-2 text-xs"
        >
          <ArrowLeft className="h-3.5 w-3.5 mr-1" />
          <span>Quay lại danh sách</span>
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Thanh điều hướng & Cụm nút hành động Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 border-b border-border pb-4">
        <div className="space-y-1.5">
          {/* Breadcrumb */}
          <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
            <Link to="/devices" className="hover:text-foreground transition-colors">
              Quản lý thiết bị ngoại vi
            </Link>
            <span>/</span>
            <span className="text-foreground font-medium truncate max-w-[200px]">
              {device.name}
            </span>
          </div>

          <div className="flex flex-wrap items-center gap-2.5">
            <Button
              variant="outline"
              size="sm"
              onClick={() => navigate('/devices')}
              className="h-8 px-2.5 text-xs cursor-pointer gap-1"
            >
              <ArrowLeft className="h-3.5 w-3.5" />
              <span>Quay lại</span>
            </Button>

            <h1 className="text-xl font-bold tracking-tight text-foreground flex items-center gap-2">
              {device.name}
            </h1>

            {/* Badge Trạng thái hoạt động */}
            <Badge
              variant={device.isActive ? 'default' : 'secondary'}
              className={`text-xs font-semibold py-0.5 px-2.5 ${
                device.isActive
                  ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300'
                  : 'bg-muted text-muted-foreground'
              }`}
            >
              {device.isActive ? 'Đang hoạt động' : 'Ngừng hoạt động'}
            </Badge>

            {/* Badge Phân loại */}
            {getDeviceTypeBadge(device.type)}
          </div>
        </div>

        {/* Nút thao tác chính */}
        <div className="flex items-center gap-2 flex-wrap">
          <Button
            variant="outline"
            size="sm"
            onClick={handlePing}
            disabled={isPinging || !device.ipAddress}
            className="h-9 gap-1.5 text-xs font-semibold border-blue-200 dark:border-blue-900 text-blue-700 dark:text-blue-300 hover:bg-blue-50 dark:hover:bg-blue-950/60 cursor-pointer shadow-xs"
            title="Gửi gói tin ping kiểm tra kết nối ngay"
          >
            {isPinging ? (
              <Loader2 className="h-3.5 w-3.5 animate-spin text-blue-600" />
            ) : (
              <Activity className="h-3.5 w-3.5 text-blue-600" />
            )}
            <span>{isPinging ? 'Đang kiểm tra...' : 'Kiểm tra kết nối'}</span>
          </Button>

          {canWrite && (
            <Button
              variant="outline"
              size="sm"
              onClick={() => setIsEditOpen(true)}
              className="h-9 gap-1.5 text-xs font-semibold cursor-pointer shadow-xs"
            >
              <Edit className="h-3.5 w-3.5 text-muted-foreground" />
              <span>Chỉnh sửa</span>
            </Button>
          )}

          {canWrite && (
            <Button
              variant="outline"
              size="sm"
              onClick={() => setIsDeleteOpen(true)}
              className="h-9 gap-1.5 text-xs font-semibold text-rose-700 dark:text-rose-400 border-rose-200 dark:border-rose-900 hover:bg-rose-50 dark:hover:bg-rose-950/50 cursor-pointer shadow-xs"
            >
              <Trash2 className="h-3.5 w-3.5 text-rose-600" />
              <span>Xóa vào thùng rác</span>
            </Button>
          )}
        </div>
      </div>

      {/* Bố cục Grid 2 cột */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* CỘT TRÁI (2/3): THÔNG SỐ KỸ THUẬT & LIÊN KẾT LÀN XE */}
        <div className="lg:col-span-2 space-y-6">
          {/* Card 1: Thông số kỹ thuật & Cấu hình */}
          <div className="rounded-xl border border-border bg-card p-5 shadow-xs space-y-4">
            <div className="flex items-center gap-2 pb-3 border-b border-border/70">
              <Cpu className="h-4 w-4 text-blue-600" />
              <h2 className="text-sm font-semibold text-foreground">
                Thông Số Kỹ Thuật & Nhận Diện
              </h2>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 text-xs">
              <div className="space-y-1">
                <span className="text-muted-foreground font-medium">Mã thiết bị:</span>
                <div className="flex items-center gap-2">
                  <span className="font-mono text-sm font-bold text-foreground bg-muted/60 px-2.5 py-1 rounded-md">
                    {device.code}
                  </span>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={handleCopyCode}
                    className="h-7 w-7 p-0 text-muted-foreground hover:text-foreground cursor-pointer"
                    title="Sao chép mã thiết bị"
                  >
                    {isCopied ? (
                      <Check className="h-3.5 w-3.5 text-emerald-600" />
                    ) : (
                      <Copy className="h-3.5 w-3.5" />
                    )}
                  </Button>
                </div>
              </div>

              <div className="space-y-1">
                <span className="text-muted-foreground font-medium">Tên hiển thị:</span>
                <p className="text-sm font-medium text-foreground">{device.name}</p>
              </div>

              <div className="space-y-1">
                <span className="text-muted-foreground font-medium">Phân loại thiết bị:</span>
                <div>{getDeviceTypeBadge(device.type)}</div>
              </div>

              <div className="space-y-1">
                <span className="text-muted-foreground font-medium">Trạng thái vận hành:</span>
                <div className="flex items-center gap-2">
                  <span
                    className={`h-2.5 w-2.5 rounded-full ${
                      device.isActive ? 'bg-emerald-500' : 'bg-muted-foreground'
                    }`}
                  />
                  <span className="font-medium text-foreground">
                    {device.isActive
                      ? 'Đang hoạt động bình thường'
                      : 'Đang tạm dừng hoạt động'}
                  </span>
                </div>
              </div>

              <div className="space-y-1">
                <span className="text-muted-foreground font-medium">Tài khoản xác thực (User):</span>
                <div className="flex items-center gap-1.5 text-foreground font-mono">
                  <User className="h-3.5 w-3.5 text-muted-foreground" />
                  <span>{device.userName || <span className="text-muted-foreground">—</span>}</span>
                </div>
              </div>

              <div className="space-y-1">
                <span className="text-muted-foreground font-medium">Bảo mật mật khẩu:</span>
                <div>
                  {device.hasPassword ? (
                    <Badge
                      variant="outline"
                      className="gap-1 text-emerald-700 dark:text-emerald-400 border-emerald-300 dark:border-emerald-800 bg-emerald-50/50 dark:bg-emerald-950/30"
                    >
                      <ShieldCheck className="h-3.5 w-3.5" />
                      <span>Đã thiết lập mật khẩu</span>
                    </Badge>
                  ) : (
                    <Badge
                      variant="outline"
                      className="gap-1 text-amber-700 dark:text-amber-400 border-amber-300 dark:border-amber-800 bg-amber-50/50 dark:bg-amber-950/30"
                    >
                      <ShieldAlert className="h-3.5 w-3.5" />
                      <span>Chưa cài đặt mật khẩu</span>
                    </Badge>
                  )}
                </div>
              </div>
            </div>
          </div>

          {/* Card 2: Phân bổ & Ràng buộc Làn xe (Lane Topology) */}
          <div className="rounded-xl border border-border bg-card p-5 shadow-xs space-y-4">
            <div className="flex items-center justify-between pb-3 border-b border-border/70">
              <div className="flex items-center gap-2">
                <Layers className="h-4 w-4 text-indigo-600" />
                <h2 className="text-sm font-semibold text-foreground">
                  Phân Bổ & Ràng Buộc Làn Xe
                </h2>
              </div>
              <Badge variant="outline" className="text-xs font-semibold">
                {associatedLanes.length} làn đang dùng
              </Badge>
            </div>

            {isLanesLoading ? (
              <div className="space-y-2">
                <Skeleton className="h-12 rounded-lg" />
                <Skeleton className="h-12 rounded-lg" />
              </div>
            ) : associatedLanes.length === 0 ? (
              <div className="p-4 rounded-lg border border-dashed border-border bg-muted/20 text-center space-y-1.5">
                <Info className="h-5 w-5 text-muted-foreground mx-auto" />
                <p className="text-xs font-medium text-foreground">
                  Thiết bị chưa được gán vào làn xe nào
                </p>
                <p className="text-[11px] text-muted-foreground max-w-md mx-auto">
                  Thiết bị này có thể an toàn để cập nhật thông số kết nối hoặc xóa vào thùng rác mà không gây xung đột toàn vẹn tham chiếu (ADR 0030/0031).
                </p>
              </div>
            ) : (
              <div className="space-y-3">
                <div className="p-2.5 rounded-lg bg-amber-50/60 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-900 text-[11px] text-amber-800 dark:text-amber-300 flex items-start gap-2">
                  <AlertTriangle className="h-4 w-4 shrink-0 text-amber-600 mt-0.5" />
                  <span>
                    <strong>Lưu ý toàn vẹn:</strong> Thiết bị đang liên kết trực tiếp với {associatedLanes.length} làn xe. Bạn phải gỡ thiết bị khỏi cấu hình làn xe trước khi có thể xóa thiết bị.
                  </span>
                </div>

                <div className="divide-y divide-border/60 rounded-lg border border-border overflow-hidden">
                  {associatedLanes.map((lane) => {
                    const roles: string[] = [];
                    if (lane.plateCameraDeviceId === device.id) roles.push('Camera Biển Số (LPR)');
                    if (lane.overviewCameraDeviceId === device.id) roles.push('Camera Toàn Cảnh');
                    if (lane.controllerDeviceId === device.id) roles.push('Bộ Điều Khiển Barrier');
                    if (lane.faceDeviceId === device.id) roles.push('Đầu Đọc Khuôn Mặt (FaceID)');

                    return (
                      <div
                        key={lane.id}
                        className="p-3 bg-card hover:bg-muted/30 transition-colors flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2 text-xs"
                      >
                        <div className="space-y-1">
                          <div className="flex items-center gap-2">
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
                              className="text-[11px] font-medium bg-blue-50 text-blue-700 dark:bg-blue-950 dark:text-blue-300 border border-blue-200 dark:border-blue-900"
                            >
                              {role}
                            </Badge>
                          ))}
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => navigate('/lanes')}
                            className="h-6 w-6 p-0 text-muted-foreground hover:text-foreground cursor-pointer"
                            title="Xem cấu hình làn xe"
                          >
                            <ExternalLink className="h-3 w-3" />
                          </Button>
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>
            )}
          </div>
        </div>

        {/* CỘT PHẢI (1/3): CHẨN ĐOÁN MẠNG LIVE & KIỂM TOÁN */}
        <div className="space-y-6">
          {/* Card 3: Thẻ Chẩn Đoán Mạng Trực Tiếp (Live Network Diagnostics) */}
          <div className="rounded-xl border border-border bg-card p-5 shadow-xs space-y-4">
            <div className="flex items-center justify-between pb-3 border-b border-border/70">
              <div className="flex items-center gap-2">
                <Network className="h-4 w-4 text-emerald-600" />
                <h2 className="text-sm font-semibold text-foreground">
                  Chẩn Đoán Kết Nối Mạng
                </h2>
              </div>
              <Button
                variant="ghost"
                size="sm"
                onClick={handlePing}
                disabled={isPinging || !device.ipAddress}
                className="h-7 px-2 text-xs text-muted-foreground hover:text-foreground cursor-pointer"
                title="Làm mới kết quả ping"
              >
                <RefreshCw className={`h-3 w-3 ${isPinging ? 'animate-spin' : ''}`} />
              </Button>
            </div>

            {/* Hiển thị IP & Port to rõ */}
            <div className="p-3.5 rounded-lg bg-muted/40 border border-border/80 space-y-1">
              <span className="text-[11px] text-muted-foreground font-medium block">
                Địa chỉ kết nối (Endpoint):
              </span>
              <div className="font-mono text-base font-bold text-foreground flex items-center justify-between">
                <span>
                  {device.ipAddress}:{device.port}
                </span>
                <Badge variant="outline" className="text-[10px] font-mono">
                  Port {device.port}
                </Badge>
              </div>
            </div>

            {/* Trạng thái Live Ping */}
            <div className="space-y-2.5">
              <span className="text-xs font-semibold text-foreground block">
                Trạng thái kiểm tra thời gian thực:
              </span>

              {isPinging ? (
                <div className="p-4 rounded-lg border border-blue-200 dark:border-blue-900 bg-blue-50/60 dark:bg-blue-950/40 text-center space-y-1.5 animate-pulse">
                  <Loader2 className="h-5 w-5 animate-spin text-blue-600 mx-auto" />
                  <p className="text-xs font-semibold text-blue-700 dark:text-blue-300">
                    Đang gửi gói tin kiểm tra...
                  </p>
                  <p className="text-[11px] text-blue-600/80 dark:text-blue-400">
                    Đo kiểm qua ICMP và fallback TCP socket
                  </p>
                </div>
              ) : pingResult ? (
                <div
                  className={`p-3.5 rounded-lg border space-y-2 ${
                    pingResult.isAlive
                      ? 'border-emerald-200 dark:border-emerald-900 bg-emerald-50/50 dark:bg-emerald-950/40'
                      : 'border-rose-200 dark:border-rose-900 bg-rose-50/50 dark:bg-rose-950/40'
                  }`}
                >
                  <div className="flex items-center justify-between">
                    <Badge
                      className={`gap-1.5 text-xs font-semibold ${
                        pingResult.isAlive
                          ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300'
                          : 'bg-rose-100 text-rose-800 dark:bg-rose-950 dark:text-rose-300'
                      }`}
                    >
                      {pingResult.isAlive ? (
                        <>
                          <span className="relative flex h-2 w-2">
                            <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75" />
                            <span className="relative inline-flex rounded-full h-2 w-2 bg-emerald-500" />
                          </span>
                          <span>ONLINE</span>
                        </>
                      ) : (
                        <>
                          <span className="h-2 w-2 rounded-full bg-rose-500" />
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

                  <div className="text-[11px] space-y-1 text-foreground">
                    <p>
                      <strong>Phương thức:</strong>{' '}
                      <span className="font-mono">{pingResult.method || 'ICMP'}</span>
                    </p>
                    <p className="text-muted-foreground">
                      {pingResult.message || (pingResult.isAlive ? 'Phản hồi tốt' : 'Không có phản hồi')}
                    </p>
                  </div>

                  {pingResult.timestamp && (
                    <div className="text-[10px] text-muted-foreground flex items-center gap-1 pt-1 border-t border-border/50">
                      <Clock className="h-3 w-3" />
                      <span>Đo lúc: {new Date(pingResult.timestamp).toLocaleTimeString('vi-VN')}</span>
                    </div>
                  )}
                </div>
              ) : (
                <div className="p-3.5 rounded-lg border border-border bg-muted/20 text-center space-y-2">
                  <p className="text-xs text-muted-foreground">
                    Chưa thực hiện đo kiểm kết nối trong phiên này.
                  </p>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={handlePing}
                    className="h-7 text-xs gap-1 cursor-pointer"
                  >
                    <Activity className="h-3 w-3 text-blue-500" />
                    <span>Ping ngay</span>
                  </Button>
                </div>
              )}
            </div>
          </div>

          {/* Card 4: Thông tin thời gian & Kiểm toán */}
          <div className="rounded-xl border border-border bg-card p-5 shadow-xs space-y-4">
            <div className="flex items-center gap-2 pb-3 border-b border-border/70">
              <Clock className="h-4 w-4 text-purple-600" />
              <h2 className="text-sm font-semibold text-foreground">
                Thời Gian & Vết Kiểm Toán
              </h2>
            </div>

            <div className="space-y-3 text-xs">
              <div className="flex items-center justify-between">
                <span className="text-muted-foreground flex items-center gap-1.5">
                  <Calendar className="h-3.5 w-3.5" />
                  <span>Ngày khởi tạo:</span>
                </span>
                <span className="font-medium text-foreground">
                  {device.createdAt ? new Date(device.createdAt).toLocaleString('vi-VN') : '—'}
                </span>
              </div>

              {device.updatedAt && (
                <div className="flex items-center justify-between">
                  <span className="text-muted-foreground flex items-center gap-1.5">
                    <Clock className="h-3.5 w-3.5" />
                    <span>Cập nhật gần nhất:</span>
                  </span>
                  <span className="font-medium text-foreground">
                    {new Date(device.updatedAt).toLocaleString('vi-VN')}
                  </span>
                </div>
              )}

              {isAdmin && (
                <div className="pt-2 border-t border-border/60">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => navigate(`/audit-logs?keyword=${encodeURIComponent(device.code)}`)}
                    className="w-full text-xs h-8 gap-1.5 cursor-pointer justify-center text-purple-700 dark:text-purple-300 border-purple-200 dark:border-purple-900 hover:bg-purple-50 dark:hover:bg-purple-950/50"
                  >
                    <ExternalLink className="h-3.5 w-3.5" />
                    <span>Xem nhật ký kiểm toán thiết bị này</span>
                  </Button>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Modal Chỉnh sửa thông tin thiết bị */}
      <DeviceFormDialog
        open={isEditOpen}
        onOpenChange={setIsEditOpen}
        initialData={device}
        onSubmit={async (payload) => {
          await updateMutation.mutateAsync(payload as UpdateDeviceRequest);
        }}
        isSubmitting={updateMutation.isPending}
      />

      {/* Confirm Xóa mềm thiết bị */}
      <ConfirmDialog
        open={isDeleteOpen}
        onOpenChange={setIsDeleteOpen}
        title="Xác Nhận Xóa Thiết Bị Vào Thùng Rác"
        description={
          associatedLanes.length > 0
            ? `CẢNH BÁO: Thiết bị "${device.name}" đang được sử dụng trong ${associatedLanes.length} làn xe. Hệ thống sẽ từ chối xóa để bảo vệ toàn vẹn tham chiếu. Vui lòng gỡ liên kết khỏi các làn xe trước.`
            : `Bạn có chắc chắn muốn chuyển thiết bị "${device.name}" (${device.code}) vào thùng rác không? Bản ghi có thể được khôi phục tại màn hình Thùng rác hệ thống.`
        }
        confirmText="Chuyển Vào Thùng Rác"
        cancelText="Hủy Bỏ"
        variant="destructive"
        isLoading={deleteMutation.isPending}
        icon={<Trash2 className="h-5 w-5 text-destructive" />}
        confirmIcon={<Trash2 className="h-3.5 w-3.5" />}
        onConfirm={() => deleteMutation.mutate()}
      />
    </div>
  );
}
