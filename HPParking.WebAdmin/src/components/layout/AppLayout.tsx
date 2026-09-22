import React from 'react';
import { Sidebar } from '@/components/layout/Sidebar';
import { Header } from '@/components/layout/Header';

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
  return (
    <div className="flex h-screen w-full overflow-hidden bg-background text-foreground">
      {/* Sidebar navigation */}
      <Sidebar />

      {/* Main workspace */}
      <div className="flex flex-1 flex-col min-w-0 overflow-hidden">
        <Header title={title} subtitle={subtitle} />

        {/* Content area */}
        <main className="flex-1 overflow-y-auto p-5 sm:p-6">
          <div className="mx-auto max-w-7xl">
            {children}
          </div>
        </main>
      </div>
    </div>
  );
}
