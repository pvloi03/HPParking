import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { AvatarUploadField } from '@/components/clients/AvatarUploadField';

describe('AvatarUploadField Component', () => {
  it('hiển thị ảnh đại diện khi có currentUrl', () => {
    render(
      <AvatarUploadField
        currentUrl="https://example.com/avatar.jpg"
        onFileSelected={vi.fn()}
      />
    );

    const img = screen.getByAltText('Avatar khách hàng');
    expect(img).toBeInTheDocument();
    expect(img).toHaveAttribute('src', 'https://example.com/avatar.jpg');
    expect(screen.getByText(/Gỡ ảnh/i)).toBeInTheDocument();
  });

  it('gọi callback onFileSelected(null) khi bấm Gỡ ảnh', () => {
    const handleFileSelected = vi.fn();
    render(
      <AvatarUploadField
        currentUrl="https://example.com/avatar.jpg"
        onFileSelected={handleFileSelected}
      />
    );

    const removeBtn = screen.getByText(/Gỡ ảnh/i);
    fireEvent.click(removeBtn);

    expect(handleFileSelected).toHaveBeenCalledWith(null);
  });

  it('hiển thị badge Khớp CCCD khi nhận sự kiện FaceCompared thành công', () => {
    render(
      <AvatarUploadField
        currentUrl="https://example.com/avatar.jpg"
        onFileSelected={vi.fn()}
        compareResult={{
          score: 88,
          isMatch: true,
          message: 'Khuôn mặt khớp (88%)',
        }}
      />
    );

    expect(screen.getByText(/Khớp CCCD: 88%/i)).toBeInTheDocument();
  });

  it('hiển thị badge Không khớp CCCD khi nhận sự kiện FaceCompared thất bại', () => {
    render(
      <AvatarUploadField
        currentUrl="https://example.com/avatar.jpg"
        onFileSelected={vi.fn()}
        compareResult={{
          score: 35,
          isMatch: false,
          message: 'Khuôn mặt không khớp (35%)',
        }}
      />
    );

    expect(screen.getByText(/Không khớp CCCD: 35%/i)).toBeInTheDocument();
  });

  it('hiển thị nút Dùng ảnh CCCD khi có cccdPhotoFile và cập nhật ảnh khi click', () => {
    const handleFileSelected = vi.fn();
    const dummyCccdFile = new File(['dummy'], 'cccd.jpg', { type: 'image/jpeg' });

    render(
      <AvatarUploadField
        currentUrl="https://example.com/existing-avatar.jpg"
        cccdPhotoFile={dummyCccdFile}
        onFileSelected={handleFileSelected}
      />
    );

    // Vẫn hiển thị avatar hiện tại của client
    const img = screen.getByAltText('Avatar khách hàng');
    expect(img).toHaveAttribute('src', 'https://example.com/existing-avatar.jpg');

    // Nút Dùng ảnh CCCD xuất hiện
    const cccdBtn = screen.getByRole('button', { name: /Dùng ảnh CCCD/i });
    expect(cccdBtn).toBeInTheDocument();

    // Click chọn ảnh từ CCCD
    fireEvent.click(cccdBtn);
    expect(handleFileSelected).toHaveBeenCalledWith(dummyCccdFile);
  });
});
