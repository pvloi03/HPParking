import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { AppLayout } from '../components/layout/AppLayout';
import { ThemeProvider } from '../components/layout/ThemeProvider';

describe('AppLayout Component', () => {
  it('dựng khung sườn hoàn chỉnh gồm Sidebar, Header và vùng nội dung chính', () => {
    render(
      <ThemeProvider defaultTheme="light">
        <AppLayout>
          <div data-testid="page-content">Trang kiểm thử nội dung</div>
        </AppLayout>
      </ThemeProvider>
    );

    expect(screen.getByTestId('page-content')).toBeInTheDocument();
    expect(screen.getByText('HPParking Admin')).toBeInTheDocument();
    expect(screen.getByText('Bảng điều khiển')).toBeInTheDocument();
  });
});
