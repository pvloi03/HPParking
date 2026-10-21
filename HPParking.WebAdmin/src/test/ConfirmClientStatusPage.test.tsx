import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { StrictMode } from 'react';
import { MemoryRouter } from 'react-router-dom';
import { ConfirmClientStatusPage } from '@/pages/ConfirmClientStatusPage';
import { clientApi } from '@/api/clientApi';
import { ClientApprovalActionType } from '@/types/client';

vi.mock('@/api/clientApi', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/api/clientApi')>();
  return {
    ...actual,
    clientApi: {
      ...actual.clientApi,
      confirmStatus: vi.fn(),
    },
  };
});

describe('ConfirmClientStatusPage Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('hiển thị thông báo lỗi khi không tìm thấy token trên URL', async () => {
    render(
      <MemoryRouter initialEntries={['/confirm-client-status']}>
        <ConfirmClientStatusPage />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Không Thể Hoàn Tất Phê Duyệt')).toBeInTheDocument();
      expect(
        screen.getByText('Không tìm thấy mã xác thực token hợp lệ trên đường dẫn liên kết.')
      ).toBeInTheDocument();
    });
  });

  it('hiển thị giao diện thành công khi xác nhận token hợp lệ', async () => {
    vi.mocked(clientApi.confirmStatus).mockResolvedValueOnce({
      clientId: 'client-123',
      clientName: 'Nguyễn Văn A',
      clientCode: '001200000001',
      actionType: ClientApprovalActionType.Activate,
      newActiveStatus: true,
      approvingDepartmentName: 'Phòng Hành chính Nhân sự',
      processedAt: '2026-10-20T10:00:00Z',
      message: 'Hồ sơ đã được kích hoạt thành công.',
    });

    render(
      <MemoryRouter initialEntries={['/confirm-client-status?token=valid-token-123']}>
        <ConfirmClientStatusPage />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Đã Kích Hoạt Hồ Sơ Nhân Sự')).toBeInTheDocument();
      expect(screen.getByText('Nguyễn Văn A')).toBeInTheDocument();
      expect(screen.getByText('001200000001')).toBeInTheDocument();
      expect(screen.getByText('✓ HOẠT ĐỘNG (ĐÃ KÍCH HOẠT)')).toBeInTheDocument();
      expect(screen.getByText('Phòng Hành chính Nhân sự')).toBeInTheDocument();
      expect(screen.getByText('Hồ sơ đã được kích hoạt thành công.')).toBeInTheDocument();
    });
  });

  it('hiển thị giao diện hết hạn riêng biệt (Expired State) khi liên kết quá 24 giờ', async () => {
    const expiredError = {
      isAxiosError: true,
      response: {
        status: 400,
        data: {
          message: 'Liên kết xác nhận đã hết hạn (quá 24 giờ). Vui lòng liên hệ quản trị viên để gửi lại yêu cầu mới.',
          errors: ['APPROVAL_TOKEN_EXPIRED'],
        },
      },
    };
    vi.mocked(clientApi.confirmStatus).mockRejectedValueOnce(expiredError);

    render(
      <MemoryRouter initialEntries={['/confirm-client-status?token=expired-token-456']}>
        <ConfirmClientStatusPage />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Yêu Cầu Phê Duyệt Đã Hết Hạn')).toBeInTheDocument();
      expect(screen.getByText('Liên kết đã hết hiệu lực')).toBeInTheDocument();
      expect(screen.getByText(/Hướng dẫn xử lý tiếp theo/i)).toBeInTheDocument();
      expect(screen.getByText(/Gửi lại email phê duyệt/i)).toBeInTheDocument();
    });
  });

  it('hiển thị giao diện đã xử lý riêng biệt (Already Processed State) khi token đã được xác nhận trước đó', async () => {
    const alreadyProcessedError = {
      isAxiosError: true,
      response: {
        status: 409,
        data: {
          message: 'Yêu cầu này đã được phê duyệt xác nhận vào lúc 20/10/2026 14:00.',
          errors: ['APPROVAL_REQUEST_ALREADY_PROCESSED'],
        },
      },
    };
    vi.mocked(clientApi.confirmStatus).mockRejectedValueOnce(alreadyProcessedError);

    render(
      <MemoryRouter initialEntries={['/confirm-client-status?token=used-token-789']}>
        <ConfirmClientStatusPage />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Yêu Cầu Này Đã Được Xử Lý')).toBeInTheDocument();
      expect(screen.getByText('Yêu cầu đã được xác nhận')).toBeInTheDocument();
      expect(screen.getByText(/Thông tin trạng thái/i)).toBeInTheDocument();
      expect(
        screen.getByText(/Yêu cầu này đã được phê duyệt xác nhận vào lúc 20\/10\/2026 14:00/i)
      ).toBeInTheDocument();
    });
  });

  it('hiển thị giao diện lỗi chung khi gặp lỗi khác', async () => {
    const genericError = {
      isAxiosError: true,
      response: {
        status: 404,
        data: {
          message: 'Mã xác thực không tồn tại hoặc không hợp lệ.',
        },
      },
    };
    vi.mocked(clientApi.confirmStatus).mockRejectedValueOnce(genericError);

    render(
      <MemoryRouter initialEntries={['/confirm-client-status?token=invalid-token-000']}>
        <ConfirmClientStatusPage />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Không Thể Hoàn Tất Phê Duyệt')).toBeInTheDocument();
      expect(screen.getByText('Xác nhận không thành công')).toBeInTheDocument();
      expect(screen.getByText('Mã xác thực không tồn tại hoặc không hợp lệ.')).toBeInTheDocument();
    });
  });

  it('hoạt động chính xác dưới React StrictMode (không bị kẹt loading)', async () => {
    vi.mocked(clientApi.confirmStatus).mockResolvedValueOnce({
      clientId: 'client-strict-1',
      clientName: 'Trần Thị B',
      clientCode: '001200000002',
      actionType: ClientApprovalActionType.Activate,
      newActiveStatus: true,
      approvingDepartmentName: 'Phòng Nhân sự',
      processedAt: '2026-10-21T09:00:00Z',
      message: 'Kích hoạt thành công',
    });

    render(
      <StrictMode>
        <MemoryRouter initialEntries={['/confirm-client-status?token=strict-mode-token']}>
          <ConfirmClientStatusPage />
        </MemoryRouter>
      </StrictMode>
    );

    await waitFor(() => {
      expect(screen.getByText('Đã Kích Hoạt Hồ Sơ Nhân Sự')).toBeInTheDocument();
      expect(screen.getByText('Trần Thị B')).toBeInTheDocument();
    });
  });
});