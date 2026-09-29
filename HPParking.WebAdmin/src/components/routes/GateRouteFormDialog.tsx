import { useEffect } from 'react';
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
import { Route, Plus, Trash2, Save, Clock, ArrowDown } from 'lucide-react';
import { gatesApi } from '@/api/infrastructureApi';
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

const gateRouteSchema = z.object({
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
  isActive: z.boolean(),
  gateSteps: z
    .array(stepSchema)
    .min(1, 'Tuyến đường phải có ít nhất 1 chặng cổng'),
});

type GateRouteFormData = z.infer<typeof gateRouteSchema>;

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

  const { data: gatesData } = useQuery({
    queryKey: ['gates-all-select'],
    queryFn: () => gatesApi.getPaged({ pageIndex: 1, pageSize: 200, isActive: true }),
    enabled: open,
  });
  const gates = gatesData?.items || [];

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
      gateSteps: [
        { gateId: '', stepIndex: 1, maxTravelMinutes: 15, maxStayMinutes: 30 },
      ],
    },
  });

  const { fields, append, remove } = useFieldArray({
    control,
    name: 'gateSteps',
  });

  const isActive = watch('isActive');
  const isClosedLoop = watch('isClosedLoop');

  useEffect(() => {
    if (open) {
      if (initialData) {
        reset({
          routeCode: initialData.routeCode,
          routeName: initialData.routeName,
          description: initialData.description || '',
          isClosedLoop: initialData.isClosedLoop ?? true,
          alertEmailsText: (initialData.alertEmails || []).join(', '),
          isActive: initialData.isActive,
          gateSteps:
            initialData.gateSteps && initialData.gateSteps.length > 0
              ? initialData.gateSteps.map((s, idx) => ({
                  gateId: s.gateId,
                  stepIndex: idx + 1,
                  maxTravelMinutes: s.maxTravelMinutes,
                  maxStayMinutes: s.maxStayMinutes,
                }))
              : [{ gateId: '', stepIndex: 1, maxTravelMinutes: 15, maxStayMinutes: 30 }],
        });
      } else {
        reset({
          routeCode: '',
          routeName: '',
          description: '',
          isClosedLoop: true,
          alertEmailsText: '',
          isActive: true,
          gateSteps: [
            { gateId: '', stepIndex: 1, maxTravelMinutes: 15, maxStayMinutes: 30 },
          ],
        });
      }
    }
  }, [open, initialData, reset]);

  const onFormSubmit = async (formData: GateRouteFormData) => {
    const formattedSteps = formData.gateSteps.map((s, idx) => ({
      gateId: s.gateId,
      stepIndex: idx + 1,
      maxTravelMinutes: Number(s.maxTravelMinutes),
      maxStayMinutes: Number(s.maxStayMinutes),
    }));

    const emails = formData.alertEmailsText
      ? formData.alertEmailsText
          .split(/[,;\s]+/)
          .map((e) => e.trim())
          .filter((e) => e.length > 0)
      : [];

    if (isEditing) {
      const payload: UpdateGateRouteRequest = {
        routeCode: formData.routeCode.trim().toUpperCase(),
        routeName: formData.routeName.trim(),
        description: formData.description || undefined,
        isClosedLoop: formData.isClosedLoop,
        alertEmails: emails,
        isActive: formData.isActive,
        gateSteps: formattedSteps,
      };
      await onSubmit(payload);
    } else {
      const payload: CreateGateRouteRequest = {
        routeCode: formData.routeCode.trim().toUpperCase(),
        routeName: formData.routeName.trim(),
        description: formData.description || undefined,
        isClosedLoop: formData.isClosedLoop,
        alertEmails: emails,
        isActive: formData.isActive,
        gateSteps: formattedSteps,
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
              />
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

          {/* Danh sách các chặng cổng (gateSteps) */}
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

            {errors.gateSteps && (
              <p className="text-[11px] text-destructive">{errors.gateSteps.message}</p>
            )}

            <div className="space-y-2.5">
              {fields.map((field, idx) => (
                <div
                  key={field.id}
                  className="p-3 rounded-lg border border-border bg-muted/20 relative space-y-2"
                >
                  <div className="flex items-center justify-between">
                    <span className="text-xs font-bold text-blue-600 dark:text-blue-400 flex items-center gap-1.5">
                      <span className="w-5 h-5 rounded-full bg-blue-100 dark:bg-blue-900/60 text-blue-700 dark:text-blue-300 flex items-center justify-center text-[10px]">
                        {idx + 1}
                      </span>
                      Chặng {idx + 1}
                    </span>

                    {fields.length > 1 && (
                      <button
                        type="button"
                        onClick={() => remove(idx)}
                        className="text-muted-foreground hover:text-destructive transition-colors p-1"
                        title="Xóa chặng này"
                      >
                        <Trash2 className="h-3.5 w-3.5" />
                      </button>
                    )}
                  </div>

                  <div className="grid grid-cols-1 sm:grid-cols-3 gap-2">
                    {/* Chọn cổng */}
                    <div className="space-y-1">
                      <label className="text-[11px] font-medium text-foreground">
                        Cổng đến <span className="text-destructive">*</span>
                      </label>
                      <Select
                        value={watch(`gateSteps.${idx}.gateId`)}
                        onValueChange={(val) =>
                          setValue(`gateSteps.${idx}.gateId`, val, { shouldValidate: true })
                        }
                      >
                        <SelectTrigger className="text-xs h-8">
                          <SelectValue placeholder="-- Chọn cổng --" />
                        </SelectTrigger>
                        <SelectContent>
                          {gates.map((g) => (
                            <SelectItem key={g.id} value={g.id} className="text-xs">
                              {g.name} ({g.code})
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                      {errors.gateSteps?.[idx]?.gateId && (
                        <p className="text-[10px] text-destructive">
                          {errors.gateSteps[idx]?.gateId?.message}
                        </p>
                      )}
                    </div>

                    {/* Max travel minutes */}
                    <div className="space-y-1">
                      <label className="text-[11px] font-medium text-foreground flex items-center gap-1">
                        <Clock className="h-3 w-3 text-muted-foreground" />
                        Tối đa di chuyển (phút)
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

                    {/* Max stay minutes */}
                    <div className="space-y-1">
                      <label className="text-[11px] font-medium text-foreground flex items-center gap-1">
                        <Clock className="h-3 w-3 text-muted-foreground" />
                        Tối đa lưu lại (phút)
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

                  {idx < fields.length - 1 && (
                    <div className="flex justify-center pt-1 text-muted-foreground/50">
                      <ArrowDown className="h-3.5 w-3.5" />
                    </div>
                  )}
                </div>
              ))}
            </div>
          </div>

          {/* Vòng lặp khép kín */}
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
