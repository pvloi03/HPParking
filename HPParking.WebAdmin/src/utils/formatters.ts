/**
 * Định dạng chuỗi ngày tháng ISO sang định dạng chuẩn tiếng Việt (dd/MM/yyyy, HH:mm:ss).
 * Trả về '—' nếu chuỗi ngày tháng rỗng hoặc không hợp lệ.
 */
export function formatDateTimeVi(isoString?: string | null): string {
  if (!isoString) return '—';
  try {
    const d = new Date(isoString);
    return isNaN(d.getTime()) ? '—' : d.toLocaleString('vi-VN');
  } catch {
    return '—';
  }
}
