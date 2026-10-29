import { describe, it, expect } from 'vitest';
import {
  getLocalToday,
  getLocalPastDate,
  getLocalMonthStart,
  getLocalMonthEnd,
  formatDateDisplay,
  formatDayMonth,
  formatMonthDisplay,
  toCompactDate,
} from '@/utils/dateUtils';

describe('dateUtils', () => {
  it('getLocalToday trả về định dạng YYYY-MM-DD', () => {
    const today = getLocalToday();
    expect(today).toMatch(/^\d{4}-\d{2}-\d{2}$/);
  });

  it('getLocalPastDate trả về định dạng YYYY-MM-DD', () => {
    const pastDate = getLocalPastDate(7);
    expect(pastDate).toMatch(/^\d{4}-\d{2}-\d{2}$/);
  });

  it('getLocalMonthStart trả về ngày đầu tiên của tháng (-01)', () => {
    const start = getLocalMonthStart();
    expect(start).toMatch(/^\d{4}-\d{2}-01$/);
  });

  it('getLocalMonthEnd trả về ngày cuối cùng hợp lệ của tháng', () => {
    const end = getLocalMonthEnd();
    expect(end).toMatch(/^\d{4}-\d{2}-(28|29|30|31)$/);
  });

  it('formatDateDisplay format ngày ISO sang DD/MM/YYYY', () => {
    expect(formatDateDisplay('2026-10-29')).toBe('29/10/2026');
    expect(formatDateDisplay('')).toBe('');
    expect(formatDateDisplay(undefined)).toBe('');
    expect(formatDateDisplay('invalid')).toBe('invalid');
  });

  it('formatDayMonth format ngày ISO sang DD/MM', () => {
    expect(formatDayMonth('2026-10-29')).toBe('29/10');
    expect(formatDayMonth('')).toBe('');
    expect(formatDayMonth(undefined)).toBe('');
    expect(formatDayMonth('invalid')).toBe('invalid');
  });

  it('formatMonthDisplay format tháng ISO sang MM/YYYY', () => {
    expect(formatMonthDisplay('2026-10')).toBe('10/2026');
    expect(formatMonthDisplay('')).toBe('');
    expect(formatMonthDisplay(undefined)).toBe('');
    expect(formatMonthDisplay('invalid')).toBe('invalid');
  });

  it('toCompactDate format ngày ISO sang yyyyMMdd (tên tệp báo cáo, ADR 0023)', () => {
    expect(toCompactDate('2026-10-28')).toBe('20261028');
    expect(toCompactDate('')).toBe('');
    expect(toCompactDate(undefined)).toBe('');
    expect(toCompactDate('invalid')).toBe('invalid');
  });
});
