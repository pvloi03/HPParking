import '@testing-library/jest-dom/vitest';

// Mock matchMedia for ThemeProvider and responsive tests
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: (query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: () => {},
    removeListener: () => {},
    addEventListener: () => {},
    removeEventListener: () => {},
    dispatchEvent: () => false,
  }),
});

// Mock ResizeObserver for Recharts ResponsiveContainer
class MockResizeObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
}
window.ResizeObserver = MockResizeObserver;
globalThis.ResizeObserver = MockResizeObserver;

// Mock URL.createObjectURL / revokeObjectURL for JSDOM
globalThis.URL.createObjectURL = (blob: any) => `blob:mock-url-${blob?.name || 'file'}`;
globalThis.URL.revokeObjectURL = () => {};



