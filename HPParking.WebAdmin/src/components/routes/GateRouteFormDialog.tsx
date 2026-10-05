import { useEffect, useState, useMemo, useRef } from 'react';
import { useForm, useFieldArray } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useQuery } from '@tanstack/react-query';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/components/ui/dialog';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Checkbox } from '@/components/ui/checkbox';
import {
  Route,
  Plus,
  Trash2,
  Save,
  Clock,
  ArrowDown,
  Truck,
  Search,
  Car,
  Bike,
  MapPin,
  Loader2,
} from 'lucide-react';
import { gatesApi } from '@/api/infrastructureApi';
import { vehicleApi } from '@/api/vehicleApi';
import { VehicleType, type VehicleDto } from '@/types/vehicle';
import { useInfiniteSelectQuery } from '@/hooks/useInfiniteSelectQuery';
import { useIntersectionSentinel } from '@/hooks/useIntersectionSentinel';
import type {
  GateRouteDto,
  CreateGateRouteRequest,
  UpdateGateRouteRequest,
} from '@/types/gateRoute';

const stepSchema = z.object({
  gateId: z.string().min(1, 'Vui lòng chọn cổng'),
  stepIndex: z.number().int().min(1),
  maxTravelMinutes: z.number().int().min(1, 'Thời gian di chuyển tối thiểu 1 phút'),
  maxStayMinutes: z.number().int().min(0, 'Thời gian dừng đỗ không được âm'),
});

const gateRouteSchema = z
  .object({
    routeCode: z
      .string()
      .trim()
      .min(2, 'Mã tuyến tối thiểu 2 ký tự')
      .max(50, 'Mã tuyến tối đa 50 ký tự'),
    routeName: z
      .string()
      .trim()
      .min(2, 'Tên tuyến tối thiểu 2 ký tự')
      .max(200, 'Tên tuyến tối đa 200 ký tự'),
    description: z.string().trim().optional(),
    isClosedLoop: z.boolean(),
    alertEmailsText: z.string().trim().optional(),
    isDefault: z.boolean().optional(),
    defaultTravelMinutes: z.number().int().min(1, 'Thời gian tối thiểu 1 phút').optional(),
    defaultStayMinutes: z.number().int().min(1, 'Thời gian tối thiểu 1 phút').optional(),
    isActive: z.boolean(),
    gateSteps: z.array(stepSchema),
  })
  .refine(
    (data) => data.isDefault || (data.gateSteps && data.gateSteps.length >= 2),
    {
      message: 'Tuyến cố định phải có tối thiểu 2 chặng (1 điểm xuất phát & quay về + ít nhất 1 điểm đến)',
      path: ['gateSteps'],
    }
  )
  .refine(
    (data) => {
      if (data.isDefault || !data.gateSteps || data.gateSteps.length < 2) return true;
      for (let i = 1; i < data.gateSteps.length; i++) {
        if (data.gateSteps[i].gateId && data.gateSteps[i].gateId === data.gateSteps[i - 1].gateId) {
          return false;
        }
      }
      return true;
    },
    {
      message: 'Hai chặng cổng liền kề không được chọn cùng một cổng kiểm soát',
      path: ['gateSteps'],
    }
  );

type GateRouteFormData = z.infer<typeof gateRouteSchema>;

export const DEFAULT_ROUTE_STEPS: z.infer<typeof stepSchema>[] = [
  { gateId: '', stepIndex: 1, maxTravelMinutes: 15, maxStayMinutes: 0 },
  { gateId: '', stepIndex: 2, maxTravelMinutes: 15, maxStayMinutes: 30 },
];

function getArrayErrorMessage(error?: unknown): string | undefined {
  if (!error || typeof error !== 'object') return undefined;
  if ('message' in error && typeof (error as { message?: unknown }).message === 'string') {
    return (error as { message: string }).message;
  }
  if ('root' in error && typeof (error as { root?: unknown }).root === 'object') {
    const root = (error as { root?: { message?: unknown } }).root;
    if (root && typeof root.message === 'string') {
      return root.message;
    }
  }
  return undefined;
}

const getStayMinutesForStepIndex = (stayMinutes: number | undefined, idx: number) =>
  idx === 0 ? 0 : Number(stayMinutes ?? 0);

interface GateSelectFieldProps {
  label: string;
  placeholder: string;
  isOrigin?: boolean;
  value?: string;
  onChange: (val: string) => void;
  error?: string;
  gates: Array<{ id: string; name: string; code: string }>;
}

