import { useEffect } from 'react';
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
import { Building, Save } from 'lucide-react';
import type {
  DepartmentDto,
  CompanyDto,
  CreateDepartmentRequest,
  UpdateDepartmentRequest,
} from '@/types/masterData';

const departmentSchema = z.object({
  companyId: z
    .string()
    .trim()
    .min(1, 'Vui lòng chọn công ty trực thuộc'),
  code: z
    .string()
    .trim()
    .min(2, 'Mã phòng ban phải có ít nhất 2 ký tự')
    .max(50, 'Mã phòng ban không được quá 50 ký tự')
    .regex(/^[A-Za-z0-9_-]+$/, 'Mã phòng ban chỉ chứa chữ, số, gạch dưới hoặc gạch ngang'),
  name: z
    .string()
    .trim()
    .min(2, 'Tên phòng ban phải có ít nhất 2 ký tự')
    .max(100, 'Tên phòng ban không được quá 100 ký tự'),
  managerName: z.string().trim().optional(),
  phoneNumber: z
    .string()
    .trim()
    .optional()
    .refine((val) => !val || /^[0-9+.\s()-]{8,20}$/.test(val), {
      message: 'Số điện thoại không đúng định dạng',
    }),
  email: z
    .string()
    .trim()
    .optional()
    .refine((val) => !val || z.string().email().safeParse(val).success, {
      message: 'Email không đúng định dạng',
    }),
  isActive: z.boolean(),
});

type DepartmentFormData = z.infer<typeof departmentSchema>;

interface DepartmentFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  initialData?: DepartmentDto | null;
  companies: CompanyDto[];
  onSubmit: (data: CreateDepartmentRequest | UpdateDepartmentRequest) => Promise<void>;
  isSubmitting?: boolean;
}

export function DepartmentFormDialog({
  open,
  onOpenChange,
  initialData,
  companies,
  onSubmit,
  isSubmitting = false,
}: DepartmentFormDialogProps) {
  const isEditing = Boolean(initialData);

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    watch,
    formState: { errors },
  } = useForm<DepartmentFormData>({
    resolver: zodResolver(departmentSchema),
    defaultValues: {
      companyId: '',
      code: '',
      name: '',
      managerName: '',
      phoneNumber: '',
      email: '',
      isActive: true,
    },
  });

  const selectedCompanyId = watch('companyId');
  const isActive = watch('isActive');

  useEffect(() => {
    if (open) {
      if (initialData) {
        reset({
          companyId: initialData.companyId || '',
          code: initialData.code,
          name: initialData.name,
          managerName: initialData.managerName || '',
          phoneNumber: initialData.phoneNumber || '',
          email: initialData.email || '',
          isActive: initialData.isActive,
        });
      } else {
        reset({
          companyId: companies.length > 0 ? companies[0].id : '',
          code: '',
          name: '',
          managerName: '',
          phoneNumber: '',
          email: '',
          isActive: true,
        });
      }
    }
  }, [open, initialData, companies, reset]);

  const onFormSubmit = async (formData: DepartmentFormData) => {
    await onSubmit({
      companyId: formData.companyId,
      code: formData.code.toUpperCase(),
      name: formData.name,
      managerName: formData.managerName || undefined,
      phoneNumber: formData.phoneNumber || undefined,
      email: formData.email || undefined,
      isActive: formData.isActive,
    });
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <div className="flex items-center gap-2 mb-1">
            <div className="p-2 rounded-lg bg-blue-100 dark:bg-blue-950 text-blue-600 dark:text-blue-400">
              <Building className="h-5 w-5" />
            </div>
            <DialogTitle className="text-base font-bold">
              {isEditing ? 'Cập Nhật Phòng Ban' : 'Thêm Mới Phòng Ban'}
            </DialogTitle>
          </div>
          <DialogDescription>
            {isEditing
              ? 'Chỉnh sửa thông tin phòng ban trực thuộc công ty.'
              : 'Chọn công ty cha và nhập thông tin phòng ban mới.'}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onFormSubmit)} className="space-y-3 py-1">
          {/* Chọn Công ty cha */}
          <div className="space-y-1">
            <label className="text-xs font-semibold text-foreground">
              Công ty trực thuộc <span className="text-destructive">*</span>
            </label>
            <Select
              value={selectedCompanyId}
              onValueChange={(val) => setValue('companyId', val, { shouldValidate: true })}
            >
              <SelectTrigger className="text-xs">
                <SelectValue placeholder="-- Chọn công ty trực thuộc --" />
              </SelectTrigger>
              <SelectContent>
                {companies.map((c) => (
                  <SelectItem key={c.id} value={c.id} className="text-xs">
                    {c.name} ({c.code})
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            {errors.companyId && (
              <p className="text-[11px] text-destructive">{errors.companyId.message}</p>
            )}
          </div>

          {/* Mã phòng ban & Tên phòng ban */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">
                Mã phòng ban <span className="text-destructive">*</span>
              </label>
              <Input
                {...register('code')}
                placeholder="VD: PB_KE_TOAN"
                className="uppercase text-xs"
                autoFocus={!isEditing}
              />
              {errors.code && (
                <p className="text-[11px] text-destructive">{errors.code.message}</p>
              )}
            </div>

            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">
                Tên phòng ban <span className="text-destructive">*</span>
              </label>
              <Input
                {...register('name')}
                placeholder="VD: Phòng Kế Toán"
                className="text-xs"
              />
              {errors.name && (
                <p className="text-[11px] text-destructive">{errors.name.message}</p>
              )}
            </div>
          </div>

          {/* Người quản lý & Số điện thoại */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">Trưởng phòng / Quản lý</label>
              <Input
                {...register('managerName')}
                placeholder="VD: Nguyễn Văn A"
                className="text-xs"
              />
            </div>

            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">Số điện thoại</label>
              <Input
                {...register('phoneNumber')}
                placeholder="VD: 0912345678"
                className="text-xs"
              />
              {errors.phoneNumber && (
                <p className="text-[11px] text-destructive">{errors.phoneNumber.message}</p>
              )}
            </div>
          </div>

          {/* Email */}
          <div className="space-y-1">
            <label className="text-xs font-semibold text-foreground">Email phòng ban</label>
            <Input
              {...register('email')}
              placeholder="ketoan@hoangphat.vn"
              type="email"
              className="text-xs"
            />
            {errors.email && (
              <p className="text-[11px] text-destructive">{errors.email.message}</p>
            )}
          </div>

          {/* Trạng thái hoạt động */}
          <div className="pt-2 border-t border-border flex items-center justify-between">
            <div>
              <span className="text-xs font-semibold text-foreground block">
                Trạng thái hoạt động
              </span>
              <span className="text-[11px] text-muted-foreground block">
                Cho phép khách hàng/nhân sự thuộc phòng ban này ra vào
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
