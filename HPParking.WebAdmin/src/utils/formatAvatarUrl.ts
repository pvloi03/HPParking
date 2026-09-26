/**
 * Chuẩn hóa URL ảnh đại diện (avatar) của khách hàng và thêm query param cache-busting (?t=...)
 * nhằm đảm bảo trình duyệt luôn nhận diện và tải ảnh mới nhất ngay khi file ảnh trên máy chủ được cập nhật.
 *
 * @param url Đường dẫn ảnh từ server (ví dụ: '/Avatar/001201012345.jpg' hoặc URL đầy đủ)
 * @param version Thời gian cập nhật hoặc chuỗi đánh dấu phiên bản (thường là client.updatedAt || client.createdAt)
 * @returns URL chuẩn hóa kèm query parameter ?t=... (nếu có version)
 */
export function formatAvatarUrl(url?: string | null, version?: string | number): string {
  if (!url) return '';
  const clean = url.trim();
  if (!clean) return '';

  // Blob URL (ảnh vừa chọn preview ở client) hoặc data URI không cần cache-busting
  if (clean.startsWith('blob:') || clean.startsWith('data:')) {
    return clean;
  }

  const base = clean.startsWith('http://') || clean.startsWith('https://')
    ? clean
    : clean.startsWith('/')
      ? clean
      : `/${clean}`;

  if (version) {
    let timestamp: number | string = version;
    if (typeof version === 'string') {
      const parsed = new Date(version).getTime();
      timestamp = !isNaN(parsed) && parsed > 0 ? parsed : version;
    }
    const separator = base.includes('?') ? '&' : '?';
    return `${base}${separator}t=${timestamp}`;
  }

  return base;
}
