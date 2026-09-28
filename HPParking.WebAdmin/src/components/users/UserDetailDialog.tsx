import { useQuery } from '@tanstack/react-query';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { ActiveStatusBadge } from '@/components/common/ActiveStatusBadge';
import {
  User,
  Shield,
  Phone,
  Mail,
  Calendar,
  Clock,
  KeyRound,
  Edit,
  AlertCircle,
  LogIn,
  LogOut,
} from 'lucide-react';
import { usersApi } from '@/api/userApi';
import { USER_ROLE_BADGES, type UserDto } from '@/types/user';

export interface UserDetailDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  userId: string | null;
  onEdit?: (user: UserDto) => void;
  onResetPassword?: (user: UserDto) => void;
}

export function UserDetailDialog({
  open,
  onOpenChange,
  userId,
  onEdit,
  onResetPassword,
}: UserDetailDialogProps) {
  // Fetch chi tiết tài khoản
  const {
    data: user,
    isLoading: isLoadingUser,
    isError: isUserError,
  } = useQuery({
    queryKey: ['user-detail', userId],
    queryFn: () => (userId ? usersApi.getById(userId) : Promise.reject('No ID')),
    enabled: Boolean(open && userId),
  });

  const formatDate = (isoString?: string | null) => {
    if (!isoString) return '—';
    try {
      const d = new Date(isoString);
      return isNaN(d.getTime()) ? '—' : d.toLocaleString('vi-VN');
    } catch {
      return '—';
    }
  };

  const roleBadge = user?.role ? USER_ROLE_BADGES[user.role] : null;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <div className="flex items-center gap-3">
            <div className="p-2.5 rounded-xl bg-purple-500/10 text-purple-600 dark:text-purple-400">
              <User className="h-6 w-6" />
            </div>
            <div>
              <DialogTitle className="text-lg font-bold flex items-center gap-2">
                {isLoadingUser ? (
                  <Skeleton className="h-6 w-48" />
                ) : (
                  <>
                    <span>{user?.fullName || 'Chi Tiết Tài Khoản'}</span>
                    {user && <ActiveStatusBadge isActive={user.isActive} />}
                  </>
                )}
              </DialogTitle>
              <DialogDescription className="text-xs text-muted-foreground mt-0.5">
                {user?.username ? `Tên đăng nhập: @${user.username}` : 'Hồ sơ người dùng và phân quyền vận hành'}
              </DialogDescription>
            </div>
          </div>
        </DialogHeader>

        {isLoadingUser ? (
          <div className="space-y-4 py-4">
            <div className="grid grid-cols-2 gap-4">
              <Skeleton className="h-14 rounded-lg" />
              <Skeleton className="h-14 rounded-lg" />
            </div>
            <Skeleton className="h-24 rounded-lg" />
          </div>
        ) : isUserError || !user ? (
          <div className="py-8 flex flex-col items-center justify-center text-center">
            <AlertCircle className="h-10 w-10 text-destructive mb-2 opacity-80" />
            <p className="text-sm font-semibold text-foreground">Không thể tải thông tin người dùng</p>
            <p className="text-xs text-muted-foreground mt-1">
              Bản ghi có thể đã bị xóa hoặc không thể kết nối tới máy chủ.
            </p>
          </div>
        ) : (
          <div className="space-y-4 pt-2">
            {/* Lưới thông tin cơ bản */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              {/* Tên đăng nhập */}
              <div className="p-3 rounded-xl border border-border bg-card/60">
                <span className="text-[11px] font-medium text-muted-foreground block">
                  Tên đăng nhập (Username)
                </span>
                <span className="text-sm font-semibold text-foreground font-mono mt-0.5 block">
                  @{user.username}
                </span>
              </div>

              {/* Vai trò */}
              <div className="p-3 rounded-xl border border-border bg-card/60">
                <span className="text-[11px] font-medium text-muted-foreground flex items-center gap-1">
                  <Shield className="h-3 w-3 text-muted-foreground" />
                  Vai trò phân quyền
                </span>
                <div className="mt-1">
                  {roleBadge ? (
                    <Badge variant={roleBadge.variant} className="text-xs">
                      {roleBadge.label}
                    </Badge>
                  ) : (
                    <Badge variant="outline" className="text-xs">
                      {String(user.role)}
                    </Badge>
                  )}
                </div>
              </div>

              {/* Số điện thoại */}
              <div className="p-3 rounded-xl border border-border bg-card/60">
                <span className="text-[11px] font-medium text-muted-foreground flex items-center gap-1">
                  <Phone className="h-3 w-3 text-muted-foreground" />
                  Số điện thoại
                </span>
                {user.phoneNumber ? (
                  <a
                    href={`tel:${user.phoneNumber}`}
                    className="text-sm font-medium text-blue-600 dark:text-blue-400 hover:underline mt-0.5 block"
                  >
                    {user.phoneNumber}
                  </a>
                ) : (
                  <span className="text-sm text-muted-foreground italic mt-0.5 block">
                    Chưa cập nhật
                  </span>
                )}
              </div>

              {/* Email */}
              <div className="p-3 rounded-xl border border-border bg-card/60">
                <span className="text-[11px] font-medium text-muted-foreground flex items-center gap-1">
                  <Mail className="h-3 w-3 text-muted-foreground" />
                  Hộp thư điện tử
                </span>
                {user.email ? (
                  <a
                    href={`mailto:${user.email}`}
                    className="text-sm font-medium text-blue-600 dark:text-blue-400 hover:underline mt-0.5 block"
                  >
                    {user.email}
                  </a>
                ) : (
                  <span className="text-sm text-muted-foreground italic mt-0.5 block">
                    Chưa cập nhật
                  </span>
                )}
              </div>
            </div>

            {/* Hoạt động đăng nhập & Bảo mật */}
            <div className="p-3.5 rounded-xl border border-border bg-muted/30 space-y-2">
              <h4 className="text-xs font-semibold text-foreground flex items-center gap-1.5">
                <Clock className="h-3.5 w-3.5 text-blue-600 dark:text-blue-400" />
                Lịch sử đăng nhập & bảo mật
              </h4>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 text-xs">
                <div className="p-2.5 rounded-lg bg-background border border-border flex items-start gap-2">
                  <LogIn className="h-3.5 w-3.5 text-emerald-600 shrink-0 mt-0.5" />
                  <div>
                    <span className="text-[11px] text-muted-foreground block">Đăng nhập gần nhất:</span>
                    <span className="font-medium text-foreground">
                      {formatDate(user.lastLoginAt)}
                    </span>
                  </div>
                </div>

                <div className="p-2.5 rounded-lg bg-background border border-border flex items-start gap-2">
                  <LogOut className="h-3.5 w-3.5 text-amber-600 shrink-0 mt-0.5" />
                  <div>
                    <span className="text-[11px] text-muted-foreground block">Đăng xuất gần nhất:</span>
                    <span className="font-medium text-foreground">
                      {formatDate(user.lastLogoutAt)}
                    </span>
                  </div>
                </div>
              </div>
            </div>

            {/* Thời gian tạo & Cập nhật */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs">
              <div className="p-3 rounded-xl border border-border bg-card/60">
                <span className="text-[11px] font-medium text-muted-foreground flex items-center gap-1">
                  <Calendar className="h-3 w-3 text-muted-foreground" />
                  Thời điểm khởi tạo tài khoản
                </span>
                <span className="text-xs text-foreground mt-0.5 block">
                  {formatDate(user.createdAt)}
                </span>
              </div>

              <div className="p-3 rounded-xl border border-border bg-card/60">
                <span className="text-[11px] font-medium text-muted-foreground flex items-center gap-1">
                  <Clock className="h-3 w-3 text-muted-foreground" />
                  Cập nhật hồ sơ lần cuối
                </span>
                <span className="text-xs text-foreground mt-0.5 block">
                  {formatDate(user.updatedAt)}
                </span>
              </div>

              {user.note && (
                <div className="p-3 rounded-xl border border-border bg-card/60 sm:col-span-2">
                  <span className="text-[11px] font-medium text-muted-foreground block">
                    Ghi chú tài khoản
                  </span>
                  <p className="text-xs text-foreground mt-0.5 whitespace-pre-wrap">
                    {user.note}
                  </p>
                </div>
              )}
            </div>
          </div>
        )}

        <DialogFooter className="flex items-center justify-end gap-2 pt-2 border-t border-border">
          {user && onResetPassword && (
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => {
                onOpenChange(false);
                onResetPassword(user);
              }}
              className="text-xs gap-1.5 text-amber-600 border-amber-200 hover:bg-amber-50 dark:hover:bg-amber-950/40 cursor-pointer"
            >
              <KeyRound className="h-3.5 w-3.5" />
              <span>Đổi mật khẩu</span>
            </Button>
          )}

          {user && onEdit && (
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => {
                onOpenChange(false);
                onEdit(user);
              }}
              className="text-xs gap-1.5 text-blue-600 border-blue-200 hover:bg-blue-50 dark:hover:bg-blue-950/40 cursor-pointer"
            >
              <Edit className="h-3.5 w-3.5" />
              <span>Chỉnh sửa</span>
            </Button>
          )}

          <Button
            type="button"
            variant="default"
            size="sm"
            onClick={() => onOpenChange(false)}
            className="text-xs cursor-pointer"
          >
            Đóng
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
