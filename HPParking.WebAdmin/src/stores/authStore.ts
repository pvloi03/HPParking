import { create } from 'zustand';
import type { UserInfoDto } from '@/types/auth';

interface AuthState {
  user: UserInfoDto | null;
  isAuthenticated: boolean;
  isInitialized: boolean;
  setAuth: (user: UserInfoDto) => void;
  clearAuth: () => void;
  setInitialized: (initialized: boolean) => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  isAuthenticated: false,
  isInitialized: false,

  setAuth: (user) =>
    set({
      user,
      isAuthenticated: true,
      isInitialized: true,
    }),

  clearAuth: () =>
    set({
      user: null,
      isAuthenticated: false,
      isInitialized: true,
    }),

  setInitialized: (isInitialized) =>
    set({
      isInitialized,
    }),
}));
