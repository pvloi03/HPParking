import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useQuery } from '@tanstack/react-query';
import {
  CreditCard,
  Car,
  User,
  Save,
  Lock,
  Edit,
  AlertTriangle,
  Receipt,
  Calendar,
} from 'lucide-react';
import { toast } from '@/hooks/use-toast';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Switch } from '@/components/ui/switch';
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
import { InfiniteSearchableSelect } from '@/components/ui/infinite-searchable-select';
import { clientApi } from '@/api/clientApi';
import { vehicleApi } from '@/api/vehicleApi';
import { tariffApi } from '@/api/tariffApi';
import { useFeatureFlags } from '@/config/featureFlags';
import type { ClientDto } from '@/types/client';
import type { VehicleDto } from '@/types/vehicle';
import { VehicleType, getVehicleTypeLabel } from '@/types/vehicle';
import {
  CardTargetType,
  CardStatus,
  type CardDto,
  type CreateCardRequest,
  type UpdateCardRequest,
  normalizeCardCode,
} from '@/types/card';

const cardFormSchema = z.object({
  cardNumber: z
    .string()
    .trim()
    .min(1, 'Vui lòng nhập mã thẻ')
    .max(10, 'Mã thẻ tối đa 10 chữ số')
    .regex(/^\d+$/, 'Mã thẻ chỉ được chứa các chữ số 0-9'),
  targetType: z.number().int().min(1).max(5),
  clientId: z.string().optional(),
  vehicleId: z.string().optional(),
  vehicleType: z.number().int().optional(),
  isChargeable: z.boolean().optional(),
  tariffId: z.string().optional(),
  validFrom: z.string().optional(),
  validTo: z.string().optional(),
  status: z.number().int().min(0).max(3),
  note: z.string().trim().optional(),
});

type CardFormData = z.infer<typeof cardFormSchema>;

export interface CardFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  initialData?: CardDto | null;
  onSubmit: (data: CreateCardRequest | UpdateCardRequest) => Promise<void>;
  isSubmitting?: boolean;
}

