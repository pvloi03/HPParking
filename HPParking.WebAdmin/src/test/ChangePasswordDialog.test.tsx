import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { ChangePasswordDialog } from '@/components/auth/ChangePasswordDialog';
import { useAuthStore } from '@/stores/authStore';
import { authApi } from '@/api/authApi';

vi.mock('@/api/authApi', () => ({
  authApi: {
    changePassword: vi.fn(),
  },
}));

describe('ChangePasswordDialog Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useAuthStore.setState({
      user: {
        id: 'user-01',
        username: 'nv_quanly',
        fullName: 'Nguyễn Quản Lý',
        role: 'Manager',
        isActive: true,
      },
      isAuthenticated: true,
      isInitialized: true,
    });
  });

  it('gửi đúng oldPassword, newPassword, confirmNewPassword khi đổi mật khẩu', async () => {
    vi.mocked(authApi.changePassword).mockResolvedValue(true);
    const onOpenChange = vi.fn();

    render(<ChangePasswordDialog open={true} onOpenChange={onOpenChange} />);

    expect(screen.getByText('Đổi Mật Khẩu Cá Nhân')).toBeInTheDocument();

    const oldPasswordInput = screen.getByPlaceholderText(/Nhập mật khẩu hiện tại/i);
    const newPasswordInput = screen.getByPlaceholderText(/Nhập mật khẩu mới/i);
    const confirmPasswordInput = screen.getByPlaceholderText(/Nhập lại mật khẩu mới/i);

    fireEvent.change(oldPasswordInput, { target: { value: 'oldPass123' } });
    fireEvent.change(newPasswordInput, { target: { value: 'newPass456' } });
    fireEvent.change(confirmPasswordInput, { target: { value: 'newPass456' } });

    const submitBtn = screen.getByRole('button', { name: /Đổi mật khẩu/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(authApi.changePassword).toHaveBeenCalledWith({
        oldPassword: 'oldPass123',
        newPassword: 'newPass456',
        confirmNewPassword: 'newPass456',
      });
      expect(onOpenChange).toHaveBeenCalledWith(false);
    });
  });
});
