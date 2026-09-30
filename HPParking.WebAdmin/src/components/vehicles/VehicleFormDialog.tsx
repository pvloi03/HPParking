import { useEffect } from 'react';
import { useQueryClient, useQuery } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
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
import { Car, Save, Truck, Route, CreditCard } from 'lucide-react';
import { ClientSelect } from './ClientSelect';
import { InfiniteSearchableSelect } from '@/components/ui/infinite-searchable-select';
import {
  VehicleType,
  normalizePlateNumber,
  type VehicleDto,
  type CreateVehicleRequest,
  type UpdateVehicleRequest,
} from '@/types/vehicle';
import type { ClientDto } from '@/types/client';
import type { GateRouteDto } from '@/types/gateRoute';
import { gateRouteApi } from '@/api/gateRouteApi';
import { cardApi } from '@/api/cardApi';
import { CardTargetType, type CardDto } from '@/types/card';

const vehicleSchema = z
  .object({
    plateNumber: z
      .string()
      .trim()
      .min(4, 'Biển số xe phải có ít nhất 4 ký tự')
      .max(20, 'Biển số xe không được quá 20 ký tự'),
    type: z.number().int().min(1).max(4),
    clientId: z.string().trim().optional(),
    isShared: z.boolean(),
    assignedRouteId: z.string().optional(),
    cardCode: z.string().trim().optional(),
    note: z.string().trim().optional(),
    isActive: z.boolean(),
  })
  .refine(
    (data) => {
      if (!data.isShared && (!data.clientId || data.clientId.trim() === '')) {
        return false;
      }
      return true;
    },
    {
      message: 'Vui lòng chọn nhân sự chủ sở hữu phương tiện cá nhân',
      path: ['clientId'],
    }
  )
  .refine(
    (data) => {
      if (data.isShared && (!data.assignedRouteId || data.assignedRouteId.trim() === '')) {
        return false;
      }
      return true;
    },
    {
      message: 'Vui lòng chọn tuyến đường điều vận cho phương tiện nội bộ',
      path: ['assignedRouteId'],
    }
  );

type VehicleFormData = z.infer<typeof vehicleSchema>;

interface VehicleFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  initialData?: VehicleDto | null;
  clients: ClientDto[];
  onSubmit: (data: CreateVehicleRequest | UpdateVehicleRequest) => Promise<void>;
  isSubmitting?: boolean;
}

