import { normalizeVietnamese } from '@/lib/select-search';
import type { DepartmentDto } from '@/types/masterData';

/**
 * Tìm ID phòng ban nhân sự mặc định (chứa từ khóa 'nhan su' hoặc 'hr' sau khi chuẩn hóa tiếng Việt).
 * Nếu không có phòng ban nào khớp từ khóa, trả về ID của phòng ban đầu tiên trong danh sách.
 */
export function findDefaultHrDepartmentId(depts: DepartmentDto[]): string {
  if (!depts || depts.length === 0) return '';
  const hrDept = depts.find((d) => {
    const clean = normalizeVietnamese(d.name);
    return clean.includes('nhan su') || /\bhr\b/.test(clean);
  });
  return hrDept ? hrDept.id : depts[0].id;
}
