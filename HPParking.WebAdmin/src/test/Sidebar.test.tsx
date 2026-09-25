import { render, screen, act } from '@testing-library/react';
import { describe, it, expect, beforeEach } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import { Sidebar } from '../components/layout/Sidebar';
import { useUiStore } from '../stores/uiStore';

describe('Sidebar Component', () => {
  beforeEach(() => {
    useUiStore.setState({ isSidebarCollapsed: false });
  });

  it('hiển thị đầy đủ các nhóm danh mục tiếng Việt khi mở rộng', () => {
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
