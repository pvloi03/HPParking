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
  toMonthRange,
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

  describe('toMonthRange', () => {
    it('chuyển đổi đúng tháng hợp lệ YYYY-MM', () => {
      const range = toMonthRange('2026-10');
      expect(range.fromDate).toBe('2026-10-01T00:00:00');
      expect(range.toDate).toBe('2026-10-31T23:59:59');
      expect(range.year).toBe(2026);
      expect(range.month).toBe(10);
      expect(range.daysInMonth).toBe(31);
    });

    it('tính đúng số ngày cho tháng 2 năm thường và năm nhuận', () => {
      const nonLeap = toMonthRange('2026-02');
      expect(nonLeap.fromDate).toBe('2026-02-01T00:00:00');
      expect(nonLeap.toDate).toBe('2026-02-28T23:59:59');
      expect(nonLeap.daysInMonth).toBe(28);

      const leap = toMonthRange('2024-02');
      expect(leap.fromDate).toBe('2024-02-01T00:00:00');
      expect(leap.toDate).toBe('2024-02-29T23:59:59');
      expect(leap.daysInMonth).toBe(29);
    });

    it('loại trừ input dạng YYYY-MM-DD lọt qua guard và fallback về tháng hiện tại', () => {
      // Khi input là ngày đầy đủ '2026-10-15', guard phải từ chối và fallback
      const range = toMonthRange('2026-10-15');
      const start = getLocalMonthStart();
      const end = getLocalMonthEnd();
      expect(range.fromDate).toBe(`${start}T00:00:00`);
      expect(range.toDate).toBe(`${end}T23:59:59`);
      expect(range.fromDate).not.toContain('2026-10-15-01');
    });

    it('fallback về tháng hiện tại khi chuỗi rỗng, undefined hoặc tháng không hợp lệ', () => {
      const empty = toMonthRange('');
      const undef = toMonthRange(undefined);
      const invalidMonth = toMonthRange('2026-13');
      const garbage = toMonthRange('invalid');

      const start = getLocalMonthStart();
      const end = getLocalMonthEnd();

      expect(empty.fromDate).toBe(`${start}T00:00:00`);
      expect(undef.fromDate).toBe(`${start}T00:00:00`);
      expect(invalidMonth.fromDate).toBe(`${start}T00:00:00`);
      expect(garbage.fromDate).toBe(`${start}T00:00:00`);

      expect(empty.toDate).toBe(`${end}T23:59:59`);
      expect(undef.toDate).toBe(`${end}T23:59:59`);
      expect(invalidMonth.toDate).toBe(`${end}T23:59:59`);
      expect(garbage.toDate).toBe(`${end}T23:59:59`);
    });
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
