import { useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '@/stores/authStore';
import { authApi } from '@/api/authApi';
import type { LoginRequest } from '@/types/auth';

export function useAuth() {
  const { user, isAuthenticated, isInitialized, setAuth, clearAuth, setInitialized } =
    useAuthStore();
  const navigate = useNavigate();

  const login = useCallback(
    async (credentials: LoginRequest) => {
      const res = await authApi.login(credentials);
      setAuth(res.user);
      return res;
    },
    [setAuth]
  );

  const logout = useCallback(async () => {
    try {
      await authApi.logout();
    } finally {
      clearAuth();
      navigate('/login');
    }
  }, [clearAuth, navigate]);

  const checkAuth = useCallback(async () => {
    try {
      const currentUser = await authApi.getCurrentUser();
      setAuth(currentUser);
    } catch {
      clearAuth();
    } finally {
      setInitialized(true);
    }
  }, [setAuth, clearAuth, setInitialized]);

  const isAdmin = user?.role === 'Admin';
  const isManager = user?.role === 'Manager' || isAdmin;

  return {
    user,
    isAuthenticated,
    isInitialized,
    isAdmin,
    isManager,
    login,
    logout,
    checkAuth,
  };
}
