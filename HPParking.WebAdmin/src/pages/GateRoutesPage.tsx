import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from '@/hooks/use-toast';
import { Route, Plus, Edit2, Trash2, ArrowRight, Clock, Truck } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { ActiveStatusBadge } from '@/components/common/ActiveStatusBadge';
import { ConfirmDialog } from '@/components/common/ConfirmDialog';
import { GateRouteFormDialog } from '@/components/routes/GateRouteFormDialog';
import { gateRouteApi } from '@/api/gateRouteApi';
import type {
  GateRouteDto,
  CreateGateRouteRequest,
  UpdateGateRouteRequest,
  RouteGateStep,
} from '@/types/gateRoute';
import { Badge } from '@/components/ui/badge';

export function getStepGateDisplayName(step?: RouteGateStep | null, fallback = 'Cổng không xác định'): string {
  if (!step) return fallback;
  if (step.gateName) return step.gateName;
  if (step.gateCode) return `Cổng ${step.gateCode}`;
  if (step.gateId) return `Cổng ID: ${step.gateId.slice(0, 8)}`;
  return fallback;
}

export function GateRoutesPage() {
  const queryClient = useQueryClient();
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [selectedRoute, setSelectedRoute] = useState<GateRouteDto | null>(null);
  const [deleteCandidate, setDeleteCandidate] = useState<GateRouteDto | null>(null);

  const { data: routes = [], isLoading } = useQuery<GateRouteDto[]>({
    queryKey: ['gateRoutes'],
    queryFn: () => gateRouteApi.getAll(),
  });

  const createMutation = useMutation({
    mutationFn: (data: CreateGateRouteRequest) => gateRouteApi.create(data),
    onSuccess: () => {
      toast.success('Đã tạo tuyến điều vận mới thành công');
      setIsFormOpen(false);
      queryClient.invalidateQueries({ queryKey: ['gateRoutes'] });
      queryClient.invalidateQueries({ queryKey: ['vehicles'] });
      queryClient.invalidateQueries({ queryKey: ['vehicles-shared-all'] });
    },
    onError: (err: any) => {
      toast.error(err.response?.data?.message || err.message || 'Lỗi khi tạo tuyến');
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateGateRouteRequest }) =>
      gateRouteApi.update(id, data),
    onSuccess: () => {
      toast.success('Đã cập nhật tuyến điều vận thành công');
      setIsFormOpen(false);
      setSelectedRoute(null);
      queryClient.invalidateQueries({ queryKey: ['gateRoutes'] });
      queryClient.invalidateQueries({ queryKey: ['vehicles'] });
      queryClient.invalidateQueries({ queryKey: ['vehicles-shared-all'] });
    },
    onError: (err: any) => {
      toast.error(err.response?.data?.message || err.message || 'Lỗi khi cập nhật tuyến');
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => gateRouteApi.delete(id),
    onSuccess: () => {
      toast.success('Đã xóa tuyến điều vận thành công');
      setDeleteCandidate(null);
      queryClient.invalidateQueries({ queryKey: ['gateRoutes'] });
    },
    onError: (err: any) => {
      toast.error(err.response?.data?.message || err.message || 'Lỗi khi xóa tuyến');
    },
  });

  const handleFormSubmit = async (data: CreateGateRouteRequest | UpdateGateRouteRequest) => {
    if (selectedRoute) {
      await updateMutation.mutateAsync({ id: selectedRoute.id, data: data as UpdateGateRouteRequest });
    } else {
      await createMutation.mutateAsync(data as CreateGateRouteRequest);
    }
  };

  return (
    <div className="space-y-4 p-4 lg:p-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <div>
          <h1 className="text-xl font-bold tracking-tight text-foreground flex items-center gap-2">
            <Route className="h-6 w-6 text-blue-600" />
            Tuyến Điều Vận Phương Tiện Nội Bộ
          </h1>
          <p className="text-xs text-muted-foreground mt-0.5">
            Quản lý lộ trình đa cổng liên nhà máy kèm cấu hình thời gian SLA tối đa di chuyển và dừng đỗ.
          </p>
        </div>
        <Button
          onClick={() => {
            setSelectedRoute(null);
            setIsFormOpen(true);
          }}
          size="sm"
          className="bg-blue-600 hover:bg-blue-700 text-white text-xs h-9 gap-1.5 cursor-pointer self-start sm:self-auto"
        >
          <Plus className="h-4 w-4" />
          <span>Tạo Tuyến Mới</span>
        </Button>
      </div>

      {/* Routes List */}
      {isLoading ? (
        <div className="py-12 text-center text-xs text-muted-foreground">Đang tải danh sách tuyến...</div>
      ) : routes.length === 0 ? (
        <div className="py-12 text-center border rounded-lg bg-card text-muted-foreground space-y-2">
          <Route className="h-8 w-8 mx-auto text-muted-foreground/60" />
          <p className="text-sm font-medium">Chưa có tuyến điều vận cố định nào</p>
          <p className="text-xs">Tất cả phương tiện nội bộ sẽ áp dụng tuyến tự do mặc định (15 phút).</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-3">
          {routes.map((route: GateRouteDto) => (
            <div
              key={route.id}
              className="rounded-lg border border-border bg-card p-4 transition-all hover:border-blue-300 dark:hover:border-blue-900 shadow-xs space-y-3"
            >
              <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2 border-b border-border/60 pb-3">
                <div className="flex items-center gap-2.5">
                  <div className="px-2 py-0.5 rounded font-mono font-bold text-xs bg-blue-100 dark:bg-blue-950 text-blue-700 dark:text-blue-300">
                    {route.routeCode}
                  </div>
                  <div>
                    <div className="flex items-center gap-2">
                      <h3 className="text-sm font-bold text-foreground">{route.routeName}</h3>
                      {(route.isDefault || route.routeCode === 'DEFAULT') ? (
                        <Badge className="bg-amber-100 text-amber-800 border border-amber-300 dark:bg-amber-950 dark:text-amber-300 text-[10px] font-semibold">
                          Mặc định (Tự do)
                        </Badge>
                      ) : (
                        <Badge variant="outline" className="text-[10px] gap-1 font-normal text-muted-foreground">
                          <Truck className="h-3 w-3" />
                          {route.assignedVehicleIds && route.assignedVehicleIds.length > 0
                            ? `${route.assignedVehicleIds.length} xe áp dụng`
                            : 'Chưa gán xe'}
                        </Badge>
                      )}
                    </div>
                    {route.description && (
                      <p className="text-xs text-muted-foreground mt-0.5">{route.description}</p>
                    )}
                  </div>
                </div>

                <div className="flex items-center gap-2 self-end sm:self-auto">
                  <ActiveStatusBadge isActive={route.isActive} />
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-7 w-7 p-0 text-muted-foreground hover:text-foreground cursor-pointer"
                    onClick={() => {
                      setSelectedRoute(route);
                      setIsFormOpen(true);
                    }}
                    title="Chỉnh sửa tuyến"
                  >
                    <Edit2 className="h-3.5 w-3.5" />
                  </Button>
                  {!(route.isDefault || route.routeCode === 'DEFAULT') && (
                    <Button
                      variant="ghost"
                      size="sm"
                      className="h-7 w-7 p-0 text-muted-foreground hover:text-destructive cursor-pointer"
                      onClick={() => setDeleteCandidate(route)}
                      title="Xóa tuyến"
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                    </Button>
                  )}
                </div>
              </div>

              {/* Chặng Timeline hoặc Cấu hình SLA Tự Do */}
              {route.isDefault || route.routeCode === 'DEFAULT' ? (
                <div className="rounded-md bg-amber-50/50 dark:bg-amber-950/20 p-2.5 border border-amber-200/60 dark:border-amber-900/40 flex flex-wrap items-center gap-4 text-xs text-muted-foreground">
                  <span className="flex items-center gap-1.5">
                    <Clock className="h-3.5 w-3.5 text-amber-600 dark:text-amber-400" />
                    <span>SLA di chuyển tối đa:</span>
                    <strong className="text-foreground font-mono">{route.defaultTravelMinutes ?? 15} phút</strong>
                  </span>
                  <span>•</span>
                  <span className="flex items-center gap-1.5">
                    <Clock className="h-3.5 w-3.5 text-amber-600 dark:text-amber-400" />
                    <span>SLA dừng làm việc tối đa:</span>
                    <strong className="text-foreground font-mono">{route.defaultStayMinutes ?? 15} phút</strong>
                  </span>
                  {route.alertEmails && route.alertEmails.length > 0 && (
                    <>
                      <span>•</span>
                      <span>Email cảnh báo: <strong className="text-foreground">{route.alertEmails.join(', ')}</strong></span>
                    </>
                  )}
                </div>
              ) : (
                <div className="space-y-1.5">
                  <span className="text-[11px] font-semibold text-muted-foreground uppercase tracking-wider block">
                    Lộ trình khứ hồi ({(route.gateSteps || []).length} chặng):
                  </span>
                  <div className="flex flex-wrap items-center gap-2">
                    {(() => {
                      const originStep = route.gateSteps?.find((s) => s.stepIndex === 1) || route.gateSteps?.[0];
                      return (route.gateSteps || []).map((step: RouteGateStep, idx: number) => {
                        const isOrigin = idx === 0;
                        return (
                          <div key={idx} className="flex items-center gap-2">
                            <div
                              className={`flex items-center gap-2 px-3 py-1.5 rounded-lg border text-xs ${
                                isOrigin
                                  ? 'border-blue-300 dark:border-blue-800 bg-blue-50/50 dark:bg-blue-950/30'
                                  : 'border-border bg-muted/40'
                              }`}
                            >
                              <span
                                className={`w-4 h-4 rounded-full text-white font-bold text-[9px] flex items-center justify-center shrink-0 ${
                                  isOrigin ? 'bg-blue-600' : 'bg-emerald-600'
                                }`}
                              >
                                {step.stepIndex}
                              </span>
                              <div className="flex flex-col">
                                <span className="font-semibold text-foreground">
                                  {isOrigin
                                    ? `📍 Xuất phát: ${getStepGateDisplayName(step)}`
                                    : `🏁 Điểm ${idx}: ${getStepGateDisplayName(step)}`}
                                </span>
                                <span className="text-[10px] text-muted-foreground flex items-center gap-1">
                                  {isOrigin ? (
                                    <span>Quay về: {step.maxTravelMinutes} phút</span>
                                  ) : (
                                    <>
                                      <span>SLA: {step.maxTravelMinutes}p di chuyển</span>
                                      <span>•</span>
                                      <span>{step.maxStayMinutes}p dừng đỗ</span>
                                    </>
                                  )}
                                </span>
                              </div>
                            </div>

                            {idx < (route.gateSteps || []).length - 1 ? (
                              <ArrowRight className="h-3.5 w-3.5 text-muted-foreground/60 shrink-0" />
                            ) : (
                              route.gateSteps &&
                              route.gateSteps.length >= 2 && (
                                <div className="flex items-center gap-1 text-[10px] font-medium text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-950/40 px-2 py-1 rounded border border-blue-200 dark:border-blue-900">
                                  <span>
                                    ↩ Về lại {getStepGateDisplayName(originStep, 'Cổng 1')} ({originStep?.maxTravelMinutes ?? 15}p)
                                  </span>
                                </div>
                              )
                            )}
                          </div>
                        );
                      });
                    })()}
                  </div>
                </div>
              )}
            </div>
          ))}
        </div>
      )}

      {/* Form Dialog */}
      <GateRouteFormDialog
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        initialData={selectedRoute}
        onSubmit={handleFormSubmit}
        isSubmitting={createMutation.isPending || updateMutation.isPending}
      />

      {/* Confirm Delete Dialog */}
      <ConfirmDialog
        open={Boolean(deleteCandidate)}
        onOpenChange={(open) => !open && setDeleteCandidate(null)}
        title="Xóa Tuyến Điều Vận"
        description={`Bạn có chắc chắn muốn xóa tuyến "${deleteCandidate?.routeName}" (${deleteCandidate?.routeCode}) không? Hành động này sẽ chuyển các phương tiện nội bộ đang gán tuyến này sang tuyến tự do mặc định.`}
        confirmText="Xác Nhận Xóa"
        cancelText="Hủy Bỏ"
        variant="destructive"
        onConfirm={() => {
          if (deleteCandidate) {
            deleteMutation.mutate(deleteCandidate.id);
          }
        }}
      />
    </div>
  );
}
