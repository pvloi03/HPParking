import { useState, useEffect } from 'react';
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
import { KeyRound, Eye, EyeOff, ShieldCheck, Lock } from 'lucide-react';
import { usePermissions } from '@/hooks/usePermissions';
import { authApi } from '@/api/authApi';
import { extractErrorMessage } from '@/api/userApi';
import { toast } from 'sonner';

const changePasswordSchema = z
  .object({
    oldPassword: z.string().optional(),
    newPassword: z
      .string()
      .min(6, 'Mật khẩu mới phải có tối thiểu 6 ký tự')
      .max(100, 'Mật khẩu không được vượt quá 100 ký tự'),
    confirmPassword: z.string().min(1, 'Vui lòng xác nhận mật khẩu mới'),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: 'Mật khẩu xác nhận không trùng khớp',
    path: ['confirmPassword'],
  });

type ChangePasswordFormData = z.infer<typeof changePasswordSchema>;

interface ChangePasswordDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function ChangePasswordDialog({
  open,
  onOpenChange,
}: ChangePasswordDialogProps) {
  const { user, isAdmin } = usePermissions();

  const [showOldPassword, setShowOldPassword] = useState(false);
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<ChangePasswordFormData>({
    resolver: zodResolver(changePasswordSchema),
    defaultValues: {
      oldPassword: '',
      newPassword: '',
      confirmPassword: '',
    },
  });

  useEffect(() => {
    if (open) {
      reset({
        oldPassword: '',
        newPassword: '',
        confirmPassword: '',
      });
      setShowOldPassword(false);
      setShowNewPassword(false);
      setIsSubmitting(false);
    }
  }, [open, reset]);

  const onFormSubmit = async (data: ChangePasswordFormData) => {
    if (!isAdmin && !data.oldPassword) {
      toast.error('Vui lòng nhập mật khẩu hiện tại');
      return;
    }

    try {
      setIsSubmitting(true);
      await authApi.changePassword({
        oldPassword: data.oldPassword ? data.oldPassword : undefined,
        newPassword: data.newPassword,
        confirmNewPassword: data.confirmPassword,
      });

      toast.success('Đổi mật khẩu thành công!');
      onOpenChange(false);
    } catch (err) {
      toast.error(extractErrorMessage(err));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <div className="flex items-center gap-2 mb-1">
            <div className="p-2 rounded-lg bg-blue-100 dark:bg-blue-950 text-blue-600 dark:text-blue-400">
              <KeyRound className="h-5 w-5" />
            </div>
            <DialogTitle className="text-base font-bold">
              Đổi Mật Khẩu Cá Nhân
            </DialogTitle>
          </div>
          <DialogDescription>
            Cập nhật mật khẩu bảo vệ tài khoản <strong className="text-foreground">{user?.username}</strong>.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onFormSubmit)} className="space-y-3.5 py-1">
          {/* Mật khẩu cũ */}
          <div className="space-y-1">
            <label className="text-xs font-semibold text-foreground flex items-center justify-between">
              <span>
                Mật khẩu hiện tại {!isAdmin && <span className="text-destructive">*</span>}
              </span>
              {isAdmin && (
                <span className="text-[10px] text-muted-foreground font-normal">
                  (Tùy chọn với Quản trị viên)
                </span>
              )}
            </label>
            <div className="relative">
              <Input
                type={showOldPassword ? 'text' : 'password'}
                {...register('oldPassword')}
                placeholder={isAdmin ? 'Nhập mật khẩu hiện tại (nếu có)' : 'Nhập mật khẩu hiện tại'}
                className="pr-10 text-xs"
                autoFocus
              />
              <button
                type="button"
                onClick={() => setShowOldPassword(!showOldPassword)}
                className="absolute right-2.5 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground cursor-pointer"
                title={showOldPassword ? 'Ẩn' : 'Hiện'}
              >
                {showOldPassword ? (
                  <EyeOff className="h-4 w-4" />
                ) : (
                  <Eye className="h-4 w-4" />
                )}
              </button>
            </div>
            {errors.oldPassword && (
              <p className="text-[11px] text-destructive">{errors.oldPassword.message}</p>
            )}
          </div>

          {/* Mật khẩu mới */}
          <div className="space-y-1">
            <label className="text-xs font-semibold text-foreground">
              Mật khẩu mới <span className="text-destructive">*</span>
            </label>
            <div className="relative">
              <Input
                type={showNewPassword ? 'text' : 'password'}
                {...register('newPassword')}
                placeholder="Nhập mật khẩu mới (tối thiểu 6 ký tự)"
                className="pr-10 text-xs"
              />
              <button
                type="button"
                onClick={() => setShowNewPassword(!showNewPassword)}
                className="absolute right-2.5 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground cursor-pointer"
                title={showNewPassword ? 'Ẩn' : 'Hiện'}
              >
                {showNewPassword ? (
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
              type={showNewPassword ? 'text' : 'password'}
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
              Mật khẩu mới cần có độ dài từ 6 ký tự trở lên. Sau khi đổi mật khẩu thành công, bạn sẽ sử dụng mật khẩu mới cho các lần đăng nhập tiếp theo.
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
              className="text-xs h-9 bg-blue-600 hover:bg-blue-700 text-white gap-1.5 cursor-pointer shadow-xs"
            >
              <Lock className="h-3.5 w-3.5" />
              <span>{isSubmitting ? 'Đang cập nhật...' : 'Đổi mật khẩu'}</span>
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
