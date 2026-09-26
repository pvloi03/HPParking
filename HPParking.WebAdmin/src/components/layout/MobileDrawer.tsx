import { useEffect } from 'react';
import { useUiStore } from '@/stores/uiStore';
import { Sidebar } from '@/components/layout/Sidebar';

export function MobileDrawer() {
  const { isMobileDrawerOpen, setMobileDrawerOpen } = useUiStore();

  // Tự động đóng Drawer khi resize màn hình lên >= 1024px (lg breakpoint)
  useEffect(() => {
    const handleResize = () => {
      if (window.innerWidth >= 1024) {
        setMobileDrawerOpen(false);
      }
    };
    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, [setMobileDrawerOpen]);

  // Đóng Drawer khi nhấn phím Escape
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        setMobileDrawerOpen(false);
      }
    };
    if (isMobileDrawerOpen) {
      window.addEventListener('keydown', handleKeyDown);
    }
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isMobileDrawerOpen, setMobileDrawerOpen]);

  if (!isMobileDrawerOpen) return null;

  return (
    <div className="fixed inset-0 z-50 lg:hidden">
      {/* Backdrop mờ */}
      <div
        className="fixed inset-0 bg-black/60 backdrop-blur-xs transition-opacity duration-200"
        onClick={() => setMobileDrawerOpen(false)}
        aria-hidden="true"
      />

      {/* Slide-over Drawer Container */}
      <div className="fixed inset-y-0 left-0 w-72 max-w-[85vw] bg-card shadow-2xl z-10 animate-in slide-in-from-left duration-200 flex flex-col">
        <Sidebar isMobile onClose={() => setMobileDrawerOpen(false)} />
      </div>
    </div>
  );
}
