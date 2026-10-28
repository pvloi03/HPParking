import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import {
  Receipt,
  Edit,
  Clock,
  DollarSign,
  Save,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/components/ui/dialog';
import { Switch } from '@/components/ui/switch';
import { VehicleType, getVehicleTypeLabel } from '@/types/vehicle';
import {
  TariffPricingMode,
  getPricingModeLabel,
  type TariffDto,
  type CreateTariffRequest,
  type UpdateTariffRequest,
} from '@/types/tariff';

const tariffFormSchema = z
  .object({
    code: z
      .string()
      .trim()
      .min(1, 'Vui lòng nhập mã biểu phí')
      .max(50, 'Mã biểu phí tối đa 50 ký tự')
      .regex(/^[a-zA-Z0-9_\-]+$/, 'Mã biểu phí chỉ chứa ký tự chữ, số, gạch dưới hoặc gạch ngang'),
    name: z
      .string()
      .trim()
      .min(1, 'Vui lòng nhập tên biểu phí')
      .max(200, 'Tên biểu phí tối đa 200 ký tự'),
    vehicleType: z.number().int().min(1).max(4),
    pricingMode: z.number().int().min(1).max(2),
    gracePeriodMinutes: z.coerce.number().min(0, 'Số phút ân hạn không được âm'),
    flatPrice: z.coerce.number().min(0, 'Giá cố định không được âm'),
    basePrice: z.coerce.number().min(0, 'Giá block đầu không được âm'),
    baseBlockMinutes: z.coerce.number().min(0, 'Thời lượng block đầu không được âm'),
    additionalBlockPrice: z.coerce.number().min(0, 'Giá block tiếp theo không được âm'),
    additionalBlockMinutes: z.coerce.number().min(0, 'Thời lượng block tiếp theo không được âm'),
    overnightSurcharge: z.coerce.number().min(0, 'Phụ thu qua đêm không được âm'),
    maxDailyCap: z.coerce.number().min(0, 'Mức trần mỗi ngày không được âm'),
    isActive: z.boolean(),
  })
  .superRefine((data, ctx) => {
    if (data.pricingMode === TariffPricingMode.Block) {
      if (data.baseBlockMinutes <= 0) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          path: ['baseBlockMinutes'],
          message: 'Thời lượng block đầu phải lớn hơn 0 phút',
        });
      }
      if (data.additionalBlockMinutes <= 0) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          path: ['additionalBlockMinutes'],
          message: 'Thời lượng block tiếp theo phải lớn hơn 0 phút',
        });
      }
    }
  });

type TariffFormData = z.infer<typeof tariffFormSchema>;

export interface TariffFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  initialData?: TariffDto | null;
  onSubmit: (data: CreateTariffRequest | UpdateTariffRequest) => Promise<void>;
  isSubmitting?: boolean;
}

