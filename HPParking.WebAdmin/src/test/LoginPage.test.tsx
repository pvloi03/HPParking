import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import { LoginPage } from '@/pages/LoginPage';

// Mock useAuth
const mockLogin = vi.fn();
vi.mock('@/hooks/useAuth', () => ({
  useAuth: () => ({
    login: mockLogin,
    isAuthenticated: false,
    user: null,
  }),
}));

describe('LoginPage Component', () => {
  it('hiển thị form đăng nhập với đầy đủ các trường và nút đăng nhập', () => {
    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>
    );

    expect(screen.getByText('HỆ THỐNG QUẢN TRỊ HPPARKING')).toBeInTheDocument();
    expect(screen.getByLabelText(/Tên đăng nhập/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Mật khẩu/i)).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /Đăng Nhập Vào Hệ Thống/i })
    ).toBeInTheDocument();
  });

  it('báo lỗi validation khi mật khẩu dưới 6 ký tự', async () => {
    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>
    );

    const usernameInput = screen.getByLabelText(/Tên đăng nhập/i);
    const passwordInput = screen.getByLabelText(/Mật khẩu/i);
    const submitBtn = screen.getByRole('button', {
      name: /Đăng Nhập Vào Hệ Thống/i,
    });

    fireEvent.change(usernameInput, { target: { value: 'admin' } });
    fireEvent.change(passwordInput, { target: { value: '123' } });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(
        screen.getByText('Mật khẩu phải có tối thiểu 6 ký tự')
      ).toBeInTheDocument();
    });
  });

  it('gọi hàm login khi thông tin hợp lệ', async () => {
    mockLogin.mockResolvedValueOnce({ user: { id: '1', username: 'admin' } });

    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>
    );

    const usernameInput = screen.getByLabelText(/Tên đăng nhập/i);
    const passwordInput = screen.getByLabelText(/Mật khẩu/i);
    const submitBtn = screen.getByRole('button', {
      name: /Đăng Nhập Vào Hệ Thống/i,
    });

    fireEvent.change(usernameInput, { target: { value: 'admin' } });
    fireEvent.change(passwordInput, { target: { value: 'password123' } });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(mockLogin).toHaveBeenCalledWith({
        username: 'admin',
        password: 'password123',
      });
    });
  });
});
