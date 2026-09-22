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
import { UserCheck, Save } from 'lucide-react';
import { AvatarUploadField } from './AvatarUploadField';
import type { CompanyDto, DepartmentDto, ContractorDto } from '@/types/masterData';
import type {
  ClientDto,
  CreateClientRequest,
  UpdateClientRequest,
} from '@/types/client';

const clientSchema = z.object({
  code: z
    .string()
    .trim()
    .min(2, 'Mã khách hàng phải có ít nhất 2 ký tự')
    .max(50, 'Mã khách hàng không được quá 50 ký tự')
    .regex(/^[A-Za-z0-9_-]+$/, 'Mã khách hàng chỉ chứa chữ, số, gạch dưới hoặc gạch ngang'),
  fullName: z
    .string()
    .trim()
    .min(2, 'Họ và tên phải có ít nhất 2 ký tự')
    .max(100, 'Họ và tên không được quá 100 ký tự'),
  phoneNumber: z
    .string()
    .trim()
    .regex(
      /^(03|05|07|08|09)\d{8}$/,
      'Số điện thoại di động không hợp lệ (gồm 10 số, bắt đầu bằng 03, 05, 07, 08, 09)'
    ),
  identityNumber: z
    .string()
    .trim()
    .regex(/^(\d{9}|\d{12})$/, 'Số CCCD/Định danh phải gồm đúng 9 hoặc 12 chữ số'),
  email: z
    .string()
    .trim()
    .optional()
    .refine((val) => !val || z.string().email().safeParse(val).success, {
      message: 'Email không đúng định dạng',
    }),
  companyId: z.string().optional(),
  departmentId: z.string().optional(),
  contractorId: z.string().optional(),
  isActive: z.boolean(),
});

type ClientFormData = z.infer<typeof clientSchema>;

interface ClientFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  initialData?: ClientDto | null;
  companies: CompanyDto[];
  departments: DepartmentDto[];
  contractors: ContractorDto[];
  onSubmit: (
    data: CreateClientRequest | UpdateClientRequest,
    avatarFile: File | null
  ) => Promise<void>;
  isSubmitting?: boolean;
}