export function VehicleFormDialog({
  open,
  onOpenChange,
  initialData,
  clients,
  onSubmit,
  isSubmitting = false,
}: VehicleFormDialogProps) {
  const isEditing = Boolean(initialData);
  const queryClient = useQueryClient();

  // Truy vấn tuyến đường mặc định của hệ thống (isDefault hoặc RouteCode DEFAULT)
  const { data: defaultRoute } = useQuery({
    queryKey: ['gateRoutes', 'default'],
    queryFn: async () => {
      const res = await gateRouteApi.getRoutes({ pageSize: 50, isActive: true });
      return (
        res.items.find((r) => r.isDefault || r.routeCode?.toUpperCase() === 'DEFAULT') ||
        res.items[0] ||
        null
      );
    },
    staleTime: 5 * 60 * 1000,
  });

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    watch,
    formState: { errors },
  } = useForm<VehicleFormData>({
    resolver: zodResolver(vehicleSchema),
    defaultValues: {
      plateNumber: '',
      type: VehicleType.Car,
      clientId: '',
      isShared: false,
      assignedRouteId: '',
      cardCode: '',
      note: '',
      isActive: true,
    },
  });

  const selectedType = watch('type');
  const selectedClientId = watch('clientId');
  const isShared = watch('isShared');
  const assignedRouteId = watch('assignedRouteId');
  const selectedCardCode = watch('cardCode');
  const isActive = watch('isActive');
  const watchedPlate = watch('plateNumber');

  useEffect(() => {
    if (open) {
      queryClient.invalidateQueries({ queryKey: ['cards', 'available-vehicle'] });
      const defaultRouteId = defaultRoute?.id || '';

      if (initialData) {
        reset({
          plateNumber: initialData.plateNumber,
          type: initialData.type,
          clientId: initialData.isShared ? '' : (initialData.ownerClientId || ''),
          isShared: initialData.isShared ?? false,
          assignedRouteId: initialData.assignedRouteId || (initialData.isShared ? defaultRouteId : ''),
          cardCode: initialData.cardCode || '',
          note: initialData.note || '',
          isActive: initialData.isActive,
        });
      } else {
        reset({
          plateNumber: '',
          type: VehicleType.Car,
          clientId: clients.length > 0 ? clients[0].id : '',
          isShared: false,
          assignedRouteId: defaultRouteId,
          cardCode: '',
          note: '',
          isActive: true,
        });
      }
    }
  }, [open, initialData, clients, reset, defaultRoute]);

  // Luôn đảm bảo khi tuyến default tải xong mà xe nội bộ chưa có tuyến thì tự động điền sẵn
  useEffect(() => {
    if (open && isShared && !assignedRouteId && defaultRoute?.id) {
      setValue('assignedRouteId', defaultRoute.id, { shouldValidate: true, shouldDirty: true });
    }
  }, [open, isShared, assignedRouteId, defaultRoute, setValue]);

  const onFormSubmit = async (formData: VehicleFormData) => {
    const cleanedPlate = normalizePlateNumber(formData.plateNumber);

    if (isEditing) {
      const payload: UpdateVehicleRequest = {
        plateNumber: cleanedPlate,
        type: formData.type as VehicleType,
        isShared: formData.isShared,
        assignedRouteId: formData.isShared && formData.assignedRouteId ? formData.assignedRouteId : undefined,
        cardCode: formData.cardCode?.trim() || undefined,
        isActive: formData.isActive,
        note: formData.note || undefined,
      };
      await onSubmit(payload);
    } else {
      const payload: CreateVehicleRequest = {
        clientId: formData.isShared ? undefined : (formData.clientId || undefined),
        plateNumber: cleanedPlate,
        type: formData.type as VehicleType,
        isShared: formData.isShared,
        assignedRouteId: formData.isShared && formData.assignedRouteId ? formData.assignedRouteId : undefined,
        cardCode: formData.cardCode?.trim() || undefined,
        isActive: formData.isActive,
        note: formData.note || undefined,
      };
      await onSubmit(payload);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <div className="flex items-center gap-2 mb-1">
            <div className="p-2 rounded-lg bg-blue-100 dark:bg-blue-950 text-blue-600 dark:text-blue-400">
              <Car className="h-5 w-5" />
            </div>
            <DialogTitle className="text-base font-bold">
              {isEditing ? 'Cập Nhật Phương Tiện' : 'Đăng Ký Phương Tiện Mới'}
            </DialogTitle>
          </div>
          <DialogDescription>
            {isEditing
              ? 'Chỉnh sửa biển số, phân loại phương tiện, chế độ xe dùng chung và tuyến điều vận.'
              : 'Gán phương tiện cho nhân sự hoặc thiết lập phương tiện nội bộ liên nhà máy.'}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onFormSubmit)} className="space-y-3.5 py-1">
          {/* Cấu hình Phương Tiện Nội Bộ / Xe Dùng Chung */}
          <div className="rounded-lg border border-border p-3 bg-muted/20 space-y-2.5">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <Truck className="h-4 w-4 text-amber-500" />
                <div>
                  <span className="text-xs font-semibold text-foreground block">
                    Phương tiện nội bộ / Xe dùng chung liên nhà máy
                  </span>
                  <span className="text-[11px] text-muted-foreground block">
                    Phương tiện được cấp phát di chuyển qua lại giữa các nhà máy/cổng theo SLA
                  </span>
                </div>
              </div>
              <button
                type="button"
                onClick={() => {
                  const nextVal = !isShared;
                  setValue('isShared', nextVal, { shouldDirty: true, shouldValidate: true });
                  if (nextVal) {
                    setValue('clientId', '', { shouldDirty: true, shouldValidate: true });
                    if (!watch('assignedRouteId') && defaultRoute?.id) {
                      setValue('assignedRouteId', defaultRoute.id, { shouldDirty: true, shouldValidate: true });
                    }
                  } else if (!watch('clientId') && clients.length > 0) {
                    setValue('clientId', clients[0].id, { shouldDirty: true, shouldValidate: true });
                  }
                }}
                className={`relative inline-flex h-5 w-9 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none ${
                  isShared ? 'bg-amber-600' : 'bg-muted'
                }`}
              >
                <span
                  className={`pointer-events-none inline-block h-4 w-4 transform rounded-full bg-white shadow-lg ring-0 transition duration-200 ease-in-out ${
                    isShared ? 'translate-x-4' : 'translate-x-0'
                  }`}
                />
              </button>
            </div>

            {isShared && (
              <div className="pt-2 border-t border-border/60 space-y-2">
                <div className="space-y-1">
                  <label className="text-xs font-semibold text-foreground flex items-center gap-1.5">
                    <Route className="h-3.5 w-3.5 text-blue-500" />
                    Tuyến đường gán trước <span className="text-destructive">*</span>
                  </label>
                  <InfiniteSearchableSelect<GateRouteDto>
                    queryKey={['gateRoutes-infinite-select']}
                    fetchFn={(params) => gateRouteApi.getRoutes({ ...params, isActive: true })}
                    fetchById={(id) => gateRouteApi.getById(String(id))}
                    value={assignedRouteId || ''}
                    onValueChange={(val) =>
                      setValue('assignedRouteId', val, {
                        shouldDirty: true,
                        shouldValidate: true,
                      })
                    }
                    placeholder="-- Chọn tuyến điều vận từ danh sách --"
                    selectedItems={defaultRoute ? [defaultRoute] : undefined}
                    getLabel={(route) => `${route.routeCode} - ${route.routeName} (${route.gateSteps?.length || 0} chặng)`}
                  />
                  {errors.assignedRouteId && (
                    <p className="text-[11px] text-destructive">{errors.assignedRouteId.message}</p>
                  )}
                </div>
              </div>
            )}
          </div>

          {/* Chọn Chủ sở hữu chỉ hiển thị đối với xe cá nhân */}
          {!isShared && (
            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">
                Chủ sở hữu phương tiện <span className="text-destructive">*</span>
              </label>
              <ClientSelect
                value={selectedClientId}
                onValueChange={(val) => setValue('clientId', val, { shouldValidate: true })}
                clients={clients}
                placeholder="-- Chọn chủ sở hữu xe --"
                disabled={isEditing}
              />
              {isEditing && (
                <p className="text-[11px] text-muted-foreground">
                  Để chuyển quyền sở hữu xe, vui lòng liên hệ bộ phận hỗ trợ kỹ thuật.
                </p>
              )}
              {errors.clientId && (
                <p className="text-[11px] text-destructive">{errors.clientId.message}</p>
              )}
            </div>
          )}

          {/* Biển số xe & Phân loại phương tiện */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">
                Biển số xe <span className="text-destructive">*</span>
              </label>
              <Input
                {...register('plateNumber')}
                placeholder="VD: 30A-123.45"
                className="uppercase font-mono text-xs"
                autoFocus={!isEditing}
              />
              {watchedPlate && (
                <span className="text-[10px] text-muted-foreground block">
                  Chuẩn hóa: <strong>{normalizePlateNumber(watchedPlate)}</strong>
                </span>
              )}
              {errors.plateNumber && (
                <p className="text-[11px] text-destructive">{errors.plateNumber.message}</p>
              )}
            </div>

            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">
                Loại phương tiện <span className="text-destructive">*</span>
              </label>
              <Select
                value={String(selectedType)}
                onValueChange={(val) => setValue('type', Number(val), { shouldValidate: true })}
              >
                <SelectTrigger className="text-xs">
                  <SelectValue placeholder="-- Chọn loại xe --" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={String(VehicleType.Car)} className="text-xs">
                    Ô tô (Car)
                  </SelectItem>
                  <SelectItem value={String(VehicleType.Motorbike)} className="text-xs">
                    Xe máy (Motorbike)
                  </SelectItem>
                  <SelectItem value={String(VehicleType.Bicycle)} className="text-xs">
                    Xe đạp / Xe điện
                  </SelectItem>
                  <SelectItem value={String(VehicleType.Other)} className="text-xs">
                    Khác (Xe tải, ba gác...)
                  </SelectItem>
                </SelectContent>
              </Select>
              {errors.type && (
                <p className="text-[11px] text-destructive">{errors.type.message}</p>
              )}
            </div>
          </div>

          {/* Thẻ định danh phương tiện (Thẻ xe) */}
          <div className="space-y-1">
            <label className="text-xs font-semibold text-foreground flex items-center justify-between">
              <span className="flex items-center gap-1.5">
                <CreditCard className="h-3.5 w-3.5 text-indigo-500" />
                Thẻ định danh phương tiện (Thẻ xe)
              </span>
              {selectedCardCode && selectedCardCode.trim() && (
                <span className="text-[11px] font-mono font-bold text-indigo-600 dark:text-indigo-400 bg-indigo-50 dark:bg-indigo-950/60 px-2 py-0.5 rounded border border-indigo-200 dark:border-indigo-800">
                  Thẻ: {selectedCardCode.trim()}
                </span>
              )}
            </label>
            <InfiniteSearchableSelect<CardDto>
              queryKey={['cards', 'available-vehicle', initialData?.id || 'new']}
              fetchFn={(params) =>
                cardApi.getCards({
                  ...params,
                  targetType: CardTargetType.Vehicle,
                  unassignedOnly: true,
                  assignedVehicleId: initialData?.id,
                })
              }
              value={selectedCardCode || ''}
              getValue={(card) => card.cardNumber}
              getLabel={(card) =>
                `${card.cardNumber} ${card.note ? `(${card.note})` : ''} ${card.cardNumber === initialData?.cardCode ? '★ Thẻ hiện tại' : ''}`
              }
              onValueChange={(val) =>
                setValue('cardCode', val, {
                  shouldDirty: true,
                  shouldValidate: true,
                })
              }
              placeholder="-- Chọn thẻ xe trong kho (Tùy chọn) --"
              allowClear={true}
              clearLabel="-- Không gán thẻ xe (None) --"
            />
            <p className="text-[11px] text-muted-foreground">
              Chọn thẻ từ kho thẻ có loại Phương tiện (Vehicle). Thường dùng cho phương tiện nội bộ hoặc xe dùng chung qua lại các cổng.
            </p>
          </div>

          {/* Ghi chú */}
          <div className="space-y-1">
            <label className="text-xs font-semibold text-foreground">Ghi chú phương tiện</label>
            <Input
              {...register('note')}
              placeholder="VD: Xe bán tải chở hàng giữa xưởng 1 và xưởng 2"
              className="text-xs"
            />
          </div>

          {/* Trạng thái hoạt động */}
          <div className="pt-2 border-t border-border flex items-center justify-between">
            <div>
              <span className="text-xs font-semibold text-foreground block">
                Trạng thái hoạt động
              </span>
              <span className="text-[11px] text-muted-foreground block">
                Cho phép phương tiện này mở barrier và ra vào bãi xe
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
              <span>{isSubmitting ? 'Đang lưu...' : isEditing ? 'Cập nhật' : 'Thêm mới'}</span>
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
