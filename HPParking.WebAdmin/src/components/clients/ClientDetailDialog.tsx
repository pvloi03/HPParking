import { useState, useMemo } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
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
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { formatAvatarUrl } from '@/utils/formatAvatarUrl';
import { toast } from 'sonner';
import {
  User,
  Phone,
  Mail,
  MapPin,
  Calendar,
  CreditCard,
  Building2,
  Car,
  Bike,
  ScanFace,
  CheckCircle2,
  AlertCircle,
  Clock,
  Edit,
  Loader2,
  RotateCcw,
} from 'lucide-react';
import { clientApi, extractErrorMessage } from '@/api/clientApi';
import {
  ClientType,
  type ClientDto,
  type ClientDetailDto,
} from '@/types/client';
import { VehicleType } from '@/types/vehicle';
import type { CompanyDto, DepartmentDto, ContractorDto } from '@/types/masterData';

export interface ClientDetailDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  clientId: string | null;
  companies: CompanyDto[];
  departments: DepartmentDto[];
  contractors: ContractorDto[];
  onEdit?: (client: ClientDto) => void;
}

export function ClientDetailDialog({
  open,
  onOpenChange,
  clientId,
  companies,
  departments,
  contractors,
  onEdit,
}: ClientDetailDialogProps) {
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState<'info' | 'vehicles' | 'faceid'>('info');
  const [isSyncingFaceId, setIsSyncingFaceId] = useState(false);

  // Fetch chi tiết khách hàng qua API GET /v1/clients/{id}
  const {
    data: client,
    isLoading,
    isError,
    error,
    refetch,
  } = useQuery<ClientDetailDto>({
    queryKey: ['client-detail', clientId],
    queryFn: () => (clientId ? clientApi.getById(clientId) : Promise.reject('No ID')),
    enabled: Boolean(open && clientId),
  });

  // Bản đồ tra cứu tên đơn vị
  const companyMap = useMemo(() => new Map(companies.map((c) => [c.id, c.name])), [companies]);
  const departmentMap = useMemo(() => new Map(departments.map((d) => [d.id, d.name])), [departments]);
  const contractorMap = useMemo(() => new Map(contractors.map((c) => [c.id, c.name])), [contractors]);


  // Tính tuổi từ ngày sinh
  const formatBirthDayAndAge = (iso?: string) => {
    if (!iso) return '—';
    try {
      const d = new Date(iso);
      if (isNaN(d.getTime())) return '—';
      const formatted = d.toLocaleDateString('vi-VN');
      return `${formatted}`;
    } catch {
      return '—';
    }
  };

  // Định dạng ngày giờ
  const formatDateTime = (iso?: string) => {
    if (!iso) return '—';
    try {
      const d = new Date(iso);
      if (isNaN(d.getTime())) return '—';
      return d.toLocaleString('vi-VN', {
        hour: '2-digit',
        minute: '2-digit',
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
      });
    } catch {
      return '—';
    }
  };

  // Nhãn phân loại đối tượng
  const getClientTypeBadge = (type?: ClientType) => {
    switch (type) {
      case ClientType.Employee:
        return (
          <Badge className="bg-blue-100 text-blue-800 dark:bg-blue-950 dark:text-blue-300 border-blue-200 dark:border-blue-800 text-xs font-semibold">
            Cán bộ nhân viên
          </Badge>
        );
      case ClientType.Contractor:
        return (
          <Badge className="bg-amber-100 text-amber-900 dark:bg-amber-950 dark:text-amber-300 border-amber-300 dark:border-amber-800 text-xs font-semibold">
            Nhân sự nhà thầu
          </Badge>
        );
      case ClientType.Visitor:
        return (
          <Badge className="bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300 border-emerald-200 dark:border-emerald-800 text-xs font-semibold">
            Khách vãng lai
          </Badge>
        );
      case ClientType.VIP:
        return (
          <Badge className="bg-purple-100 text-purple-800 dark:bg-purple-950 dark:text-purple-300 border-purple-200 dark:border-purple-800 text-xs font-semibold">
            Khách VIP
          </Badge>
        );
      default:
        return (
          <Badge variant="secondary" className="text-xs font-semibold">
            Khác
          </Badge>
        );
    }
  };

  // Phát lệnh đồng bộ FaceID trực tiếp từ chi tiết
  const handleSyncFaceId = async () => {
    if (!clientId) return;
    try {
      setIsSyncingFaceId(true);
      const res = await clientApi.syncFaceId(clientId);
      if (res.totalDevices > 0 && res.failureCount > 0) {
        toast.warning(
          `Đồng bộ FaceID hoàn tất: ${res.successCount}/${res.totalDevices} thiết bị thành công (${res.failureCount} thiết bị mất kết nối hoặc lỗi).`
        );
      } else if (res.totalDevices > 0) {
        toast.success(
          `Đã đồng bộ FaceID lên toàn bộ ${res.totalDevices} thiết bị thành công!`
        );
      } else {
        toast.info('Không có thiết bị điểm danh FaceID nào đang hoạt động trong hệ thống.');
      }
      void refetch();
      void queryClient.invalidateQueries({ queryKey: ['clients'] });
    } catch (err) {
      toast.error(extractErrorMessage(err));
    } finally {
      setIsSyncingFaceId(false);
    }
  };

  const vehicles = client?.vehicles || [];
  const terminals = client?.faceIdTerminals || [];
  const onlineTerminalsCount = terminals.filter((t) => t.isOnline).length;
  const enrolledTerminalsCount = terminals.filter((t) => t.hasFace).length;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-3xl max-h-[92vh] overflow-y-auto p-0 gap-0 bg-card border border-border shadow-2xl">
        {isLoading ? (
          <div className="p-8 space-y-4">
            <div className="flex items-center gap-4">
              <Skeleton className="h-16 w-16 rounded-full" />
              <div className="space-y-2 flex-1">
                <Skeleton className="h-6 w-48" />
                <Skeleton className="h-4 w-32" />
              </div>
            </div>
            <Skeleton className="h-48 w-full rounded-xl" />
          </div>
        ) : isError || !client ? (
          <div className="p-8 text-center space-y-4">
            <AlertCircle className="h-12 w-12 text-destructive mx-auto" />
            <div className="space-y-1">
              <h3 className="text-base font-bold text-foreground">Không thể tải thông tin khách hàng</h3>
              <p className="text-xs text-muted-foreground">{extractErrorMessage(error)}</p>
            </div>
            <Button variant="outline" size="sm" onClick={() => refetch()} className="text-xs">
              Thử lại
            </Button>
          </div>
        ) : (
          <>
            {/* Header hồ sơ nổi bật với Avatar và tóm tắt */}
            <div className="p-6 mt-4 bg-gradient-to-r from-blue-50/80 via-background to-blue-50/30 dark:from-blue-950/40 dark:via-background dark:to-blue-950/20 border-b border-border">
              <DialogHeader className="space-y-0 text-left">
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                  <div className="flex items-center gap-4">
                    <div className="relative">
                      <Avatar className="h-16 w-16 border-2 border-background shadow-md shrink-0">
                        <AvatarImage
                          src={formatAvatarUrl(client.avatar, client.updatedAt || client.createdAt)}
                          alt={client.name}
                          className="object-cover"
                        />
                        <AvatarFallback className="font-extrabold text-lg text-blue-600 dark:text-blue-400 bg-muted">
                          {(client.name || '')
                            .trim()
                            .split(/\s+/)
                            .filter(Boolean)
                            .map((n) => n[0])
                            .slice(-2)
                            .join('')
                            .toUpperCase() || 'KH'}
                        </AvatarFallback>
                      </Avatar>
                      <span
                        className={`absolute bottom-0 right-0 h-4 w-4 rounded-full border-2 border-background shadow-xs ${client.isActive ? 'bg-emerald-500' : 'bg-neutral-400'
                          }`}
                        title={client.isActive ? 'Hồ sơ đang hoạt động' : 'Hồ sơ ngừng hoạt động'}
                      />
                    </div>

                    <div className="space-y-1">
                      <div className="flex items-center gap-2 flex-wrap">
                        <DialogTitle className="text-lg font-bold text-foreground">
                          {client.name}
                        </DialogTitle>
                        {getClientTypeBadge(client.type)}
                      </div>
                      <DialogDescription className="sr-only">
                        Chi tiết hồ sơ khách hàng, phương tiện và đồng bộ khuôn mặt FaceID.
                      </DialogDescription>

                      <div className="flex items-center gap-3 text-xs text-muted-foreground font-mono flex-wrap">
                        <span className="flex items-center gap-1 font-semibold text-foreground">
                          <CreditCard className="h-3.5 w-3.5 text-blue-600 dark:text-blue-400" />
                          CCCD/Mã định danh: {client.code || '—'}
                        </span>
                        <span>•</span>
                        <span className="flex items-center gap-1">
                          <Phone className="h-3.5 w-3.5 text-muted-foreground" />
                          {client.phoneNumber}
                        </span>
                      </div>
                    </div>
                  </div>

                  {/* Nút hành động nhanh */}
                  <div className="flex items-center gap-2 shrink-0 self-start sm:self-auto">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={handleSyncFaceId}
                      disabled={isSyncingFaceId}
                      className="text-xs h-8 gap-1.5 border-blue-200 dark:border-blue-900 text-blue-700 dark:text-blue-300 hover:bg-blue-50 dark:hover:bg-blue-950/60 cursor-pointer shadow-2xs"
                      title="Đồng bộ lại FaceID của khách hàng này xuống toàn bộ thiết bị"
                    >
                      {isSyncingFaceId ? (
                        <Loader2 className="h-3.5 w-3.5 animate-spin text-blue-600" />
                      ) : (
                        <ScanFace className="h-3.5 w-3.5 text-blue-600 dark:text-blue-400" />
                      )}
                      <span>Đồng bộ FaceID</span>
                    </Button>

                    {onEdit && (
                      <Button
                        size="sm"
                        onClick={() => {
                          onOpenChange(false);
                          onEdit(client);
                        }}
                        className="text-xs h-8 gap-1.5 bg-blue-600 hover:bg-blue-700 text-white cursor-pointer shadow-2xs"
                      >
                        <Edit className="h-3.5 w-3.5" />
                        <span>Chỉnh sửa</span>
                      </Button>
                    )}
                  </div>
                </div>
              </DialogHeader>

              {/* Tab Navigation Switches */}
              <div className="flex items-center gap-1 mt-5 pt-3 border-t border-border/60">
                <button
                  type="button"
                  onClick={() => setActiveTab('info')}
                  className={`text-xs px-3 py-1.5 rounded-lg font-medium transition-colors cursor-pointer flex items-center gap-1.5 ${activeTab === 'info'
                    ? 'bg-blue-600 text-white shadow-xs'
                    : 'text-muted-foreground hover:bg-muted/80 hover:text-foreground'
                    }`}
                >
                  <User className="h-3.5 w-3.5" />
                  <span>Thông Tin Cá Nhân</span>
                </button>

                <button
                  type="button"
                  onClick={() => setActiveTab('vehicles')}
                  className={`text-xs px-3 py-1.5 rounded-lg font-medium transition-colors cursor-pointer flex items-center gap-1.5 ${activeTab === 'vehicles'
                    ? 'bg-blue-600 text-white shadow-xs'
                    : 'text-muted-foreground hover:bg-muted/80 hover:text-foreground'
                    }`}
                >
                  <Car className="h-3.5 w-3.5" />
                  <span>Phương Tiện</span>
                  <span
                    className={`ml-0.5 text-[10px] px-1.5 py-0.2 rounded-full font-bold ${activeTab === 'vehicles'
                      ? 'bg-white/20 text-white'
                      : 'bg-muted text-muted-foreground'
                      }`}
                  >
                    {vehicles.length}
                  </span>
                </button>

                <button
                  type="button"
                  onClick={() => setActiveTab('faceid')}
                  className={`text-xs px-3 py-1.5 rounded-lg font-medium transition-colors cursor-pointer flex items-center gap-1.5 ${activeTab === 'faceid'
                    ? 'bg-blue-600 text-white shadow-xs'
                    : 'text-muted-foreground hover:bg-muted/80 hover:text-foreground'
                    }`}
                >
                  <ScanFace className="h-3.5 w-3.5" />
                  <span>Thiết Bị FaceID</span>
                  {terminals.length > 0 && (
                    <span
                      className={`ml-0.5 text-[10px] px-1.5 py-0.2 rounded-full font-bold ${activeTab === 'faceid'
                        ? 'bg-white/20 text-white'
                        : 'bg-muted text-muted-foreground'
                        }`}
                    >
                      {onlineTerminalsCount}/{terminals.length}
                    </span>
                  )}
                </button>
              </div>
            </div>

            {/* Nội dung Tab */}
            <div className="p-6 space-y-4">
              {/* TAB 1: THÔNG TIN CÁ NHÂN & ĐƠN VỊ */}
              {activeTab === 'info' && (
                <div className="space-y-4 animate-in fade-in-50 duration-200">
                  {/* Grid chi tiết định danh cá nhân */}
                  <div className="p-4 rounded-xl border border-border bg-card space-y-3 shadow-2xs">
                    <div className="flex items-center gap-2 pb-2 border-b border-border/60">
                      <User className="h-4 w-4 text-blue-600 dark:text-blue-400" />
                      <span className="text-xs font-bold text-foreground uppercase tracking-wide">
                        1. Thông tin định danh & Liên hệ
                      </span>
                    </div>

                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs">
                      <div className="p-2.5 rounded-lg bg-muted/30 border border-border/40">
                        <span className="text-[11px] text-muted-foreground block mb-0.5">Số CCCD / Mã định danh:</span>
                        <span className="font-mono font-bold text-sm text-foreground">{client.code || '—'}</span>
                      </div>

                      <div className="p-2.5 rounded-lg bg-muted/30 border border-border/40">
                        <span className="text-[11px] text-muted-foreground block mb-0.5">Số điện thoại liên hệ:</span>
                        <span className="font-mono font-bold text-sm text-foreground">{client.phoneNumber || '—'}</span>
                      </div>

                      <div className="p-2.5 rounded-lg bg-muted/30 border border-border/40">
                        <span className="text-[11px] text-muted-foreground flex items-center gap-1 mb-0.5">
                          <Calendar className="h-3 w-3 text-muted-foreground" />
                          <span>Ngày tháng năm sinh:</span>
                        </span>
                        <span className="font-medium text-foreground">{formatBirthDayAndAge(client.birthDay)}</span>
                      </div>

                      <div className="p-2.5 rounded-lg bg-muted/30 border border-border/40">
                        <span className="text-[11px] text-muted-foreground block mb-0.5">Giới tính:</span>
                        <span className="font-medium text-foreground">{client.gender === 1 ? 'Nam' : 'Nữ'}</span>
                      </div>

                      <div className="p-2.5 rounded-lg bg-muted/30 border border-border/40">
                        <span className="text-[11px] text-muted-foreground flex items-center gap-1 mb-0.5">
                          <Mail className="h-3 w-3 text-muted-foreground" />
                          <span>Hòm thư điện tử (Email):</span>
                        </span>
                        <span className="font-medium text-foreground">{client.email || '—'}</span>
                      </div>

                      <div className="p-2.5 rounded-lg bg-muted/30 border border-border/40">
                        <span className="text-[11px] text-muted-foreground block mb-0.5">Trạng thái hồ sơ:</span>
                        <span className="inline-flex items-center gap-1.5 font-semibold">
                          <span
                            className={`h-2 w-2 rounded-full ${client.isActive ? 'bg-emerald-500' : 'bg-neutral-400'
                              }`}
                          />
                          <span className={client.isActive ? 'text-emerald-700 dark:text-emerald-400' : 'text-muted-foreground'}>
                            {client.isActive ? 'Đang hoạt động' : 'Ngừng hoạt động'}
                          </span>
                        </span>
                      </div>

                      <div className="col-span-1 sm:col-span-2 p-2.5 rounded-lg bg-muted/30 border border-border/40">
                        <span className="text-[11px] text-muted-foreground flex items-center gap-1 mb-0.5">
                          <MapPin className="h-3 w-3 text-muted-foreground" />
                          <span>Địa chỉ thường trú / tạm trú:</span>
                        </span>
                        <span className="font-medium text-foreground">{client.address || '—'}</span>
                      </div>
                    </div>
                  </div>

                  {/* Grid đơn vị trực thuộc */}
                  <div className="p-4 rounded-xl border border-border bg-card space-y-3 shadow-2xs">
                    <div className="flex items-center gap-2 pb-2 border-b border-border/60">
                      <Building2 className="h-4 w-4 text-emerald-600 dark:text-emerald-400" />
                      <span className="text-xs font-bold text-foreground uppercase tracking-wide">
                        2. Đơn vị quản lý & Trực thuộc
                      </span>
                    </div>

                    {client.type === ClientType.Employee ? (
                      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs">
                        <div className="p-3 rounded-lg bg-blue-50/40 dark:bg-blue-950/20 border border-blue-200/60 dark:border-blue-900/40 space-y-1">
                          <span className="text-[11px] text-blue-800 dark:text-blue-300 font-semibold block">
                            Công ty trực thuộc:
                          </span>
                          <span className="font-bold text-foreground text-sm block">
                            {client.companyId ? companyMap.get(client.companyId) || 'Công ty đã xóa hoặc không tồn tại' : '— Không thuộc công ty nào —'}
                          </span>
                        </div>

                        <div className="p-3 rounded-lg bg-blue-50/40 dark:bg-blue-950/20 border border-blue-200/60 dark:border-blue-900/40 space-y-1">
                          <span className="text-[11px] text-blue-800 dark:text-blue-300 font-semibold block">
                            Phòng ban trực thuộc:
                          </span>
                          <span className="font-bold text-foreground text-sm block">
                            {client.departmentId ? departmentMap.get(client.departmentId) || 'Phòng ban đã xóa hoặc không tồn tại' : '— Không thuộc phòng ban nào —'}
                          </span>
                        </div>
                      </div>
                    ) : client.type === ClientType.Contractor ? (
                      <div className="p-3 rounded-lg bg-amber-50/40 dark:bg-amber-950/20 border border-amber-200/60 dark:border-amber-900/40 space-y-1 text-xs">
                        <span className="text-[11px] text-amber-800 dark:text-amber-300 font-semibold block">
                          Nhà thầu đối tác thi công:
                        </span>
                        <span className="font-bold text-foreground text-sm block">
                          {client.contractorId ? contractorMap.get(client.contractorId) || 'Nhà thầu đã xóa hoặc không tồn tại' : '— Chưa gán nhà thầu —'}
                        </span>
                      </div>
                    ) : (
                      <div className="p-3 rounded-lg bg-muted/40 border border-border/60 text-xs text-muted-foreground flex items-center gap-2">
                        <Building2 className="h-4 w-4 text-muted-foreground shrink-0" />
                        <span>Đối tượng khách không trực thuộc Công ty, Phòng ban hoặc Nhà thầu đối tác.</span>
                      </div>
                    )}
                  </div>

                  {/* Grid thời hạn ra vào & Ghi chú */}
                  <div className="p-4 rounded-xl border border-border bg-card space-y-3 shadow-2xs">
                    <div className="flex items-center gap-2 pb-2 border-b border-border/60">
                      <Clock className="h-4 w-4 text-purple-600 dark:text-purple-400" />
                      <span className="text-xs font-bold text-foreground uppercase tracking-wide">
                        3. Thời hạn ra vào &amp; Ghi chú
                      </span>
                    </div>

                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs">
                      <div className="p-3 rounded-lg bg-muted/30 border border-border/40 space-y-1">
                        <span className="text-[11px] text-muted-foreground block">Quy định thời hạn ra vào:</span>
                        {client.expired?.enable ? (
                          <div className="flex items-center gap-1.5 text-emerald-700 dark:text-emerald-400 font-semibold">
                            <CheckCircle2 className="h-4 w-4" />
                            <span>Không giới hạn thời gian (Vô thời hạn)</span>
                          </div>
                        ) : (
                          <div className="space-y-0.5">
                            <span className="text-amber-700 dark:text-amber-400 font-semibold block">
                              Có áp dụng giới hạn thời gian:
                            </span>
                            <span className="font-mono text-muted-foreground block">
                              Từ: {formatDateTime(client.expired?.startDay)}
                            </span>
                            <span className="font-mono text-muted-foreground block">
                              Đến: {formatDateTime(client.expired?.endDay)}
                            </span>
                          </div>
                        )}
                      </div>

                      <div className="p-3 rounded-lg bg-muted/30 border border-border/40 space-y-1">
                        <span className="text-[11px] text-muted-foreground block">Ghi chú bổ sung:</span>
                        <p className="text-xs text-foreground font-medium italic">
                          {client.note || '— Không có ghi chú —'}
                        </p>
                      </div>

                      <div className="p-2.5 rounded-lg bg-muted/20 border border-border/30">
                        <span className="text-[10px] text-muted-foreground block">Thời gian tạo hồ sơ:</span>
                        <span className="font-mono text-xs text-foreground">{formatDateTime(client.createdAt)}</span>
                      </div>

                      <div className="p-2.5 rounded-lg bg-muted/20 border border-border/30">
                        <span className="text-[10px] text-muted-foreground block">Cập nhật lần cuối:</span>
                        <span className="font-mono text-xs text-foreground">{formatDateTime(client.updatedAt)}</span>
                      </div>
                    </div>
                  </div>
                </div>
              )}

              {/* TAB 2: DANH SÁCH PHƯƠNG TIỆN */}
              {activeTab === 'vehicles' && (
                <div className="space-y-3 animate-in fade-in-50 duration-200">
                  <div className="flex items-center justify-between">
                    <span className="text-xs font-bold text-foreground uppercase tracking-wide">
                      Phương tiện đăng ký ({vehicles.length})
                    </span>
                    {onEdit && (
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => {
                          onOpenChange(false);
                          onEdit(client);
                        }}
                        className="text-xs h-7 gap-1 border-blue-200 dark:border-blue-900 text-blue-600 dark:text-blue-400 cursor-pointer"
                      >
                        <Car className="h-3.5 w-3.5" />
                        <span>Thêm / Quản lý xe</span>
                      </Button>
                    )}
                  </div>

                  {vehicles.length === 0 ? (
                    <div className="border border-dashed border-border rounded-2xl p-8 flex flex-col items-center justify-center text-center space-y-2 bg-muted/20">
                      <div className="h-10 w-10 rounded-full bg-muted flex items-center justify-center text-muted-foreground">
                        <Car className="h-5 w-5" />
                      </div>
                      <div className="space-y-0.5">
                        <p className="text-sm font-semibold text-foreground">Chưa có phương tiện nào</p>
                        <p className="text-xs text-muted-foreground">
                          Khách hàng này hiện chưa đăng ký biển số xe nào trong hệ thống.
                        </p>
                      </div>
                      {onEdit && (
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => {
                            onOpenChange(false);
                            onEdit(client);
                          }}
                          className="text-xs h-8 gap-1.5 cursor-pointer mt-2"
                        >
                          <Car className="h-3.5 w-3.5" />
                          <span>Đăng ký phương tiện ngay</span>
                        </Button>
                      )}
                    </div>
                  ) : (
                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                      {vehicles.map((v) => (
                        <div
                          key={v.id}
                          className="p-3.5 rounded-xl border border-border bg-card shadow-2xs space-y-2"
                        >
                          <div className="flex items-center justify-between">
                            <div className="flex items-center gap-2">
                              <div className="p-2 rounded-lg bg-blue-50 dark:bg-blue-950 text-blue-600 dark:text-blue-400 shrink-0">
                                {v.type === VehicleType.Motorbike ? (
                                  <Bike className="h-4 w-4" />
                                ) : (
                                  <Car className="h-4 w-4" />
                                )}
                              </div>
                              <div>
                                <span className="font-mono font-extrabold text-sm text-foreground block">
                                  {v.plateNumber}
                                </span>
                                <span className="text-[11px] text-muted-foreground block">
                                  {v.type === VehicleType.Car
                                    ? 'Ô tô'
                                    : v.type === VehicleType.Motorbike
                                      ? 'Xe máy'
                                      : v.type === VehicleType.Bicycle
                                        ? 'Xe đạp / Xe điện'
                                        : 'Khác'}
                                </span>
                              </div>
                            </div>

                            <span
                              className={`text-[10px] px-2 py-0.5 rounded-full font-semibold ${v.isActive
                                ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300'
                                : 'bg-muted text-muted-foreground'
                                }`}
                            >
                              {v.isActive ? 'Đang hoạt động' : 'Tạm dừng'}
                            </span>
                          </div>

                          {v.note && (
                            <p className="text-xs text-muted-foreground bg-muted/40 p-2 rounded-lg italic">
                              {v.note}
                            </p>
                          )}

                          <div className="pt-1.5 border-t border-border/50 text-[10px] text-muted-foreground flex items-center justify-between font-mono">
                            <span>Mã xe: {v.id.slice(-8)}</span>
                            <span>Đăng ký: {formatDateTime(v.createdAt)}</span>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              )}

              {/* TAB 3: THIẾT BỊ FACEID */}
              {activeTab === 'faceid' && (
                <div className="space-y-4 animate-in fade-in-50 duration-200">
                  <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
                    <span className="text-xs font-bold text-foreground uppercase tracking-wide">
                      Trạng thái trên các thiết bị FaceID ({terminals.length})
                    </span>

                    <Button
                      variant="outline"
                      size="sm"
                      onClick={handleSyncFaceId}
                      disabled={isSyncingFaceId}
                      className="text-xs h-7 gap-1.5 border-blue-200 dark:border-blue-900 text-blue-600 dark:text-blue-400 cursor-pointer self-start sm:self-auto"
                    >
                      {isSyncingFaceId ? (
                        <Loader2 className="h-3 w-3 animate-spin" />
                      ) : (
                        <RotateCcw className="h-3 w-3" />
                      )}
                      <span>Làm mới &amp; Đồng bộ</span>
                    </Button>
                  </div>

                  {/* Summary bar */}
                  <div className="grid grid-cols-2 sm:grid-cols-3 gap-2.5 text-xs">
                    <div className="p-3 rounded-xl border border-border bg-card shadow-2xs text-center space-y-0.5">
                      <span className="text-[11px] text-muted-foreground block">Tổng thiết bị làn xe:</span>
                      <span className="font-extrabold text-base text-foreground block">{terminals.length}</span>
                    </div>

                    <div className="p-3 rounded-xl border border-border bg-card shadow-2xs text-center space-y-0.5">
                      <span className="text-[11px] text-muted-foreground block">Thiết bị Online:</span>
                      <span className="font-extrabold text-base text-emerald-600 dark:text-emerald-400 block">
                        {onlineTerminalsCount}/{terminals.length}
                      </span>
                    </div>

                    <div className="p-3 rounded-xl border border-border bg-card shadow-2xs text-center space-y-0.5 col-span-2 sm:col-span-1">
                      <span className="text-[11px] text-muted-foreground block">Đã nạp khuôn mặt:</span>
                      <span className="font-extrabold text-base text-blue-600 dark:text-blue-400 block">
                        {enrolledTerminalsCount}/{terminals.length}
                      </span>
                    </div>
                  </div>

                  {terminals.length === 0 ? (
                    <div className="border border-dashed border-border rounded-2xl p-8 flex flex-col items-center justify-center text-center space-y-2 bg-muted/20">
                      <ScanFace className="h-8 w-8 text-muted-foreground" />
                      <p className="text-xs text-muted-foreground">
                        Chưa có thông tin thiết bị làn xe điểm danh hoặc không có thiết bị FaceID nào đang trực tuyến.
                      </p>
                    </div>
                  ) : (
                    <div className="space-y-2">
                      {terminals.map((t, idx) => (
                        <div
                          key={idx}
                          className="flex flex-col sm:flex-row sm:items-center justify-between p-3 rounded-xl border border-border bg-card shadow-2xs gap-2 text-xs"
                        >
                          <div className="flex items-center gap-2.5">
                            <span
                              className={`h-2.5 w-2.5 rounded-full shrink-0 ${t.isOnline ? 'bg-emerald-500 animate-pulse' : 'bg-rose-500'
                                }`}
                            />
                            <div>
                              <span className="font-semibold text-foreground block">{t.deviceName}</span>
                              <span className="font-mono text-[11px] text-muted-foreground block">
                                IP: {t.deviceIp}
                              </span>
                            </div>
                          </div>

                          <div className="flex items-center gap-2 flex-wrap">
                            <Badge
                              variant={t.isOnline ? 'success' : 'destructive'}
                            >
                              {t.isOnline ? 'Trực tuyến' : 'Mất kết nối'}
                            </Badge>

                            <span
                              className={`text-[10px] px-2 py-0.5 rounded-full font-medium ${t.hasFace
                                ? 'bg-blue-100 text-blue-800 dark:bg-blue-950 dark:text-blue-300'
                                : 'bg-muted text-muted-foreground'
                                }`}
                            >
                              {t.hasFace ? '✓ Đã có ảnh khuôn mặt' : 'Chưa có ảnh'}
                            </span>

                            {t.cardCount > 0 && (
                              <span className="text-[10px] bg-muted px-2 py-0.5 rounded-full text-foreground font-mono">
                                {t.cardCount} thẻ
                              </span>
                            )}
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              )}
            </div>

            {/* Footer */}
            <DialogFooter className="p-4 border-t border-border bg-muted/30 flex items-center justify-between gap-2">
              <span className="text-[11px] text-muted-foreground font-mono">
                ID Khách hàng: {client.id}
              </span>

              <Button
                variant="outline"
                size="sm"
                onClick={() => onOpenChange(false)}
                className="text-xs h-8 cursor-pointer"
              >
                Đóng
              </Button>
            </DialogFooter>
          </>
        )}
      </DialogContent>
    </Dialog>
  );
}
