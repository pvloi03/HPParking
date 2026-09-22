import { describe, it, expect, beforeEach } from 'vitest';
import { useAuthStore } from '../stores/authStore';
import type { UserInfoDto } from '../types/auth';

const mockUser: UserInfoDto = {
  id: 'usr-1',
  username: 'admin',
  fullName: 'Quản trị viên',
  email: 'admin@hpparking.vn',
  phoneNumber: '0987654321',
  role: 'Admin',
  isActive: true,
};

describe('useAuthStore', () => {
  beforeEach(() => {
    useAuthStore.setState({
      user: null,
      isAuthenticated: false,
      isInitialized: false,
    });
  });

  it('khởi tạo với trạng thái chưa đăng nhập và chưa nạp phiên', () => {
    const state = useAuthStore.getState();
    expect(state.user).toBeNull();
    expect(state.isAuthenticated).toBe(false);
    expect(state.isInitialized).toBe(false);
  });

  it('cập nhật trạng thái khi đăng nhập thành công', () => {
    useAuthStore.getState().setAuth(mockUser);

    const state = useAuthStore.getState();
    expect(state.user).toEqual(mockUser);
    expect(state.isAuthenticated).toBe(true);
    expect(state.isInitialized).toBe(true);
  });

  it('dọn sạch thông tin người dùng khi đăng xuất', () => {
    useAuthStore.getState().setAuth(mockUser);
    useAuthStore.getState().clearAuth();

    const state = useAuthStore.getState();
    expect(state.user).toBeNull();
    expect(state.isAuthenticated).toBe(false);
    expect(state.isInitialized).toBe(true);
  });
});
