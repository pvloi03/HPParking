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
  Briefcase,
  UserCheck,
  Phone,
  Mail,
  Calendar,
  Clock,
  Edit,
  AlertCircle,
  HardHat,
  User,
} from 'lucide-react';
import { contractorsApi } from '@/api/masterDataApi';
import { clientApi } from '@/api/clientApi';
import type { ContractorDto } from '@/types/masterData';

export interface ContractorDetailDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  contractorId: string | null;
  onEdit?: (contractor: ContractorDto) => void;
}

export function ContractorDetailDialog({
  open,
  onOpenChange,
  contractorId,
  onEdit,
}: ContractorDetailDialogProps) {
  const [activeTab, setActiveTab] = useState<'info' | 'workers'>('info');

  // Fetch chi tiết nhà thầu
  const {
    data: contractor,
    isLoading: isLoadingContractor,
    isError: isContractorError,
  } = useQuery({
    queryKey: ['contractor-detail', contractorId],
    queryFn: () => (contractorId ? contractorsApi.getById(contractorId) : Promise.reject('No ID')),
    enabled: Boolean(open && contractorId),
  });

  // Fetch danh sách nhân sự / công nhân trực thuộc nhà thầu
  const { data: workersData, isLoading: isLoadingWorkers } = useQuery({
    queryKey: ['contractor-workers', contractorId],
    queryFn: () =>
      contractorId
        ? clientApi.getPaged({ contractorId, pageSize: 20, isActive: true })
        : Promise.resolve({ items: [], pagination: { totalCount: 0, totalPages: 0, pageIndex: 1, pageSize: 20, hasNextPage: false, hasPreviousPage: false } }),
    enabled: Boolean(open && contractorId && (activeTab === 'workers' || activeTab === 'info')),
  });

  const workers = workersData?.items || [];

  const formatDate = (isoString?: string) => {
    if (!isoString) return '—';
    try {
      const d = new Date(isoString);
      return isNaN(d.getTime()) ? '—' : d.toLocaleString('vi-VN');
    } catch {
      return '—';
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <div className="flex items-center gap-3">
            <div className="p-2.5 rounded-xl bg-amber-500/10 text-amber-600 dark:text-amber-400">
              <Briefcase className="h-6 w-6" />
            </div>
            <div>
              <DialogTitle className="text-lg font-bold flex items-center gap-2">
                {isLoadingContractor ? (
                  <Skeleton className="h-6 w-48" />
                ) : (
                  <>
                    <span>{contractor?.name || 'Chi Tiết Nhà Thầu'}</span>
                    {contractor && <ActiveStatusBadge isActive={contractor.isActive} />}
                  </>
                )}
              </DialogTitle>
              <DialogDescription className="text-xs text-muted-foreground mt-0.5">
                {contractor?.code ? `Mã nhà thầu: ${contractor.code}` : 'Xem hồ sơ nhà thầu và danh sách công nhân viên'}
              </DialogDescription>
            </div>
          </div>
        </DialogHeader>

        {isLoadingContractor ? (
          <div className="space-y-4 py-4">
            <div className="grid grid-cols-2 gap-4">
              <Skeleton className="h-14 rounded-lg" />
              <Skeleton className="h-14 rounded-lg" />
            </div>
            <Skeleton className="h-24 rounded-lg" />
          </div>
        ) : isContractorError || !contractor ? (
          <div className="py-8 flex flex-col items-center justify-center text-center">
            <AlertCircle className="h-10 w-10 text-destructive mb-2 opacity-80" />
            <p className="text-sm font-semibold text-foreground">Không thể tải thông tin nhà thầu</p>
            <p className="text-xs text-muted-foreground mt-1">
              Bản ghi có thể đã bị xóa hoặc không thể kết nối tới máy chủ.
            </p>
          </div>
        ) : (
          <div className="space-y-4 pt-2">
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
                Thông tin chung
              </button>
              <button
                type="button"
                onClick={() => setActiveTab('workers')}
                className={`text-xs font-semibold px-3 py-1.5 rounded-lg transition-colors cursor-pointer flex items-center gap-1.5 ${
                  activeTab === 'workers'
                    ? 'bg-primary text-primary-foreground'
                    : 'text-muted-foreground hover:bg-muted'
                }`}
              >
                <HardHat className="h-3.5 w-3.5" />
                <span>Nhân sự nhà thầu ({workers.length})</span>
              </button>
            </div>

            {/* Tab 1: Thông tin chung */}
            {activeTab === 'info' && (
              <div className="space-y-4">
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  {/* Mã định danh */}
                  <div className="p-3 rounded-xl border border-border bg-card/60">
                    <span className="text-[11px] font-medium text-muted-foreground block">
                      Mã nhà thầu (Code)
                    </span>
                    <span className="text-sm font-semibold text-foreground font-mono">
                      {contractor.code}
                    </span>
                  </div>

                  {/* Trạng thái hoạt động */}
                  <div className="p-3 rounded-xl border border-border bg-card/60">
                    <span className="text-[11px] font-medium text-muted-foreground block">
                      Trạng thái hoạt động
                    </span>
                    <div className="mt-1">
                      <ActiveStatusBadge isActive={contractor.isActive} />
                    </div>
                  </div>

                  {/* Người liên hệ đại diện */}
                  <div className="p-3 rounded-xl border border-border bg-card/60">
                    <span className="text-[11px] font-medium text-muted-foreground flex items-center gap-1">
                      <UserCheck className="h-3 w-3 text-muted-foreground" />
                      Người liên hệ đại diện
                    </span>
                    <span className="text-sm font-medium text-foreground block mt-0.5">
                      {contractor.contactPerson || (
                        <span className="text-muted-foreground italic">Chưa cập nhật</span>
                      )}
                    </span>
                  </div>

                  {/* Điện thoại */}
                  <div className="p-3 rounded-xl border border-border bg-card/60">
                    <span className="text-[11px] font-medium text-muted-foreground flex items-center gap-1">
                      <Phone className="h-3 w-3 text-muted-foreground" />
                      Số điện thoại liên hệ
                    </span>
                    {contractor.phoneNumber ? (
                      <a
                        href={`tel:${contractor.phoneNumber}`}
                        className="text-sm font-medium text-blue-600 dark:text-blue-400 hover:underline"
                      >
                        {contractor.phoneNumber}
                      </a>
                    ) : (
                      <span className="text-sm text-muted-foreground italic">Chưa cập nhật</span>
                    )}
                  </div>

                  {/* Email */}
                  <div className="p-3 rounded-xl border border-border bg-card/60">
                    <span className="text-[11px] font-medium text-muted-foreground flex items-center gap-1">
                      <Mail className="h-3 w-3 text-muted-foreground" />
                      Hộp thư điện tử
                    </span>
                    {contractor.email ? (
                      <a
                        href={`mailto:${contractor.email}`}
                        className="text-sm font-medium text-blue-600 dark:text-blue-400 hover:underline"
                      >
                        {contractor.email}
                      </a>
                    ) : (
                      <span className="text-sm text-muted-foreground italic">Chưa cập nhật</span>
                    )}
                  </div>

                  {/* Thời gian tạo */}
                  <div className="p-3 rounded-xl border border-border bg-card/60">
                    <span className="text-[11px] font-medium text-muted-foreground flex items-center gap-1">
                      <Calendar className="h-3 w-3 text-muted-foreground" />
                      Thời gian tạo bản ghi
                    </span>
                    <span className="text-xs text-foreground mt-0.5 block">
                      {formatDate(contractor.createdAt)}
                    </span>
                  </div>

                  {/* Thời gian cập nhật */}
                  <div className="p-3 rounded-xl border border-border bg-card/60 sm:col-span-2">
                    <span className="text-[11px] font-medium text-muted-foreground flex items-center gap-1">
                      <Clock className="h-3 w-3 text-muted-foreground" />
                      Cập nhật lần cuối
                    </span>
                    <span className="text-xs text-foreground mt-0.5 block">
                      {formatDate(contractor.updatedAt)}
                    </span>
                  </div>
                </div>

                {/* Thống kê nhanh nhân sự */}
                <div className="p-3.5 rounded-xl border border-border bg-muted/40 flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <HardHat className="h-4 w-4 text-amber-600 dark:text-amber-400" />
                    <span className="text-xs font-semibold text-foreground">
                      Số lượng nhân sự / công nhân đăng ký:
                    </span>
                  </div>
                  <span className="text-sm font-bold text-foreground font-mono">
                    {workers.length} nhân sự
                  </span>
                </div>
              </div>
            )}

            {/* Tab 2: Danh sách công nhân viên */}
            {activeTab === 'workers' && (
              <div className="space-y-2">
                {isLoadingWorkers ? (
                  <div className="space-y-2">
                    <Skeleton className="h-12 w-full rounded-lg" />
                    <Skeleton className="h-12 w-full rounded-lg" />
                  </div>
                ) : workers.length === 0 ? (
                  <div className="py-6 text-center border border-dashed border-border rounded-xl">
                    <HardHat className="h-8 w-8 text-muted-foreground mx-auto mb-1.5 opacity-40" />
                    <p className="text-xs text-muted-foreground">
                      Chưa có nhân sự nào được đăng ký dưới nhà thầu này
                    </p>
                  </div>
                ) : (
                  <div className="max-h-60 overflow-y-auto space-y-2 pr-1">
                    {workers.map((client) => (
                      <div
                        key={client.id}
                        className="p-2.5 rounded-lg border border-border bg-card flex items-center justify-between gap-2"
                      >
                        <div className="flex items-center gap-2.5 min-w-0">
                          <div className="p-1.5 rounded-lg bg-amber-500/10 text-amber-600">
                            <User className="h-4 w-4" />
                          </div>
                          <div className="min-w-0">
                            <div className="flex items-center gap-2">
                              <span className="text-xs font-semibold text-foreground truncate">
                                {client.name}
                              </span>
                              <Badge variant="outline" className="text-[10px] py-0 px-1 font-mono">
                                {client.code}
                              </Badge>
                            </div>
                            {client.phoneNumber && (
                              <p className="text-[11px] text-muted-foreground mt-0.5">
                                SĐT: {client.phoneNumber}
                              </p>
                            )}
                          </div>
                        </div>
                        <ActiveStatusBadge isActive={client.isActive} />
                      </div>
                    ))}
                  </div>
                )}
              </div>
            )}
          </div>
        )}

        <DialogFooter className="flex items-center justify-end gap-2 pt-2 border-t border-border">
          {contractor && onEdit && (
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => {
                onOpenChange(false);
                onEdit(contractor);
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
