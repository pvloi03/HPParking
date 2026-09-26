import { useMemo } from 'react';
import { useAuthStore } from '@/stores/authStore';
import { UserRole } from '@/types/user';

/**
 * Chuẩn hóa giá trị role từ nhiều dạng khác nhau (enum number, string tên, string số)
 */
export function normalizeRole(role: unknown): UserRole | undefined {
  if (role === null || role === undefined) return undefined;
  if (role === UserRole.Admin || role === 'Admin' || role === '1' || role === 1) {
    return UserRole.Admin;
  }
  if (role === UserRole.Manager || role === 'Manager' || role === '2' || role === 2) {
    return UserRole.Manager;
  }
  if (role === UserRole.Viewer || role === 'Viewer' || role === '3' || role === 3) {
    return UserRole.Viewer;
  }
  return undefined;
}

/**
 * Hook phân quyền tập trung cho toàn bộ ứng dụng WebAdmin.
 * Nguyên tắc: "Không có quyền thì ẨN HOÀN TOÀN, không hiển thị trên DOM."
 */
export function usePermissions() {
  const user = useAuthStore((s) => s.user);
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);

  const role = useMemo(() => {
    if (!isAuthenticated || !user) return undefined;
    return normalizeRole(user.role);
  }, [user, isAuthenticated]);

  const isAdmin = role === UserRole.Admin;
  const isManager = role === UserRole.Manager;
  const isViewer = role === UserRole.Viewer;

  // Quyền ghi dữ liệu (Thêm, Sửa, Xóa mềm, Import Excel)
  const canWrite = isAdmin || isManager;

  // Quyền quản trị hệ thống tối cao (Quản lý User, Nhật ký kiểm toán, quản lý User trong Thùng rác)
  const canAdmin = isAdmin;

  // Quyền truy cập Thùng rác hệ thống (Chỉ Admin và Manager)
  const canAccessRecycleBin = isAdmin || isManager;

  /**
   * Kiểm tra xem role hiện tại có nằm trong danh sách các roles được phép hay không
   */
  const hasRole = (allowedRoles: (UserRole | string | number)[]): boolean => {
    if (!role) return false;
    const normalizedAllowed = allowedRoles
      .map(normalizeRole)
      .filter((r): r is UserRole => r !== undefined);
    return normalizedAllowed.includes(role);
  };

  return {
    user,
    role,
    isAdmin,
    isManager,
    isViewer,
    canWrite,
    canAdmin,
    canAccessRecycleBin,
    hasRole,
  };
}