export function CardFormDialog({
  open,
  onOpenChange,
  initialData,
  onSubmit,
  isSubmitting = false,
}: CardFormDialogProps) {
  const isEditing = Boolean(initialData);
  const hasAssignedClient = Boolean(isEditing && initialData?.clientId);
  const { enableFleetDispatch } = useFeatureFlags();

  const { data: activeTariffs } = useQuery({
    queryKey: ['activeTariffsForCards'],
    queryFn: () => tariffApi.getAllActive(),
    enabled: open,
  });

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    reset,
    formState: { errors },
  } = useForm<CardFormData>({
    resolver: zodResolver(cardFormSchema),
    defaultValues: {
      cardNumber: '',
      targetType: CardTargetType.Person,
      clientId: '',
      vehicleId: '',
      vehicleType: VehicleType.Car,
      isChargeable: false,
      tariffId: '',
      validFrom: '',
      validTo: '',
      status: CardStatus.Available,
      note: '',
    },
  });

  const watchedTargetType = watch('targetType');
  const watchedStatus = watch('status');
  const watchedClientId = watch('clientId');
  const watchedVehicleId = watch('vehicleId');
  const watchedVehicleType = watch('vehicleType');
  const watchedIsChargeable = watch('isChargeable');
  const watchedTariffId = watch('tariffId');

  const isAssigned = Boolean(
    (watchedTargetType === CardTargetType.Person && watchedClientId) ||
    (watchedTargetType === CardTargetType.Vehicle && watchedVehicleId) ||
    (watchedTargetType === CardTargetType.Monthly && watchedClientId) ||
    (watchedTargetType === CardTargetType.VIP && watchedClientId)
  );

  useEffect(() => {
    if (isAssigned && watchedStatus === CardStatus.Available) {
      setValue('status', CardStatus.InUse, { shouldDirty: true });
    } else if (!isAssigned && watchedStatus === CardStatus.InUse) {
      setValue('status', CardStatus.Available, { shouldDirty: true });
    }
  }, [isAssigned, watchedStatus, setValue]);

  useEffect(() => {
    if (open) {
      if (initialData) {
        const assigned = Boolean(
          (initialData.targetType === CardTargetType.Person && initialData.clientId) ||
          (initialData.targetType === CardTargetType.Vehicle && initialData.vehicleId) ||
          (initialData.targetType === CardTargetType.Monthly && initialData.clientId) ||
          (initialData.targetType === CardTargetType.VIP && initialData.clientId)
        );
        let status = initialData.status;
        if (assigned && status === CardStatus.Available) {
          status = CardStatus.InUse;
        } else if (!assigned && status === CardStatus.InUse) {
          status = CardStatus.Available;
        }

        reset({
          cardNumber: initialData.cardNumber,
          targetType: initialData.targetType,
          clientId: initialData.clientId || '',
          vehicleId: initialData.vehicleId || '',
          vehicleType: initialData.vehicleType ?? VehicleType.Car,
          isChargeable: initialData.isChargeable ?? (initialData.targetType === CardTargetType.Visitor),
          tariffId: initialData.tariffId || '',
          validFrom: initialData.validFrom ? initialData.validFrom.split('T')[0] : '',
          validTo: initialData.validTo ? initialData.validTo.split('T')[0] : '',
          status,
          note: initialData.note || '',
        });
      } else {
        reset({
          cardNumber: '',
          targetType: CardTargetType.Person,
          clientId: '',
          vehicleId: '',
          vehicleType: VehicleType.Car,
          isChargeable: false,
          tariffId: '',
          validFrom: '',
          validTo: '',
          status: CardStatus.Available,
          note: '',
        });
      }
    }
  }, [open, initialData, reset]);

  const handleFormSubmit = async (data: CardFormData) => {
    const isPersonLike =
      Number(data.targetType) === CardTargetType.Person ||
      Number(data.targetType) === CardTargetType.Monthly ||
      Number(data.targetType) === CardTargetType.VIP;

    if (isEditing) {
      const payload: UpdateCardRequest = {
        targetType: Number(data.targetType) as CardTargetType,
        clientId: isPersonLike && data.clientId ? data.clientId : undefined,
        vehicleId: Number(data.targetType) === CardTargetType.Vehicle && data.vehicleId ? data.vehicleId : undefined,
        vehicleType: Number(data.targetType) === CardTargetType.Visitor && data.vehicleType ? Number(data.vehicleType) : undefined,
        isChargeable: Boolean(data.isChargeable),
        tariffId: data.isChargeable && data.tariffId ? data.tariffId : undefined,
        validFrom: Number(data.targetType) === CardTargetType.Monthly && data.validFrom ? data.validFrom : undefined,
        validTo: Number(data.targetType) === CardTargetType.Monthly && data.validTo ? data.validTo : undefined,
        status: data.status !== undefined ? (Number(data.status) as CardStatus) : undefined,
        note: data.note || undefined,
      };
      await onSubmit(payload);
    } else {
      const padded = normalizeCardCode(data.cardNumber);
      const payload: CreateCardRequest = {
        cardNumber: padded,
        targetType: Number(data.targetType) as CardTargetType,
        clientId: isPersonLike && data.clientId ? data.clientId : undefined,
        vehicleId: Number(data.targetType) === CardTargetType.Vehicle && data.vehicleId ? data.vehicleId : undefined,
        vehicleType: Number(data.targetType) === CardTargetType.Visitor && data.vehicleType ? Number(data.vehicleType) : undefined,
        isChargeable: Boolean(data.isChargeable),
        tariffId: data.isChargeable && data.tariffId ? data.tariffId : undefined,
        validFrom: Number(data.targetType) === CardTargetType.Monthly && data.validFrom ? data.validFrom : undefined,
        validTo: Number(data.targetType) === CardTargetType.Monthly && data.validTo ? data.validTo : undefined,
        status: data.status !== undefined ? (Number(data.status) as CardStatus) : undefined,
        note: data.note || undefined,
      };
      await onSubmit(payload);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2 text-base font-bold">
            {isEditing ? (
              <>
                <Edit className="h-5 w-5 text-indigo-600" />
                <span>Cập Nhật Thông Tin Thẻ</span>
              </>
            ) : (
              <>
                <CreditCard className="h-5 w-5 text-indigo-600" />
                <span>Thêm Thẻ Định Danh Mới</span>
              </>
            )}
          </DialogTitle>
          <DialogDescription className="text-xs">
            {isEditing
              ? 'Chỉnh sửa đối tượng gán, biểu phí áp dụng, trạng thái sử dụng và ghi chú của thẻ.'
              : 'Mã thẻ sẽ tự động được chuẩn hóa đủ 10 chữ số (thêm các số 0 ở đầu nếu chưa đủ).'}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-3.5 py-1">
          {/* Mã thẻ */}
          <div className="space-y-1">
            <div className="flex items-center justify-between">
              <label className="text-xs font-semibold text-foreground">
                Mã thẻ (tối đa 10 chữ số) <span className="text-destructive">*</span>
              </label>
              {isEditing && (
                <span className="text-[10px] text-muted-foreground flex items-center gap-1">
                  <Lock className="h-3 w-3" /> Mã chip cố định
                </span>
              )}
            </div>
            <Input
              {...register('cardNumber')}
              placeholder="VD: 12345 hoặc 0000012345"
              className="font-mono text-xs"
              maxLength={10}
              disabled={isEditing}
              autoFocus={!isEditing}
            />
            {errors.cardNumber && (
              <p className="text-[11px] text-destructive">{errors.cardNumber.message}</p>
            )}
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">Loại đối tượng</label>
              <Select
                value={String(watchedTargetType)}
                onValueChange={(val) => {
                  const parsed = Number(val) as CardTargetType;
                  setValue('targetType', parsed);
                  setValue('clientId', '');
                  setValue('vehicleId', '');
                  if (parsed === CardTargetType.Visitor) {
                    setValue('isChargeable', true);
                  }
                  if (!isEditing) {
                    setValue('status', CardStatus.Available);
                  }
                }}
              >
                <SelectTrigger className="text-xs">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={String(CardTargetType.Person)} className="text-xs">
                    Thẻ người (Nhân sự)
                  </SelectItem>
                  {(enableFleetDispatch || (isEditing && initialData?.targetType === CardTargetType.Vehicle)) && (
                    <SelectItem value={String(CardTargetType.Vehicle)} className="text-xs">
                      Thẻ phương tiện nội bộ
                    </SelectItem>
                  )}
                  <SelectItem value={String(CardTargetType.Visitor)} className="text-xs">
                    Thẻ khách (Vãng lai)
                  </SelectItem>
                  <SelectItem value={String(CardTargetType.Monthly)} className="text-xs">
                    Thẻ tháng (Vé tháng)
                  </SelectItem>
                  <SelectItem value={String(CardTargetType.VIP)} className="text-xs">
                    Thẻ VIP / Miễn phí
                  </SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">Trạng thái</label>
              <Select
                value={String(watchedStatus)}
                onValueChange={(val) => setValue('status', Number(val) as CardStatus)}
              >
                <SelectTrigger className="text-xs">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {isAssigned ? (
                    <>
                      <SelectItem value={String(CardStatus.InUse)} className="text-xs">
                        Đang sử dụng
                      </SelectItem>
                      <SelectItem value={String(CardStatus.Locked)} className="text-xs">
                        Tạm khóa
                      </SelectItem>
                      <SelectItem value={String(CardStatus.Lost)} className="text-xs">
                        Báo mất
                      </SelectItem>
                    </>
                  ) : (
                    <>
                      <SelectItem value={String(CardStatus.Available)} className="text-xs">
                        Trong kho (Chưa gán)
                      </SelectItem>
                      <SelectItem value={String(CardStatus.Locked)} className="text-xs">
                        Tạm khóa
                      </SelectItem>
                      <SelectItem value={String(CardStatus.Lost)} className="text-xs">
                        Báo mất
                      </SelectItem>
                    </>
                  )}
                </SelectContent>
              </Select>
            </div>
          </div>

          {/* Gán nhân sự cho Thẻ người, Thẻ tháng, Thẻ VIP */}
          {(watchedTargetType === CardTargetType.Person ||
            watchedTargetType === CardTargetType.Monthly ||
            watchedTargetType === CardTargetType.VIP) && (
            <div className="space-y-2">
              <label className="text-xs font-semibold text-foreground flex items-center justify-between">
                <span className="flex items-center gap-1.5">
                  <User className="h-3.5 w-3.5 text-purple-600" />
                  Gán cho Nhân sự (Khách hàng)
                </span>
                <span className="text-[10px] text-muted-foreground">Chọn người để gán thẻ</span>
              </label>
              {hasAssignedClient && (
                <div className="flex items-start gap-2 p-2.5 rounded-lg bg-amber-50 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-800 text-xs text-amber-800 dark:text-amber-200">
                  <AlertTriangle className="h-4 w-4 shrink-0 text-amber-600 mt-0.5" />
                  <div className="space-y-0.5">
                    <span className="font-semibold">Thẻ đang được gán cho nhân sự này</span>
                    <p className="text-[11px] leading-relaxed">
                      Hệ thống không cho phép chuyển thẻ trực tiếp giữa 2 nhân sự. Để đổi chủ thẻ, vui lòng gỡ thẻ về kho bằng cách chọn &quot;-- Chưa gán (Lưu trong kho) --&quot; rồi lưu lại trước.
                    </p>
                  </div>
                </div>
              )}
              <InfiniteSearchableSelect<ClientDto>
                queryKey={['activeClientsForCardsInfinite']}
                fetchFn={(params) => clientApi.getPaged({ ...params, isActive: true })}
                fetchById={(id) => clientApi.getById(String(id))}
                value={watchedClientId || ''}
                onValueChange={(val) => {
                  if (hasAssignedClient && val && val !== initialData?.clientId) {
                    toast.warning(
                      'Thẻ hiện đang được gán cho nhân sự khác. Vui lòng chọn "-- Chưa gán (Lưu trong kho) --" và lưu lại trước khi gán cho người mới.'
                    );
                    return;
                  }
                  setValue('clientId', val, { shouldDirty: true });
                  if (val && watchedStatus === CardStatus.Available) {
                    setValue('status', CardStatus.InUse, { shouldDirty: true });
                  } else if (!val && watchedStatus === CardStatus.InUse) {
                    setValue('status', CardStatus.Available, { shouldDirty: true });
                  }
                }}
                placeholder="-- Chưa gán (Lưu trong kho) --"
                allowClear={true}
                clearLabel="-- Chưa gán (Lưu trong kho) --"
                getLabel={(client) => `${client.name} (${client.code}${client.phoneNumber ? ` - ${client.phoneNumber}` : ''})`}
              />
            </div>
          )}

          {/* Gán phương tiện nội bộ */}
          {watchedTargetType === CardTargetType.Vehicle && (
            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground flex items-center justify-between">
                <span className="flex items-center gap-1.5">
                  <Car className="h-3.5 w-3.5 text-blue-600" />
                  Gán cho Phương tiện nội bộ (Xe dùng chung)
                </span>
                <span className="text-[10px] text-muted-foreground">Chọn xe để gán thẻ</span>
              </label>
              <InfiniteSearchableSelect<VehicleDto>
                queryKey={['activeVehiclesForCardsInfinite']}
                fetchFn={(params) => vehicleApi.getPaged({ ...params, isActive: true })}
                fetchById={(id) => vehicleApi.getById(String(id))}
                value={watchedVehicleId || ''}
                onValueChange={(val) => {
                  setValue('vehicleId', val, { shouldDirty: true });
                  if (val && watchedStatus === CardStatus.Available) {
                    setValue('status', CardStatus.InUse, { shouldDirty: true });
                  } else if (!val && watchedStatus === CardStatus.InUse) {
                    setValue('status', CardStatus.Available, { shouldDirty: true });
                  }
                }}
                placeholder="-- Chưa gán (Lưu trong kho) --"
                allowClear={true}
                clearLabel="-- Chưa gán (Lưu trong kho) --"
                getLabel={(vehicle) => `${vehicle.plateNumber} (${vehicle.isShared ? 'Xe dùng chung' : 'Xe cá nhân'}${vehicle.note ? ` - ${vehicle.note}` : ''})`}
              />
            </div>
          )}

          {/* Thẻ khách (Vãng lai) - Chọn loại phương tiện */}
          {watchedTargetType === CardTargetType.Visitor && (
            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground flex items-center gap-1.5">
                <Car className="h-3.5 w-3.5 text-amber-600" />
                Loại phương tiện mặc định cho thẻ khách
              </label>
              <Select
                value={String(watchedVehicleType || VehicleType.Car)}
                onValueChange={(val) => setValue('vehicleType', Number(val))}
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
          )}

          {/* Thẻ tháng: Thời hạn hiệu lực */}
          {watchedTargetType === CardTargetType.Monthly && (
            <div className="grid grid-cols-2 gap-3 p-3 rounded-lg border bg-muted/20">
              <div className="space-y-1">
                <label className="text-xs font-semibold text-foreground flex items-center gap-1">
                  <Calendar className="h-3.5 w-3.5 text-indigo-600" />
                  Hiệu lực từ ngày
                </label>
                <Input
                  type="date"
                  {...register('validFrom')}
                  className="text-xs"
                />
              </div>
              <div className="space-y-1">
                <label className="text-xs font-semibold text-foreground flex items-center gap-1">
                  <Calendar className="h-3.5 w-3.5 text-indigo-600" />
                  Hiệu lực đến ngày
                </label>
                <Input
                  type="date"
                  {...register('validTo')}
                  className="text-xs"
                />
              </div>
            </div>
          )}

          {/* Cấu hình tính phí (Áp dụng cho Thẻ khách hoặc Thẻ người) */}
          {(watchedTargetType === CardTargetType.Visitor || watchedTargetType === CardTargetType.Person) && (
            <div className="p-3 rounded-lg border bg-muted/20 space-y-3">
              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <label className="text-xs font-semibold text-foreground flex items-center gap-1.5">
                    <Receipt className="h-3.5 w-3.5 text-emerald-600" />
                    Tính phí đỗ xe theo lượt
                  </label>
                  <p className="text-[11px] text-muted-foreground">
                    Khi bật, trạm kiểm soát sẽ tính phí theo biểu phí khi quẹt thẻ xe ra.
                  </p>
                </div>
                <Switch
                  checked={Boolean(watchedIsChargeable)}
                  onCheckedChange={(val) => setValue('isChargeable', val)}
                />
              </div>

              {watchedIsChargeable && (
                <div className="space-y-1 pt-2 border-t">
                  <label className="text-xs font-semibold text-foreground">
                    Biểu phí áp dụng <span className="text-destructive">*</span>
                  </label>
                  <Select
                    value={watchedTariffId || ''}
                    onValueChange={(val) => setValue('tariffId', val)}
                  >
                    <SelectTrigger className="text-xs">
                      <SelectValue placeholder="-- Chọn biểu phí tính tiền --" />
                    </SelectTrigger>
                    <SelectContent>
                      {activeTariffs?.map((tariff) => (
                        <SelectItem key={tariff.id} value={tariff.id} className="text-xs">
                          {tariff.name} ({tariff.code})
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  {!watchedTariffId && (
                    <p className="text-[11px] text-amber-600">
                      Vui lòng chọn biểu phí để hệ thống tính phí tại cổng ra.
                    </p>
                  )}
                </div>
              )}
            </div>
          )}

          <div className="space-y-1">
            <label className="text-xs font-semibold text-foreground">Ghi chú</label>
            <Input
              {...register('note')}
              placeholder="VD: Thẻ Wiegand 26-bit dự phòng kho"
              className="text-xs"
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
              <span>{isSubmitting ? 'Đang lưu...' : isEditing ? 'Lưu thay đổi' : 'Thêm thẻ'}</span>
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
