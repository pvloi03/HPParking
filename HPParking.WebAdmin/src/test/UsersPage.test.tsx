import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { UsersPage } from '@/pages/UsersPage';
import { usersApi } from '@/api/userApi';
import { UserRole } from '@/types/user';

vi.mock('@/api/userApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/userApi')>();
  return {
    ...actual,
    usersApi: {
      getPaged: vi.fn(),
      getById: vi.fn(),
      create: vi.fn(),
      update: vi.fn(),
      delete: vi.fn(),
      restore: vi.fn(),
      resetPassword: vi.fn(),
      toggleStatus: vi.fn(),
    },
  };
});

describe('UsersPage Component', () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    vi.clearAllMocks();
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });
  });

  it('hiển thị tiêu đề trang và danh sách người dùng tải về từ API', async () => {
    vi.mocked(usersApi.getPaged).mockResolvedValueOnce({
      items: [
        {
          id: 'user-01',
          username: 'admin_test',
          fullName: 'Nguyễn Quản Trị',
          email: 'admin@hpparking.vn',
          phoneNumber: '0912345678',
          role: UserRole.Admin,
          isActive: true,
          lastLoginAt: new Date().toISOString(),
          createdAt: new Date().toISOString(),
        },
      ],
      pagination: {
        pageIndex: 1,
        pageSize: 15,
        totalCount: 1,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    });

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter>
          <UsersPage />
        </MemoryRouter>
      </QueryClientProvider>
    );

    expect(
      screen.getByText(/Quản Lý Tài Khoản Người Dùng/i)
    ).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getAllByText('admin_test').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Nguyễn Quản Trị').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Quản trị viên').length).toBeGreaterThan(0);
    });
  });
});
