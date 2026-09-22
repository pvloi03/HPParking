import { render, screen } from '@testing-library/react';
import { describe, it, expect, beforeEach } from 'vitest';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { AuthGuard } from '../components/layout/AuthGuard';
import { useAuthStore } from '../stores/authStore';

describe('AuthGuard Component', () => {
  beforeEach(() => {
    useAuthStore.setState({
      user: null,
      isAuthenticated: false,
      isInitialized: false,
    });
  });

  it('hiển thị màn hình loading khi phiên chưa được khởi tạo', () => {
    useAuthStore.setState({ isInitialized: false, isAuthenticated: false });

    render(
      <MemoryRouter initialEntries={['/dashboard']}>
        <AuthGuard>
          <div data-testid="protected-content">Nội dung bảo mật</div>
        </AuthGuard>
      </MemoryRouter>
    );

    expect(screen.getByTestId('auth-loading')).toBeInTheDocument();
    expect(screen.queryByTestId('protected-content')).not.toBeInTheDocument();
  });

  it('chuyển hướng sang /login khi chưa đăng nhập', () => {
    useAuthStore.setState({ isInitialized: true, isAuthenticated: false });

    render(
      <MemoryRouter initialEntries={['/dashboard']}>
        <Routes>
          <Route
            path="/dashboard"
            element={
              <AuthGuard>
                <div data-testid="protected-content">Nội dung bảo mật</div>
              </AuthGuard>
            }
          />
          <Route path="/login" element={<div data-testid="login-page">Trang Đăng Nhập</div>} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByTestId('login-page')).toBeInTheDocument();
    expect(screen.queryByTestId('protected-content')).not.toBeInTheDocument();
  });

  it('cho phép hiển thị nội dung khi đã xác thực', () => {
    useAuthStore.setState({
      isInitialized: true,
      isAuthenticated: true,
      user: {
        id: '1',
        username: 'admin',
        fullName: 'Admin',
        role: 'Admin',
        isActive: true,
      },
    });

    render(
      <MemoryRouter initialEntries={['/dashboard']}>
        <AuthGuard>
          <div data-testid="protected-content">Nội dung bảo mật</div>
        </AuthGuard>
      </MemoryRouter>
    );

    expect(screen.getByTestId('protected-content')).toBeInTheDocument();
  });
});
