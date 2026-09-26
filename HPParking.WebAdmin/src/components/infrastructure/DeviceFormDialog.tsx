import { useEffect, useState } from 'react';
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
import { Cpu, Eye, EyeOff, Save } from 'lucide-react';
import {
  DeviceType,
  type DeviceDto,
  type CreateDeviceRequest,
  type UpdateDeviceRequest,
} from '@/types/infrastructure';

const ipv4Regex =
  /^(25[0-5]|2[0-4]\d|1\d{2}|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d{2}|[1-9]?\d)){3}$/;

const deviceSchema = z.object({
  code: z
    .string()
    .trim()
    .min(2, 'Mã thiết bị phải có ít nhất 2 ký tự')
    .max(50, 'Mã thiết bị không được quá 50 ký tự')
    .regex(/^[A-Za-z0-9_-]+$/, 'Mã thiết bị chỉ chứa chữ, số, gạch dưới hoặc gạch ngang'),
  name: z
    .string()
    .trim()
    .min(2, 'Tên thiết bị phải có ít nhất 2 ký tự')
    .max(100, 'Tên thiết bị không được quá 100 ký tự'),
  type: z.number().int().min(1).max(4),
  ipAddress: z
    .string()
    .trim()
    .regex(ipv4Regex, 'Địa chỉ IP không đúng định dạng IPv4 (VD: 192.168.1.100)'),
  port: z.coerce
    .number()
    .int('Cổng phải là số nguyên')
    .min(1, 'Cổng kết nối tối thiểu là 1')
    .max(65535, 'Cổng kết nối tối đa là 65535'),
  userName: z.string().trim().optional(),
  password: z.string().optional(),
  isActive: z.boolean(),
});

type DeviceFormData = z.infer<typeof deviceSchema>;

interface DeviceFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  initialData?: DeviceDto | null;
  onSubmit: (data: CreateDeviceRequest | UpdateDeviceRequest) => Promise<void>;
  isSubmitting?: boolean;
}

