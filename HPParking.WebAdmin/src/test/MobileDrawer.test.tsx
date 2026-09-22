import { render, screen, fireEvent, act } from '@testing-library/react';
import { describe, it, expect, beforeEach } from 'vitest';
import { MobileDrawer } from '@/components/layout/MobileDrawer';
import { useUiStore } from '@/stores/uiStore';

describe('MobileDrawer Component', () => {
  beforeEach(() => {
    useUiStore.setState({
      isMobileDrawerOpen: false,
      isSidebarCollapsed: false,
    });
  });

  it('không hiển thị khi isMobileDrawerOpen là false', () => {
    render(<MobileDrawer />);
    expect(screen.queryByLabelText('Đóng thanh bên')).not.toBeInTheDocument();
  });

  it('hiển thị drawer trượt khi isMobileDrawerOpen là true', () => {
    useUiStore.setState({ isMobileDrawerOpen: true });
    render(<MobileDrawer />);

    expect(screen.getByLabelText('Đóng thanh bên')).toBeInTheDocument();
    expect(screen.getByText('Dashboard')).toBeInTheDocument();
  });

  it('đóng drawer khi bấm nút (X)', () => {
    useUiStore.setState({ isMobileDrawerOpen: true });
    render(<MobileDrawer />);

    const closeBtn = screen.getByLabelText('Đóng thanh bên');
    act(() => {
      fireEvent.click(closeBtn);
    });

    expect(useUiStore.getState().isMobileDrawerOpen).toBe(false);
  });

  it('đóng drawer khi nhấn phím Escape', () => {
    useUiStore.setState({ isMobileDrawerOpen: true });
    render(<MobileDrawer />);

    act(() => {
      fireEvent.keyDown(window, { key: 'Escape', code: 'Escape' });
    });

    expect(useUiStore.getState().isMobileDrawerOpen).toBe(false);
  });
});
