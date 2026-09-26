export interface ApiResponse<T = unknown> {
  success: boolean;
  data?: T;
  message: string;
  errors: string[];
  traceId?: string;
  timestamp: string;
}

export interface UserInfoDto {
  id: string;
  username: string;
  fullName: string;
  email?: string | null;
  phoneNumber?: string | null;
  role: string | number;
  isActive: boolean;
  lastLoginAt?: string | null;
  createdAt?: string;
  updatedAt?: string | null;
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken?: string;
  expiresIn: number;
  tokenType: string;
  user: UserInfoDto;
}

export interface RefreshTokenResponse {
  accessToken: string;
  refreshToken?: string;
  expiresIn: number;
  tokenType: string;
}

export interface ChangePasswordRequest {
  oldPassword?: string;
  newPassword: string;
  confirmNewPassword?: string;
}
