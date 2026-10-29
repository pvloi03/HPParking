import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { RevenueReportPage } from '@/pages/RevenueReportPage';
import { statisticsApi } from '@/api/statisticsApi';
import { downloadBlob } from '@/utils/downloadBlob';
import { toast } from '@/hooks/use-toast';
import type { RevenueReportDto } from '@/types/statistics';
import type { ExportBlob } from '@/types/excel';

vi.mock('@/api/statisticsApi', () => ({
  statisticsApi: {
    getRevenueReport: vi.fn(),
    exportRevenueReportExcel: vi.fn(),
  },
}));

vi.mock('@/utils/downloadBlob', () => ({
  downloadBlob: vi.fn(),
}));

vi.mock('@/hooks/use-toast', () => ({
  toast: {
    success: vi.fn(),
    warning: vi.fn(),
    error: vi.fn(),
    info: vi.fn(),
  },
}));

const mockReportData: RevenueReportDto = {
  totalRevenue: 1500000,
  paidSessionsCount: 30,
  completedSessionsCount: 40,
  averageFeePerSession: 50000,
  paidRatio: 75.0,
  timeline: [
    {
      date: '2026-10-28',
      revenue: 600000,
      paidCount: 12,
      completedCount: 15,
      averageFee: 50000,
    },
    {
      date: '2026-10-29',
      revenue: 900000,
      paidCount: 18,
      completedCount: 25,
      averageFee: 50000,
    },
  ],
};

function renderWithClient(ui: React.ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
      },
    },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      {ui}
    </QueryClientProvider>
  );
}

describe('RevenueReportPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('hiển thị đầy đủ 4 thẻ KPI với giá trị được format tiền tệ và subtext', async () => {
    vi.mocked(statisticsApi.getRevenueReport).mockResolvedValue(mockReportData);

    renderWithClient(<RevenueReportPage />);

    // Chờ dữ liệu load xong
    await waitFor(() => {
      expect(screen.getByText('Phiên hoàn thành đã trả phí')).toBeInTheDocument();
    });

    expect(screen.getAllByText('1.500.000 đ').length).toBeGreaterThan(0);
    expect(screen.getAllByText('30 lượt').length).toBeGreaterThan(0);
    expect(screen.getByText('50.000 đ/lượt')).toBeInTheDocument();
    expect(screen.getByText('75.0%')).toBeInTheDocument();
    expect(screen.getByText(/30 \/ 40 lượt hoàn thành/i)).toBeInTheDocument();
  });

  it('xử lý an toàn khi không có lượt thu tiền và hoàn thành (mẫu số bằng 0)', async () => {
    const emptyReport: RevenueReportDto = {
      totalRevenue: 0,
      paidSessionsCount: 0,
      completedSessionsCount: 0,
      averageFeePerSession: 0,
      paidRatio: 0,
      timeline: [],
    };
    vi.mocked(statisticsApi.getRevenueReport).mockResolvedValue(emptyReport);

    renderWithClient(<RevenueReportPage />);

    await waitFor(() => {
      expect(screen.getByText('0.0%')).toBeInTheDocument();
    });

    expect(screen.getByText(/0 \/ 0 lượt hoàn thành/i)).toBeInTheDocument();
    expect(screen.getByText('0 đ/lượt')).toBeInTheDocument();
  });

  it('hiển thị bảng kê chi tiết từng ngày', async () => {
    vi.mocked(statisticsApi.getRevenueReport).mockResolvedValue(mockReportData);

    renderWithClient(<RevenueReportPage />);

    await waitFor(() => {
      expect(screen.getByText('28/10/2026')).toBeInTheDocument();
      expect(screen.getByText('29/10/2026')).toBeInTheDocument();
    });

    expect(screen.getByText('600.000 đ')).toBeInTheDocument();
    expect(screen.getByText('900.000 đ')).toBeInTheDocument();
  });

  it('chọn preset "7 ngày qua" và cập nhật khoảng ngày gửi API', async () => {
    vi.mocked(statisticsApi.getRevenueReport).mockResolvedValue(mockReportData);

    renderWithClient(<RevenueReportPage />);

    await waitFor(() => {
      expect(statisticsApi.getRevenueReport).toHaveBeenCalled();
    });

    const preset7DaysBtn = screen.getByRole('button', { name: '7 ngày qua' });
    fireEvent.click(preset7DaysBtn);

    await waitFor(() => {
      expect(statisticsApi.getRevenueReport).toHaveBeenCalledWith(
        expect.objectContaining({
          fromDate: expect.stringMatching(/^\d{4}-\d{2}-\d{2}T00:00:00$/),
          toDate: expect.stringMatching(/^\d{4}-\d{2}-\d{2}T23:59:59$/),
        })
      );
    });
  });

  it('gọi xuất Excel và xử lý tải file thành công', async () => {
    vi.mocked(statisticsApi.getRevenueReport).mockResolvedValue(mockReportData);
    const mockBlob = new Blob(['mock content']) as ExportBlob;
    mockBlob.isTruncated = false;
    vi.mocked(statisticsApi.exportRevenueReportExcel).mockResolvedValue(mockBlob);

    renderWithClient(<RevenueReportPage />);

    const exportBtn = screen.getByRole('button', { name: /Xuất Excel/i });
    await waitFor(() => {
      expect(exportBtn).not.toBeDisabled();
    });

    fireEvent.click(exportBtn);

    await waitFor(() => {
      expect(statisticsApi.exportRevenueReportExcel).toHaveBeenCalled();
      expect(downloadBlob).toHaveBeenCalledWith(
        mockBlob,
        expect.stringMatching(/^Bao_Cao_Doanh_Thu_\d{8}_\d{8}\.xlsx$/)
      );
      expect(toast.success).toHaveBeenCalledWith('Đã xuất báo cáo doanh thu thành công');
    });
  });

  it('cảnh báo toast warning khi tệp Excel bị cắt ngắn (isTruncated = true)', async () => {
    vi.mocked(statisticsApi.getRevenueReport).mockResolvedValue(mockReportData);
    const mockBlob = new Blob(['mock content']) as ExportBlob;
    mockBlob.isTruncated = true;
    vi.mocked(statisticsApi.exportRevenueReportExcel).mockResolvedValue(mockBlob);

    renderWithClient(<RevenueReportPage />);

    const exportBtn = screen.getByRole('button', { name: /Xuất Excel/i });
    await waitFor(() => {
      expect(exportBtn).not.toBeDisabled();
    });

    fireEvent.click(exportBtn);

    await waitFor(() => {
      expect(toast.warning).toHaveBeenCalledWith(
        expect.stringContaining('10.000'),
        expect.anything()
      );
    });
  });

  it('chọn preset "Tháng này" và cập nhật khoảng ngày từ đầu tháng đến cuối tháng gửi API', async () => {
    vi.mocked(statisticsApi.getRevenueReport).mockResolvedValue(mockReportData);

    renderWithClient(<RevenueReportPage />);

    await waitFor(() => {
      expect(statisticsApi.getRevenueReport).toHaveBeenCalled();
    });

    const presetMonthBtn = screen.getByRole('button', { name: 'Tháng này' });
    fireEvent.click(presetMonthBtn);

    await waitFor(() => {
      expect(statisticsApi.getRevenueReport).toHaveBeenCalledWith(
        expect.objectContaining({
          fromDate: expect.stringMatching(/^\d{4}-\d{2}-01T00:00:00$/),
          toDate: expect.stringMatching(/^\d{4}-\d{2}-\d{2}T23:59:59$/),
        })
      );
    });
  });
});
