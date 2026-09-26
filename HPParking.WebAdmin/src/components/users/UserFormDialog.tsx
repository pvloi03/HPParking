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
import { UserCog, Save, Lock } from 'lucide-react';
import { UserRole, type UserDto, type CreateUserRequest, type UpdateUserRequest } from '@/types/user';

const createUserSchema = z.object({
  username: z
    .string()
    .trim()
    .min(3, 'Tên đăng nhập phải có ít nhất 3 ký tự')
    .max(50, 'Tên đăng nhập không được quá 50 ký tự')
    .regex(/^[a-zA-Z0-9_.-]+$/, 'Tên đăng nhập chỉ chứa chữ, số, dấu chấm, gạch dưới hoặc gạch ngang'),
  password: z
    .string()
    .min(6, 'Mật khẩu phải có tối thiểu 6 ký tự (theo chuẩn ADR 0025)')
    .max(100, 'Mật khẩu không được quá 100 ký tự'),
  fullName: z
    .string()
    .trim()
    .min(2, 'Họ và tên phải có ít nhất 2 ký tự')
    .max(100, 'Họ và tên không được quá 100 ký tự'),
  email: z
    .string()
    .trim()
    .optional()
    .refine((val) => !val || z.string().email().safeParse(val).success, {
      message: 'Email không đúng định dạng',
    }),
  phoneNumber: z
    .string()
    .trim()
    .optional()
    .refine((val) => !val || /^[0-9+.\s()-]{8,20}$/.test(val), {
      message: 'Số điện thoại không đúng định dạng',
    }),
  role: z.coerce.number().refine((val) => [1, 2, 3].includes(val), {
    message: 'Vui lòng chọn vai trò hợp lệ',
  }),
  isActive: z.boolean(),
  note: z.string().trim().max(500, 'Ghi chú không quá 500 ký tự').optional(),
});

const updateUserSchema = z.object({
  fullName: z
    .string()
    .trim()
    .min(2, 'Họ và tên phải có ít nhất 2 ký tự')
    .max(100, 'Họ và tên không được quá 100 ký tự'),
  email: z
    .string()
    .trim()
    .optional()
    .refine((val) => !val || z.string().email().safeParse(val).success, {
      message: 'Email không đúng định dạng',
    }),
  phoneNumber: z
    .string()
    .trim()
    .optional()
    .refine((val) => !val || /^[0-9+.\s()-]{8,20}$/.test(val), {
      message: 'Số điện thoại không đúng định dạng',
    }),
  role: z.coerce.number().refine((val) => [1, 2, 3].includes(val), {
    message: 'Vui lòng chọn vai trò hợp lệ',
  }),
  isActive: z.boolean(),
  note: z.string().trim().max(500, 'Ghi chú không quá 500 ký tự').optional(),
});

type CreateUserFormData = z.infer<typeof createUserSchema>;

interface UserFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  initialData?: UserDto | null;
  onSubmit: (data: CreateUserRequest | UpdateUserRequest) => Promise<void>;
  isSubmitting?: boolean;
}

