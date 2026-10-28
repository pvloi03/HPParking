import { create } from 'zustand';
import { systemApi } from '@/api/systemApi';

export interface FeatureFlags {
  enableFleetDispatch: boolean;
}

export const defaultFeatureFlags: FeatureFlags = {
  enableFleetDispatch: import.meta.env.VITE_ENABLE_FLEET_DISPATCH === 'true',
};

interface FeatureFlagsState extends FeatureFlags {
  setFlags: (flags: Partial<FeatureFlags>) => void;
  loadFlags: () => Promise<FeatureFlags>;
}

export const useFeatureFlagsStore = create<FeatureFlagsState>((set, get) => ({
  ...defaultFeatureFlags,
  setFlags: (flags) => set((state) => ({ ...state, ...flags })),
  loadFlags: async () => {
    try {
      const remote = await systemApi.getFeatures();
      const newFlags = {
        enableFleetDispatch: remote.enableFleetDispatch ?? defaultFeatureFlags.enableFleetDispatch,
      };
      set(newFlags);
      return newFlags;
    } catch {
      return get();
    }
  },
}));

export function useFeatureFlags(): FeatureFlags {
  return useFeatureFlagsStore();
}

export async function loadFeatureFlags(): Promise<FeatureFlags> {
  return useFeatureFlagsStore.getState().loadFlags();
}

export function getFeatureFlags(): FeatureFlags {
  return useFeatureFlagsStore.getState();
}
