import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { UserDetailDialog } from '@/components/users/UserDetailDialog';
import { usersApi } from '@/api/userApi';
import { UserRole } from '@/types/user';

vi.mock('@/api/userApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/userApi')>();
  return {
    ...actual,
    usersApi: {
      getById: vi.fn(),
    },
  };
});

describe('UserDetailDialog Component', () => {
  let queryClient: QueryClient;

  const mockUser = {
    id: 'user-101',
    username: 'admin_sys',
    fullName: 'Nguyễn Quản Trị Hệ Thống',
    role: UserRole.Admin,
    email: 'admin@hpparking.vn',
    phoneNumber: '0988000111',
    isActive: true,
    lastLoginAt: '2026-09-28T07:30:00Z',
    lastLogoutAt: '2026-09-27T17:00:00Z',
    note: 'Tài khoản SuperAdmin vận hành bãi đỗ xe',
    createdAt: '2026-01-01T08:00:00Z',
    updatedAt: '2026-09-20T10:00:00Z',
  };

  beforeEach(() => {
    vi.clearAllMocks();
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false, gcTime: 0 },
      },
    });

    vi.mocked(usersApi.getById).mockResolvedValue(mockUser);
  });

  it('hiển thị đầy đủ thông tin người dùng, username, vai trò Admin và nhật ký đăng nhập', async () => {
    render(
      <QueryClientProvider client={queryClient}>
        <UserDetailDialog
          open={true}
          onOpenChange={vi.fn()}
          userId="user-101"
        />
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('Nguyễn Quản Trị Hệ Thống')).toBeInTheDocument();
      expect(screen.getAllByText(/admin_sys/i).length).toBeGreaterThan(0);
      expect(screen.getByText('Quản trị viên')).toBeInTheDocument();
      expect(screen.getByText('admin@hpparking.vn')).toBeInTheDocument();
      expect(screen.getByText('0988000111')).toBeInTheDocument();
      expect(screen.getByText('Tài khoản SuperAdmin vận hành bãi đỗ xe')).toBeInTheDocument();
      expect(screen.getAllByText('Đã kích hoạt').length).toBeGreaterThan(0);
    });
  });

  it('gọi callback onResetPassword khi click nút Đổi mật khẩu trong dialog', async () => {
    const handleResetPassword = vi.fn();
    const handleOpenChange = vi.fn();

    render(
      <QueryClientProvider client={queryClient}>
        <UserDetailDialog
          open={true}
          onOpenChange={handleOpenChange}
          userId="user-101"
          onResetPassword={handleResetPassword}
        />
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('Nguyễn Quản Trị Hệ Thống')).toBeInTheDocument();
    });

    const resetBtn = screen.getByRole('button', { name: /Đổi mật khẩu/i });
    fireEvent.click(resetBtn);

    expect(handleOpenChange).toHaveBeenCalledWith(false);
    expect(handleResetPassword).toHaveBeenCalledWith(mockUser);
  });

  it('gọi callback onEdit khi click nút Chỉnh sửa trong dialog', async () => {
    const handleEdit = vi.fn();
    const handleOpenChange = vi.fn();

    render(
      <QueryClientProvider client={queryClient}>
        <UserDetailDialog
          open={true}
          onOpenChange={handleOpenChange}
          userId="user-101"
          onEdit={handleEdit}
        />
      </QueryClientProvider>
    );

    await waitFor(() => {
      expect(screen.getByText('Nguyễn Quản Trị Hệ Thống')).toBeInTheDocument();
    });

    const editBtn = screen.getByRole('button', { name: /Chỉnh sửa/i });
    fireEvent.click(editBtn);

    expect(handleOpenChange).toHaveBeenCalledWith(false);
    expect(handleEdit).toHaveBeenCalledWith(mockUser);
  });
});
