/**
 * Utility helper functions for client-side date formatting and ranges (Vietnam local time)
 */

export function getLocalToday(): string {
  const now = new Date();
  const year = now.getFullYear();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

export function getLocalPastDate(daysAgo: number): string {
  const d = new Date(Date.now() - daysAgo * 86400000);
  const year = d.getFullYear();
  const month = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

export interface MonthRange {
  fromDate: string;
  toDate: string;
  year: number;
  month: number;
  daysInMonth: number;
}

/**
 * Chuyển đổi chuỗi tháng (định dạng chuẩn YYYY-MM) sang khoảng thời gian { fromDate, toDate }.
 * Chỉ chấp nhận định dạng chính xác YYYY-MM (loại trừ định dạng YYYY-MM-DD hoặc chuỗi không hợp lệ).
 * Nếu không hợp lệ hoặc thiếu, tự động fallback về tháng hiện tại (local).
 */
export function toMonthRange(monthStr?: string): MonthRange {
  const match = monthStr?.trim().match(/^(\d{4})-(0[1-9]|1[0-2])$/);
  if (match) {
    const year = Number(match[1]);
    const month = Number(match[2]);
    const monthFormatted = String(month).padStart(2, '0');
    const daysInMonth = new Date(year, month, 0).getDate();
    const daysFormatted = String(daysInMonth).padStart(2, '0');
    return {
      fromDate: `${year}-${monthFormatted}-01T00:00:00`,
      toDate: `${year}-${monthFormatted}-${daysFormatted}T23:59:59`,
      year,
      month,
      daysInMonth,
    };
  }

  const now = new Date();
  const year = now.getFullYear();
  const month = now.getMonth() + 1;
  const monthFormatted = String(month).padStart(2, '0');
  const daysInMonth = new Date(year, month, 0).getDate();
  const daysFormatted = String(daysInMonth).padStart(2, '0');
  return {
    fromDate: `${year}-${monthFormatted}-01T00:00:00`,
    toDate: `${year}-${monthFormatted}-${daysFormatted}T23:59:59`,
    year,
    month,
    daysInMonth,
  };
}

export function getLocalMonthStart(): string {
  return toMonthRange().fromDate.slice(0, 10);
}

export function getLocalMonthEnd(): string {
  return toMonthRange().toDate.slice(0, 10);
}

export function formatDateDisplay(isoDateStr?: string): string {
  if (!isoDateStr) return '';
  const parts = isoDateStr.split('-');
  if (parts.length !== 3) return isoDateStr;
  return `${parts[2]}/${parts[1]}/${parts[0]}`;
}

/**
 * Định dạng ngày ISO (YYYY-MM-DD) sang DD/MM — dùng cho trục ngắn của biểu đồ
 */
export function formatDayMonth(isoDateStr?: string): string {
  if (!isoDateStr) return '';
  const parts = isoDateStr.split('-');
  if (parts.length !== 3) return isoDateStr;
  return `${parts[2]}/${parts[1]}`;
}

/**
 * Định dạng tháng ISO (YYYY-MM) sang MM/YYYY — dùng cho trục tháng của biểu đồ
 */
export function formatMonthDisplay(isoMonthStr?: string): string {
  if (!isoMonthStr) return '';
  const parts = isoMonthStr.split('-');
  if (parts.length !== 2) return isoMonthStr;
  return `${parts[1]}/${parts[0]}`;
}

/**
 * Định dạng ngày ISO (YYYY-MM-DD) sang compact yyyyMMdd — dùng cho tên tệp báo cáo
 * theo phạm vi thời gian (ADR 0023, phải khớp tên do backend sinh cho audit log).
 */
export function toCompactDate(isoDateStr?: string): string {
  if (!isoDateStr) return '';
  const parts = isoDateStr.split('-');
  if (parts.length !== 3) return isoDateStr;
  return `${parts[0]}${parts[1]}${parts[2]}`;
}