function GateSelectField({
  label,
  placeholder,
  isOrigin,
  value,
  onChange,
  error,
  gates,
}: GateSelectFieldProps) {
  return (
    <div className="flex flex-col gap-1">
      <label className="text-[11px] font-medium text-foreground flex items-center gap-1 h-5">
        <MapPin className={`h-3 w-3 shrink-0 ${isOrigin ? 'text-blue-600' : 'text-muted-foreground'}`} />
        <span>{label}</span> <span className="text-destructive">*</span>
      </label>
      <Select value={value} onValueChange={onChange}>
        <SelectTrigger className="text-xs h-8">
          <SelectValue placeholder={placeholder} />
        </SelectTrigger>
        <SelectContent>
          {gates.map((g) => (
            <SelectItem key={g.id} value={g.id} className="text-xs">
              {g.name} ({g.code})
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
      {error && <p className="text-[10px] text-destructive">{error}</p>}
    </div>
  );
}

interface GateRouteFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  initialData?: GateRouteDto | null;
  onSubmit: (data: CreateGateRouteRequest | UpdateGateRouteRequest) => Promise<void>;
  isSubmitting?: boolean;
}

export function GateRouteFormDialog({
  open,
  onOpenChange,
  initialData,
  onSubmit,
  isSubmitting = false,
}: GateRouteFormDialogProps) {
  const isEditing = Boolean(initialData);

  const [applyToAllShared, setApplyToAllShared] = useState(false);
  const [selectedVehicleIds, setSelectedVehicleIds] = useState<string[]>([]);

  const { data: gatesData } = useQuery({
    queryKey: ['gates-all-select'],
    queryFn: () => gatesApi.getPaged({ pageIndex: 1, pageSize: 200, isActive: true }),
    enabled: open,
  });
  const gates = gatesData?.items || [];

  // Tải danh sách xe dùng chung theo trang vô tận (Server-side Infinite Scroll)
  const {
    items: sharedVehicles,
    isLoading: isLoadingVehicles,
    isFetchingNextPage: isFetchingMoreVehicles,
    hasNextPage: hasMoreVehicles,
    fetchNextPage: fetchMoreVehicles,
    search: vehicleSearch,
    setSearch: setVehicleSearch,
    totalCount: totalSharedVehiclesCount,
  } = useInfiniteSelectQuery<VehicleDto>({
    queryKey: ['vehicles-shared-infinite'],
    fetchFn: (params) =>
      vehicleApi.getPaged({
        ...params,
        isShared: true,
        isActive: true,
      }),
    pageSize: 20,
    filters: { isShared: true, isActive: true },
    enabled: open,
  });

  const { sentinelRef: vehicleSentinelRef } = useIntersectionSentinel({
    enabled: open && hasMoreVehicles && !isFetchingMoreVehicles,
    onIntersect: fetchMoreVehicles,
    rootMargin: '60px',
  });

  const {
    register,
    control,
    handleSubmit,
    reset,
    setValue,
    watch,
    formState: { errors },
  } = useForm<GateRouteFormData>({
    resolver: zodResolver(gateRouteSchema),
    defaultValues: {
      routeCode: '',
      routeName: '',
      description: '',
      isClosedLoop: true,
      alertEmailsText: '',
      isActive: true,
      gateSteps: [...DEFAULT_ROUTE_STEPS],
    },
  });

  const { fields, append, remove } = useFieldArray({
    control,
    name: 'gateSteps',
  });

  const isActive = watch('isActive');
  const isClosedLoop = watch('isClosedLoop');
  const gateStepsErrorMessage = getArrayErrorMessage(errors.gateSteps);

  const isDefaultRoute = Boolean(initialData?.isDefault || initialData?.routeCode === 'DEFAULT');
  const initializedRouteIdRef = useRef<string | null>(null);

  useEffect(() => {
    if (open) {
      const currentTargetKey = initialData ? initialData.id : 'NEW';
      if (initializedRouteIdRef.current !== currentTargetKey) {
        initializedRouteIdRef.current = currentTargetKey;

        if (initialData) {
          reset({
            routeCode: initialData.routeCode,
            routeName: initialData.routeName,
            description: initialData.description || '',
            isClosedLoop: initialData.isClosedLoop ?? true,
            alertEmailsText: (initialData.alertEmails || []).join(', '),
            isDefault: Boolean(initialData.isDefault || initialData.routeCode === 'DEFAULT'),
            defaultTravelMinutes: initialData.defaultTravelMinutes || 15,
            defaultStayMinutes: initialData.defaultStayMinutes || 15,
            isActive: initialData.isActive,
            gateSteps:
              initialData.gateSteps && initialData.gateSteps.length > 0
                ? initialData.gateSteps.map((s, idx) => ({
                    gateId: s.gateId,
                    stepIndex: idx + 1,
                    maxTravelMinutes: s.maxTravelMinutes,
                    maxStayMinutes: getStayMinutesForStepIndex(s.maxStayMinutes, idx),
                  }))
                : isDefaultRoute
                ? []
                : [...DEFAULT_ROUTE_STEPS],
          });

          // Điền trước các xe đã gán tuyến này
          const preAssigned = new Set<string>();
          if (initialData.assignedVehicleIds && initialData.assignedVehicleIds.length > 0) {
            initialData.assignedVehicleIds.forEach((id) => preAssigned.add(id));
          }
          sharedVehicles.forEach((v) => {
            if (v.assignedRouteId && String(v.assignedRouteId) === String(initialData.id)) {
              preAssigned.add(v.id);
            }
          });

          setSelectedVehicleIds(Array.from(preAssigned));
          setApplyToAllShared(false);
        } else {
          reset({
            routeCode: '',
            routeName: '',
            description: '',
            isClosedLoop: true,
            alertEmailsText: '',
            isDefault: false,
            defaultTravelMinutes: 15,
            defaultStayMinutes: 15,
            isActive: true,
            gateSteps: [...DEFAULT_ROUTE_STEPS],
          });
          setSelectedVehicleIds([]);
          setApplyToAllShared(false);
        }
        setVehicleSearch('');
      }
    } else {
      initializedRouteIdRef.current = null;
    }
  }, [open, initialData, isDefaultRoute, reset, sharedVehicles, setVehicleSearch]);

  const areAllFilteredSelected = useMemo(() => {
    if (sharedVehicles.length === 0) return false;
    return sharedVehicles.every((v) => selectedVehicleIds.includes(v.id));
  }, [sharedVehicles, selectedVehicleIds]);

  const isSomeFilteredSelected = useMemo(() => {
    if (sharedVehicles.length === 0) return false;
    return (
      sharedVehicles.some((v) => selectedVehicleIds.includes(v.id)) &&
      !areAllFilteredSelected
    );
  }, [sharedVehicles, selectedVehicleIds, areAllFilteredSelected]);

  const handleToggleSelectAll = (forceChecked?: boolean) => {
    const shouldSelect = forceChecked !== undefined ? forceChecked : !areAllFilteredSelected;
    if (!shouldSelect) {
      const currentIds = new Set(sharedVehicles.map((v) => v.id));
      setSelectedVehicleIds((prev) => prev.filter((id) => !currentIds.has(id)));
    } else {
      const combined = new Set([...selectedVehicleIds, ...sharedVehicles.map((v) => v.id)]);
      setSelectedVehicleIds(Array.from(combined));
    }
  };

  const handleToggleVehicle = (vehicleId: string, forcedState?: boolean) => {
    setSelectedVehicleIds((prev) => {
      const isCurrentlySelected = prev.includes(vehicleId);
      const nextState = forcedState !== undefined ? forcedState : !isCurrentlySelected;
      if (nextState === isCurrentlySelected) return prev;
      return nextState ? [...prev, vehicleId] : prev.filter((id) => id !== vehicleId);
    });
  };

  const onFormSubmit = async (formData: GateRouteFormData) => {
    const formattedSteps = (formData.gateSteps || []).map((s, idx) => ({
      gateId: s.gateId,
      stepIndex: idx + 1,
      maxTravelMinutes: Number(s.maxTravelMinutes),
      maxStayMinutes: getStayMinutesForStepIndex(s.maxStayMinutes, idx),
    }));

    const emails = formData.alertEmailsText
      ? formData.alertEmailsText
          .split(/[,;\s]+/)
          .map((e) => e.trim())
          .filter((e) => e.length > 0)
      : [];

    const vehiclePayload = {
      applyToAllSharedVehicles: applyToAllShared,
      assignedVehicleIds: applyToAllShared ? [] : selectedVehicleIds,
    };

    if (isEditing) {
      const payload: UpdateGateRouteRequest = {
        routeCode: formData.routeCode.trim().toUpperCase(),
        routeName: formData.routeName.trim(),
        description: formData.description || undefined,
        isClosedLoop: formData.isClosedLoop,
        alertEmails: emails,
        isDefault: formData.isDefault,
        defaultTravelMinutes: formData.defaultTravelMinutes,
        defaultStayMinutes: formData.defaultStayMinutes,
        isActive: formData.isActive,
        gateSteps: formattedSteps,
        ...vehiclePayload,
      };
      await onSubmit(payload);
    } else {
      const payload: CreateGateRouteRequest = {
        routeCode: formData.routeCode.trim().toUpperCase(),
        routeName: formData.routeName.trim(),
        description: formData.description || undefined,
        isClosedLoop: formData.isClosedLoop,
        alertEmails: emails,
        isDefault: formData.isDefault,
        defaultTravelMinutes: formData.defaultTravelMinutes,
        defaultStayMinutes: formData.defaultStayMinutes,
        isActive: formData.isActive,
        gateSteps: formattedSteps,
        ...vehiclePayload,
      };
      await onSubmit(payload);
    }
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
              {isEditing ? 'Cập Nhật Tuyến Điều Vận' : 'Tạo Tuyến Điều Vận Cố Định'}
            </DialogTitle>
          </div>
          <DialogDescription>
            Thiết lập danh sách các cổng/nhà máy theo thứ tự di chuyển kèm thời gian SLA tối đa (phút).
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onFormSubmit)} className="space-y-4 py-2">
          {/* Mã và tên tuyến */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">
                Mã tuyến <span className="text-destructive">*</span>
              </label>
              <Input
                {...register('routeCode')}
                placeholder="VD: ROUTE-01"
                className="uppercase font-mono text-xs"
                autoFocus={!isEditing}
                disabled={isDefaultRoute}
              />
              {isDefaultRoute && (
                <p className="text-[11px] text-amber-600 dark:text-amber-400">
                  Mã tuyến mặc định được bảo vệ, không thể thay đổi.
                </p>
              )}
              {errors.routeCode && (
                <p className="text-[11px] text-destructive">{errors.routeCode.message}</p>
              )}
            </div>

            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">
                Tên tuyến đường <span className="text-destructive">*</span>
              </label>
              <Input
                {...register('routeName')}
                placeholder="VD: Tuyến Nhà Máy A -> B -> C"
                className="text-xs"
              />
              {errors.routeName && (
                <p className="text-[11px] text-destructive">{errors.routeName.message}</p>
              )}
            </div>
          </div>

          {/* Cấu hình SLA Mặc Định (Tuyến tự do) */}
          {isDefaultRoute && (
            <div className="rounded-lg border border-amber-300 dark:border-amber-800 bg-amber-50/40 dark:bg-amber-950/20 p-3 space-y-3">
              <div className="flex items-center gap-2">
                <Clock className="h-4 w-4 text-amber-600 dark:text-amber-400" />
                <span className="text-xs font-bold text-foreground">
                  Cấu hình SLA Tuyến Tự Do Mặc Định
                </span>
              </div>
              <p className="text-[11px] text-muted-foreground">
                Áp dụng cho mọi phương tiện nội bộ di chuyển tự do giữa các nhà máy. Cập nhật mốc SLA bên dưới sẽ tự động áp dụng khi xe quẹt thẻ qua trạm.
              </p>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div className="space-y-1">
                  <label className="text-xs font-semibold text-foreground">
                    Thời gian di chuyển tối đa giữa các cổng (phút) <span className="text-destructive">*</span>
                  </label>
                  <Input
                    type="number"
                    min={1}
                    {...register('defaultTravelMinutes', { valueAsNumber: true })}
                    className="text-xs font-mono"
                  />
                  {errors.defaultTravelMinutes && (
                    <p className="text-[11px] text-destructive">{errors.defaultTravelMinutes.message}</p>
                  )}
                </div>
                <div className="space-y-1">
                  <label className="text-xs font-semibold text-foreground">
                    Thời gian làm việc tối đa tại mỗi điểm (phút) <span className="text-destructive">*</span>
                  </label>
                  <Input
                    type="number"
                    min={1}
                    {...register('defaultStayMinutes', { valueAsNumber: true })}
                    className="text-xs font-mono"
                  />
                  {errors.defaultStayMinutes && (
                    <p className="text-[11px] text-destructive">{errors.defaultStayMinutes.message}</p>
                  )}
                </div>
              </div>
            </div>
          )}

          {/* Mô tả */}
          <div className="space-y-1">
            <label className="text-xs font-semibold text-foreground">Mô tả hành trình</label>
            <Input
              {...register('description')}
              placeholder="VD: Giao nhận linh kiện định kỳ hàng ngày"
              className="text-xs"
            />
          </div>

          {/* Email nhận cảnh báo vi phạm */}
          <div className="space-y-1">
            <label className="text-xs font-semibold text-foreground">
              Email nhận cảnh báo SLA (cách nhau bởi dấu phẩy)
            </label>
            <Input
              {...register('alertEmailsText')}
              placeholder="VD: dispatch@factory.com, manager@factory.com"
              className="text-xs"
            />
          </div>

          {/* Danh sách các chặng cổng (gateSteps) - Chỉ áp dụng cho tuyến cố định */}
          {!isDefaultRoute ? (
            <div className="space-y-2 pt-2 border-t border-border">
              <div className="flex items-center justify-between">
                <div>
                  <span className="text-xs font-semibold text-foreground block">
                    Danh sách chặng cổng ({fields.length}) <span className="text-destructive">*</span>
                  </span>
                  <span className="text-[11px] text-muted-foreground block">
                    Xe phải đến các cổng theo đúng thứ tự chặng đã định cấu hình
                  </span>
                </div>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  className="h-7 text-xs gap-1 cursor-pointer"
                  onClick={() =>
                    append({
                      gateId: '',
                      stepIndex: fields.length + 1,
                      maxTravelMinutes: 15,
                      maxStayMinutes: 30,
                    })
                  }
                >
                  <Plus className="h-3.5 w-3.5" />
                  Thêm chặng
                </Button>
              </div>

              {gateStepsErrorMessage && (
                <p className="text-[11px] text-destructive">
                  {gateStepsErrorMessage}
                </p>
              )}

              <div className="space-y-2.5">
                {fields.map((field, idx) => {
                  const isOriginStep = idx === 0;
                  return (
                    <div
                      key={field.id}
                      className={`p-3 rounded-lg border relative space-y-2 ${
                        isOriginStep
                          ? 'border-blue-300 dark:border-blue-900 bg-blue-50/30 dark:bg-blue-950/20'
                          : 'border-border bg-muted/20'
                      }`}
                    >
                      <div className="flex items-center justify-between">
                        {isOriginStep ? (
                          <div className="flex items-center gap-2">
                            <span className="text-xs font-bold text-blue-700 dark:text-blue-300 flex items-center gap-1.5">
                              <span className="w-5 h-5 rounded-full bg-blue-600 text-white flex items-center justify-center text-[10px] font-bold">
                                1
                              </span>
                              📍 Chặng 1: Điểm Xuất Phát & Quay Về
                            </span>
                            <Badge variant="outline" className="text-[10px] text-blue-600 border-blue-300 bg-blue-50 dark:bg-blue-950/50">
                              Cố định
                            </Badge>
                          </div>
                        ) : (
                          <span className="text-xs font-bold text-emerald-600 dark:text-emerald-400 flex items-center gap-1.5">
                            <span className="w-5 h-5 rounded-full bg-emerald-100 dark:bg-emerald-900/60 text-emerald-700 dark:text-emerald-300 flex items-center justify-center text-[10px] font-bold">
                              {idx + 1}
                            </span>
                            🏁 Chặng {idx + 1}: Điểm Đến {idx}
                          </span>
                        )}

                        <button
                          type="button"
                          onClick={() => remove(idx)}
                          disabled={isOriginStep}
                          className={
                            isOriginStep
                              ? 'text-muted-foreground/30 cursor-not-allowed p-1'
                              : 'text-muted-foreground hover:text-destructive transition-colors p-1 cursor-pointer'
                          }
                          title={isOriginStep ? 'Không thể xóa chặng xuất phát & quay về' : 'Xóa chặng này'}
                        >
                          <Trash2 className="h-3.5 w-3.5" />
                        </button>
                      </div>

                      {isOriginStep ? (
                        /* Chặng 1: Cổng xuất phát & quay về + Thời gian quay về (ẩn maxStayMinutes) */
                        <div className="grid grid-cols-1 sm:grid-cols-2 gap-2.5 items-start">
                          <GateSelectField
                            label="Cổng xuất phát & quay về"
                            placeholder="-- Chọn Cổng xuất phát & quay về --"
                            isOrigin
                            value={watch(`gateSteps.${idx}.gateId`)}
                            onChange={(val) =>
                              setValue(`gateSteps.${idx}.gateId`, val, { shouldValidate: true })
                            }
                            error={errors.gateSteps?.[idx]?.gateId?.message}
                            gates={gates}
                          />

                          <div className="flex flex-col gap-1">
                            <label className="text-[11px] font-medium text-foreground flex items-center gap-1 h-5">
                              <Clock className="h-3 w-3 text-blue-600 shrink-0" />
                              <span>⏱️ Thời gian quay về (phút)</span> <span className="text-destructive">*</span>
                            </label>
                            <Input
                              type="number"
                              min={1}
                              {...register(`gateSteps.${idx}.maxTravelMinutes`, { valueAsNumber: true })}
                              className="text-xs h-8 font-mono"
                            />
                            <p className="text-[10px] text-muted-foreground">
                              Thời gian xe từ chặng cuối quay về lại cổng này
                            </p>
                            {errors.gateSteps?.[idx]?.maxTravelMinutes && (
                              <p className="text-[10px] text-destructive">
                                {errors.gateSteps[idx]?.maxTravelMinutes?.message}
                              </p>
                            )}
                          </div>
                        </div>
                      ) : (
                        /* Chặng 2 trở đi: Điểm đến, Tối đa di chuyển, Tối đa lưu lại */
                        <div className="grid grid-cols-1 sm:grid-cols-3 gap-2 items-start">
                          <GateSelectField
                            label="Cổng đến"
                            placeholder="-- Chọn cổng đến --"
                            value={watch(`gateSteps.${idx}.gateId`)}
                            onChange={(val) =>
                              setValue(`gateSteps.${idx}.gateId`, val, { shouldValidate: true })
                            }
                            error={errors.gateSteps?.[idx]?.gateId?.message}
                            gates={gates}
                          />

                          <div className="flex flex-col gap-1">
                            <label className="text-[11px] font-medium text-foreground flex items-center gap-1 h-5">
                              <Clock className="h-3 w-3 text-muted-foreground shrink-0" />
                              <span>⏱️ Tối đa di chuyển đến cổng này (phút)</span>
                            </label>
                            <Input
                              type="number"
                              min={1}
                              {...register(`gateSteps.${idx}.maxTravelMinutes`, { valueAsNumber: true })}
                              className="text-xs h-8"
                            />
                            {errors.gateSteps?.[idx]?.maxTravelMinutes && (
                              <p className="text-[10px] text-destructive">
                                {errors.gateSteps[idx]?.maxTravelMinutes?.message}
                              </p>
                            )}
                          </div>

                          <div className="flex flex-col gap-1">
                            <label className="text-[11px] font-medium text-foreground flex items-center gap-1 h-5">
                              <Clock className="h-3 w-3 text-muted-foreground shrink-0" />
                              <span>⏱️ Tối đa lưu lại (phút)</span>
                            </label>
                            <Input
                              type="number"
                              min={0}
                              {...register(`gateSteps.${idx}.maxStayMinutes`, { valueAsNumber: true })}
                              className="text-xs h-8"
                            />
                            {errors.gateSteps?.[idx]?.maxStayMinutes && (
                              <p className="text-[10px] text-destructive">
                                {errors.gateSteps[idx]?.maxStayMinutes?.message}
                              </p>
                            )}
                          </div>
                        </div>
                      )}

                      {idx < fields.length - 1 ? (
                        <div className="flex justify-center pt-1 text-muted-foreground/50">
                          <ArrowDown className="h-3.5 w-3.5" />
                        </div>
                      ) : (
                        fields.length >= 2 && (
                          <div className="flex items-center justify-center gap-1.5 pt-1 text-[11px] text-blue-600 dark:text-blue-400 font-medium">
                            <span>↩ Quay về Cổng xuất phát & quay về (Chặng 1)</span>
                          </div>
                        )
                      )}
                    </div>
                  );
                })}
              </div>
            </div>
          ) : (
            <div className="p-3.5 rounded-lg border border-dashed border-amber-300 dark:border-amber-900 bg-amber-50/20 text-xs text-muted-foreground flex items-center gap-2">
              <Route className="h-4 w-4 text-amber-500 shrink-0" />
              <span>
                Tuyến tự do mặc định không ràng buộc các chặng cổng cố định. Phương tiện được phép di chuyển qua lại tự do giữa mọi cổng kiểm soát trong hệ thống theo thời gian SLA cấu hình ở trên.
              </span>
            </div>
          )}

          {/* Vòng lặp khép kín - chỉ cho tuyến cố định */}
          {!isDefaultRoute && (
            <div className="pt-2 border-t border-border flex items-center justify-between">
              <div>
                <span className="text-xs font-semibold text-foreground block">
                  Hành trình khép kín (Closed loop)
                </span>
                <span className="text-[11px] text-muted-foreground block">
                  Xe phải quay trở lại nhà máy xuất phát ban đầu để kết thúc chuyến
                </span>
              </div>
              <button
                type="button"
                onClick={() => setValue('isClosedLoop', !isClosedLoop, { shouldDirty: true })}
                className={`relative inline-flex h-5 w-9 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none ${
                  isClosedLoop ? 'bg-blue-600' : 'bg-muted'
                }`}
              >
                <span
                  className={`pointer-events-none inline-block h-4 w-4 transform rounded-full bg-white shadow-lg ring-0 transition duration-200 ease-in-out ${
                    isClosedLoop ? 'translate-x-4' : 'translate-x-0'
                  }`}
                />
              </button>
            </div>
          )}

          {/* Gán phương tiện dùng chung */}
          <div className="pt-3 border-t border-border space-y-3">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <div className="p-1.5 rounded-md bg-blue-50 dark:bg-blue-950/60 text-blue-600 dark:text-blue-400">
                  <Truck className="h-4 w-4" />
                </div>
                <div>
                  <span className="text-xs font-semibold text-foreground block">
                    Gán phương tiện dùng chung áp dụng tuyến này
                  </span>
                  <span className="text-[11px] text-muted-foreground block">
                    Các phương tiện được gán sẽ tự động tuân theo lộ trình kiểm soát của tuyến này
                  </span>
                </div>
              </div>
              {applyToAllShared ? (
                <Badge variant="default" className="bg-blue-600 text-white text-[10px] h-5">
                  Tất cả ({sharedVehicles.length} xe)
                </Badge>
              ) : (
                <Badge variant="secondary" className="text-[10px] h-5">
                  Đã chọn: {selectedVehicleIds.length}/{sharedVehicles.length} xe
                </Badge>
              )}
            </div>

            {/* Thẻ chuyển đổi: Áp dụng cho tất cả */}
            <div className="flex items-center justify-between p-2.5 rounded-lg border border-border bg-muted/20">
              <div className="space-y-0.5">
                <span className="text-xs font-medium text-foreground block">
                  Áp dụng cho tất cả xe dùng chung
                </span>
                <span className="text-[11px] text-muted-foreground block">
                  Tự động gán toàn bộ phương tiện nội bộ / dùng chung ({sharedVehicles.length} xe) vào tuyến này
                </span>
              </div>
              <button
                type="button"
                onClick={() => setApplyToAllShared(!applyToAllShared)}
                className={`relative inline-flex h-5 w-9 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none ${
                  applyToAllShared ? 'bg-blue-600' : 'bg-muted'
                }`}
              >
                <span
                  className={`pointer-events-none inline-block h-4 w-4 transform rounded-full bg-white shadow-lg ring-0 transition duration-200 ease-in-out ${
                    applyToAllShared ? 'translate-x-4' : 'translate-x-0'
                  }`}
                />
              </button>
            </div>

            {/* Danh sách chọn 1 hoặc nhiều xe cụ thể khi không chọn 'Áp dụng cho tất cả' */}
            {!applyToAllShared && (
              <div className="space-y-2 rounded-lg border border-border bg-background p-3">
                {/* Thanh công cụ tìm kiếm và chọn tất cả */}
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 pb-2 border-b border-border/70">
                  <div
                    className="flex items-center gap-2 text-xs font-medium text-foreground cursor-pointer select-none py-1 group"
                    onClick={() => handleToggleSelectAll()}
                  >
                    <div className="pointer-events-none flex items-center">
                      <Checkbox
                        checked={areAllFilteredSelected}
                        indeterminate={isSomeFilteredSelected}
                        tabIndex={-1}
                      />
                    </div>
                    <span className="group-hover:text-blue-600 transition-colors">
                      Chọn tất cả ({sharedVehicles.length} xe đang hiển thị)
                    </span>
                  </div>

                  <div className="relative flex-1 sm:max-w-[220px]">
                    <Search className="h-3.5 w-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-muted-foreground pointer-events-none" />
                    <Input
                      type="text"
                      placeholder="Tìm biển số, mã thẻ..."
                      value={vehicleSearch}
                      onChange={(e) => setVehicleSearch(e.target.value)}
                      className="text-xs h-7 pl-8 pr-7"
                    />
                    {vehicleSearch && (
                      <button
                        type="button"
                        onClick={() => setVehicleSearch('')}
                        className="absolute right-2 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground text-xs"
                      >
                        ✕
                      </button>
                    )}
                  </div>
                </div>

                {/* Danh sách xe */}
                <div className="max-h-56 overflow-y-auto space-y-1.5 pr-1 divide-y divide-border/40">
                  {isLoadingVehicles ? (
                    <div className="py-6 text-center text-xs text-muted-foreground flex flex-col items-center justify-center gap-1.5">
                      <Loader2 className="h-4 w-4 animate-spin text-primary" />
                      <span>Đang tải danh sách phương tiện...</span>
                    </div>
                  ) : sharedVehicles.length === 0 ? (
                    <div className="py-6 text-center text-xs text-muted-foreground">
                      {vehicleSearch
                        ? `Không tìm thấy phương tiện nào phù hợp với từ khóa "${vehicleSearch}".`
                        : 'Chưa có phương tiện dùng chung nào được thiết lập trong hệ thống.'}
                    </div>
                  ) : (
                    <>
                      {sharedVehicles.map((vehicle) => {
                        const isSelected = selectedVehicleIds.includes(vehicle.id);
                        const isAssignedOtherRoute =
                          vehicle.assignedRouteId &&
                          vehicle.assignedRouteId !== initialData?.id;
                        const isCurrentRoute =
                          Boolean(initialData && vehicle.assignedRouteId === initialData.id);

                        return (
                          <div
                            key={vehicle.id}
                            role="checkbox"
                            aria-checked={isSelected}
                            tabIndex={0}
                            onKeyDown={(e) => {
                              if (e.key === ' ' || e.key === 'Enter') {
                                e.preventDefault();
                                handleToggleVehicle(vehicle.id);
                              }
                            }}
                            onClick={() => handleToggleVehicle(vehicle.id)}
                            className={`flex items-center justify-between p-2 rounded-md cursor-pointer transition-colors pt-2 select-none ${
                              isSelected
                                ? 'bg-blue-50/70 dark:bg-blue-950/40 border border-blue-200 dark:border-blue-900/60'
                                : 'hover:bg-muted/40 border border-transparent'
                            }`}
                          >
                            <div className="flex items-center gap-2.5 min-w-0 pointer-events-none">
                              <Checkbox
                                checked={isSelected}
                                tabIndex={-1}
                              />
                              <div className="flex items-center gap-1.5">
                                {vehicle.type === VehicleType.Motorbike ? (
                                  <span className="p-1 rounded bg-amber-100 dark:bg-amber-950 text-amber-600 dark:text-amber-400">
                                    <Bike className="h-3.5 w-3.5" />
                                  </span>
                                ) : (
                                  <span className="p-1 rounded bg-blue-100 dark:bg-blue-950 text-blue-600 dark:text-blue-400">
                                    <Car className="h-3.5 w-3.5" />
                                  </span>
                                )}
                                <span className="font-mono text-xs font-bold text-foreground">
                                  {vehicle.plateNumber}
                                </span>
                              </div>

                              {vehicle.cardCode && (
                                <span className="text-[10px] text-muted-foreground bg-muted px-1.5 py-0.5 rounded font-mono">
                                  Thẻ: {vehicle.cardCode}
                                </span>
                              )}
                            </div>

                            <div className="flex items-center gap-1.5 shrink-0 pointer-events-none">
                              {isCurrentRoute && (
                                <Badge variant="outline" className="text-[9px] text-emerald-600 border-emerald-300 dark:border-emerald-800 bg-emerald-50 dark:bg-emerald-950/50">
                                  Tuyến hiện tại
                                </Badge>
                              )}
                              {isAssignedOtherRoute && (
                                <Badge variant="outline" className="text-[9px] text-amber-600 border-amber-300 dark:border-amber-800 bg-amber-50 dark:bg-amber-950/50">
                                  Gán tuyến khác
                                </Badge>
                              )}
                            </div>
                          </div>
                        );
                      })}

                      {/* Phần tử Sentinel kích hoạt tải thêm khi cuộn */}
                      {hasMoreVehicles && (
                        <div
                          ref={vehicleSentinelRef}
                          className="py-2.5 flex items-center justify-center text-xs text-muted-foreground border-t border-border/40"
                        >
                          {isFetchingMoreVehicles ? (
                            <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
                              <Loader2 className="h-3.5 w-3.5 animate-spin text-primary" />
                              <span>Đang tải thêm phương tiện...</span>
                            </div>
                          ) : (
                            <span className="text-[11px] text-muted-foreground/70">
                              Cuộn xuống để tải thêm ({sharedVehicles.length}/{totalSharedVehiclesCount})
                            </span>
                          )}
                        </div>
                      )}
                    </>
                  )}
                </div>
              </div>
            )}
          </div>

          {/* Trạng thái hoạt động */}
          <div className="pt-2 border-t border-border flex items-center justify-between">
            <div>
              <span className="text-xs font-semibold text-foreground block">
                Trạng thái hoạt động
              </span>
              <span className="text-[11px] text-muted-foreground block">
                Cho phép áp dụng tuyến đường này cho việc điều vận xe
              </span>
            </div>
            <button
              type="button"
              onClick={() => setValue('isActive', !isActive, { shouldDirty: true })}
              className={`relative inline-flex h-5 w-9 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none ${
                isActive ? 'bg-blue-600' : 'bg-muted'
              }`}
            >
              <span
                className={`pointer-events-none inline-block h-4 w-4 transform rounded-full bg-white shadow-lg ring-0 transition duration-200 ease-in-out ${
                  isActive ? 'translate-x-4' : 'translate-x-0'
                }`}
              />
            </button>
          </div>

          <DialogFooter className="pt-3 gap-2 sm:gap-0">
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => onOpenChange(false)}
              disabled={isSubmitting}
              className="text-xs h-9 cursor-pointer"
            >
              Hủy bỏ
            </Button>
            <Button
              type="submit"
              size="sm"
              disabled={isSubmitting}
              className="text-xs h-9 bg-blue-600 hover:bg-blue-700 text-white cursor-pointer"
            >
              <Save className="h-3.5 w-3.5 mr-1.5" />
              <span>{isSubmitting ? 'Đang lưu...' : isEditing ? 'Cập nhật' : 'Tạo tuyến'}</span>
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
