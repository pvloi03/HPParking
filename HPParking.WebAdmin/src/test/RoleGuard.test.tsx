import { describe, it, expect, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { RoleGuard } from '@/components/layout/RoleGuard';
import { useAuthStore } from '@/stores/authStore';
import { UserRole } from '@/types/user';

describe('RoleGuard Component', () => {
  beforeEach(() => {
    useAuthStore.setState({
      user: null,
      isAuthenticated: false,
      isInitialized: true,
    });
  });

  it('cho phép truy cập khi vai trò người dùng nằm trong allowedRoles', () => {
    useAuthStore.setState({
      user: {
        id: 'u-admin',
        username: 'admin',
        fullName: 'Admin User',
        role: UserRole.Admin,
        isActive: true,
      },
      isAuthenticated: true,
      isInitialized: true,
    });

    render(
      <MemoryRouter initialEntries={['/protected']}>
        <Routes>
          <Route
            path="/protected"
            element={
              <RoleGuard allowedRoles={[UserRole.Admin]}>
                <div>Nội dung chỉ dành cho Admin</div>
              </RoleGuard>
            }
          />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByText('Nội dung chỉ dành cho Admin')).toBeInTheDocument();
  });

  it('chuyển hướng về /dashboard khi vai trò người dùng không nằm trong allowedRoles', () => {
    useAuthStore.setState({
      user: {
        id: 'u-viewer',
        username: 'viewer',
        fullName: 'Viewer User',
        role: UserRole.Viewer,
        isActive: true,
      },
      isAuthenticated: true,
      isInitialized: true,
    });

    render(
      <MemoryRouter initialEntries={['/admin-only']}>
        <Routes>
          <Route
            path="/admin-only"
            element={
              <RoleGuard allowedRoles={[UserRole.Admin]}>
                <div>Nội dung quản trị tối mật</div>
              </RoleGuard>
            }
          />
          <Route path="/dashboard" element={<div>Trang Chủ Dashboard</div>} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.queryByText('Nội dung quản trị tối mật')).not.toBeInTheDocument();
    expect(screen.getByText('Trang Chủ Dashboard')).toBeInTheDocument();
  });
});
