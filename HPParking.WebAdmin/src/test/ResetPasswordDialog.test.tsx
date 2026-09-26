import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { ResetPasswordDialog } from '@/components/users/ResetPasswordDialog';
import { UserRole } from '@/types/user';

describe('ResetPasswordDialog Component', () => {
  const mockUser = {
    id: 'user-01',
    username: 'testuser',
    fullName: 'Người Dùng Test',
    role: UserRole.Manager,
    isActive: true,
    createdAt: new Date().toISOString(),
  };

  it('hiển thị thông tin người dùng và validate mật khẩu khớp', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    const onOpenChange = vi.fn();

    render(
      <ResetPasswordDialog
        open={true}
        onOpenChange={onOpenChange}
        user={mockUser}
        onSubmit={onSubmit}
      />
    );

    expect(screen.getByText('Đặt Lại Mật Khẩu Người Dùng')).toBeInTheDocument();
    expect(screen.getAllByText('testuser').length).toBeGreaterThan(0);

    const newPasswordInput = screen.getByPlaceholderText(/Nhập mật khẩu mới/i);
    const confirmPasswordInput = screen.getByPlaceholderText(/Nhập lại mật khẩu mới/i);

    fireEvent.change(newPasswordInput, { target: { value: 'password123' } });
    fireEvent.change(confirmPasswordInput, { target: { value: 'password123' } });

    const submitBtn = screen.getByRole('button', { name: /Xác nhận đặt lại/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalledWith({
        newPassword: 'password123',
        confirmNewPassword: 'password123',
      });
    });
  });

  it('báo lỗi khi mật khẩu xác nhận không trùng khớp', async () => {
    const onSubmit = vi.fn();
    const onOpenChange = vi.fn();

    render(
      <ResetPasswordDialog
        open={true}
        onOpenChange={onOpenChange}
        user={mockUser}
        onSubmit={onSubmit}
      />
    );

    const newPasswordInput = screen.getByPlaceholderText(/Nhập mật khẩu mới/i);
    const confirmPasswordInput = screen.getByPlaceholderText(/Nhập lại mật khẩu mới/i);

    fireEvent.change(newPasswordInput, { target: { value: 'password123' } });
    fireEvent.change(confirmPasswordInput, { target: { value: 'different123' } });

    const submitBtn = screen.getByRole('button', { name: /Xác nhận đặt lại/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText(/Mật khẩu xác nhận không trùng khớp/i)).toBeInTheDocument();
    });

    expect(onSubmit).not.toHaveBeenCalled();
  });
});
