import { describe, it, expect, vi, beforeEach } from 'vitest';
import { defaultFeatureFlags, useFeatureFlagsStore, loadFeatureFlags } from '@/config/featureFlags';
import { systemApi } from '@/api/systemApi';

describe('Feature Flags Configuration & Fallback Tests', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('fallback mặc định đọc từ === "true" (false khi biến môi trường không được cấu hình)', () => {
    // Không cấu hình hoặc giá trị khác 'true' -> enableFleetDispatch phải là false
    expect(defaultFeatureFlags.enableFleetDispatch).toBe(false);
  });

  it('loadFeatureFlags cập nhật đúng trạng thái từ systemApi.getFeatures', async () => {
    vi.spyOn(systemApi, 'getFeatures').mockResolvedValueOnce({
      enableFleetDispatch: true,
    });

    const flags = await loadFeatureFlags();
    expect(flags.enableFleetDispatch).toBe(true);
    expect(useFeatureFlagsStore.getState().enableFleetDispatch).toBe(true);
  });

  it('loadFeatureFlags giữ nguyên fallback nếu API thất bại', async () => {
    useFeatureFlagsStore.setState({ enableFleetDispatch: false });
    vi.spyOn(systemApi, 'getFeatures').mockRejectedValueOnce(new Error('Network error'));

    const flags = await loadFeatureFlags();
    expect(flags.enableFleetDispatch).toBe(false);
  });
});
