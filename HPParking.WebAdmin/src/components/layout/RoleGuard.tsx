import React from 'react';
import { Navigate, Outlet } from 'react-router-dom';
import { usePermissions } from '@/hooks/usePermissions';
import { UserRole } from '@/types/user';

interface RoleGuardProps {
  allowedRoles: (UserRole | string | number)[];
  redirectTo?: string;
  children?: React.ReactNode;
}

/**
 * Chốt chặn bảo vệ các trang theo vai trò (RBAC Route Guard).
 * Nếu người dùng không có quyền tương ứng, tự động điều hướng về redirectTo (mặc định '/dashboard').
 */
export function RoleGuard({
  allowedRoles,
  redirectTo = '/dashboard',
  children,
}: RoleGuardProps) {
  const { hasRole } = usePermissions();

  const isAllowed = hasRole(allowedRoles);

  if (!isAllowed) {
    return <Navigate to={redirectTo} replace />;
  }

  return children ? <>{children}</> : <Outlet />;
}
