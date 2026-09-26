import { render, screen, act } from '@testing-library/react';
import { describe, it, expect, beforeEach } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import { Sidebar } from '../components/layout/Sidebar';
import { useUiStore } from '../stores/uiStore';
import { useAuthStore } from '../stores/authStore';
import { UserRole } from '@/types/user';

describe('Sidebar Component', () => {
  beforeEach(() => {
    useUiStore.setState({ isSidebarCollapsed: false });
    // Mặc định đăng nhập với vai trò Admin
    useAuthStore.setState({
      user: {
        id: 'u-admin',
        username: 'admin',
        fullName: 'Quản Trị Viên',
        role: UserRole.Admin,
        isActive: true,
      },
      isAuthenticated: true,
      isInitialized: true,
    });
  });

  it('hiển thị đầy đủ các nhóm danh mục tiếng Việt khi mở rộng với vai trò Admin', () => {
    render(
      <MemoryRouter>
        <Sidebar />
      </MemoryRouter>
    );

    expect(screen.getByText('Tổng quan')).toBeInTheDocument();
    expect(screen.getByText('Tổ chức & đơn vị')).toBeInTheDocument();
    expect(screen.getByText('Hạ tầng')).toBeInTheDocument();
    expect(screen.getByText('Nhân sự & phương tiện')).toBeInTheDocument();
    expect(screen.getByText('Sổ cái & Kiểm toán')).toBeInTheDocument();

    // Kiểm tra các menu items cụ thể
    expect(screen.getByText('Dashboard')).toBeInTheDocument();
    expect(screen.getByText('Công ty')).toBeInTheDocument();
    expect(screen.getByText('Phòng ban')).toBeInTheDocument();
    expect(screen.getByText('Cổng ra vào')).toBeInTheDocument();
    expect(screen.getByText('Làn kiểm soát')).toBeInTheDocument();
    expect(screen.getByText('Nhân sự')).toBeInTheDocument();
    expect(screen.getByText('Phương tiện')).toBeInTheDocument();
    expect(screen.getByText('Tài khoản')).toBeInTheDocument();
    expect(screen.getByText('Nhật ký kiểm toán')).toBeInTheDocument();
    expect(screen.getByText('Thùng rác hệ thống')).toBeInTheDocument();
  });

  it('ẩn Tài khoản và Nhật ký kiểm toán khi đăng nhập với vai trò Manager', () => {
    useAuthStore.setState({
      user: {
        id: 'u-mgr',
        username: 'manager',
        fullName: 'Quản Lý Bãi Xe',
        role: UserRole.Manager,
        isActive: true,
      },
      isAuthenticated: true,
      isInitialized: true,
    });

    render(
      <MemoryRouter>
        <Sidebar />
      </MemoryRouter>
    );

    // Manager thấy Thùng rác hệ thống nhưng không thấy Tài khoản và Nhật ký kiểm toán
    expect(screen.getByText('Thùng rác hệ thống')).toBeInTheDocument();
    expect(screen.queryByText('Tài khoản')).not.toBeInTheDocument();
    expect(screen.queryByText('Nhật ký kiểm toán')).not.toBeInTheDocument();
  });

  it('ẩn hoàn toàn nhóm Sổ cái & Kiểm toán khi đăng nhập với vai trò Viewer', () => {
    useAuthStore.setState({
      user: {
        id: 'u-view',
        username: 'viewer',
        fullName: 'Người Xem Báo Cáo',
        role: UserRole.Viewer,
        isActive: true,
      },
      isAuthenticated: true,
      isInitialized: true,
    });

    render(
      <MemoryRouter>
        <Sidebar />
      </MemoryRouter>
    );

    // Không còn mục con nào thuộc quyền -> Ẩn toàn bộ nhóm
    expect(screen.queryByText('Sổ cái & Kiểm toán')).not.toBeInTheDocument();
    expect(screen.queryByText('Tài khoản')).not.toBeInTheDocument();
    expect(screen.queryByText('Nhật ký kiểm toán')).not.toBeInTheDocument();
    expect(screen.queryByText('Thùng rác hệ thống')).not.toBeInTheDocument();

    // Các mục thông tin chung vẫn xem được
    expect(screen.getByText('Dashboard')).toBeInTheDocument();
    expect(screen.getByText('Lịch sử xe ra vào')).toBeInTheDocument();
    expect(screen.getByText('Công ty')).toBeInTheDocument();
  });

  it('hỗ trợ thu gọn thanh bên khi click nút đóng/mở', () => {
    render(
      <MemoryRouter>
        <Sidebar />
      </MemoryRouter>
    );

    const toggleBtn = screen.getByRole('button', { name: /thu gọn|mở rộng/i });
    expect(useUiStore.getState().isSidebarCollapsed).toBe(false);

    act(() => {
      toggleBtn.click();
    });

    expect(useUiStore.getState().isSidebarCollapsed).toBe(true);
  });
});
