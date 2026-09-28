import { describe, it, expect } from 'vitest';
import { formatDateTimeVi } from '@/utils/formatters';
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
