import { apiClient } from '@/api/client';
import type {
  ApiResponse,
  LoginRequest,
  LoginResponse,
  RefreshTokenResponse,
  UserInfoDto,
  ChangePasswordRequest,
} from '@/types/auth';

export const authApi = {
  async login(credentials: LoginRequest): Promise<LoginResponse> {
    const res = await apiClient.post<ApiResponse<LoginResponse>>(
      '/v1/auth/login',
      credentials
    );
    if (!res.data.success || !res.data.data) {
      throw new Error(res.data.message || 'Đăng nhập không thành công');
    }
    return res.data.data;
  },

  async getCurrentUser(): Promise<UserInfoDto> {
    const res = await apiClient.get<ApiResponse<UserInfoDto>>('/v1/auth/me');
    if (!res.data.success || !res.data.data) {
      throw new Error(res.data.message || 'Không tìm thấy thông tin tài khoản');
    }
    return res.data.data;
  },

  async refreshToken(): Promise<RefreshTokenResponse> {
    const res = await apiClient.post<ApiResponse<RefreshTokenResponse>>(
      '/v1/auth/refresh-token'
    );
    if (!res.data.success || !res.data.data) {
      throw new Error(res.data.message || 'Làm mới token thất bại');
    }
    return res.data.data;
  },

  async logout(): Promise<void> {
    try {
      await apiClient.post<ApiResponse<string>>('/v1/auth/logout');
    } catch {
      // Tiếp tục ngay cả khi server trả lỗi
    }
  },

  async changePassword(data: ChangePasswordRequest): Promise<boolean> {
    const res = await apiClient.post<ApiResponse<boolean>>(
      '/v1/auth/change-password',
      data
    );
    return res.data.success;
  },
};
