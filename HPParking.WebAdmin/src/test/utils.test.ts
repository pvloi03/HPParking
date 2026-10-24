import { describe, it, expect } from 'vitest';
import { formatDateTimeVi, formatInOrderNumber, formatInOrderTag } from '@/utils/formatters';
import { createEmptyPagedResult } from '@/utils/pagination';

describe('formatDateTimeVi', () => {
  it('trả về "—" khi chuỗi rỗng hoặc undefined/null', () => {
    expect(formatDateTimeVi()).toBe('—');
    expect(formatDateTimeVi(null)).toBe('—');
    expect(formatDateTimeVi('')).toBe('—');
  });

  it('trả về "—" khi chuỗi ngày tháng không hợp lệ', () => {
    expect(formatDateTimeVi('invalid-date')).toBe('—');
  });

  it('định dạng ngày tháng hợp lệ sang chuỗi locale vi-VN', () => {
    const isoString = '2026-09-28T07:15:00.000Z';
    const formatted = formatDateTimeVi(isoString);
    expect(formatted).not.toBe('—');
    expect(formatted).toContain('2026');
  });
});

describe('formatInOrderNumber', () => {
  it('trả về "--" khi session là null hoặc undefined', () => {
    expect(formatInOrderNumber(null)).toBe('--');
    expect(formatInOrderNumber(undefined)).toBe('--');
  });

  it('trả về "--" khi inTime bị thiếu hoặc rỗng', () => {
    expect(formatInOrderNumber({ inTime: null, inOrderNumber: 42 })).toBe('--');
    expect(formatInOrderNumber({ inTime: '', inOrderNumber: 42 })).toBe('--');
  });

  it('trả về "--" khi inOrderNumber bị thiếu hoặc null', () => {
    expect(formatInOrderNumber({ inTime: '2026-09-28T07:15:00Z', inOrderNumber: null })).toBe('--');
    expect(formatInOrderNumber({ inTime: '2026-09-28T07:15:00Z', inOrderNumber: undefined })).toBe('--');
  });

  it('trả về "#<inOrderNumber>" khi có cả inTime và inOrderNumber', () => {
    expect(formatInOrderNumber({ inTime: '2026-09-28T07:15:00Z', inOrderNumber: 137 })).toBe('#137');
    expect(formatInOrderNumber({ inTime: '2026-09-28T07:15:00Z', inOrderNumber: 1 })).toBe('#1');
  });
});

describe('formatInOrderTag', () => {
  it('trả về chuỗi rỗng khi session là null hoặc thiếu inTime / inOrderNumber', () => {
    expect(formatInOrderTag(null)).toBe('');
    expect(formatInOrderTag(undefined)).toBe('');
    expect(formatInOrderTag({ inTime: null, inOrderNumber: 42 })).toBe('');
    expect(formatInOrderTag({ inTime: '2026-09-28T07:15:00Z', inOrderNumber: null })).toBe('');
  });

  it('trả về " #<inOrderNumber>" khi có cả inTime và inOrderNumber', () => {
    expect(formatInOrderTag({ inTime: '2026-09-28T07:15:00Z', inOrderNumber: 102 })).toBe(' #102');
  });
});

describe('createEmptyPagedResult', () => {
  it('tạo đối tượng phân trang rỗng với kích thước mặc định 10', () => {
    const result = createEmptyPagedResult();
    expect(result.items).toEqual([]);
    expect(result.pagination).toEqual({
      pageIndex: 1,
      pageSize: 10,
      totalCount: 0,
      totalPages: 0,
      hasPreviousPage: false,
      hasNextPage: false,
    });
  });

  it('tạo đối tượng phân trang rỗng với kích thước tùy chỉnh', () => {
    const result = createEmptyPagedResult(50);
    expect(result.pagination.pageSize).toBe(50);
  });
});
