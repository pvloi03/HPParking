import { useEffect, useState, useMemo } from 'react';
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
import {
  UserCheck,
  Save,
  Calendar,
  Clock,
  Building2,
  User,
  ShieldCheck,
  Car,
  Plus,
  Trash2,
  Bike,
  Info,
} from 'lucide-react';
import { AvatarUploadField } from './AvatarUploadField';
import { formatAvatarUrl } from '@/utils/formatAvatarUrl';
import { type Hn212CardData, parseCccdDate, base64ToFile } from '@/services/hn212Service';
import type { CompanyDto, DepartmentDto, ContractorDto } from '@/types/masterData';
import {
  VehicleType,
  normalizePlateNumber,
  type VehicleDto,
  type CreateVehicleRequest,
} from '@/types/vehicle';
import { vehicleApi } from '@/api/vehicleApi';
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
  hn212CardData?: Hn212CardData | null;
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

export interface PendingVehicleItem {
  id: string;
  plateNumber: string;
  type: VehicleType;
  note: string;
  isActive: boolean;
  error?: string;
}

export function ClientFormDialog({
  open,
  onOpenChange,
  initialData,
  hn212CardData,
  companies,
  departments,
  contractors,
  onSubmit,
  isSubmitting = false,
}: ClientFormDialogProps) {
  const isEditing = Boolean(initialData);
  const isFromHn212 = Boolean(hn212CardData);
  const [selectedAvatarFile, setSelectedAvatarFile] = useState<File | null>(null);

  // Quản lý danh sách phương tiện hiện có và phương tiện thêm mới
  const [existingVehicles, setExistingVehicles] = useState<VehicleDto[]>([]);
  const [, setIsLoadingVehicles] = useState(false);
  const [pendingVehicles, setPendingVehicles] = useState<PendingVehicleItem[]>([]);

  const handleAddVehicle = () => {
    setPendingVehicles((prev) => [
      ...prev,
      {
        id: `veh_${Date.now()}_${Math.random().toString(36).slice(2, 6)}`,
        plateNumber: '',
        type: VehicleType.Car,
        note: '',
        isActive: true,
      },
    ]);
  };

  const handleUpdateVehicle = (id: string, patch: Partial<PendingVehicleItem>) => {
    setPendingVehicles((prev) =>
      prev.map((v) => {
        if (v.id !== id) return v;
        const updated = { ...v, ...patch };
        if (patch.plateNumber !== undefined) {
          updated.plateNumber = patch.plateNumber.toUpperCase();
          if (updated.error) updated.error = undefined;
        }
        return updated;
      })
    );
  };

  const handleRemoveVehicle = (id: string) => {
    setPendingVehicles((prev) => prev.filter((v) => v.id !== id));
  };

  // Trích xuất file ảnh chân dung trong chip thẻ CCCD nếu có
  const cccdAvatarFile = useMemo(() => {
    const chipBase64 = hn212CardData?.ChipFaceBase64 || hn212CardData?.chipFaceBase64;
    if (!chipBase64) return null;
    try {
      return base64ToFile(chipBase64, 'avatar_cccd.jpg');
    } catch {
      return null;
    }
  }, [hn212CardData]);

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
      setPendingVehicles([]);
      if (initialData?.id) {
        setIsLoadingVehicles(true);
        vehicleApi
          .getByClientId(initialData.id)
          .then((res) => {
            setExistingVehicles(res || []);
          })
          .catch(() => {
            setExistingVehicles([]);
          })
          .finally(() => {
            setIsLoadingVehicles(false);
          });
      } else {
        setExistingVehicles([]);
      }

      if (initialData) {
        if (hn212CardData) {
          // Cập nhật hồ sơ hiện có với dữ liệu mới từ thẻ chip CCCD
          const cccdCode = hn212CardData.DocumentNumber || hn212CardData.documentNumber || initialData.code || '';
          const cccdName = hn212CardData.FullName || hn212CardData.fullName || initialData.name || '';
          const rawDob = hn212CardData.DateOfBirth || hn212CardData.dateOfBirth;
          const parsedDob = rawDob ? parseCccdDate(rawDob) : toDateInputValue(initialData.birthDay);
          const rawSex = (hn212CardData.Sex || hn212CardData.sex || '').toLowerCase();
          const genderVal = rawSex
            ? (rawSex.includes('nam') || rawSex === '1' || rawSex === 'm' ? 1 : 0)
            : (initialData.gender ?? 1);
          const cccdAddress =
            hn212CardData.PermanentAddress ||
            hn212CardData.permanentAddress ||
            hn212CardData.Hometown ||
            hn212CardData.hometown ||
            initialData.address ||
            '';

          // Nếu khách hàng đã tồn tại và đã có avatar thì giữ nguyên avatar hiện tại
          if (initialData.avatar) {
            setSelectedAvatarFile(null);
          } else if (cccdAvatarFile) {
            setSelectedAvatarFile(cccdAvatarFile);
          } else {
            setSelectedAvatarFile(null);
          }

          reset({
            code: cccdCode,
            name: cccdName,
            phoneNumber: initialData.phoneNumber || '',
            type: initialData.type ?? ClientType.Employee,
            gender: genderVal,
            birthDay: parsedDob || '1995-01-01',
            address: cccdAddress,
            email: initialData.email || '',
            companyId: initialData.companyId || 'none',
            departmentId: initialData.departmentId || 'none',
            contractorId: initialData.contractorId || 'none',
            note: initialData.note || 'Cập nhật từ thẻ chip CCCD (HN212)',
            expiredEnable: initialData.expired?.enable ?? false,
            expiredStartDay: toDateInputValue(initialData.expired?.startDay) || defaultToday,
            expiredEndDay: toDateInputValue(initialData.expired?.endDay) || defaultToday,
            isActive: initialData.isActive,
          });
        } else {
          setSelectedAvatarFile(null);
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
        }
      } else if (hn212CardData) {
        const cccdCode = hn212CardData.DocumentNumber || hn212CardData.documentNumber || '';
        const cccdName = hn212CardData.FullName || hn212CardData.fullName || '';
        const rawDob = hn212CardData.DateOfBirth || hn212CardData.dateOfBirth || '';
        const parsedDob = parseCccdDate(rawDob) || '1995-01-01';
        const rawSex = (hn212CardData.Sex || hn212CardData.sex || '').toLowerCase();
        const genderVal = rawSex.includes('nam') || rawSex === '1' || rawSex === 'm' ? 1 : 0;
        const cccdAddress =
          hn212CardData.PermanentAddress ||
          hn212CardData.permanentAddress ||
          hn212CardData.Hometown ||
          hn212CardData.hometown ||
          '';

        setSelectedAvatarFile(cccdAvatarFile);

        reset({
          code: cccdCode,
          name: cccdName,
          phoneNumber: '',
          type: ClientType.Employee,
          gender: genderVal,
          birthDay: parsedDob,
          address: cccdAddress,
          email: '',
          companyId: 'none',
          departmentId: 'none',
          contractorId: 'none',
          note: 'Đăng ký tự động từ thẻ chip CCCD (HN212)',
          expiredEnable: true,
          expiredStartDay: defaultToday,
          expiredEndDay: defaultToday,
          isActive: true,
        });
      } else {
        setSelectedAvatarFile(null);
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
  }, [open, initialData, hn212CardData, cccdAvatarFile, reset, defaultToday]);

  const onFormSubmit = async (formData: ClientFormData) => {
    // Kiểm tra dữ liệu các phương tiện mới nếu có thêm
    let hasVehicleError = false;
    const validatedVehicles = pendingVehicles.map((v) => {
      const cleanPlate = normalizePlateNumber(v.plateNumber);
      if (!cleanPlate || cleanPlate.length < 3) {
        hasVehicleError = true;
        return {
          ...v,
          error: 'Biển số xe phải có ít nhất 3 ký tự (VD: 30A-123.45 hoặc 29B12345)',
        };
      }
      return { ...v, error: undefined };
    });

    if (hasVehicleError) {
      setPendingVehicles(validatedVehicles);
      return;
    }

    const birthDayIso = formData.birthDay
      ? new Date(formData.birthDay).toISOString()
      : new Date().toISOString();

    const vehicleRequests: CreateVehicleRequest[] = pendingVehicles.map((v) => ({
      plateNumber: normalizePlateNumber(v.plateNumber),
      type: v.type,
      isActive: v.isActive,
      note: v.note.trim() || undefined,
    }));

    const payload: CreateClientRequest | UpdateClientRequest = {
      code: formData.code.trim(),
      name: formData.name.trim(),
      birthDay: birthDayIso,
      address: formData.address?.trim() || '',
      companyId:
        formData.type === ClientType.Employee &&
          formData.companyId &&
          formData.companyId !== 'none'
          ? formData.companyId
          : undefined,
      departmentId:
        formData.type === ClientType.Employee &&
          formData.departmentId &&
          formData.departmentId !== 'none'
          ? formData.departmentId
          : undefined,
      contractorId:
        formData.type === ClientType.Contractor &&
          formData.contractorId &&
          formData.contractorId !== 'none'
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
      vehicles: vehicleRequests.length > 0 ? vehicleRequests : undefined,
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
              {isEditing
                ? isFromHn212
                  ? 'Cập Nhật Hồ Sơ Khách Hàng (Từ Thẻ CCCD)'
                  : 'Cập Nhật Hồ Sơ Khách Hàng'
                : isFromHn212
                  ? 'Đăng Ký Khách Hàng (Từ Thẻ CCCD)'
                  : 'Đăng Ký Khách Hàng Mới'}
            </DialogTitle>
          </div>
          <DialogDescription>
            {isEditing
              ? isFromHn212
                ? 'Dữ liệu được cập nhật từ thẻ chip CCCD. Bạn có thể kiểm tra và chỉnh sửa trước khi lưu.'
                : 'Chỉnh sửa thông tin định danh CCCD, phân loại đối tượng, thời hạn ra vào và ảnh FaceID.'
              : isFromHn212
                ? 'Dữ liệu được trích xuất từ chip CCCD. Bạn có thể chỉnh sửa và bổ sung thêm thông tin.'
                : 'Nhập đầy đủ thông tin định danh CCCD (bắt buộc), đối tượng và đơn vị trực thuộc.'}
          </DialogDescription>
        </DialogHeader>

        {/* Banner thông báo dữ liệu từ đầu đọc HN212 */}
        {isFromHn212 && (
          <div className="flex items-center justify-between p-2.5 rounded-xl bg-blue-50 dark:bg-blue-950/40 border border-blue-200 dark:border-blue-800 text-blue-800 dark:text-blue-300 text-xs">
            <div className="flex items-center gap-2">
              <ShieldCheck className="h-4 w-4 text-blue-600 dark:text-blue-400 shrink-0" />
              <span className="font-medium">
                Dữ liệu được điền tự động từ thẻ chip CCCD (HN212). Bạn có thể chỉnh sửa nếu cần.
              </span>
            </div>
            <span className="text-[10px] font-semibold bg-blue-100 dark:bg-blue-900/60 px-2 py-0.5 rounded-full border border-blue-300 dark:border-blue-700 shrink-0">
              Tự động từ CCCD
            </span>
          </div>
        )}

        <form onSubmit={handleSubmit(onFormSubmit)} className="space-y-4 py-1">
          {/* 1. KHỐI ĐỊNH DANH & ẢNH FACEID */}
          <div className="p-3.5 rounded-xl border border-border bg-card space-y-3.5 shadow-xs">
            <div className="flex items-center gap-2 pb-1 border-b border-border/60">
              <User className="h-4 w-4 text-blue-600 dark:text-blue-400" />
              <span className="text-xs font-bold text-foreground uppercase tracking-wide">
                1. Thông tin cá nhân & Định danh CCCD
              </span>
            </div>

            {/* Ảnh đại diện Avatar & FaceID (Vẫn cho phép chỉnh sửa hoặc chụp lại) */}
            <AvatarUploadField
              currentUrl={formatAvatarUrl(initialData?.avatar, initialData?.updatedAt || initialData?.createdAt)}
              initialFile={selectedAvatarFile}
              cccdPhotoFile={cccdAvatarFile}
              onFileSelected={setSelectedAvatarFile}
              disabled={isSubmitting}
            />

            {/* Số CCCD & Họ tên */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div className="space-y-1">
                <label className="text-xs font-semibold text-foreground flex items-center justify-between h-5">
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
                  className="font-mono text-xs h-9"
                  maxLength={12}
                  autoFocus={!isEditing && !isFromHn212}
                />
                {errors.code && (
                  <p className="text-[11px] text-destructive">{errors.code.message}</p>
                )}
              </div>

              <div className="space-y-1">
                <label className="text-xs font-semibold text-foreground flex items-center h-5">
                  <span>Họ và tên <span className="text-destructive">*</span></span>
                </label>
                <Input
                  {...register('name')}
                  placeholder="VD: Nguyễn Văn Nam"
                  className="text-xs h-9"
                />
                {errors.name && (
                  <p className="text-[11px] text-destructive">{errors.name.message}</p>
                )}
              </div>
            </div>

            {/* Số điện thoại, Giới tính & Ngày sinh */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              {/* Số điện thoại */}
              <div className="space-y-1">
                <label className="text-xs font-semibold text-foreground flex items-center h-5">
                  <span>Số điện thoại <span className="text-destructive">*</span></span>
                </label>
                <Input
                  {...register('phoneNumber')}
                  placeholder="VD: 0987654321"
                  className="text-xs font-mono h-9"
                  maxLength={10}
                  autoFocus={isFromHn212}
                />
                {errors.phoneNumber && (
                  <p className="text-[11px] text-destructive">
                    {errors.phoneNumber.message}
                  </p>
                )}
              </div>

              {/* Giới tính & Ngày sinh (chia đôi đều nhau ở cột phải) */}
              <div className="grid grid-cols-2 gap-2">
                {/* Giới tính */}
                <div className="space-y-1">
                  <label className="text-xs font-semibold text-foreground flex items-center h-5">
                    <span>Giới tính <span className="text-destructive">*</span></span>
                  </label>
                  <Select
                    value={String(selectedGender)}
                    onValueChange={(val) => {
                      setValue('gender', parseInt(val, 10));
                    }}
                  >
                    <SelectTrigger className="text-xs bg-background h-9">
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
                  <label className="text-xs font-semibold text-foreground flex items-center gap-1 h-5">
                    <Calendar className="h-3.5 w-3.5 text-muted-foreground shrink-0" />
                    <span>Ngày sinh</span>
                  </label>
                  <Input
                    {...register('birthDay')}
                    type="date"
                    className="text-xs font-mono h-9"
                  />
                  {errors.birthDay && (
                    <p className="text-[11px] text-destructive">
                      {errors.birthDay.message}
                    </p>
                  )}
                </div>
              </div>
            </div>

            {/* Email & Địa chỉ */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div className="space-y-1">
                <label className="text-xs font-semibold text-foreground flex items-center h-5">
                  <span>Email liên hệ</span>
                </label>
                <Input
                  {...register('email')}
                  placeholder="nam.nguyen@hoangphat.vn"
                  type="email"
                  className="text-xs h-9"
                />
                {errors.email && (
                  <p className="text-[11px] text-destructive">
                    {errors.email.message}
                  </p>
                )}
              </div>

              <div className="space-y-1">
                <label className="text-xs font-semibold text-foreground flex items-center h-5">
                  <span>Địa chỉ thường trú / tạm trú</span>
                </label>
                <Input
                  {...register('address')}
                  placeholder="VD: Hải Phòng, Việt Nam"
                  className="text-xs h-9"
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

            <div className="space-y-1">
              {/* Phân loại đối tượng */}
              <label className="text-xs font-semibold text-foreground flex items-center h-5">
                <span>Loại khách hàng <span className="text-destructive">*</span></span>
              </label>
              <Select
                value={String(selectedType)}
                onValueChange={(val) => {
                  const parsed = parseInt(val, 10);
                  setValue('type', parsed, { shouldDirty: true });
                  if (parsed === ClientType.Employee) {
                    setValue('contractorId', 'none', { shouldDirty: true });
                  } else if (parsed === ClientType.Contractor) {
                    setValue('companyId', 'none', { shouldDirty: true });
                    setValue('departmentId', 'none', { shouldDirty: true });
                  } else {
                    setValue('companyId', 'none', { shouldDirty: true });
                    setValue('departmentId', 'none', { shouldDirty: true });
                    setValue('contractorId', 'none', { shouldDirty: true });
                  }
                }}
              >
                <SelectTrigger className="text-xs bg-background h-9">
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

            {/* Chỉ hiển thị chọn Công ty và Phòng ban khi chọn Cán bộ nhân viên */}
            {selectedType === ClientType.Employee && (
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-2 border-t border-border/50 animate-in fade-in-50 duration-200">
                {/* Công ty */}
                <div className="space-y-1">
                  <label className="text-xs font-medium text-foreground flex items-center justify-between h-5">
                    <span>Công ty trực thuộc</span>
                    <span className="text-[10px] text-blue-600 dark:text-blue-400 font-semibold">
                      (Cán bộ nhân viên)
                    </span>
                  </label>
                  <Select
                    value={selectedCompanyId}
                    onValueChange={(val) => {
                      setValue('companyId', val, { shouldDirty: true });
                      setValue('departmentId', 'none', { shouldDirty: true });
                    }}
                  >
                    <SelectTrigger className="text-xs bg-background h-9">
                      <SelectValue placeholder="-- Không trực thuộc công ty --" />
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
                  <label className="text-xs font-medium text-foreground flex items-center justify-between h-5">
                    <span>Phòng ban</span>
                    {selectedCompanyId && selectedCompanyId !== 'none' && (
                      <span className="text-[10px] text-muted-foreground">
                        ({filteredDepartments.length} phòng ban)
                      </span>
                    )}
                  </label>
                  <Select
                    value={selectedDepartmentId}
                    onValueChange={(val) => setValue('departmentId', val, { shouldDirty: true })}
                    disabled={!selectedCompanyId || selectedCompanyId === 'none'}
                  >
                    <SelectTrigger className="text-xs bg-background h-9 disabled:opacity-60 disabled:cursor-not-allowed">
                      <SelectValue
                        placeholder={
                          !selectedCompanyId || selectedCompanyId === 'none'
                            ? '-- Chọn công ty trước --'
                            : '-- Chọn phòng ban --'
                        }
                      />
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
            )}

            {/* Chỉ hiển thị chọn Nhà thầu khi chọn Nhân sự nhà thầu */}
            {selectedType === ClientType.Contractor && (
              <div className="space-y-1 pt-2 border-t border-border/50 animate-in fade-in-50 duration-200">
                <label className="text-xs font-medium text-foreground flex items-center justify-between h-5">
                  <span>Nhà thầu đối tác</span>
                  <span className="text-[10px] text-amber-600 dark:text-amber-400 font-semibold">
                    ★ Dành riêng cho đối tượng Nhà thầu
                  </span>
                </label>
                <Select
                  value={selectedContractorId}
                  onValueChange={(val) => setValue('contractorId', val, { shouldDirty: true })}
                >
                  <SelectTrigger className="text-xs bg-background h-9">
                    <SelectValue placeholder="-- Chọn nhà thầu --" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none" className="text-xs text-muted-foreground">
                      -- Không chọn nhà thầu --
                    </SelectItem>
                    {contractors.map((c) => (
                      <SelectItem key={c.id} value={c.id} className="text-xs">
                        {c.name} ({c.code})
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            )}

            {/* Ghi chú thông tin khi chọn các loại khách khác */}
            {selectedType !== ClientType.Employee && selectedType !== ClientType.Contractor && (
              <div className="p-2.5 rounded-lg bg-muted/40 border border-border/60 text-[11px] text-muted-foreground flex items-center gap-2 pt-2 border-t animate-in fade-in-50 duration-200">
                <Info className="h-3.5 w-3.5 text-blue-500 shrink-0" />
                <span>
                  Đối tượng <strong>{CLIENT_TYPE_OPTIONS.find((o) => o.value === selectedType)?.label || 'khác'}</strong> không áp dụng gắn Công ty, Phòng ban hoặc Nhà thầu.
                </span>
              </div>
            )}
          </div>

          {/* 3. KHỐI PHƯƠNG TIỆN ĐĂNG KÝ (TÙY CHỌN) */}
          <div className="p-3.5 rounded-xl border border-border bg-card space-y-3.5 shadow-xs">
            <div className="flex items-center justify-between pb-1 border-b border-border/60">
              <div className="flex items-center gap-2">
                <Car className="h-4 w-4 text-emerald-600 dark:text-emerald-400" />
                <span className="text-xs font-bold text-foreground uppercase tracking-wide">
                  3. Phương tiện đăng ký (Tùy chọn)
                </span>
                {(existingVehicles.length > 0 || pendingVehicles.length > 0) && (
                  <span className="text-[10px] font-semibold bg-emerald-100 dark:bg-emerald-950 text-emerald-700 dark:text-emerald-400 px-2 py-0.5 rounded-full border border-emerald-300 dark:border-emerald-800">
                    {existingVehicles.length + pendingVehicles.length} phương tiện
                  </span>
                )}
              </div>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={handleAddVehicle}
                className="text-xs h-7 gap-1 border-emerald-200 dark:border-emerald-800 text-emerald-700 dark:text-emerald-400 hover:bg-emerald-50 dark:hover:bg-emerald-950/40 cursor-pointer"
              >
                <Plus className="h-3.5 w-3.5" />
                <span>Thêm phương tiện</span>
              </Button>
            </div>

            {/* Danh sách xe hiện có (chỉ hiển thị khi cập nhật khách hàng) */}
            {isEditing && existingVehicles.length > 0 && (
              <div className="space-y-2">
                <span className="text-[11px] font-semibold text-muted-foreground block">
                  Xe đang đăng ký ({existingVehicles.length}):
                </span>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                  {existingVehicles.map((ev) => (
                    <div
                      key={ev.id}
                      className="flex items-center justify-between p-2.5 rounded-lg border border-border/80 bg-muted/40 text-xs"
                    >
                      <div className="flex items-center gap-2 min-w-0">
                        <div className="p-1 rounded bg-background border border-border text-foreground shrink-0">
                          {ev.type === VehicleType.Motorbike ? (
                            <Bike className="h-3.5 w-3.5" />
                          ) : (
                            <Car className="h-3.5 w-3.5" />
                          )}
                        </div>
                        <div className="min-w-0">
                          <span className="font-mono font-bold text-foreground block truncate">
                            {ev.plateNumber}
                          </span>
                          <span className="text-[10px] text-muted-foreground block truncate">
                            {ev.type === VehicleType.Car
                              ? 'Ô tô'
                              : ev.type === VehicleType.Motorbike
                                ? 'Xe máy'
                                : ev.type === VehicleType.Bicycle
                                  ? 'Xe đạp/điện'
                                  : 'Khác'}
                            {ev.note ? ` • ${ev.note}` : ''}
                          </span>
                        </div>
                      </div>
                      <span
                        className={`text-[10px] px-1.5 py-0.5 rounded-full font-medium shrink-0 ${ev.isActive
                          ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-950/60 dark:text-emerald-400'
                          : 'bg-muted text-muted-foreground'
                          }`}
                      >
                        {ev.isActive ? 'Hoạt động' : 'Tạm dừng'}
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* Danh sách các trường nhập phương tiện mới */}
            {pendingVehicles.length > 0 ? (
              <div className="space-y-3">
                {pendingVehicles.map((item, idx) => (
                  <div
                    key={item.id}
                    className="p-3 rounded-xl border border-blue-200/80 dark:border-blue-900/60 bg-blue-50/30 dark:bg-blue-950/20 space-y-3"
                  >
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-1.5">
                        <span className="text-[11px] font-bold text-blue-700 dark:text-blue-300">
                          Phương tiện mới #{idx + 1}
                        </span>
                      </div>
                      <Button
                        type="button"
                        variant="ghost"
                        size="icon"
                        onClick={() => handleRemoveVehicle(item.id)}
                        className="h-6 w-6 text-muted-foreground hover:text-destructive hover:bg-destructive/10 cursor-pointer"
                        title="Xóa phương tiện này"
                      >
                        <Trash2 className="h-3.5 w-3.5" />
                      </Button>
                    </div>

                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                      {/* Biển số xe */}
                      <div className="space-y-1">
                        <label className="text-xs font-semibold text-foreground flex items-center justify-between">
                          <span>
                            Biển số xe <span className="text-destructive">*</span>
                          </span>
                        </label>
                        <Input
                          value={item.plateNumber}
                          onChange={(e) =>
                            handleUpdateVehicle(item.id, { plateNumber: e.target.value })
                          }
                          placeholder="VD: 30A-123.45 hoặc 29B12345"
                          className={`text-xs font-mono font-bold uppercase tracking-wider bg-background h-9 ${item.error ? 'border-destructive focus-visible:ring-destructive' : ''
                            }`}
                        />
                        {item.error && (
                          <p className="text-[11px] text-destructive">{item.error}</p>
                        )}
                      </div>

                      {/* Loại phương tiện */}
                      <div className="space-y-1">
                        <label className="text-xs font-semibold text-foreground">
                          Loại phương tiện
                        </label>
                        <Select
                          value={String(item.type)}
                          onValueChange={(val) =>
                            handleUpdateVehicle(item.id, { type: Number(val) as VehicleType })
                          }
                        >
                          <SelectTrigger className="text-xs bg-background h-9">
                            <SelectValue />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value={String(VehicleType.Car)} className="text-xs">
                              🚗 Ô tô
                            </SelectItem>
                            <SelectItem value={String(VehicleType.Motorbike)} className="text-xs">
                              🛵 Xe máy
                            </SelectItem>
                            <SelectItem value={String(VehicleType.Bicycle)} className="text-xs">
                              🚲 Xe đạp / Xe điện
                            </SelectItem>
                            <SelectItem value={String(VehicleType.Other)} className="text-xs">
                              🚚 Khác
                            </SelectItem>
                          </SelectContent>
                        </Select>
                      </div>
                    </div>

                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 items-end">
                      {/* Ghi chú */}
                      <div className="space-y-1">
                        <label className="text-xs font-semibold text-foreground">
                          Ghi chú phương tiện
                        </label>
                        <Input
                          value={item.note}
                          onChange={(e) =>
                            handleUpdateVehicle(item.id, { note: e.target.value })
                          }
                          placeholder="VD: Xe cá nhân, xe cơ quan..."
                          className="text-xs bg-background h-9"
                        />
                      </div>

                      {/* Trạng thái hoạt động của xe */}
                      <div className="flex items-center justify-between h-9 px-3 rounded-lg border border-border bg-background">
                        <span className="text-xs font-medium text-foreground">
                          Kích hoạt (Cho phép gửi)
                        </span>
                        <button
                          type="button"
                          onClick={() =>
                            handleUpdateVehicle(item.id, { isActive: !item.isActive })
                          }
                          className={`relative inline-flex h-4 w-7 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none ${item.isActive ? 'bg-emerald-600' : 'bg-muted'
                            }`}
                        >
                          <span
                            className={`pointer-events-none inline-block h-3 w-3 transform rounded-full bg-white shadow-sm ring-0 transition duration-200 ease-in-out ${item.isActive ? 'translate-x-3' : 'translate-x-0'
                              }`}
                          />
                        </button>
                      </div>
                    </div>
                  </div>
                ))}

                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={handleAddVehicle}
                  className="w-full text-xs h-8 border-dashed border-border hover:border-emerald-500 hover:text-emerald-600 gap-1.5 cursor-pointer"
                >
                  <Plus className="h-3.5 w-3.5" />
                  <span>Thêm phương tiện khác</span>
                </Button>
              </div>
            ) : (
              (!isEditing || existingVehicles.length === 0) && (
                <div className="border border-dashed border-border rounded-xl p-4 flex flex-col items-center justify-center text-center space-y-2 bg-muted/20">
                  <div className="h-8 w-8 rounded-full bg-muted flex items-center justify-center text-muted-foreground">
                    <Car className="h-4 w-4" />
                  </div>
                  <div className="space-y-0.5">
                    <p className="text-xs font-medium text-foreground">
                      Chưa thêm phương tiện nào
                    </p>
                    <p className="text-[11px] text-muted-foreground">
                      Bấm nút bên dưới để nhập biển số và loại phương tiện gán cho khách hàng này.
                    </p>
                  </div>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={handleAddVehicle}
                    className="text-xs h-8 gap-1.5 cursor-pointer mt-1"
                  >
                    <Plus className="h-3.5 w-3.5" />
                    <span>Thêm phương tiện</span>
                  </Button>
                </div>
              )
            )}
          </div>

          {/* 4. KHỐI CẤU HÌNH THỜI HẠN RA VÀO (EXPIRED) & GHI CHÚ */}
          <div className="p-3.5 rounded-xl border border-border bg-card space-y-3.5 shadow-xs">
            <div className="flex items-center justify-between pb-1 border-b border-border/60">
              <div className="flex items-center gap-2">
                <Clock className="h-4 w-4 text-purple-600 dark:text-purple-400" />
                <span className="text-xs font-bold text-foreground uppercase tracking-wide">
                  4. Thời hạn ra vào & Trạng thái
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
                  className={`relative inline-flex h-5 w-9 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none ${expiredEnable ? 'bg-emerald-600' : 'bg-muted'
                    }`}
                >
                  <span
                    className={`pointer-events-none inline-block h-4 w-4 transform rounded-full bg-white shadow-lg ring-0 transition duration-200 ease-in-out ${expiredEnable ? 'translate-x-4' : 'translate-x-0'
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
                    <label className="text-xs font-medium text-amber-900 dark:text-amber-300 flex items-center h-5">
                      <span>Từ ngày (StartDay)</span>
                    </label>
                    <Input
                      {...register('expiredStartDay')}
                      type="date"
                      className="text-xs font-mono bg-background h-9"
                    />
                  </div>
                  <div className="space-y-1">
                    <label className="text-xs font-medium text-amber-900 dark:text-amber-300 flex items-center h-5">
                      <span>Đến ngày (EndDay)</span>
                    </label>
                    <Input
                      {...register('expiredEndDay')}
                      type="date"
                      className="text-xs font-mono bg-background h-9"
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
                className={`relative inline-flex h-5 w-9 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none ${isActive ? 'bg-blue-600' : 'bg-muted'
                  }`}
              >
                <span
                  className={`pointer-events-none inline-block h-4 w-4 transform rounded-full bg-white shadow-lg ring-0 transition duration-200 ease-in-out ${isActive ? 'translate-x-4' : 'translate-x-0'
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