export function ClientFormDialog({
  open,
  onOpenChange,
  initialData,
  companies,
  departments,
  contractors,
  onSubmit,
  isSubmitting = false,
}: ClientFormDialogProps) {
  const isEditing = Boolean(initialData);
  const [selectedAvatarFile, setSelectedAvatarFile] = useState<File | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    watch,
    formState: { errors },
  } = useForm<ClientFormData>({
    resolver: zodResolver(clientSchema),
    defaultValues: {
      code: '',
      fullName: '',
      phoneNumber: '',
      identityNumber: '',
      email: '',
      companyId: 'none',
      departmentId: 'none',
      contractorId: 'none',
      isActive: true,
    },
  });

  const selectedCompanyId = watch('companyId');
  const selectedDepartmentId = watch('departmentId');
  const selectedContractorId = watch('contractorId');
  const isActive = watch('isActive');

  // Lọc danh sách phòng ban theo công ty đã chọn
  const filteredDepartments =
    selectedCompanyId && selectedCompanyId !== 'none'
      ? departments.filter((d) => d.companyId === selectedCompanyId)
      : departments;

  useEffect(() => {
    if (open) {
      setSelectedAvatarFile(null);
      if (initialData) {
        reset({
          code: initialData.code,
          fullName: initialData.fullName,
          phoneNumber: initialData.phoneNumber,
          identityNumber: initialData.identityNumber,
          email: initialData.email || '',
          companyId: initialData.companyId || 'none',
          departmentId: initialData.departmentId || 'none',
          contractorId: initialData.contractorId || 'none',
          isActive: initialData.isActive,
        });
      } else {
        reset({
          code: '',
          fullName: '',
          phoneNumber: '',
          identityNumber: '',
          email: '',
          companyId: 'none',
          departmentId: 'none',
          contractorId: 'none',
          isActive: true,
        });
      }
    }
  }, [open, initialData, reset]);

  const onFormSubmit = async (formData: ClientFormData) => {
    const payload: CreateClientRequest | UpdateClientRequest = {
      code: formData.code.toUpperCase(),
      fullName: formData.fullName,
      phoneNumber: formData.phoneNumber,
      identityNumber: formData.identityNumber,
      email: formData.email || undefined,
      companyId:
        formData.companyId && formData.companyId !== 'none'
          ? formData.companyId
          : undefined,
      departmentId:
        formData.departmentId && formData.departmentId !== 'none'
          ? formData.departmentId
          : undefined,
      contractorId:
        formData.contractorId && formData.contractorId !== 'none'
          ? formData.contractorId
          : undefined,
      isActive: formData.isActive,
    };
    await onSubmit(payload, selectedAvatarFile);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <div className="flex items-center gap-2 mb-1">
            <div className="p-2 rounded-lg bg-blue-100 dark:bg-blue-950 text-blue-600 dark:text-blue-400">
              <UserCheck className="h-5 w-5" />
            </div>
            <DialogTitle className="text-base font-bold">
              {isEditing ? 'Cập Nhật Hồ Sơ Khách Hàng' : 'Đăng Ký Khách Hàng Mới'}
            </DialogTitle>
          </div>
          <DialogDescription>
            {isEditing
              ? 'Chỉnh sửa thông tin liên lạc, đơn vị trực thuộc và ảnh chân dung FaceID.'
              : 'Nhập thông tin cá nhân, CCCD và chọn đơn vị trực thuộc của khách hàng.'}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onFormSubmit)} className="space-y-3.5 py-1">
          {/* Ảnh đại diện Avatar & FaceID */}
          <AvatarUploadField
            currentUrl={initialData?.avatarUrl}
            onFileSelected={setSelectedAvatarFile}
            disabled={isSubmitting}
          />

          {/* Mã khách hàng & Họ tên */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">
                Mã khách hàng <span className="text-destructive">*</span>
              </label>
              <Input
                {...register('code')}
                placeholder="VD: KH_HOANGPHAT_01"
                className="uppercase text-xs"
                autoFocus={!isEditing}
              />
              {errors.code && (
                <p className="text-[11px] text-destructive">{errors.code.message}</p>
              )}
            </div>

            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">
                Họ và tên <span className="text-destructive">*</span>
              </label>
              <Input
                {...register('fullName')}
                placeholder="VD: Nguyễn Văn Nam"
                className="text-xs"
              />
              {errors.fullName && (
                <p className="text-[11px] text-destructive">{errors.fullName.message}</p>
              )}
            </div>
          </div>

          {/* Số điện thoại & CCCD/Định danh */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">
                Số điện thoại (SĐT) <span className="text-destructive">*</span>
              </label>
              <Input
                {...register('phoneNumber')}
                placeholder="VD: 0987654321"
                className="text-xs font-mono"
              />
              <p className="text-[10px] text-muted-foreground">
                Dùng làm định danh nhận diện FaceID và đăng nhập ứng dụng.
              </p>
              {errors.phoneNumber && (
                <p className="text-[11px] text-destructive">{errors.phoneNumber.message}</p>
              )}
            </div>

            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">
                Số CCCD / Mã định danh <span className="text-destructive">*</span>
              </label>
              <Input
                {...register('identityNumber')}
                placeholder="VD: 001234567890"
                className="text-xs font-mono"
              />
              <p className="text-[10px] text-muted-foreground">
                9 hoặc 12 số định danh công dân Việt Nam.
              </p>
              {errors.identityNumber && (
                <p className="text-[11px] text-destructive">{errors.identityNumber.message}</p>
              )}
            </div>
          </div>

          {/* Email liên hệ */}
          <div className="space-y-1">
            <label className="text-xs font-semibold text-foreground">Email liên hệ</label>
            <Input
              {...register('email')}
              placeholder="nam.nguyen@hoangphat.vn"
              type="email"
              className="text-xs"
            />
            {errors.email && (
              <p className="text-[11px] text-destructive">{errors.email.message}</p>
            )}
          </div>

          {/* Đơn vị trực thuộc: Công ty / Phòng ban / Nhà thầu */}
          <div className="p-3 rounded-xl border border-border bg-muted/20 space-y-3">
            <span className="text-xs font-bold text-foreground block">
              Đơn vị trực thuộc / Cơ cấu tổ chức
            </span>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              {/* Công ty */}
              <div className="space-y-1">
                <label className="text-xs font-medium text-foreground">Công ty</label>
                <Select
                  value={selectedCompanyId}
                  onValueChange={(val) => {
                    setValue('companyId', val);
                    setValue('departmentId', 'none');
                  }}
                >
                  <SelectTrigger className="text-xs bg-background">
                    <SelectValue placeholder="-- Không chọn công ty --" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none" className="text-xs text-muted-foreground">
                      -- Không trực thuộc công ty --
                    </SelectItem>
                    {companies.map((c) => (
                      <SelectItem key={c.id} value={c.id} className="text-xs">
                        {c.name} ({c.code})
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              {/* Phòng ban */}
              <div className="space-y-1">
                <label className="text-xs font-medium text-foreground">Phòng ban</label>
                <Select
                  value={selectedDepartmentId}
                  onValueChange={(val) => setValue('departmentId', val)}
                >
                  <SelectTrigger className="text-xs bg-background">
                    <SelectValue placeholder="-- Không chọn phòng ban --" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none" className="text-xs text-muted-foreground">
                      -- Không trực thuộc phòng ban --
                    </SelectItem>
                    {filteredDepartments.map((d) => (
                      <SelectItem key={d.id} value={d.id} className="text-xs">
                        {d.name} ({d.code})
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>

            {/* Nhà thầu thi công */}
            <div className="space-y-1">
              <label className="text-xs font-medium text-foreground">
                Nhà thầu đối tác (Dành cho nhân sự nhà thầu)
              </label>
              <Select
                value={selectedContractorId}
                onValueChange={(val) => setValue('contractorId', val)}
              >
                <SelectTrigger className="text-xs bg-background">
                  <SelectValue placeholder="-- Không chọn nhà thầu --" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="none" className="text-xs text-muted-foreground">
                    -- Không phải nhân sự nhà thầu --
                  </SelectItem>
                  {contractors.map((c) => (
                    <SelectItem key={c.id} value={c.id} className="text-xs">
                      {c.name} ({c.code})
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          {/* Trạng thái hoạt động */}
          <div className="pt-2 border-t border-border flex items-center justify-between">
            <div>
              <span className="text-xs font-semibold text-foreground block">
                Trạng thái hoạt động
              </span>
              <span className="text-[11px] text-muted-foreground block">
                Cho phép khách hàng ra vào bãi đỗ xe và sử dụng quyền gửi xe
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
