import { render, screen, act } from '@testing-library/react';
import { describe, it, expect, beforeEach } from 'vitest';
import { ThemeProvider, useTheme } from '../components/layout/ThemeProvider';

function TestConsumer() {
  const { theme, setTheme } = useTheme();
  return (
    <div>
      <span data-testid="current-theme">{theme}</span>
      <button onClick={() => setTheme('dark')}>Set Dark</button>
      <button onClick={() => setTheme('light')}>Set Light</button>
      <button onClick={() => setTheme('system')}>Set System</button>
    </div>
  );
}

describe('ThemeProvider & useTheme', () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.classList.remove('dark');
  });

  it('áp dụng theme mặc định và cập nhật class trên document.documentElement', () => {
    render(
      <ThemeProvider defaultTheme="dark" storageKey="hpparking-ui-theme">
        <TestConsumer />
      </ThemeProvider>
    );

    expect(screen.getByTestId('current-theme').textContent).toBe('dark');
    expect(document.documentElement.classList.contains('dark')).toBe(true);
  });

  it('lưu theme vào localStorage khi chuyển đổi sang light', () => {
    render(
      <ThemeProvider defaultTheme="dark" storageKey="hpparking-ui-theme">
        <TestConsumer />
      </ThemeProvider>
    );

    act(() => {
      screen.getByText('Set Light').click();
    });

    expect(screen.getByTestId('current-theme').textContent).toBe('light');
    expect(document.documentElement.classList.contains('dark')).toBe(false);
    expect(localStorage.getItem('hpparking-ui-theme')).toBe('light');
  });

  it('khôi phục theme đã lưu từ localStorage', () => {
    localStorage.setItem('hpparking-ui-theme', 'dark');

    render(
      <ThemeProvider storageKey="hpparking-ui-theme">
        <TestConsumer />
      </ThemeProvider>
    );

    expect(screen.getByTestId('current-theme').textContent).toBe('dark');
    expect(document.documentElement.classList.contains('dark')).toBe(true);
  });
});
