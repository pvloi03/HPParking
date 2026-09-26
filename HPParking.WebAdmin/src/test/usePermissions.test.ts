import { describe, it, expect, beforeEach } from 'vitest';
import { renderHook } from '@testing-library/react';
import { usePermissions } from '@/hooks/usePermissions';
import { useAuthStore } from '@/stores/authStore';
import { UserRole } from '@/types/user';

describe('usePermissions Hook', () => {
  beforeEach(() => {
    useAuthStore.setState({
      user: null,
      isAuthenticated: false,
      isInitialized: true,
    });
  });

  it('xác định chính xác quyền của vai trò Admin (chuỗi "Admin" hoặc số 1)', () => {
    useAuthStore.setState({
      user: {
        id: 'u-admin',
        username: 'admin',
        fullName: 'Quản Trị Viên',
        role: 'Admin',
        isActive: true,
      },
      isAuthenticated: true,
      isInitialized: true,
    });

    const { result } = renderHook(() => usePermissions());

    expect(result.current.isAdmin).toBe(true);
    expect(result.current.isManager).toBe(false);
    expect(result.current.isViewer).toBe(false);
    expect(result.current.canWrite).toBe(true);
    expect(result.current.canAdmin).toBe(true);
    expect(result.current.canAccessRecycleBin).toBe(true);
    expect(result.current.hasRole([UserRole.Admin])).toBe(true);
    expect(result.current.hasRole([UserRole.Manager])).toBe(false);
  });

  it('xác định chính xác quyền của vai trò Manager (chuỗi "Manager" hoặc số 2)', () => {
    useAuthStore.setState({
      user: {
        id: 'u-mgr',
        username: 'manager',
        fullName: 'Quản Lý Bãi Xe',
        role: 'Manager',
        isActive: true,
      },
      isAuthenticated: true,
      isInitialized: true,
    });

    const { result } = renderHook(() => usePermissions());

    expect(result.current.isAdmin).toBe(false);
    expect(result.current.isManager).toBe(true);
    expect(result.current.isViewer).toBe(false);
    expect(result.current.canWrite).toBe(true);
    expect(result.current.canAdmin).toBe(false);
    expect(result.current.canAccessRecycleBin).toBe(true);
    expect(result.current.hasRole([UserRole.Admin])).toBe(false);
    expect(result.current.hasRole([UserRole.Manager, UserRole.Admin])).toBe(true);
  });

  it('xác định chính xác quyền của vai trò Viewer (chuỗi "Viewer" hoặc số 3)', () => {
    useAuthStore.setState({
      user: {
        id: 'u-view',
        username: 'viewer',
        fullName: 'Người Xem Báo Cáo',
        role: 'Viewer',
        isActive: true,
      },
      isAuthenticated: true,
      isInitialized: true,
    });

    const { result } = renderHook(() => usePermissions());

    expect(result.current.isAdmin).toBe(false);
    expect(result.current.isManager).toBe(false);
    expect(result.current.isViewer).toBe(true);
    expect(result.current.canWrite).toBe(false);
    expect(result.current.canAdmin).toBe(false);
    expect(result.current.canAccessRecycleBin).toBe(false);
    expect(result.current.hasRole([UserRole.Admin, UserRole.Manager])).toBe(false);
    expect(result.current.hasRole([UserRole.Viewer])).toBe(true);
  });

  it('trả về cờ false an toàn khi người dùng chưa đăng nhập', () => {
    const { result } = renderHook(() => usePermissions());

    expect(result.current.isAdmin).toBe(false);
    expect(result.current.isManager).toBe(false);
    expect(result.current.isViewer).toBe(false);
    expect(result.current.canWrite).toBe(false);
    expect(result.current.canAdmin).toBe(false);
    expect(result.current.canAccessRecycleBin).toBe(false);
  });
});
