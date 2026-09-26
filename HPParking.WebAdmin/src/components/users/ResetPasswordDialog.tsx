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
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { KeyRound, Eye, EyeOff, ShieldCheck } from 'lucide-react';
import type { UserDto, ResetPasswordRequest } from '@/types/user';

const resetPasswordSchema = z
  .object({
    newPassword: z
      .string()
      .min(6, 'Mật khẩu mới phải có tối thiểu 6 ký tự (theo chuẩn ADR 0025)')
      .max(100, 'Mật khẩu không được quá 100 ký tự'),
    confirmPassword: z.string().min(1, 'Vui lòng xác nhận mật khẩu mới'),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: 'Mật khẩu xác nhận không trùng khớp',
    path: ['confirmPassword'],
  });

type ResetPasswordFormData = z.infer<typeof resetPasswordSchema>;

interface ResetPasswordDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  user: UserDto | null;
  onSubmit: (data: ResetPasswordRequest) => Promise<void>;
  isSubmitting?: boolean;
}

export function ResetPasswordDialog({
  open,
  onOpenChange,
  user,
  onSubmit,
  isSubmitting = false,
}: ResetPasswordDialogProps) {
  const [showPassword, setShowPassword] = useState(false);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<ResetPasswordFormData>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: {
      newPassword: '',
      confirmPassword: '',
    },
  });

  useEffect(() => {
    if (open) {
      reset({
        newPassword: '',
        confirmPassword: '',
      });
      setShowPassword(false);
    }
  }, [open, reset]);

  const onFormSubmit = async (data: ResetPasswordFormData) => {
    try {
      await onSubmit({
        newPassword: data.newPassword,
        confirmNewPassword: data.confirmPassword,
      });
    } catch {
      // Đã được xử lý trong onError của mutation
    }
  };

  if (!user) return null;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <div className="flex items-center gap-2 mb-1">
            <div className="p-2 rounded-lg bg-amber-100 dark:bg-amber-950 text-amber-600 dark:text-amber-400">
              <KeyRound className="h-5 w-5" />
            </div>
            <DialogTitle className="text-base font-bold">
              Đặt Lại Mật Khẩu Người Dùng
            </DialogTitle>
          </div>
          <DialogDescription>
            Đặt mật khẩu mới cho tài khoản <strong className="text-foreground">{user.username}</strong> ({user.fullName}).
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onFormSubmit)} className="space-y-3.5 py-1">
          {/* Thông tin tài khoản tóm tắt */}
          <div className="p-3 rounded-lg border border-border bg-muted/30 text-xs space-y-1">
            <div className="flex justify-between">
              <span className="text-muted-foreground">Tên đăng nhập:</span>
              <span className="font-semibold text-foreground">{user.username}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-muted-foreground">Họ và tên:</span>
              <span className="font-medium text-foreground">{user.fullName}</span>
            </div>
          </div>

          {/* Mật khẩu mới */}
          <div className="space-y-1">
            <label className="text-xs font-semibold text-foreground">
              Mật khẩu mới <span className="text-destructive">*</span>
            </label>
            <div className="relative">
              <Input
                type={showPassword ? 'text' : 'password'}
                {...register('newPassword')}
                placeholder="Nhập mật khẩu mới (tối thiểu 6 ký tự)"
                className="pr-10 text-xs"
                autoFocus
              />
              <button
                type="button"
                onClick={() => setShowPassword(!showPassword)}
                className="absolute right-2.5 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground cursor-pointer"
                title={showPassword ? 'Ẩn mật khẩu' : 'Hiển thị mật khẩu'}
              >
                {showPassword ? (
                  <EyeOff className="h-4 w-4" />
                ) : (
                  <Eye className="h-4 w-4" />
                )}
              </button>
            </div>
            {errors.newPassword && (
              <p className="text-[11px] text-destructive">{errors.newPassword.message}</p>
            )}
          </div>

          {/* Xác nhận mật khẩu mới */}
          <div className="space-y-1">
            <label className="text-xs font-semibold text-foreground">
              Xác nhận mật khẩu mới <span className="text-destructive">*</span>
            </label>
            <Input
              type={showPassword ? 'text' : 'password'}
              {...register('confirmPassword')}
              placeholder="Nhập lại mật khẩu mới"
              className="text-xs"
            />
            {errors.confirmPassword && (
              <p className="text-[11px] text-destructive">{errors.confirmPassword.message}</p>
            )}
          </div>

          <div className="flex items-start gap-2 p-2.5 rounded-lg bg-blue-50/50 dark:bg-blue-950/30 border border-blue-200 dark:border-blue-900 text-blue-800 dark:text-blue-300 text-[11px]">
            <ShieldCheck className="h-4 w-4 shrink-0 text-blue-600 mt-0.5" />
            <span>
              Người dùng sẽ có thể đăng nhập ngay với mật khẩu mới này. Hành động đổi mật khẩu sẽ được ghi lại trong nhật ký kiểm toán hệ thống.
            </span>
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
              className="text-xs h-9 bg-amber-600 hover:bg-amber-700 text-white gap-1.5 cursor-pointer shadow-xs"
            >
              <KeyRound className="h-3.5 w-3.5" />
              <span>{isSubmitting ? 'Đang xử lý...' : 'Xác nhận đặt lại'}</span>
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
