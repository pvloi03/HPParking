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
import { UserCheck, Save, Calendar, Clock, Building2, User } from 'lucide-react';
import { AvatarUploadField } from './AvatarUploadField';
import type { CompanyDto, DepartmentDto, ContractorDto } from '@/types/masterData';
import {
  ClientType,
  type ClientDto,
  type CreateClientRequest,
  type UpdateClientRequest,
} from '@/types/client';

export const CLIENT_TYPE_OPTIONS = [
  { value: ClientType.Employee, label: 'Cán bộ nhân viên' },
  { value: ClientType.Contractor, label: 'Nhân sự nhà thầu' },
  { value: ClientType.Visitor, label: 'Khách vãng lai' },
  { value: ClientType.VIP, label: 'Khách VIP' },
  { value: ClientType.Other, label: 'Khác' },
];

const clientSchema = z.object({
  code: z
    .string()
    .trim()
    .min(1, 'Số CCCD/Định danh cá nhân không được để trống')
    .regex(/^[0-9]{9,12}$/, 'Số CCCD/Định danh cá nhân phải gồm 9 đến 12 chữ số'),
  name: z
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
  type: z.number().int().min(0).max(4),
  gender: z.number().int().min(0).max(1),
  birthDay: z.string().min(1, 'Vui lòng chọn ngày tháng năm sinh'),
  address: z.string().trim().optional(),
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
  note: z.string().trim().max(500, 'Ghi chú không được quá 500 ký tự').optional(),
  expiredEnable: z.boolean(),
  expiredStartDay: z.string().optional(),
  expiredEndDay: z.string().optional(),
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

function toDateInputValue(isoStr?: string): string {
  if (!isoStr) return '';
  try {
    const d = new Date(isoStr);
    if (isNaN(d.getTime())) return '';
    return d.toISOString().split('T')[0];
  } catch {
    return '';
  }
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

  const defaultToday = new Date().toISOString().split('T')[0];

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
      name: '',
      phoneNumber: '',
      type: ClientType.Employee,
      gender: 1, // 1: Nam
      birthDay: '1995-01-01',
      address: '',
      email: '',
      companyId: 'none',
      departmentId: 'none',
      contractorId: 'none',
      note: '',
      expiredEnable: true,
      expiredStartDay: defaultToday,
      expiredEndDay: defaultToday,
      isActive: true,
    },
  });

  const selectedType = watch('type');
  const selectedGender = watch('gender');
  const selectedCompanyId = watch('companyId');
  const selectedDepartmentId = watch('departmentId');
  const selectedContractorId = watch('contractorId');
  const expiredEnable = watch('expiredEnable');
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
          code: initialData.code || '',
          name: initialData.name || '',
          phoneNumber: initialData.phoneNumber || '',
          type: initialData.type ?? ClientType.Employee,
          gender: initialData.gender ?? 1,
          birthDay: toDateInputValue(initialData.birthDay) || '1995-01-01',
          address: initialData.address || '',
          email: initialData.email || '',
          companyId: initialData.companyId || 'none',
          departmentId: initialData.departmentId || 'none',
          contractorId: initialData.contractorId || 'none',
          note: initialData.note || '',
          expiredEnable: initialData.expired?.enable ?? false,
          expiredStartDay: toDateInputValue(initialData.expired?.startDay) || defaultToday,
          expiredEndDay: toDateInputValue(initialData.expired?.endDay) || defaultToday,
          isActive: initialData.isActive,
        });
      } else {
        reset({
          code: '',
          name: '',
          phoneNumber: '',
          type: ClientType.Employee,
          gender: 1,
          birthDay: '1995-01-01',
          address: '',
          email: '',
          companyId: 'none',
          departmentId: 'none',
          contractorId: 'none',
          note: '',
          expiredEnable: true,
          expiredStartDay: defaultToday,
          expiredEndDay: defaultToday,
          isActive: true,
        });
      }
    }
  }, [open, initialData, reset, defaultToday]);

  const onFormSubmit = async (formData: ClientFormData) => {
    const birthDayIso = formData.birthDay
      ? new Date(formData.birthDay).toISOString()
      : new Date().toISOString();

    const payload: CreateClientRequest | UpdateClientRequest = {
      code: formData.code.trim(),
      name: formData.name.trim(),
      birthDay: birthDayIso,
      address: formData.address?.trim() || '',
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
      type: formData.type as ClientType,
      email: formData.email?.trim() || undefined,
      gender: formData.gender,
      phoneNumber: formData.phoneNumber.trim(),
      isActive: formData.isActive,
      note: formData.note?.trim() || undefined,
      expired: {
        enable: formData.expiredEnable,
        startDay: formData.expiredStartDay
          ? new Date(formData.expiredStartDay).toISOString()
          : new Date().toISOString(),
        endDay: formData.expiredEndDay
          ? new Date(`${formData.expiredEndDay}T23:59:59`).toISOString()
          : new Date().toISOString(),
      },
    };
    await onSubmit(payload, selectedAvatarFile);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-2xl max-h-[90vh] overflow-y-auto">
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
              ? 'Chỉnh sửa thông tin định danh CCCD, phân loại đối tượng, thời hạn ra vào và ảnh FaceID.'
              : 'Nhập đầy đủ thông tin định danh CCCD (bắt buộc), đối tượng và đơn vị trực thuộc.'}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onFormSubmit)} className="space-y-4 py-1">
          {/* 1. KHỐI ĐỊNH DANH & ẢNH FACEID */}
          <div className="p-3.5 rounded-xl border border-border bg-card space-y-3.5 shadow-xs">
            <div className="flex items-center gap-2 pb-1 border-b border-border/60">
              <User className="h-4 w-4 text-blue-600 dark:text-blue-400" />
              <span className="text-xs font-bold text-foreground uppercase tracking-wide">
                1. Thông tin cá nhân & Định danh CCCD
              </span>
            </div>

            {/* Ảnh đại diện Avatar & FaceID */}
            <AvatarUploadField
              currentUrl={initialData?.avatar}
              onFileSelected={setSelectedAvatarFile}
              disabled={isSubmitting}
            />

            {/* Số CCCD & Họ tên */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div className="space-y-1">
                <label className="text-xs font-semibold text-foreground flex items-center justify-between">
                  <span>
                    Số CCCD / Mã định danh <span className="text-destructive">*</span>
                  </span>
                  <span className="text-[10px] text-muted-foreground font-normal">
                    (9-12 chữ số)
                  </span>
                </label>
                <Input
                  {...register('code')}
                  placeholder="VD: 001200012345"
                  className="font-mono text-xs"
                  maxLength={12}
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
                  {...register('name')}
                  placeholder="VD: Nguyễn Văn Nam"
                  className="text-xs"
                />
                {errors.name && (
                  <p className="text-[11px] text-destructive">{errors.name.message}</p>
                )}
              </div>
            </div>

            {/* Số điện thoại, Giới tính & Ngày sinh */}
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
              {/* Số điện thoại */}
              <div className="space-y-1">
                <label className="text-xs font-semibold text-foreground">
                  Số điện thoại <span className="text-destructive">*</span>
                </label>
                <Input
                  {...register('phoneNumber')}
                  placeholder="VD: 0987654321"
                  className="text-xs font-mono"
                  maxLength={10}
                />
                {errors.phoneNumber && (
                  <p className="text-[11px] text-destructive">
                    {errors.phoneNumber.message}
                  </p>
                )}
              </div>

              {/* Giới tính */}
              <div className="space-y-1">
                <label className="text-xs font-semibold text-foreground">
                  Giới tính <span className="text-destructive">*</span>
                </label>
                <Select
                  value={String(selectedGender)}
                  onValueChange={(val) => setValue('gender', parseInt(val, 10))}
                >
                  <SelectTrigger className="text-xs bg-background">
                    <SelectValue placeholder="Chọn giới tính" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="1" className="text-xs">
                      Nam
                    </SelectItem>
                    <SelectItem value="0" className="text-xs">
                      Nữ
                    </SelectItem>
                  </SelectContent>
                </Select>
              </div>

              {/* Ngày sinh */}
              <div className="space-y-1">
                <label className="text-xs font-semibold text-foreground flex items-center gap-1">
                  <Calendar className="h-3 w-3 text-muted-foreground" />
                  <span>Ngày sinh</span>
                </label>
                <Input
                  {...register('birthDay')}
                  type="date"
                  className="text-xs font-mono"
                />
                {errors.birthDay && (
                  <p className="text-[11px] text-destructive">
                    {errors.birthDay.message}
                  </p>
                )}
              </div>
            </div>

            {/* Email & Địa chỉ */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div className="space-y-1">
                <label className="text-xs font-semibold text-foreground">
                  Email liên hệ
                </label>
                <Input
                  {...register('email')}
                  placeholder="nam.nguyen@hoangphat.vn"
                  type="email"
                  className="text-xs"
                />
                {errors.email && (
                  <p className="text-[11px] text-destructive">
                    {errors.email.message}
                  </p>
                )}
              </div>

              <div className="space-y-1">
                <label className="text-xs font-semibold text-foreground">
                  Địa chỉ thường trú / tạm trú
                </label>
                <Input
                  {...register('address')}
                  placeholder="VD: Hải Phòng, Việt Nam"
                  className="text-xs"
                />
              </div>
            </div>
          </div>

          {/* 2. KHỐI PHÂN LOẠI & ĐƠN VỊ TRỰC THUỘC */}
          <div className="p-3.5 rounded-xl border border-border bg-card space-y-3.5 shadow-xs">
            <div className="flex items-center gap-2 pb-1 border-b border-border/60">
              <Building2 className="h-4 w-4 text-emerald-600 dark:text-emerald-400" />
              <span className="text-xs font-bold text-foreground uppercase tracking-wide">
                2. Phân loại đối tượng & Đơn vị quản lý
              </span>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
              {/* Phân loại đối tượng */}
              <div className="space-y-1">
                <label className="text-xs font-semibold text-foreground">
                  Loại khách hàng <span className="text-destructive">*</span>
                </label>
                <Select
                  value={String(selectedType)}
                  onValueChange={(val) => {
                    const parsed = parseInt(val, 10);
                    setValue('type', parsed);
                  }}
                >
                  <SelectTrigger className="text-xs bg-background">
                    <SelectValue placeholder="Chọn loại khách hàng" />
                  </SelectTrigger>
                  <SelectContent>
                    {CLIENT_TYPE_OPTIONS.map((opt) => (
                      <SelectItem key={opt.value} value={String(opt.value)} className="text-xs">
                        {opt.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

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
                    <SelectValue placeholder="-- Không trực thuộc --" />
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
              <label className="text-xs font-medium text-foreground flex items-center justify-between">
                <span>Nhà thầu đối tác (Dành cho nhân sự nhà thầu)</span>
                {selectedType === ClientType.Contractor && (
                  <span className="text-[10px] text-amber-600 dark:text-amber-400 font-semibold">
                    ★ Khuyến nghị chọn cho đối tượng Nhà thầu
                  </span>
                )}
              </label>
              <Select
                value={selectedContractorId}
                onValueChange={(val) => setValue('contractorId', val)}
              >
                <SelectTrigger className="text-xs bg-background">
                  <SelectValue placeholder="-- Không phải nhân sự nhà thầu --" />
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

          {/* 3. KHỐI CẤU HÌNH THỜI HẠN RA VÀO (EXPIRED) & GHI CHÚ */}
          <div className="p-3.5 rounded-xl border border-border bg-card space-y-3.5 shadow-xs">
            <div className="flex items-center justify-between pb-1 border-b border-border/60">
              <div className="flex items-center gap-2">
                <Clock className="h-4 w-4 text-purple-600 dark:text-purple-400" />
                <span className="text-xs font-bold text-foreground uppercase tracking-wide">
                  3. Thời hạn ra vào & Trạng thái
                </span>
              </div>
              <div className="flex items-center gap-2">
                <span className="text-xs font-semibold text-foreground">
                  Không giới hạn thời gian:
                </span>
                <button
                  type="button"
                  onClick={() =>
                    setValue('expiredEnable', !expiredEnable, { shouldDirty: true })
                  }
                  className={`relative inline-flex h-5 w-9 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none ${
                    expiredEnable ? 'bg-emerald-600' : 'bg-muted'
                  }`}
                >
                  <span
                    className={`pointer-events-none inline-block h-4 w-4 transform rounded-full bg-white shadow-lg ring-0 transition duration-200 ease-in-out ${
                      expiredEnable ? 'translate-x-4' : 'translate-x-0'
                    }`}
                  />
                </button>
              </div>
            </div>

            {/* Chi tiết Từ ngày - Đến ngày */}
            {expiredEnable ? (
              <p className="text-[11px] text-emerald-700 dark:text-emerald-400 font-medium bg-emerald-50/80 dark:bg-emerald-950/30 p-2.5 rounded-lg border border-emerald-200 dark:border-emerald-900/50">
                ✓ Khách hàng được ra vào tự do (không áp dụng thời hạn hết hạn, bỏ qua giới hạn ngày vào/ra).
              </p>
            ) : (
              <div className="p-3 rounded-lg bg-amber-50/70 dark:bg-amber-950/20 border border-amber-200 dark:border-amber-900/50 space-y-2.5">
                <span className="text-[11px] font-semibold text-amber-900 dark:text-amber-300 block">
                  Áp dụng giới hạn thời gian (Hệ thống sẽ chặn ra vào nếu ngoài khoảng thời gian này):
                </span>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  <div className="space-y-1">
                    <label className="text-xs font-medium text-amber-900 dark:text-amber-300">
                      Từ ngày (StartDay)
                    </label>
                    <Input
                      {...register('expiredStartDay')}
                      type="date"
                      className="text-xs font-mono bg-background"
                    />
                  </div>
                  <div className="space-y-1">
                    <label className="text-xs font-medium text-amber-900 dark:text-amber-300">
                      Đến ngày (EndDay)
                    </label>
                    <Input
                      {...register('expiredEndDay')}
                      type="date"
                      className="text-xs font-mono bg-background"
                    />
                  </div>
                </div>
              </div>
            )}

            {/* Ghi chú */}
            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">Ghi chú bổ sung</label>
              <Input
                {...register('note')}
                placeholder="VD: Khách hàng thân thiết, nhà thầu dự án mở rộng xưởng B..."
                className="text-xs"
              />
              {errors.note && (
                <p className="text-[11px] text-destructive">{errors.note.message}</p>
              )}
            </div>

            {/* Trạng thái hoạt động */}
            <div className="pt-2 border-t border-border flex items-center justify-between">
              <div>
                <span className="text-xs font-semibold text-foreground block">
                  Kích hoạt hồ sơ
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
          </div>

          <DialogFooter className="pt-2 gap-2 sm:gap-0">
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
