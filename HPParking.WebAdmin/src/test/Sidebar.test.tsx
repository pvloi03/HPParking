import { render, screen, act } from '@testing-library/react';
import { describe, it, expect, beforeEach } from 'vitest';
import { Sidebar } from '../components/layout/Sidebar';
import { useUiStore } from '../stores/uiStore';

describe('Sidebar Component', () => {
  beforeEach(() => {
    useUiStore.setState({ isSidebarCollapsed: false });
  });

  it('hiển thị đầy đủ các nhóm danh mục tiếng Việt khi mở rộng', () => {
    render(<Sidebar />);

    expect(screen.getByText('Tổng quan')).toBeInTheDocument();
    expect(screen.getByText('Cơ cấu tổ chức')).toBeInTheDocument();
    expect(screen.getByText('Hạ tầng bãi xe')).toBeInTheDocument();
    expect(screen.getByText('Khách hàng & Xe')).toBeInTheDocument();
    expect(screen.getByText('Sổ cái & Kiểm toán')).toBeInTheDocument();

    // Kiểm tra các menu items cụ thể
    expect(screen.getByText('Dashboard')).toBeInTheDocument();
    expect(screen.getByText('Công ty')).toBeInTheDocument();
    expect(screen.getByText('Phòng ban')).toBeInTheDocument();
    expect(screen.getByText('Cổng bãi xe')).toBeInTheDocument();
    expect(screen.getByText('Làn xe')).toBeInTheDocument();
    expect(screen.getByText('Khách hàng')).toBeInTheDocument();
    expect(screen.getByText('Phương tiện')).toBeInTheDocument();
  });

  it('hỗ trợ thu gọn thanh bên khi click nút đóng/mở', () => {
    render(<Sidebar />);

    const toggleBtn = screen.getByRole('button', { name: /thu gọn|mở rộng/i });
    expect(useUiStore.getState().isSidebarCollapsed).toBe(false);

    act(() => {
      toggleBtn.click();
    });

    expect(useUiStore.getState().isSidebarCollapsed).toBe(true);
  });
});
