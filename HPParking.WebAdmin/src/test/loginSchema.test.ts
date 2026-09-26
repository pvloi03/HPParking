import { describe, it, expect } from 'vitest';
import { loginSchema } from '@/lib/validations/auth';

describe('loginSchema validation', () => {
  it('hợp lệ khi có username và password từ 6 ký tự trở lên', () => {
    const result = loginSchema.safeParse({
      username: 'admin',
      password: 'adminPassword123',
    });
    expect(result.success).toBe(true);
  });

  it('báo lỗi khi để trống username', () => {
    const result = loginSchema.safeParse({
      username: '   ',
      password: 'password123',
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toBe('Vui lòng nhập tên đăng nhập');
    }
  });

  it('báo lỗi khi mật khẩu ngắn hơn 6 ký tự', () => {
    const result = loginSchema.safeParse({
      username: 'admin',
      password: '12345',
    });
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toBe('Mật khẩu phải có tối thiểu 6 ký tự');
    }
  });
});
