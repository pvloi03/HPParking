import { describe, it, expect } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { EvidenceImageGrid } from '@/components/parkingSessions/EvidenceImageGrid';

describe('EvidenceImageGrid Component', () => {
  const defaultProps = {
    plateNumber: '30A12345',
    inLaneName: 'Làn Vào 01',
    outLaneName: 'Làn Ra 01',
    inTime: '2026-09-24T08:00:00Z',
    outTime: '2026-09-24T10:00:00Z',
    inOverviewImagePath: '/images/in_ov.jpg',
    inPlateImagePath: '/images/in_plate.jpg',
    outOverviewImagePath: '/images/out_ov.jpg',
    outPlateImagePath: '/images/out_plate.jpg',
    isActiveSession: false,
  };

  it('hiển thị Slide chính và 4 thumbnail strip theo chuẩn PhuXuan', () => {
    render(<EvidenceImageGrid {...defaultProps} />);

    // Hero slide mặc định là Slide 1 (Toàn cảnh lúc vào)
    expect(screen.getByText('1. Toàn Cảnh Lúc Vào')).toBeInTheDocument();

    // 4 thumbnail strip hiển thị đầy đủ tags
    expect(screen.getAllByText('VÀO').length).toBeGreaterThan(0);
    expect(screen.getAllByText('BIỂN SỐ VÀO').length).toBeGreaterThan(0);
    expect(screen.getAllByText('RA').length).toBeGreaterThan(0);
    expect(screen.getAllByText('BIỂN SỐ RA').length).toBeGreaterThan(0);

    // Có các nút điều hướng slide Trái / Phải
    expect(screen.getByTitle(/ảnh tiếp theo/i)).toBeInTheDocument();
    expect(screen.getByTitle(/ảnh trước/i)).toBeInTheDocument();
  });

  it('chuyển slide khi click nút tiếp theo hoặc bấm vào thumbnail', () => {
    render(<EvidenceImageGrid {...defaultProps} />);

    // Mặc định ở slide 1
    expect(screen.getByText('1. Toàn Cảnh Lúc Vào')).toBeInTheDocument();

    // Bấm nút tiếp theo -> chuyển sang slide 2: Cận cảnh biển số vào
    const nextBtn = screen.getByTitle(/ảnh tiếp theo/i);
    fireEvent.click(nextBtn);
    expect(screen.getByText('2. Cận Cảnh Biển Số Vào')).toBeInTheDocument();

    // Click vào thumbnail số 3 (RA)
    const raThumbs = screen.getAllByRole('button', { name: /#3/i });
    if (raThumbs.length > 0) {
      fireEvent.click(raThumbs[0]);
      expect(screen.getByText('3. Toàn Cảnh Lúc Ra')).toBeInTheDocument();
    }
  });

  it('hiển thị thông báo xe đang đỗ trong bãi khi isActiveSession = true (chưa có lượt ra)', () => {
    render(
      <EvidenceImageGrid
        {...defaultProps}
        outTime={undefined}
        outPlateImagePath=""
        outOverviewImagePath=""
        isActiveSession={true}
      />
    );

    // Chuyển sang slide 3 (Toàn cảnh ra)
    const nextBtn = screen.getByTitle(/ảnh tiếp theo/i);
    fireEvent.click(nextBtn); // slide 2
    fireEvent.click(nextBtn); // slide 3

    expect(screen.getByText(/Xe đang đỗ trong bãi \(Chưa có ảnh toàn cảnh ra\)/i)).toBeInTheDocument();
  });

  it('không hiển thị nút phóng to ảnh', () => {
    render(<EvidenceImageGrid {...defaultProps} />);

    expect(screen.queryByTitle(/phóng to ảnh/i)).not.toBeInTheDocument();
  });
});
