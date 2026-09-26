import { describe, it, expect } from 'vitest';
import { formatAvatarUrl } from '@/utils/formatAvatarUrl';

describe('formatAvatarUrl Utility', () => {
  it('trả về chuỗi rỗng khi url rỗng hoặc undefined', () => {
    expect(formatAvatarUrl(undefined)).toBe('');
    expect(formatAvatarUrl(null)).toBe('');
    expect(formatAvatarUrl('')).toBe('');
    expect(formatAvatarUrl('   ')).toBe('');
  });

  it('chuẩn hóa đường dẫn tương đối thêm dấu / ở đầu', () => {
    expect(formatAvatarUrl('Avatar/001201012345.jpg')).toBe('/Avatar/001201012345.jpg');
    expect(formatAvatarUrl('/Avatar/001201012345.jpg')).toBe('/Avatar/001201012345.jpg');
  });

  it('giữ nguyên URL tuyệt đối và blob URI', () => {
    expect(formatAvatarUrl('https://example.com/avatar.jpg')).toBe('https://example.com/avatar.jpg');
    expect(formatAvatarUrl('blob:http://localhost:5173/abc-123')).toBe('blob:http://localhost:5173/abc-123');
    expect(formatAvatarUrl('data:image/jpeg;base64,xxxx')).toBe('data:image/jpeg;base64,xxxx');
  });

  it('gắn query param cache-busting ?t=... khi có version', () => {
    const updatedAt = '2026-09-25T15:30:00.000Z';
    const expectedTimestamp = new Date(updatedAt).getTime();

    const result = formatAvatarUrl('/Avatar/001201012345.jpg', updatedAt);
    expect(result).toBe(`/Avatar/001201012345.jpg?t=${expectedTimestamp}`);
  });

  it('thay đổi URL khi updatedAt thay đổi để ép trình duyệt tải ảnh mới', () => {
    const v1 = '2026-09-25T15:00:00.000Z';
    const v2 = '2026-09-25T16:00:00.000Z';

    const url1 = formatAvatarUrl('/Avatar/001201012345.jpg', v1);
    const url2 = formatAvatarUrl('/Avatar/001201012345.jpg', v2);

    expect(url1).not.toBe(url2);
    expect(url1).toContain(`?t=${new Date(v1).getTime()}`);
    expect(url2).toContain(`?t=${new Date(v2).getTime()}`);
  });

  it('không gắn query param vào blob URL dù có truyền version', () => {
    const blob = 'blob:http://localhost:5173/preview-123';
    expect(formatAvatarUrl(blob, '2026-09-25T15:00:00.000Z')).toBe(blob);
  });
});
