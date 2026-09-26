import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { ExcelImportDialog } from '@/components/common/ExcelImportDialog';
import { excelApi } from '@/api/excelApi';
import { downloadBlob } from '@/utils/downloadBlob';
import { toast } from 'sonner';
import { DuplicateMode } from '@/types/excel';

vi.mock('@/api/excelApi', () => ({
  excelApi: {
    downloadTemplate: vi.fn(),
    importData: vi.fn(),
  },
  extractErrorMessage: vi.fn((err: any) => err?.message || 'Có lỗi xảy ra'),
}));

vi.mock('@/utils/downloadBlob', () => ({
  downloadBlob: vi.fn(),
}));

vi.mock('sonner', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
    warning: vi.fn(),
  },
}));

describe('ExcelImportDialog Component', () => {
  const defaultProps = {
    open: true,
    onOpenChange: vi.fn(),
    entity: 'companies' as const,
    onSuccess: vi.fn(),
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('hiển thị tiêu đề thực thể tương ứng khi mở dialog', () => {
    render(<ExcelImportDialog {...defaultProps} />);

    expect(screen.getByText(/Nhập Dữ Liệu Excel:.*Công ty/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Tải tệp mẫu/i })).toBeInTheDocument();
    expect(screen.getByText(/Kéo thả tệp .xlsx vào đây/i)).toBeInTheDocument();
  });

  it('tải tệp mẫu khi người dùng nhấn nút Tải tệp mẫu', async () => {
    const mockBlob = new Blob(['mock binary']);
    vi.mocked(excelApi.downloadTemplate).mockResolvedValueOnce(mockBlob);

    render(<ExcelImportDialog {...defaultProps} />);

    const downloadBtn = screen.getByRole('button', { name: /Tải tệp mẫu/i });
    fireEvent.click(downloadBtn);

    await waitFor(() => {
      expect(excelApi.downloadTemplate).toHaveBeenCalledWith('companies');
      expect(downloadBlob).toHaveBeenCalledWith(mockBlob, 'mau_nhap_lieu_companies.xlsx');
    });
  });

  it('từ chối tệp không đúng định dạng .xlsx', () => {
    render(<ExcelImportDialog {...defaultProps} />);

    const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
    const invalidFile = new File(['dummy'], 'test.txt', { type: 'text/plain' });

    fireEvent.change(fileInput, { target: { files: [invalidFile] } });

    expect(vi.mocked(toast.error)).toHaveBeenCalledWith('Vui lòng chọn tệp bảng tính định dạng Excel (.xlsx).');
  });

  it('chấp nhận tệp .xlsx hợp lệ và hiển thị thông tin tệp', () => {
    render(<ExcelImportDialog {...defaultProps} />);

    const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
    const validFile = new File(['binary'], 'companies_list.xlsx', {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    });

    fireEvent.change(fileInput, { target: { files: [validFile] } });

    expect(screen.getByText('companies_list.xlsx')).toBeInTheDocument();
    expect(screen.getByText(/Sẵn sàng tải lên/i)).toBeInTheDocument();
  });

  it('bật checkbox kiểm tra thử nghiệm (dryRun)', () => {
    render(<ExcelImportDialog {...defaultProps} />);

    const dryRunCheckbox = screen.getByLabelText(/Chế độ kiểm tra thử/i) as HTMLInputElement;
    expect(dryRunCheckbox.checked).toBe(false);

    fireEvent.click(dryRunCheckbox);
    expect(dryRunCheckbox.checked).toBe(true);

    expect(screen.getByText(/Kiểm tra thử nghiệm/i)).toBeInTheDocument();
  });

  it('thực hiện import dữ liệu thành công và hiển thị thống kê & bảng lỗi', async () => {
    const mockImportResult = {
      totalRows: 5,
      successCount: 4,
      skippedCount: 0,
      failedCount: 1,
      isDryRun: false,
      errors: [
        {
          row: 3,
          column: 'PhoneNumber',
          value: '0123abc',
          errorMessage: 'Số điện thoại không đúng định dạng',
        },
      ],
    };
    vi.mocked(excelApi.importData).mockResolvedValueOnce(mockImportResult);

    render(<ExcelImportDialog {...defaultProps} />);

    const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
    const validFile = new File(['binary'], 'data.xlsx', {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    });
    fireEvent.change(fileInput, { target: { files: [validFile] } });

    const submitBtn = screen.getByRole('button', { name: /Nhập dữ liệu ngay/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(excelApi.importData).toHaveBeenCalledWith(
        'companies',
        validFile,
        false,
        DuplicateMode.Skip
      );
      expect(defaultProps.onSuccess).toHaveBeenCalled();
    });

    // Kiểm tra render thống kê
    expect(screen.getByText('4')).toBeInTheDocument();
    expect(screen.getByText('1')).toBeInTheDocument();

    // Kiểm tra bảng lỗi chi tiết
    expect(screen.getByText(/Chi Tiết Các Dòng Dữ Liệu Không Hợp Lệ/i)).toBeInTheDocument();
    expect(screen.getByText('#3')).toBeInTheDocument();
    expect(screen.getByText('PhoneNumber')).toBeInTheDocument();
    expect(screen.getByText('0123abc')).toBeInTheDocument();
    expect(screen.getByText('Số điện thoại không đúng định dạng')).toBeInTheDocument();
  });
});
