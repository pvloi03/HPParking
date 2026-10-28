import React from 'react';
import { Navigate, Outlet } from 'react-router-dom';
import { useFeatureFlags, type FeatureFlags } from '@/config/featureFlags';

interface FeatureGuardProps {
  feature: keyof FeatureFlags;
  redirectTo?: string;
  children?: React.ReactNode;
}

/**
 * Chốt chặn bảo vệ các trang theo cấu hình tính năng hệ thống (Feature Flags).
 * Nếu tính năng bị tắt, tự động điều hướng về redirectTo (mặc định '/dashboard').
 */
export function FeatureGuard({
  feature,
  redirectTo = '/dashboard',
  children,
}: FeatureGuardProps) {
  const flags = useFeatureFlags();

  if (!flags[feature]) {
    return <Navigate to={redirectTo} replace />;
  }

  return children ? <>{children}</> : <Outlet />;
}