export function DeviceFormDialog({
  open,
  onOpenChange,
  initialData,
  onSubmit,
  isSubmitting = false,
}: DeviceFormDialogProps) {
  const isEditing = Boolean(initialData);
  const [showPassword, setShowPassword] = useState(false);

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    watch,
    formState: { errors },
  } = useForm<DeviceFormData>({
    resolver: zodResolver(deviceSchema),
    defaultValues: {
      code: '',
      name: '',
      type: DeviceType.Camera,
      ipAddress: '',
      port: 80,
      userName: '',
      password: '',
      isActive: true,
    },
  });

  const selectedType = watch('type');
  const isActive = watch('isActive');

  useEffect(() => {
    if (open) {
      setShowPassword(false);
      if (initialData) {
        reset({
          code: initialData.code,
          name: initialData.name,
          type: initialData.type,
          ipAddress: initialData.ipAddress,
          port: initialData.port,
          userName: initialData.userName || '',
          password: '',
          isActive: initialData.isActive,
        });
      } else {
        reset({
          code: '',
          name: '',
          type: DeviceType.Camera,
          ipAddress: '192.168.1.100',
          port: 80,
          userName: 'admin',
          password: '',
          isActive: true,
        });
      }
    }
  }, [open, initialData, reset]);

  const onFormSubmit = async (formData: DeviceFormData) => {
    const payload: CreateDeviceRequest | UpdateDeviceRequest = {
      code: formData.code.toUpperCase(),
      name: formData.name,
      type: formData.type as DeviceType,
      ipAddress: formData.ipAddress,
      port: formData.port,
      userName: formData.userName?.trim() || undefined,
      password: formData.password ? formData.password : undefined,
      isActive: formData.isActive,
    };
    await onSubmit(payload);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <div className="flex items-center gap-2 mb-1">
            <div className="p-2 rounded-lg bg-blue-100 dark:bg-blue-950 text-blue-600 dark:text-blue-400">
              <Cpu className="h-5 w-5" />
            </div>
            <DialogTitle className="text-base font-bold">
              {isEditing ? 'Cập Nhật Thiết Bị Ngoại Vi' : 'Thêm Mới Thiết Bị Ngoại Vi'}
            </DialogTitle>
          </div>
          <DialogDescription>
            {isEditing
              ? 'Chỉnh sửa cấu hình kết nối IP, Port và tài khoản thiết bị ngoại vi.'
              : 'Đăng ký thiết bị (Camera biển số, Toàn cảnh, Barrier controller, FaceID) vào hệ thống.'}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onFormSubmit)} className="space-y-3 py-1">
          {/* Loại thiết bị */}
          <div className="space-y-1">
            <label className="text-xs font-semibold text-foreground">
              Loại thiết bị <span className="text-destructive">*</span>
            </label>
            <Select
              value={String(selectedType)}
              onValueChange={(val) => setValue('type', Number(val), { shouldValidate: true })}
            >
              <SelectTrigger className="text-xs">
                <SelectValue placeholder="-- Chọn loại thiết bị --" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={String(DeviceType.Camera)} className="text-xs">
                  Camera (Biển số / Toàn cảnh)
                </SelectItem>
                <SelectItem value={String(DeviceType.Controller)} className="text-xs">
                  Bộ điều khiển Barrier (Controller)
                </SelectItem>
                <SelectItem value={String(DeviceType.FaceId)} className="text-xs">
                  Thiết bị nhận diện khuôn mặt (FaceID)
                </SelectItem>
                <SelectItem value={String(DeviceType.Other)} className="text-xs">
                  Thiết bị khác (Cảm biến, Đầu đọc RFID)
                </SelectItem>
              </SelectContent>
            </Select>
            {errors.type && (
              <p className="text-[11px] text-destructive">{errors.type.message}</p>
            )}
          </div>

          {/* Mã thiết bị & Tên thiết bị */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">
                Mã thiết bị <span className="text-destructive">*</span>
              </label>
              <Input
                {...register('code')}
                placeholder="VD: CAM_VAO_01"
                className="uppercase text-xs"
                autoFocus={!isEditing}
              />
              {errors.code && (
                <p className="text-[11px] text-destructive">{errors.code.message}</p>
              )}
            </div>

            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">
                Tên thiết bị <span className="text-destructive">*</span>
              </label>
              <Input
                {...register('name')}
                placeholder="VD: Camera Biển Số Làn 1"
                className="text-xs"
              />
              {errors.name && (
                <p className="text-[11px] text-destructive">{errors.name.message}</p>
              )}
            </div>
          </div>

          {/* Địa chỉ IP & Cổng mạng */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
            <div className="sm:col-span-2 space-y-1">
              <label className="text-xs font-semibold text-foreground">
                Địa chỉ IP (IPv4) <span className="text-destructive">*</span>
              </label>
              <Input
                {...register('ipAddress')}
                placeholder="VD: 192.168.1.101"
                className="text-xs font-mono"
              />
              {errors.ipAddress && (
                <p className="text-[11px] text-destructive">{errors.ipAddress.message}</p>
              )}
            </div>

            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">
                Cổng (Port) <span className="text-destructive">*</span>
              </label>
              <Input
                {...register('port')}
                type="number"
                placeholder="80"
                className="text-xs font-mono"
              />
              {errors.port && (
                <p className="text-[11px] text-destructive">{errors.port.message}</p>
              )}
            </div>
          </div>

          {/* Tên đăng nhập & Mật khẩu kết nối thiết bị */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">Tài khoản (UserName)</label>
              <Input
                {...register('userName')}
                placeholder="VD: admin"
                className="text-xs"
              />
            </div>

            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">
                {isEditing ? 'Mật khẩu mới (Để trống nếu không đổi)' : 'Mật khẩu (Password)'}
              </label>
              <div className="relative">
                <Input
                  {...register('password')}
                  type={showPassword ? 'text' : 'password'}
                  placeholder={isEditing ? '••••••••' : 'Nhập mật khẩu'}
                  className="text-xs pr-8"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-2 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground cursor-pointer"
                  tabIndex={-1}
                >
                  {showPassword ? (
                    <EyeOff className="h-3.5 w-3.5" />
                  ) : (
                    <Eye className="h-3.5 w-3.5" />
                  )}
                </button>
              </div>
            </div>
          </div>

          {/* Trạng thái hoạt động */}
          <div className="pt-2 border-t border-border flex items-center justify-between">
            <div>
              <span className="text-xs font-semibold text-foreground block">
                Trạng thái hoạt động
              </span>
              <span className="text-[11px] text-muted-foreground block">
                Cho phép hệ thống kết nối và truyền nhận tín hiệu từ thiết bị này
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
