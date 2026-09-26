import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import {
  UserCog,
  KeyRound,
  Trash2,
  User as UserIcon,
  Phone,
  Mail,
  Clock,
  Edit,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { DataTable, type ColumnDef } from '@/components/common/DataTable';
import { ConfirmDialog } from '@/components/common/ConfirmDialog';
import { UserFormDialog } from '@/components/users/UserFormDialog';
import { ResetPasswordDialog } from '@/components/users/ResetPasswordDialog';
import { usersApi, extractErrorMessage } from '@/api/userApi';
import { useAuthStore } from '@/stores/authStore';
import {
  UserRole,
  type UserDto,
  type CreateUserRequest,
  type UpdateUserRequest,
  type ResetPasswordRequest,
} from '@/types/user';

export function UsersPage() {
  const queryClient = useQueryClient();
  const currentUser = useAuthStore((s) => s.user);

  // State bộ lọc và phân trang
  const [pageIndex, setPageIndex] = useState(1);
  const pageSize = 15;
  const [searchKeyword, setSearchKeyword] = useState('');
  const [statusFilter, setStatusFilter] = useState<boolean | 'all'>('all');
  const [roleFilter, setRoleFilter] = useState<UserRole | 'all'>('all');

  // State Modal Form & Actions
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [selectedUser, setSelectedUser] = useState<UserDto | null>(null);
  const [resetPasswordCandidate, setResetPasswordCandidate] = useState<UserDto | null>(null);
  const [deleteCandidate, setDeleteCandidate] = useState<UserDto | null>(null);

  // TanStack Query: Lấy danh sách người dùng
  const { data, isLoading } = useQuery({
    queryKey: [
      'users',
      pageIndex,
      pageSize,
      searchKeyword,
      statusFilter,
      roleFilter,
    ],
    queryFn: () =>
      usersApi.getPaged({
        pageIndex,
        pageSize,
        keyword: searchKeyword.trim() || undefined,
        isActive: statusFilter === 'all' ? undefined : statusFilter,
        role: roleFilter === 'all' ? undefined : roleFilter,
      }),
  });

  // Mutation: Thêm mới người dùng
  const createMutation = useMutation({
    mutationFn: (payload: CreateUserRequest) => usersApi.create(payload),
    onSuccess: (created) => {
      toast.success(`Đã tạo tài khoản "${created.username}" thành công`);
      setIsFormOpen(false);
      void queryClient.invalidateQueries({ queryKey: ['users'] });
      void queryClient.invalidateQueries({ queryKey: ['recycle-bin', 'users'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Mutation: Cập nhật thông tin người dùng
  const updateMutation = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateUserRequest }) =>
      usersApi.update(id, payload),
    onSuccess: (updated) => {
      toast.success(`Đã cập nhật tài khoản "${updated.username}" thành công`);
      setIsFormOpen(false);
      setSelectedUser(null);
      void queryClient.invalidateQueries({ queryKey: ['users'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Mutation: Đặt lại mật khẩu
  const resetPasswordMutation = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: ResetPasswordRequest }) =>
      usersApi.resetPassword(id, payload),
    onSuccess: () => {
      toast.success(
        `Đã đặt lại mật khẩu cho tài khoản "${resetPasswordCandidate?.username}" thành công`
      );
      setResetPasswordCandidate(null);
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Mutation: Chuyển đổi trạng thái kích hoạt (Active)
  const toggleStatusMutation = useMutation({
    mutationFn: (id: string) => usersApi.toggleStatus(id),
    onSuccess: (updated) => {
      const msg = updated.isActive
        ? `Đã kích hoạt tài khoản "${updated.username}"`
        : `Đã vô hiệu hóa tài khoản "${updated.username}"`;
      toast.success(msg);
      void queryClient.invalidateQueries({ queryKey: ['users'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Mutation: Xóa mềm (chuyển vào Thùng rác hệ thống)
  const deleteMutation = useMutation({
    mutationFn: (id: string) => usersApi.delete(id, false),
    onSuccess: () => {
      toast.success(
        `Đã chuyển tài khoản "${deleteCandidate?.username}" vào Thùng rác hệ thống`
      );
      setDeleteCandidate(null);
      void queryClient.invalidateQueries({ queryKey: ['users'] });
      void queryClient.invalidateQueries({ queryKey: ['recycle-bin', 'users'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Xử lý submit form
  const handleFormSubmit = async (payload: CreateUserRequest | UpdateUserRequest) => {
    if (selectedUser) {
      await updateMutation.mutateAsync({
        id: selectedUser.id,
        payload: payload as UpdateUserRequest,
      });
    } else {
      await createMutation.mutateAsync(payload as CreateUserRequest);
    }
  };

  const handleResetPasswordSubmit = async (payload: ResetPasswordRequest) => {
    if (!resetPasswordCandidate) return;
    await resetPasswordMutation.mutateAsync({
      id: resetPasswordCandidate.id,
      payload,
    });
  };

  const formatDateTime = (dateStr?: string | null) => {
    if (!dateStr) return null;
    try {
      const d = new Date(dateStr);
      return d.toLocaleString('vi-VN', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      });
    } catch {
      return dateStr;
    }
  };

  // Định nghĩa các cột hiển thị
  const columns: ColumnDef<UserDto>[] = [
    {
      header: 'Tên đăng nhập',
      accessorKey: 'username',
      cell: (item) => {
        const isSelf = currentUser?.id === item.id || currentUser?.username === item.username;
        return (
          <div className="flex items-center gap-2.5">
            <div className="h-8 w-8 rounded-full bg-blue-100 dark:bg-blue-950 text-blue-700 dark:text-blue-300 flex items-center justify-center font-bold text-xs uppercase shrink-0">
              {item.username.charAt(0) || <UserIcon className="h-4 w-4" />}
            </div>
            <div className="min-w-0">
              <div className="flex items-center gap-1.5 font-semibold text-foreground truncate">
                <span>{item.username}</span>
                {isSelf && (
                  <Badge variant="outline" className="text-[10px] py-0 px-1 border-blue-400 text-blue-600 bg-blue-50/50 dark:bg-blue-950/40">
                    Bạn
                  </Badge>
                )}
              </div>
              <div className="text-[11px] text-muted-foreground truncate">{item.fullName}</div>
            </div>
          </div>
        );
      },
      className: 'min-w-[180px]',
      mobileLabel: 'Tài khoản',
    },
    {
      header: 'Vai trò',
      accessorKey: 'role',
      cell: (item) => {
        const roleNum = typeof item.role === 'number' ? item.role : item.role === 'Admin' ? 1 : item.role === 'Manager' ? 2 : 3;
        if (roleNum === UserRole.Admin) {
          return (
            <Badge
              variant="destructive"
              className="text-[11px] font-medium bg-rose-100 text-rose-800 hover:bg-rose-100 dark:bg-rose-950 dark:text-rose-300"
            >
              Quản trị viên
            </Badge>
          );
        }
        if (roleNum === UserRole.Manager) {
          return (
            <Badge
              variant="default"
              className="text-[11px] font-medium bg-blue-100 text-blue-800 hover:bg-blue-100 dark:bg-blue-950 dark:text-blue-300"
            >
              Quản lý
            </Badge>
          );
        }
        return (
          <Badge
            variant="secondary"
            className="text-[11px] font-medium bg-muted text-muted-foreground"
          >
            Người xem
          </Badge>
        );
      },
      className: 'w-32',
      mobileLabel: 'Vai trò',
    },
    {
      header: 'Thông tin liên hệ',
      cell: (item) => (
        <div className="space-y-0.5 text-xs">
          {item.email ? (
            <div className="flex items-center gap-1 text-muted-foreground truncate" title={item.email}>
              <Mail className="h-3 w-3 shrink-0" />
              <span className="truncate">{item.email}</span>
            </div>
          ) : null}
          {item.phoneNumber ? (
            <div className="flex items-center gap-1 text-muted-foreground">
              <Phone className="h-3 w-3 shrink-0" />
              <span>{item.phoneNumber}</span>
            </div>
          ) : null}
          {!item.email && !item.phoneNumber && (
            <span className="text-muted-foreground">—</span>
          )}
        </div>
      ),
      className: 'w-48',
      mobileLabel: 'Liên hệ',
    },
    {
      header: 'Đăng nhập gần nhất',
      accessorKey: 'lastLoginAt',
      cell: (item) => {
        const formatted = formatDateTime(item.lastLoginAt);
        return formatted ? (
          <div className="flex items-center gap-1.5 text-xs text-muted-foreground" title={formatted}>
            <Clock className="h-3.5 w-3.5 shrink-0 text-muted-foreground/70" />
            <span>{formatted}</span>
          </div>
        ) : (
          <span className="text-muted-foreground text-xs italic">Chưa đăng nhập</span>
        );
      },
      className: 'w-44',
      mobileLabel: 'Đăng nhập gần nhất',
    },
    {
      header: 'Trạng thái',
      accessorKey: 'isActive',
      cell: (item) => {
        const isSelf = currentUser?.id === item.id;
        return (
          <button
            type="button"
            disabled={isSelf || toggleStatusMutation.isPending}
            onClick={() => toggleStatusMutation.mutate(item.id)}
            title={
              isSelf
                ? 'Không thể vô hiệu hóa tài khoản của chính mình'
                : item.isActive
                ? 'Nhấn để tạm khóa tài khoản'
                : 'Nhấn để kích hoạt tài khoản'
            }
            className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11px] font-medium transition-all ${
              item.isActive
                ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300 hover:ring-1 hover:ring-emerald-400'
                : 'bg-muted text-muted-foreground hover:ring-1 hover:ring-muted-foreground'
            } ${isSelf ? 'cursor-not-allowed opacity-90' : 'cursor-pointer'}`}
          >
            <span
              className={`h-1.5 w-1.5 rounded-full ${
                item.isActive ? 'bg-emerald-500' : 'bg-muted-foreground'
              }`}
            />
            <span>{item.isActive ? 'Hoạt động' : 'Đã khóa'}</span>
          </button>
        );
      },
      className: 'w-32',
      mobileLabel: 'Trạng thái',
    },
    {
      header: 'Thao tác',
      cell: (item) => {
        const isSelf = currentUser?.id === item.id;
        return (
          <div className="flex items-center justify-end gap-1">
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setResetPasswordCandidate(item)}
              className="h-8 w-8 p-0 text-muted-foreground hover:text-amber-600 hover:bg-amber-500/10 cursor-pointer"
              title="Đặt lại mật khẩu"
            >
              <KeyRound className="h-4 w-4 text-amber-600" />
            </Button>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => {
                setSelectedUser(item);
                setIsFormOpen(true);
              }}
              className="h-8 w-8 p-0 text-muted-foreground hover:text-blue-600 hover:bg-blue-500/10 cursor-pointer"
              title="Chỉnh sửa tài khoản"
            >
              <Edit className="h-4 w-4 text-blue-600" />
            </Button>
            <Button
              variant="ghost"
              size="sm"
              disabled={isSelf}
              onClick={() => setDeleteCandidate(item)}
              className={`h-8 w-8 p-0 text-muted-foreground hover:text-rose-600 hover:bg-rose-500/10 cursor-pointer ${
                isSelf ? 'opacity-40 cursor-not-allowed' : ''
              }`}
              title={isSelf ? 'Bạn không thể tự xóa tài khoản của chính mình' : 'Xóa vào thùng rác'}
            >
              <Trash2 className="h-4 w-4 text-rose-600" />
            </Button>
          </div>
        );
      },
      className: 'w-32 text-right',
      mobileLabel: 'Thao tác',
    },
  ];

  return (
    <div className="space-y-6">
      {/* Header trang */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2 border-b border-border pb-4">
        <div>
          <div className="flex items-center gap-2">
            <div className="p-2 rounded-xl bg-blue-50 dark:bg-blue-950 text-blue-600 dark:text-blue-400">
              <UserCog className="h-5 w-5" />
            </div>
            <div>
              <h1 className="text-xl font-bold tracking-tight text-foreground">
                Quản Lý Tài Khoản Người Dùng
              </h1>
              <p className="text-xs text-muted-foreground">
                Quản trị danh sách tài khoản, phân quyền vai trò (Admin, Quản lý, Người xem) và mật khẩu vận hành.
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Bảng dữ liệu dùng chung DataTable */}
      <DataTable
        data={data?.items || []}
        columns={columns}
        pagination={
          data?.pagination || {
            pageIndex: 1,
            pageSize: 15,
            totalCount: 0,
            totalPages: 1,
            hasPreviousPage: false,
            hasNextPage: false,
          }
        }
        onPageChange={(p) => setPageIndex(p)}
        isLoading={isLoading}
        searchKeyword={searchKeyword}
        onSearchChange={(kw) => {
          setSearchKeyword(kw);
          setPageIndex(1);
        }}
        searchPlaceholder="Tìm kiếm tên đăng nhập, họ tên, email, sđt..."
        statusFilter={statusFilter}
        onStatusFilterChange={(st) => {
          setStatusFilter(st);
          setPageIndex(1);
        }}
        extraFilters={
          <div className="flex items-center gap-1.5">
            <select
              value={roleFilter}
              onChange={(e) => {
                const val = e.target.value;
                setRoleFilter(val === 'all' ? 'all' : (Number(val) as UserRole));
                setPageIndex(1);
              }}
              className="h-9 px-2.5 rounded-lg border border-border bg-background text-xs text-foreground cursor-pointer focus:outline-none focus:ring-1 focus:ring-blue-600"
            >
              <option value="all">Tất cả vai trò</option>
              <option value={UserRole.Admin}>Quản trị viên (Admin)</option>
              <option value={UserRole.Manager}>Quản lý (Manager)</option>
              <option value={UserRole.Viewer}>Người xem (Viewer)</option>
            </select>
          </div>
        }
        onAddNew={() => {
          setSelectedUser(null);
          setIsFormOpen(true);
        }}
        addNewLabel="Thêm mới tài khoản"
        emptyTitle="Không có tài khoản nào"
        emptyDescription="Chưa có dữ liệu tài khoản hoặc không có bản ghi nào khớp với điều kiện tìm kiếm."
      />

      {/* Modal Form Thêm / Sửa */}
      <UserFormDialog
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        initialData={selectedUser}
        onSubmit={handleFormSubmit}
        isSubmitting={createMutation.isPending || updateMutation.isPending}
      />

      {/* Modal Đặt Lại Mật Khẩu */}
      <ResetPasswordDialog
        open={Boolean(resetPasswordCandidate)}
        onOpenChange={(open) => !open && setResetPasswordCandidate(null)}
        user={resetPasswordCandidate}
        onSubmit={handleResetPasswordSubmit}
        isSubmitting={resetPasswordMutation.isPending}
      />

      {/* Confirm Xóa Mềm vào Thùng rác hệ thống */}
      <ConfirmDialog
        open={Boolean(deleteCandidate)}
        onOpenChange={(open) => !open && setDeleteCandidate(null)}
        title="Xác Nhận Xóa Tài Khoản"
        description={
          <span>
            Bạn có chắc chắn muốn chuyển tài khoản{' '}
            <strong className="text-foreground">{deleteCandidate?.username}</strong> vào{' '}
            <strong>Thùng rác hệ thống</strong>? Tài khoản này sẽ bị ngừng quyền truy cập và có thể được khôi phục tại trang Thùng rác hệ thống.
          </span>
        }
        confirmText="Xóa vào thùng rác"
        variant="destructive"
        isLoading={deleteMutation.isPending}
        onConfirm={() => {
          if (deleteCandidate) {
            deleteMutation.mutate(deleteCandidate.id);
          }
        }}
      />
    </div>
  );
}
