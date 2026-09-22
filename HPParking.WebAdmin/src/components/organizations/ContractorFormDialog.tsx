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
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Briefcase, Save } from 'lucide-react';
import type {
  ContractorDto,
  CreateContractorRequest,
  UpdateContractorRequest,
} from '@/types/masterData';

const contractorSchema = z.object({
  code: z
    .string()
    .trim()
    .min(2, 'Mã nhà thầu phải có ít nhất 2 ký tự')
    .max(50, 'Mã nhà thầu không được quá 50 ký tự')
    .regex(/^[A-Za-z0-9_-]+$/, 'Mã nhà thầu chỉ chứa chữ, số, gạch dưới hoặc gạch ngang'),
  name: z
    .string()
    .trim()
    .min(2, 'Tên nhà thầu phải có ít nhất 2 ký tự')
    .max(100, 'Tên nhà thầu không được quá 100 ký tự'),
  contactPerson: z.string().trim().optional(),
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

type ContractorFormData = z.infer<typeof contractorSchema>;

interface ContractorFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  initialData?: ContractorDto | null;
  onSubmit: (data: CreateContractorRequest | UpdateContractorRequest) => Promise<void>;
  isSubmitting?: boolean;
}

export function ContractorFormDialog({
  open,
  onOpenChange,
  initialData,
  onSubmit,
  isSubmitting = false,
}: ContractorFormDialogProps) {
  const isEditing = Boolean(initialData);

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    watch,
    formState: { errors },
  } = useForm<ContractorFormData>({
    resolver: zodResolver(contractorSchema),
    defaultValues: {
      code: '',
      name: '',
      contactPerson: '',
      phoneNumber: '',
      email: '',
      isActive: true,
    },
  });

  const isActive = watch('isActive');

  useEffect(() => {
    if (open) {
      if (initialData) {
        reset({
          code: initialData.code,
          name: initialData.name,
          contactPerson: initialData.contactPerson || '',
          phoneNumber: initialData.phoneNumber || '',
          email: initialData.email || '',
          isActive: initialData.isActive,
        });
      } else {
        reset({
          code: '',
          name: '',
          contactPerson: '',
          phoneNumber: '',
          email: '',
          isActive: true,
        });
      }
    }
  }, [open, initialData, reset]);

  const onFormSubmit = async (formData: ContractorFormData) => {
    await onSubmit({
      code: formData.code.toUpperCase(),
      name: formData.name,
      contactPerson: formData.contactPerson || undefined,
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
              <Briefcase className="h-5 w-5" />
            </div>
            <DialogTitle className="text-base font-bold">
              {isEditing ? 'Cập Nhật Nhà Thầu' : 'Thêm Mới Nhà Thầu'}
            </DialogTitle>
          </div>
          <DialogDescription>
            {isEditing
              ? 'Chỉnh sửa thông tin đơn vị nhà thầu / đối tác thi công.'
              : 'Nhập thông tin nhà thầu hoặc đơn vị thi công mới vào hệ thống.'}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onFormSubmit)} className="space-y-3 py-1">
          {/* Mã nhà thầu */}
          <div className="space-y-1">
            <label className="text-xs font-semibold text-foreground">
              Mã nhà thầu <span className="text-destructive">*</span>
            </label>
            <Input
              {...register('code')}
              placeholder="VD: NT_XAYDUNG_ABC"
              className="uppercase text-xs"
              autoFocus={!isEditing}
            />
            {errors.code && (
              <p className="text-[11px] text-destructive">{errors.code.message}</p>
            )}
          </div>

          {/* Tên nhà thầu */}
          <div className="space-y-1">
            <label className="text-xs font-semibold text-foreground">
              Tên nhà thầu / Đơn vị <span className="text-destructive">*</span>
            </label>
            <Input
              {...register('name')}
              placeholder="VD: Công ty CP Xây dựng ABC"
              className="text-xs"
            />
            {errors.name && (
              <p className="text-[11px] text-destructive">{errors.name.message}</p>
            )}
          </div>

          {/* Người đại diện liên hệ */}
          <div className="space-y-1">
            <label className="text-xs font-semibold text-foreground">Người liên hệ</label>
            <Input
              {...register('contactPerson')}
              placeholder="VD: Trần Văn B"
              className="text-xs"
            />
          </div>

          {/* Số điện thoại & Email */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">Số điện thoại</label>
              <Input
                {...register('phoneNumber')}
                placeholder="VD: 0901234567"
                className="text-xs"
              />
              {errors.phoneNumber && (
                <p className="text-[11px] text-destructive">{errors.phoneNumber.message}</p>
              )}
            </div>

            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">Email</label>
              <Input
                {...register('email')}
                placeholder="info@abc-corp.vn"
                type="email"
                className="text-xs"
              />
              {errors.email && (
                <p className="text-[11px] text-destructive">{errors.email.message}</p>
              )}
            </div>
          </div>

          {/* Trạng thái hoạt động */}
          <div className="pt-2 border-t border-border flex items-center justify-between">
            <div>
              <span className="text-xs font-semibold text-foreground block">
                Trạng thái hoạt động
              </span>
              <span className="text-[11px] text-muted-foreground block">
                Cho phép phương tiện của nhân sự nhà thầu ra vào bãi xe
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
