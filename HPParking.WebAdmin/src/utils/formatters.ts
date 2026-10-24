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

/**
 * Định dạng số thứ tự lượt vào dạng #<số> (ví dụ #137).
 * Trả về '--' nếu không có inTime hoặc inOrderNumber.
 */
export function formatInOrderNumber(session?: { inTime?: string | null; inOrderNumber?: number | null } | null): string {
  return session?.inTime && session?.inOrderNumber ? `#${session.inOrderNumber}` : '--';
}

/**
 * Định dạng nhãn số thứ tự cho tiêu đề dialog (ví dụ ' #137' hoặc chuỗi rỗng '').
 */
export function formatInOrderTag(session?: { inTime?: string | null; inOrderNumber?: number | null } | null): string {
  return session?.inTime && session?.inOrderNumber ? ` #${session.inOrderNumber}` : '';
}
