import { useState, useMemo } from 'react';
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
  DoorOpen,
  Building2,
  Route,
  Monitor,
  Calendar,
  Clock,
  Edit,
  AlertCircle,
  ArrowDownLeft,
  ArrowUpRight,
  ArrowLeftRight,
} from 'lucide-react';
import { gatesApi, lanesApi } from '@/api/infrastructureApi';
import { LaneDirection, type GateDto, type LaneDto } from '@/types/infrastructure';
import type { CompanyDto } from '@/types/masterData';
import { formatDateTimeVi } from '@/utils/formatters';
import { createEmptyPagedResult } from '@/utils/pagination';

export interface GateDetailDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  gateId: string | null;
  companies?: CompanyDto[];
  onEdit?: (gate: GateDto) => void;
}

export function GateDetailDialog({
  open,
  onOpenChange,
  gateId,
  companies = [],
  onEdit,
}: GateDetailDialogProps) {
  const [activeTab, setActiveTab] = useState<'info' | 'lanes'>('info');

  const companyMap = useMemo(() => {
    const map = new Map<string, string>();
    companies.forEach((c) => map.set(c.id, c.name));
    return map;
  }, [companies]);

  // Fetch chi tiết cổng kiểm soát
  const {
    data: gate,
    isLoading: isLoadingGate,
    isError: isGateError,
  } = useQuery({
    queryKey: ['gate-detail', gateId],
    queryFn: () => (gateId ? gatesApi.getById(gateId) : Promise.reject('No ID')),
    enabled: Boolean(open && gateId),
  });

  // Fetch danh sách làn xe thuộc cổng này
  const { data: lanesData, isLoading: isLoadingLanes } = useQuery({
    queryKey: ['gate-lanes', gateId],
    queryFn: () =>
      gateId
        ? lanesApi.getPaged({ gateId, pageSize: 50 })
        : Promise.resolve(createEmptyPagedResult<LaneDto>(50)),
    enabled: Boolean(open && gateId && (activeTab === 'lanes' || activeTab === 'info')),
  });

  const lanes = lanesData?.items || [];
  const companyName =
    gate?.companyName ||
    (gate?.companyId ? companyMap.get(gate.companyId) : undefined) ||
    'Chưa liên kết công ty';

  const getDirectionBadge = (dir?: number) => {
    switch (dir) {
      case LaneDirection.In:
        return (
          <Badge className="bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300 gap-1 text-[10px]">
            <ArrowDownLeft className="h-3 w-3" />
            <span>Làn Vào (In)</span>
          </Badge>
        );
      case LaneDirection.Out:
        return (
          <Badge className="bg-amber-100 text-amber-800 dark:bg-amber-950 dark:text-amber-300 gap-1 text-[10px]">
            <ArrowUpRight className="h-3 w-3" />
            <span>Làn Ra (Out)</span>
          </Badge>
        );
      case LaneDirection.Bidirectional:
        return (
          <Badge className="bg-blue-100 text-blue-800 dark:bg-blue-950 dark:text-blue-300 gap-1 text-[10px]">
            <ArrowLeftRight className="h-3 w-3" />
            <span>Hai Chiều</span>
          </Badge>
        );
      default:
        return null;
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <div className="flex items-center gap-3">
            <div className="p-2.5 rounded-xl bg-teal-500/10 text-teal-600 dark:text-teal-400">
              <DoorOpen className="h-6 w-6" />
            </div>
            <div>
              <DialogTitle className="text-lg font-bold flex items-center gap-2">
                {isLoadingGate ? (
                  <Skeleton className="h-6 w-48" />
                ) : (
                  <>
                    <span>{gate?.name || 'Chi Tiết Cổng Kiểm Soát'}</span>
                    {gate && <ActiveStatusBadge isActive={gate.isActive} />}
                  </>
                )}
              </DialogTitle>
              <DialogDescription className="text-xs text-muted-foreground mt-0.5">
                {gate?.code ? `Mã cổng: ${gate.code}` : 'Xem hồ sơ cổng và danh sách các làn xe điều khiển'}
              </DialogDescription>
            </div>
          </div>
        </DialogHeader>

        {isLoadingGate ? (
          <div className="space-y-4 py-4">
            <div className="grid grid-cols-2 gap-4">
              <Skeleton className="h-14 rounded-lg" />
              <Skeleton className="h-14 rounded-lg" />
            </div>
            <Skeleton className="h-24 rounded-lg" />
          </div>
        ) : isGateError || !gate ? (
          <div className="py-8 flex flex-col items-center justify-center text-center">
            <AlertCircle className="h-10 w-10 text-destructive mb-2 opacity-80" />
            <p className="text-sm font-semibold text-foreground">Không thể tải thông tin cổng</p>
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
                onClick={() => setActiveTab('lanes')}
                className={`text-xs font-semibold px-3 py-1.5 rounded-lg transition-colors cursor-pointer flex items-center gap-1.5 ${
                  activeTab === 'lanes'
                    ? 'bg-primary text-primary-foreground'
                    : 'text-muted-foreground hover:bg-muted'
                }`}
              >
                <Route className="h-3.5 w-3.5" />
                <span>Danh sách làn xe ({lanes.length})</span>
              </button>
            </div>

            {/* Tab 1: Thông tin chung */}
            {activeTab === 'info' && (
              <div className="space-y-4">
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  {/* Mã cổng */}
                  <div className="p-3 rounded-xl border border-border bg-card/60">
                    <span className="text-[11px] font-medium text-muted-foreground block">
                      Mã cổng kiểm soát (Code)
                    </span>
                    <span className="text-sm font-semibold text-foreground font-mono">
                      {gate.code}
                    </span>
                  </div>

                  {/* Trạng thái hoạt động */}
                  <div className="p-3 rounded-xl border border-border bg-card/60">
                    <span className="text-[11px] font-medium text-muted-foreground block">
                      Trạng thái hoạt động
                    </span>
                    <div className="mt-1">
                      <ActiveStatusBadge isActive={gate.isActive} />
                    </div>
                  </div>

                  {/* Mã máy trạm */}
                  <div className="p-3 rounded-xl border border-border bg-card/60">
                    <span className="text-[11px] font-medium text-muted-foreground flex items-center gap-1">
                      <Monitor className="h-3 w-3 text-muted-foreground" />
                      Mã máy trạm (Machine Code)
                    </span>
                    <span className="text-sm font-semibold text-foreground font-mono mt-0.5 block">
                      {gate.machineCode || (
                        <span className="text-muted-foreground italic font-sans">Chưa cấu hình</span>
                      )}
                    </span>
                  </div>

                  {/* Đơn vị quản lý */}
                  <div className="p-3 rounded-xl border border-border bg-card/60">
                    <span className="text-[11px] font-medium text-muted-foreground flex items-center gap-1">
                      <Building2 className="h-3 w-3 text-muted-foreground" />
                      Công ty chủ quản
                    </span>
                    <span className="text-sm font-semibold text-foreground block mt-0.5">
                      {companyName}
                    </span>
                  </div>

                  {/* Thời gian tạo */}
                  <div className="p-3 rounded-xl border border-border bg-card/60">
                    <span className="text-[11px] font-medium text-muted-foreground flex items-center gap-1">
                      <Calendar className="h-3 w-3 text-muted-foreground" />
                      Thời gian tạo bản ghi
                    </span>
                    <span className="text-xs text-foreground mt-0.5 block">
                      {formatDateTimeVi(gate.createdAt)}
                    </span>
                  </div>

                  {/* Thời gian cập nhật */}
                  <div className="p-3 rounded-xl border border-border bg-card/60">
                    <span className="text-[11px] font-medium text-muted-foreground flex items-center gap-1">
                      <Clock className="h-3 w-3 text-muted-foreground" />
                      Cập nhật lần cuối
                    </span>
                    <span className="text-xs text-foreground mt-0.5 block">
                      {formatDateTimeVi(gate.updatedAt)}
                    </span>
                  </div>
                </div>

                {/* Thống kê nhanh làn xe */}
                <div className="p-3.5 rounded-xl border border-border bg-muted/40 flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <Route className="h-4 w-4 text-teal-600 dark:text-teal-400" />
                    <span className="text-xs font-semibold text-foreground">
                      Số làn xe đang kết nối qua cổng:
                    </span>
                  </div>
                  <span className="text-sm font-bold text-foreground font-mono">
                    {lanes.length} làn xe
                  </span>
                </div>
              </div>
            )}

            {/* Tab 2: Danh sách làn xe */}
            {activeTab === 'lanes' && (
              <div className="space-y-2">
                {isLoadingLanes ? (
                  <div className="space-y-2">
                    <Skeleton className="h-12 w-full rounded-lg" />
                    <Skeleton className="h-12 w-full rounded-lg" />
                  </div>
                ) : lanes.length === 0 ? (
                  <div className="py-6 text-center border border-dashed border-border rounded-xl">
                    <Route className="h-8 w-8 text-muted-foreground mx-auto mb-1.5 opacity-40" />
                    <p className="text-xs text-muted-foreground">
                      Chưa có làn xe nào được liên kết với cổng này
                    </p>
                  </div>
                ) : (
                  <div className="max-h-60 overflow-y-auto space-y-2 pr-1">
                    {lanes.map((lane) => (
                      <div
                        key={lane.id}
                        className="p-2.5 rounded-lg border border-border bg-card flex items-center justify-between gap-2"
                      >
                        <div className="min-w-0 space-y-1">
                          <div className="flex items-center gap-2">
                            <span className="text-xs font-semibold text-foreground truncate">
                              {lane.name}
                            </span>
                            <Badge variant="outline" className="text-[10px] py-0 px-1 font-mono">
                              {lane.code}
                            </Badge>
                            {getDirectionBadge(lane.direction)}
                          </div>
                          <p className="text-[11px] text-muted-foreground font-mono">
                            Relay: {lane.outputRelay} | Reader: {lane.inputReader}
                          </p>
                        </div>
                        <ActiveStatusBadge isActive={lane.isActive} />
                      </div>
                    ))}
                  </div>
                )}
              </div>
            )}
          </div>
        )}

        <DialogFooter className="flex items-center justify-end gap-2 pt-2 border-t border-border">
          {gate && onEdit && (
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => {
                onOpenChange(false);
                onEdit(gate);
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