export function TariffFormDialog({
  open,
  onOpenChange,
  initialData,
  onSubmit,
  isSubmitting = false,
}: TariffFormDialogProps) {
  const isEditing = Boolean(initialData);

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    reset,
    formState: { errors },
  } = useForm<TariffFormData>({
    resolver: zodResolver(tariffFormSchema),
    defaultValues: {
      code: '',
      name: '',
      vehicleType: VehicleType.Car,
      pricingMode: TariffPricingMode.Flat,
      gracePeriodMinutes: 15,
      flatPrice: 10000,
      basePrice: 10000,
      baseBlockMinutes: 120,
      additionalBlockPrice: 5000,
      additionalBlockMinutes: 60,
      overnightSurcharge: 20000,
      maxDailyCap: 100000,
      isActive: true,
    },
  });

  const watchedPricingMode = watch('pricingMode');
  const watchedIsActive = watch('isActive');
  const watchedVehicleType = watch('vehicleType');

  useEffect(() => {
    if (open) {
      if (initialData) {
        reset({
          code: initialData.code,
          name: initialData.name,
          vehicleType: initialData.vehicleType,
          pricingMode: initialData.pricingMode,
          gracePeriodMinutes: initialData.gracePeriodMinutes ?? 0,
          flatPrice: initialData.flatPrice ?? 0,
          basePrice: initialData.basePrice ?? 0,
          baseBlockMinutes: initialData.baseBlockMinutes ?? 0,
          additionalBlockPrice: initialData.additionalBlockPrice ?? 0,
          additionalBlockMinutes: initialData.additionalBlockMinutes ?? 0,
          overnightSurcharge: initialData.overnightSurcharge ?? 0,
          maxDailyCap: initialData.maxDailyCap ?? 0,
          isActive: initialData.isActive ?? true,
        });
      } else {
        reset({
          code: '',
          name: '',
          vehicleType: VehicleType.Car,
          pricingMode: TariffPricingMode.Flat,
          gracePeriodMinutes: 15,
          flatPrice: 10000,
          basePrice: 10000,
          baseBlockMinutes: 120,
          additionalBlockPrice: 5000,
          additionalBlockMinutes: 60,
          overnightSurcharge: 20000,
          maxDailyCap: 100000,
          isActive: true,
        });
      }
    }
  }, [open, initialData, reset]);

  const handleFormSubmit = async (data: TariffFormData) => {
    const payload = {
      code: data.code.trim().toUpperCase(),
      name: data.name.trim(),
      vehicleType: Number(data.vehicleType) as VehicleType,
      pricingMode: Number(data.pricingMode) as TariffPricingMode,
      gracePeriodMinutes: Number(data.gracePeriodMinutes) || 0,
      flatPrice: data.pricingMode === TariffPricingMode.Flat ? Number(data.flatPrice) || 0 : 0,
      basePrice: data.pricingMode === TariffPricingMode.Block ? Number(data.basePrice) || 0 : 0,
      baseBlockMinutes: data.pricingMode === TariffPricingMode.Block ? Number(data.baseBlockMinutes) || 0 : 0,
      additionalBlockPrice: data.pricingMode === TariffPricingMode.Block ? Number(data.additionalBlockPrice) || 0 : 0,
      additionalBlockMinutes: data.pricingMode === TariffPricingMode.Block ? Number(data.additionalBlockMinutes) || 0 : 0,
      overnightSurcharge: Number(data.overnightSurcharge) || 0,
      maxDailyCap: Number(data.maxDailyCap) || 0,
      isActive: Boolean(data.isActive),
    };
    await onSubmit(payload);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2 text-base font-bold">
            {isEditing ? (
              <>
                <Edit className="h-5 w-5 text-indigo-600" />
                <span>Cập Nhật Biểu Phí Đỗ Xe</span>
              </>
            ) : (
              <>
                <Receipt className="h-5 w-5 text-indigo-600" />
                <span>Thêm Biểu Phí Đỗ Xe Mới</span>
              </>
            )}
          </DialogTitle>
          <DialogDescription className="text-xs">
            {isEditing
              ? 'Chỉnh sửa đơn giá, quy tắc lũy tiến block, phụ thu qua đêm và trần phí.'
              : 'Thiết lập bảng giá tính phí đỗ xe cho khách vãng lai và xe cá nhân.'}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-4 py-1">
          {/* Thông tin cơ bản */}
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">
                Mã biểu phí <span className="text-destructive">*</span>
              </label>
              <Input
                {...register('code')}
                placeholder="VD: TARIFF_CAR_HOURLY"
                className="font-mono text-xs uppercase"
                maxLength={50}
                disabled={isEditing}
                autoFocus={!isEditing}
              />
              {errors.code && (
                <p className="text-[11px] text-destructive">{errors.code.message}</p>
              )}
            </div>

            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">
                Tên biểu phí <span className="text-destructive">*</span>
              </label>
              <Input
                {...register('name')}
                placeholder="VD: Biểu phí xe con theo giờ"
                className="text-xs"
                maxLength={200}
              />
              {errors.name && (
                <p className="text-[11px] text-destructive">{errors.name.message}</p>
              )}
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">Loại phương tiện</label>
              <Select
                value={String(watchedVehicleType)}
                onValueChange={(val) => setValue('vehicleType', Number(val) as VehicleType)}
              >
                <SelectTrigger className="text-xs">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={String(VehicleType.Car)} className="text-xs">
                    {getVehicleTypeLabel(VehicleType.Car)}
                  </SelectItem>
                  <SelectItem value={String(VehicleType.Motorbike)} className="text-xs">
                    {getVehicleTypeLabel(VehicleType.Motorbike)}
                  </SelectItem>
                  <SelectItem value={String(VehicleType.Bicycle)} className="text-xs">
                    {getVehicleTypeLabel(VehicleType.Bicycle)}
                  </SelectItem>
                  <SelectItem value={String(VehicleType.Other)} className="text-xs">
                    {getVehicleTypeLabel(VehicleType.Other)}
                  </SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">Chế độ tính phí</label>
              <Select
                value={String(watchedPricingMode)}
                onValueChange={(val) => setValue('pricingMode', Number(val) as TariffPricingMode)}
              >
                <SelectTrigger className="text-xs">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={String(TariffPricingMode.Flat)} className="text-xs">
                    {getPricingModeLabel(TariffPricingMode.Flat)}
                  </SelectItem>
                  <SelectItem value={String(TariffPricingMode.Block)} className="text-xs">
                    {getPricingModeLabel(TariffPricingMode.Block)}
                  </SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          {/* Ân hạn */}
          <div className="space-y-1">
            <label className="text-xs font-semibold text-foreground flex items-center gap-1.5">
              <Clock className="h-3.5 w-3.5 text-blue-600" />
              Thời gian ân hạn miễn phí (phút)
            </label>
            <Input
              type="number"
              {...register('gracePeriodMinutes')}
              placeholder="VD: 15"
              className="text-xs"
              min={0}
            />
            <p className="text-[10px] text-muted-foreground">
              Xe vào và ra trong thời gian này sẽ được miễn phí hoàn toàn (0 VNĐ).
            </p>
            {errors.gracePeriodMinutes && (
              <p className="text-[11px] text-destructive">{errors.gracePeriodMinutes.message}</p>
            )}
          </div>

          {/* Cấu hình chi tiết theo PricingMode */}
          {watchedPricingMode === TariffPricingMode.Flat ? (
            <div className="p-3 rounded-lg border bg-muted/40 space-y-2">
              <label className="text-xs font-semibold text-foreground flex items-center gap-1.5">
                <DollarSign className="h-3.5 w-3.5 text-emerald-600" />
                Giá cố định theo lượt (VNĐ)
              </label>
              <Input
                type="number"
                {...register('flatPrice')}
                placeholder="VD: 10000"
                className="text-xs font-mono"
                min={0}
                step={1000}
              />
              {errors.flatPrice && (
                <p className="text-[11px] text-destructive">{errors.flatPrice.message}</p>
              )}
            </div>
          ) : (
            <div className="p-3 rounded-lg border bg-muted/40 space-y-3">
              <div className="text-xs font-semibold text-foreground flex items-center gap-1.5">
                <Clock className="h-3.5 w-3.5 text-indigo-600" />
                Cấu hình Block lũy tiến
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1">
                  <label className="text-[11px] text-muted-foreground">Block đầu (phút)</label>
                  <Input
                    type="number"
                    {...register('baseBlockMinutes')}
                    placeholder="VD: 120"
                    className="text-xs"
                    min={1}
                  />
                  {errors.baseBlockMinutes && (
                    <p className="text-[11px] text-destructive">{errors.baseBlockMinutes.message}</p>
                  )}
                </div>
                <div className="space-y-1">
                  <label className="text-[11px] text-muted-foreground">Giá block đầu (VNĐ)</label>
                  <Input
                    type="number"
                    {...register('basePrice')}
                    placeholder="VD: 10000"
                    className="text-xs font-mono"
                    min={0}
                    step={1000}
                  />
                  {errors.basePrice && (
                    <p className="text-[11px] text-destructive">{errors.basePrice.message}</p>
                  )}
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1">
                  <label className="text-[11px] text-muted-foreground">Block tiếp theo (phút)</label>
                  <Input
                    type="number"
                    {...register('additionalBlockMinutes')}
                    placeholder="VD: 60"
                    className="text-xs"
                    min={1}
                  />
                  {errors.additionalBlockMinutes && (
                    <p className="text-[11px] text-destructive">{errors.additionalBlockMinutes.message}</p>
                  )}
                </div>
                <div className="space-y-1">
                  <label className="text-[11px] text-muted-foreground">Giá mỗi block tiếp theo (VNĐ)</label>
                  <Input
                    type="number"
                    {...register('additionalBlockPrice')}
                    placeholder="VD: 5000"
                    className="text-xs font-mono"
                    min={0}
                    step={1000}
                  />
                  {errors.additionalBlockPrice && (
                    <p className="text-[11px] text-destructive">{errors.additionalBlockPrice.message}</p>
                  )}
                </div>
              </div>
            </div>
          )}

          {/* Phụ thu qua đêm & Trần phí mỗi ngày */}
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">
                Phụ thu qua đêm (VNĐ)
              </label>
              <Input
                type="number"
                {...register('overnightSurcharge')}
                placeholder="VD: 20000"
                className="text-xs font-mono"
                min={0}
                step={1000}
              />
              <p className="text-[10px] text-muted-foreground">
                Cộng thêm khi xe gửi qua mốc 00:00 (giờ VN).
              </p>
              {errors.overnightSurcharge && (
                <p className="text-[11px] text-destructive">{errors.overnightSurcharge.message}</p>
              )}
            </div>

            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">
                Mức trần mỗi chu kỳ 24h (VNĐ)
              </label>
              <Input
                type="number"
                {...register('maxDailyCap')}
                placeholder="VD: 100000 (0 = không giới hạn)"
                className="text-xs font-mono"
                min={0}
                step={1000}
              />
              <p className="text-[10px] text-muted-foreground">
                Mức phí tối đa tính trong mỗi chu kỳ 24h (1440 phút).
              </p>
              {errors.maxDailyCap && (
                <p className="text-[11px] text-destructive">{errors.maxDailyCap.message}</p>
              )}
            </div>
          </div>

          {/* Trạng thái kích hoạt */}
          <div className="flex items-center justify-between p-3 rounded-lg border bg-muted/20">
            <div className="space-y-0.5">
              <label className="text-xs font-semibold text-foreground">Kích hoạt biểu phí</label>
              <p className="text-[11px] text-muted-foreground">
                Chỉ biểu phí đang kích hoạt mới có thể gán cho thẻ và tính phí ở làn.
              </p>
            </div>
            <Switch
              checked={watchedIsActive}
              onCheckedChange={(val) => setValue('isActive', val)}
            />
          </div>

          <DialogFooter className="pt-3">
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => onOpenChange(false)}
              className="text-xs h-9 cursor-pointer"
            >
              Hủy bỏ
            </Button>
            <Button
              type="submit"
              size="sm"
              disabled={isSubmitting}
              className="text-xs h-9 bg-indigo-600 hover:bg-indigo-700 text-white cursor-pointer"
            >
              <Save className="h-3.5 w-3.5 mr-1" />
              <span>{isSubmitting ? 'Đang lưu...' : isEditing ? 'Lưu thay đổi' : 'Thêm biểu phí'}</span>
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
