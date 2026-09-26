import type { PaginationQuery } from './masterData';

export const UserRole = {
  Admin: 1,
  Manager: 2,
  Viewer: 3,
} as const;

export type UserRole = (typeof UserRole)[keyof typeof UserRole];

export const USER_ROLE_LABELS: Record<UserRole | string | number, string> = {
  [UserRole.Admin]: 'Quản trị viên (Admin)',
  [UserRole.Manager]: 'Quản lý (Manager)',
  [UserRole.Viewer]: 'Người xem (Viewer)',
  Admin: 'Quản trị viên (Admin)',
  Manager: 'Quản lý (Manager)',
  Viewer: 'Người xem (Viewer)',
};

export const USER_ROLE_BADGES: Record<
  UserRole | string | number,
  { label: string; variant: 'default' | 'secondary' | 'outline' | 'destructive' }
> = {
  [UserRole.Admin]: { label: 'Quản trị viên', variant: 'destructive' },
  [UserRole.Manager]: { label: 'Quản lý', variant: 'default' },
  [UserRole.Viewer]: { label: 'Người xem', variant: 'secondary' },
  Admin: { label: 'Quản trị viên', variant: 'destructive' },
  Manager: { label: 'Quản lý', variant: 'default' },
  Viewer: { label: 'Người xem', variant: 'secondary' },
};

export interface UserDto {
  id: string;
  username: string;
  fullName: string;
  email?: string | null;
  phoneNumber?: string | null;
  role: UserRole | string;
  isActive: boolean;
  note?: string | null;
  lastLoginAt?: string | null;
  lastLogoutAt?: string | null;
  createdAt: string;
  updatedAt?: string | null;
  isDeleted?: boolean;
}

export interface CreateUserRequest {
  username: string;
  password: string;
  fullName: string;
  email?: string | null;
  phoneNumber?: string | null;
  role: UserRole;
  isActive?: boolean;
  note?: string | null;
}

export interface UpdateUserRequest {
  fullName: string;
  email?: string | null;
  phoneNumber?: string | null;
  role: UserRole;
  isActive: boolean;
  note?: string | null;
}

export interface ResetPasswordRequest {
  newPassword: string;
  confirmNewPassword?: string;
}

export interface UserFilterQuery extends PaginationQuery {
  keyword?: string;
  role?: UserRole;
  isActive?: boolean;
}
