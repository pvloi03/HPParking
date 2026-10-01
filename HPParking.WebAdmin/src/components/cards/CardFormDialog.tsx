import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import {
  CreditCard,
  Car,
  User,
  Save,
  Lock,
  Edit,
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
import { InfiniteSearchableSelect } from '@/components/ui/infinite-searchable-select';
import { clientApi } from '@/api/clientApi';
import { vehicleApi } from '@/api/vehicleApi';
import type { ClientDto } from '@/types/client';
import type { VehicleDto } from '@/types/vehicle';
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
  targetType: z.number().int().min(1).max(2),
  clientId: z.string().optional(),
  vehicleId: z.string().optional(),
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
      status: CardStatus.Available,
      note: '',
    },
  });

  const watchedTargetType = watch('targetType');
  const watchedStatus = watch('status');
  const watchedClientId = watch('clientId');
  const watchedVehicleId = watch('vehicleId');

  useEffect(() => {
    if (open) {
      if (initialData) {
        reset({
          cardNumber: initialData.cardNumber,
          targetType: initialData.targetType,
          clientId: initialData.clientId || '',
          vehicleId: initialData.vehicleId || '',
          status: initialData.status,
          note: initialData.note || '',
        });
      } else {
        reset({
          cardNumber: '',
          targetType: CardTargetType.Person,
          clientId: '',
          vehicleId: '',
          status: CardStatus.Available,
          note: '',
        });
      }
    }
  }, [open, initialData, reset]);

  const handleFormSubmit = async (data: CardFormData) => {
    if (isEditing) {
      const payload: UpdateCardRequest = {
        targetType: Number(data.targetType) as CardTargetType,
        clientId: Number(data.targetType) === CardTargetType.Person && data.clientId ? data.clientId : undefined,
        vehicleId: Number(data.targetType) === CardTargetType.Vehicle && data.vehicleId ? data.vehicleId : undefined,
        status: data.status !== undefined ? (Number(data.status) as CardStatus) : undefined,
        note: data.note || undefined,
      };
      await onSubmit(payload);
    } else {
      const padded = normalizeCardCode(data.cardNumber);
      const payload: CreateCardRequest = {
        cardNumber: padded,
        targetType: Number(data.targetType) as CardTargetType,
        clientId: Number(data.targetType) === CardTargetType.Person && data.clientId ? data.clientId : undefined,
        vehicleId: Number(data.targetType) === CardTargetType.Vehicle && data.vehicleId ? data.vehicleId : undefined,
        status: data.status !== undefined ? (Number(data.status) as CardStatus) : undefined,
        note: data.note || undefined,
      };
      await onSubmit(payload);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
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
              ? 'Chỉnh sửa đối tượng gán, trạng thái sử dụng và ghi chú của thẻ.'
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
                  <Lock className="h-3 w-3" /> Mã chip RFID cố định
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
                  <SelectItem value={String(CardTargetType.Vehicle)} className="text-xs">
                    Thẻ phương tiện nội bộ
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
                  <SelectItem value={String(CardStatus.Available)} className="text-xs">
                    Trong kho (Chưa gán)
                  </SelectItem>
                  <SelectItem value={String(CardStatus.InUse)} className="text-xs">
                    Đang sử dụng
                  </SelectItem>
                  <SelectItem value={String(CardStatus.Locked)} className="text-xs">
                    Tạm khóa
                  </SelectItem>
                  <SelectItem value={String(CardStatus.Lost)} className="text-xs">
                    Báo mất
                  </SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          {/* Gán đối tượng tương ứng */}
          {watchedTargetType === CardTargetType.Person && (
            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground flex items-center justify-between">
                <span className="flex items-center gap-1.5">
                  <User className="h-3.5 w-3.5 text-purple-600" />
                  Gán cho Nhân sự (Khách hàng)
                </span>
                <span className="text-[10px] text-muted-foreground">Chọn người để gán thẻ</span>
              </label>
              <InfiniteSearchableSelect<ClientDto>
                queryKey={['activeClientsForCardsInfinite']}
                fetchFn={(params) => clientApi.getPaged({ ...params, isActive: true })}
                fetchById={(id) => clientApi.getById(String(id))}
                value={watchedClientId || ''}
                onValueChange={(val) => {
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
