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
});
