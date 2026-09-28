import { describe, it, expect } from 'vitest';
import { DEFAULT_PAGE_SIZE } from '@/constants/pagination';
import { DEFAULT_PAGE_SIZE as REEXPORTED_PAGE_SIZE } from '@/types/masterData';

describe('Pagination Constants', () => {
  it('định nghĩa DEFAULT_PAGE_SIZE chuẩn là 10', () => {
    expect(DEFAULT_PAGE_SIZE).toBe(10);
    expect(REEXPORTED_PAGE_SIZE).toBe(10);
  });
});
