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

// Mock IntersectionObserver for Infinite Scroll tests
class MockIntersectionObserver {
  readonly root: Element | Document | null = null;
  readonly rootMargin: string = '';
  readonly thresholds: ReadonlyArray<number> = [];
  observe = () => {};
  unobserve = () => {};
  disconnect = () => {};
  takeRecords = () => [];
}
window.IntersectionObserver = MockIntersectionObserver as any;
globalThis.IntersectionObserver = MockIntersectionObserver as any;

// Mock URL.createObjectURL / revokeObjectURL for JSDOM
globalThis.URL.createObjectURL = (blob: any) => `blob:mock-url-${blob?.name || 'file'}`;
globalThis.URL.revokeObjectURL = () => {};

// Mock pointer capture and scrollIntoView for Radix UI Primitives (Select, Dialog, DropdownMenu)
window.HTMLElement.prototype.scrollIntoView = function () {};
window.HTMLElement.prototype.hasPointerCapture = function () {
  return false;
};
window.HTMLElement.prototype.setPointerCapture = function () {};
window.HTMLElement.prototype.releasePointerCapture = function () {};
