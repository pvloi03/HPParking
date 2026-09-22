import { z } from 'zod';

export const loginSchema = z.object({
  username: z
    .string()
    .trim()
    .min(1, 'Vui lòng nhập tên đăng nhập')
    .max(50, 'Tên đăng nhập không vượt quá 50 ký tự'),
  password: z
    .string()
    .min(6, 'Mật khẩu phải có tối thiểu 6 ký tự')
    .max(100, 'Mật khẩu không vượt quá 100 ký tự'),
});

export type LoginFormValues = z.infer<typeof loginSchema>;
