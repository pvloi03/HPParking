import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
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
import { Car, Save, Truck, Route } from 'lucide-react';
import { ClientSelect } from './ClientSelect';
import {
  VehicleType,
  normalizePlateNumber,
  type VehicleDto,
  type CreateVehicleRequest,
  type UpdateVehicleRequest,
} from '@/types/vehicle';
import type { ClientDto } from '@/types/client';
import { gateRouteApi } from '@/api/gateRouteApi';

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

  const { data: gateRoutes = [] } = useQuery({
    queryKey: ['gateRoutes'],
    queryFn: () => gateRouteApi.getAll(),
    enabled: open,
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
      note: '',
      isActive: true,
    },
  });

  const selectedType = watch('type');
  const selectedClientId = watch('clientId');
  const isShared = watch('isShared');
  const assignedRouteId = watch('assignedRouteId');
  const isActive = watch('isActive');
  const watchedPlate = watch('plateNumber');

  useEffect(() => {
    if (open) {
      if (initialData) {
        reset({
          plateNumber: initialData.plateNumber,
          type: initialData.type,
          clientId: initialData.ownerClientId || '',
          isShared: initialData.isShared ?? false,
          assignedRouteId: initialData.assignedRouteId || '',
          note: initialData.note || '',
          isActive: initialData.isActive,
        });
      } else {
        reset({
          plateNumber: '',
          type: VehicleType.Car,
          clientId: clients.length > 0 ? clients[0].id : '',
          isShared: false,
          assignedRouteId: '',
          note: '',
          isActive: true,
        });
      }
    }
  }, [open, initialData, clients, reset]);

  const onFormSubmit = async (formData: VehicleFormData) => {
    const cleanedPlate = normalizePlateNumber(formData.plateNumber);

    if (isEditing) {
      const payload: UpdateVehicleRequest = {
        plateNumber: cleanedPlate,
        type: formData.type as VehicleType,
        isShared: formData.isShared,
        assignedRouteId: formData.assignedRouteId ? formData.assignedRouteId : undefined,
        isActive: formData.isActive,
        note: formData.note || undefined,
      };
      await onSubmit(payload);
    } else {
      const payload: CreateVehicleRequest = {
        clientId: formData.clientId || undefined,
        plateNumber: cleanedPlate,
        type: formData.type as VehicleType,
        isShared: formData.isShared,
        assignedRouteId: formData.assignedRouteId ? formData.assignedRouteId : undefined,
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
              : 'Gán phương tiện cho nhân sự hoặc thiết lập xe công vụ liên nhà máy.'}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onFormSubmit)} className="space-y-3.5 py-1">
          {/* Cấu hình Xe Công Vụ / Xe Dùng Chung */}
          <div className="rounded-lg border border-border p-3 bg-muted/20 space-y-2.5">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <Truck className="h-4 w-4 text-amber-500" />
                <div>
                  <span className="text-xs font-semibold text-foreground block">
                    Xe công vụ / Xe dùng chung liên nhà máy
                  </span>
                  <span className="text-[11px] text-muted-foreground block">
                    Xe được cấp phát di chuyển qua lại giữa các nhà máy/cổng theo SLA
                  </span>
                </div>
              </div>
              <button
                type="button"
                onClick={() => {
                  const nextVal = !isShared;
                  setValue('isShared', nextVal, { shouldDirty: true, shouldValidate: true });
                  if (nextVal && !watch('clientId')) {
                    // Nếu là xe công vụ, có thể không cần clientId
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
                    Tuyến đường gán trước
                  </label>
                  <Select
                    value={assignedRouteId || '__none__'}
                    onValueChange={(val) =>
                      setValue('assignedRouteId', val === '__none__' ? '' : val, {
                        shouldDirty: true,
                      })
                    }
                  >
                    <SelectTrigger className="text-xs h-9">
                      <SelectValue placeholder="-- Chọn tuyến đường hoặc mặc định tự do --" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="__none__" className="text-xs">
                        Tuyến tự do mặc định (Free-roam: 15p di chuyển, 15p tại điểm)
                      </SelectItem>
                      {gateRoutes.map((route) => (
                        <SelectItem key={route.id} value={route.id} className="text-xs">
                          {route.routeCode} - {route.routeName} ({route.gateSteps?.length || 0} chặng)
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <p className="text-[11px] text-muted-foreground">
                    Nếu không gán tuyến cố định, xe sẽ chạy chế độ tự do: tối đa 15 phút di chuyển
                    và 15 phút làm việc tại mỗi nhà máy.
                  </p>
                </div>
              </div>
            )}
          </div>

          {/* Chọn Chủ sở hữu */}
          <div className="space-y-1">
            <label className="text-xs font-semibold text-foreground">
              Chủ sở hữu / Người phụ trách xe {!isShared && <span className="text-destructive">*</span>}
            </label>
            <ClientSelect
              value={selectedClientId}
              onValueChange={(val) => setValue('clientId', val, { shouldValidate: true })}
              clients={clients}
              placeholder={isShared ? '-- Không bắt buộc đối với xe công vụ --' : '-- Chọn chủ sở hữu xe --'}
              disabled={isEditing && !isShared}
            />
            {isEditing && !isShared && (
              <p className="text-[11px] text-muted-foreground">
                Để chuyển quyền sở hữu xe, vui lòng liên hệ bộ phận hỗ trợ kỹ thuật.
              </p>
            )}
            {errors.clientId && (
              <p className="text-[11px] text-destructive">{errors.clientId.message}</p>
            )}
          </div>

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