export function UserFormDialog({
  open,
  onOpenChange,
  initialData,
  onSubmit,
  isSubmitting = false,
}: UserFormDialogProps) {
  const isEditing = Boolean(initialData);

  // Convert role to number safely
  const getRoleNumber = (roleVal?: UserRole | string): number => {
    if (typeof roleVal === 'number') return roleVal;
    if (roleVal === 'Admin') return UserRole.Admin;
    if (roleVal === 'Manager') return UserRole.Manager;
    return UserRole.Viewer;
  };

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    watch,
    formState: { errors },
  } = useForm<CreateUserFormData>({
    resolver: zodResolver(isEditing ? (updateUserSchema as any) : createUserSchema),
    defaultValues: {
      username: '',
      password: '',
      fullName: '',
      email: '',
      phoneNumber: '',
      role: UserRole.Viewer,
      isActive: true,
      note: '',
    },
  });

  const isActive = watch('isActive');
  const selectedRole = watch('role');

  useEffect(() => {
    if (open) {
      if (initialData) {
        reset({
          username: initialData.username,
          password: '',
          fullName: initialData.fullName,
          email: initialData.email || '',
          phoneNumber: initialData.phoneNumber || '',
          role: getRoleNumber(initialData.role),
          isActive: initialData.isActive,
          note: initialData.note || '',
        });
      } else {
        reset({
          username: '',
          password: '',
          fullName: '',
          email: '',
          phoneNumber: '',
          role: UserRole.Viewer,
          isActive: true,
          note: '',
        });
      }
    }
  }, [open, initialData, reset]);

  const onFormSubmit = async (data: CreateUserFormData) => {
    if (isEditing) {
      const updatePayload: UpdateUserRequest = {
        fullName: data.fullName,
        email: data.email?.trim() || null,
        phoneNumber: data.phoneNumber?.trim() || null,
        role: Number(data.role) as UserRole,
        isActive: data.isActive,
        note: data.note?.trim() || null,
      };
      await onSubmit(updatePayload);
    } else {
      const createPayload: CreateUserRequest = {
        username: data.username.trim(),
        password: data.password,
        fullName: data.fullName.trim(),
        email: data.email?.trim() || null,
        phoneNumber: data.phoneNumber?.trim() || null,
        role: Number(data.role) as UserRole,
        isActive: data.isActive,
        note: data.note?.trim() || null,
      };
      await onSubmit(createPayload);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <div className="flex items-center gap-2 mb-1">
            <div className="p-2 rounded-lg bg-blue-100 dark:bg-blue-950 text-blue-600 dark:text-blue-400">
              <UserCog className="h-5 w-5" />
            </div>
            <DialogTitle className="text-base font-bold">
              {isEditing ? 'Cập Nhật Tài Khoản Người Dùng' : 'Thêm Mới Tài Khoản Người Dùng'}
            </DialogTitle>
          </div>
          <DialogDescription>
            {isEditing
              ? `Chỉnh sửa thông tin tài khoản "${initialData?.username}".`
              : 'Tạo tài khoản mới để đăng nhập và vận hành hệ thống bãi xe.'}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onFormSubmit)} className="space-y-3.5 py-1">
          {/* Tên đăng nhập */}
          <div className="space-y-1">
            <label className="text-xs font-semibold text-foreground">
              Tên đăng nhập <span className="text-destructive">*</span>
            </label>
            <Input
              {...register('username')}
              placeholder="VD: operator01, admin_hp"
              className="text-xs"
              disabled={isEditing}
              autoFocus={!isEditing}
            />
            {errors.username && (
              <p className="text-[11px] text-destructive">{errors.username.message}</p>
            )}
            {isEditing && (
              <p className="text-[11px] text-muted-foreground">
                Tên đăng nhập là định danh duy nhất và không thể thay đổi sau khi tạo.
              </p>
            )}
          </div>

          {/* Mật khẩu khởi tạo (chỉ khi tạo mới) */}
          {!isEditing && (
            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">
                Mật khẩu khởi tạo <span className="text-destructive">*</span>
              </label>
              <div className="relative">
                <Lock className="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted-foreground pointer-events-none" />
                <Input
                  type="password"
                  {...register('password')}
                  placeholder="Tối thiểu 6 ký tự"
                  className="pl-8 text-xs"
                />
              </div>
              {errors.password && (
                <p className="text-[11px] text-destructive">{errors.password.message}</p>
              )}
              <p className="text-[11px] text-muted-foreground">
                Theo chuẩn ADR 0025, mật khẩu tối thiểu 6 ký tự giúp thao tác tại bốt thuận tiện.
              </p>
            </div>
          )}

          {/* Họ và tên */}
          <div className="space-y-1">
            <label className="text-xs font-semibold text-foreground">
              Họ và tên <span className="text-destructive">*</span>
            </label>
            <Input
              {...register('fullName')}
              placeholder="VD: Nguyễn Văn A"
              className="text-xs"
            />
            {errors.fullName && (
              <p className="text-[11px] text-destructive">{errors.fullName.message}</p>
            )}
          </div>

          {/* Vai trò người dùng */}
          <div className="space-y-1">
            <label className="text-xs font-semibold text-foreground">
              Vai trò phân quyền <span className="text-destructive">*</span>
            </label>
            <div className="grid grid-cols-3 gap-2">
              <button
                type="button"
                onClick={() => setValue('role', UserRole.Viewer)}
                className={`p-2.5 rounded-lg border text-left cursor-pointer transition-all ${
                  Number(selectedRole) === UserRole.Viewer
                    ? 'border-blue-600 bg-blue-50/50 dark:bg-blue-950/30 text-blue-700 dark:text-blue-300 font-semibold ring-1 ring-blue-600'
                    : 'border-border bg-card hover:bg-muted/50 text-foreground'
                }`}
              >
                <div className="text-xs font-medium">Người xem</div>
                <div className="text-[10px] text-muted-foreground">Chỉ xem báo cáo</div>
              </button>

              <button
                type="button"
                onClick={() => setValue('role', UserRole.Manager)}
                className={`p-2.5 rounded-lg border text-left cursor-pointer transition-all ${
                  Number(selectedRole) === UserRole.Manager
                    ? 'border-blue-600 bg-blue-50/50 dark:bg-blue-950/30 text-blue-700 dark:text-blue-300 font-semibold ring-1 ring-blue-600'
                    : 'border-border bg-card hover:bg-muted/50 text-foreground'
                }`}
              >
                <div className="text-xs font-medium">Quản lý</div>
                <div className="text-[10px] text-muted-foreground">Vận hành bãi xe</div>
              </button>

              <button
                type="button"
                onClick={() => setValue('role', UserRole.Admin)}
                className={`p-2.5 rounded-lg border text-left cursor-pointer transition-all ${
                  Number(selectedRole) === UserRole.Admin
                    ? 'border-rose-600 bg-rose-50/50 dark:bg-rose-950/30 text-rose-700 dark:text-rose-300 font-semibold ring-1 ring-rose-600'
                    : 'border-border bg-card hover:bg-muted/50 text-foreground'
                }`}
              >
                <div className="text-xs font-medium">Admin</div>
                <div className="text-[10px] text-muted-foreground">Toàn quyền hệ thống</div>
              </button>
            </div>
            {errors.role && (
              <p className="text-[11px] text-destructive">{errors.role.message}</p>
            )}
          </div>

          {/* Email & Số điện thoại (2 cột) */}
          <div className="grid grid-cols-2 gap-2.5">
            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">Email</label>
              <Input
                type="email"
                {...register('email')}
                placeholder="name@example.com"
                className="text-xs"
              />
              {errors.email && (
                <p className="text-[11px] text-destructive">{errors.email.message}</p>
              )}
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

          {/* Ghi chú */}
          <div className="space-y-1">
            <label className="text-xs font-semibold text-foreground">Ghi chú</label>
            <textarea
              {...register('note')}
              placeholder="Ghi chú về phân công bốt trực, chức vụ, bộ phận..."
              rows={2}
              className="flex min-h-[60px] w-full rounded-md border border-input bg-background px-3 py-2 text-xs shadow-xs placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50 resize-none"
            />
            {errors.note && (
              <p className="text-[11px] text-destructive">{errors.note.message}</p>
            )}
          </div>

          {/* Trạng thái hoạt động */}
          <div className="flex items-center justify-between p-3 rounded-lg border border-border bg-muted/20">
            <div>
              <p className="text-xs font-semibold text-foreground">Trạng thái hoạt động</p>
              <p className="text-[11px] text-muted-foreground">
                Tài khoản bị vô hiệu hóa sẽ không thể đăng nhập vào hệ thống
              </p>
            </div>
            <button
              type="button"
              role="switch"
              aria-checked={isActive}
              onClick={() => setValue('isActive', !isActive)}
              className={`relative inline-flex h-5 w-9 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none ${
                isActive ? 'bg-emerald-600' : 'bg-input'
              }`}
            >
              <span
                className={`pointer-events-none inline-block h-4 w-4 transform rounded-full bg-background shadow-lg ring-0 transition duration-200 ease-in-out ${
                  isActive ? 'translate-x-4' : 'translate-x-0'
                }`}
              />
            </button>
          </div>

          <DialogFooter className="pt-2">
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => onOpenChange(false)}
              disabled={isSubmitting}
              className="text-xs h-9 cursor-pointer"
            >
              Hủy
            </Button>
            <Button
              type="submit"
              size="sm"
              disabled={isSubmitting}
              className="text-xs h-9 bg-blue-600 hover:bg-blue-700 text-white gap-1.5 cursor-pointer shadow-xs"
            >
              <Save className="h-3.5 w-3.5" />
              <span>{isSubmitting ? 'Đang lưu...' : isEditing ? 'Lưu thay đổi' : 'Tạo tài khoản'}</span>
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
