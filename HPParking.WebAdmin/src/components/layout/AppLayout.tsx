import React, { useEffect, useRef } from 'react';
import { Outlet, useLocation } from 'react-router-dom';
import { Sidebar } from '@/components/layout/Sidebar';
import { Header } from '@/components/layout/Header';
import { MobileDrawer } from '@/components/layout/MobileDrawer';

interface AppLayoutProps {
  children?: React.ReactNode;
  title?: string;
  subtitle?: string;
}

export function AppLayout({
  children,
  title = 'HPParking Admin',
  subtitle = 'Hệ thống quản lý đỗ xe tập trung HPParking',
}: AppLayoutProps) {
  const { pathname } = useLocation();
  const mainRef = useRef<HTMLElement>(null);

  // Tự động cuộn vùng nội dung chính lên đầu trang mỗi khi chuyển route
  useEffect(() => {
    if (mainRef.current) {
      mainRef.current.scrollTop = 0;
    }
  }, [pathname]);

  return (
    <div className="flex h-screen w-full overflow-hidden bg-background text-foreground">
      {/* Mobile Drawer Navigation (<1024px) */}
      <MobileDrawer />

      {/* Desktop Sidebar navigation (>=1024px) */}
      <Sidebar />

      {/* Main workspace */}
      <div className="flex flex-1 flex-col min-w-0 min-h-0 overflow-hidden">
        <Header title={title} subtitle={subtitle} />

        {/* Content area */}
        <main ref={mainRef} className="flex-1 min-h-0 overflow-y-auto p-4 sm:p-5 lg:p-6">
          <div className="mx-auto max-w-7xl">
            {children ?? <Outlet />}
          </div>
        </main>
      </div>
    </div>
  );
}
