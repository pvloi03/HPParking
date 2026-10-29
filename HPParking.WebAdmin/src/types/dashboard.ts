import type { LucideIcon } from 'lucide-react';

export type DashboardFilterType = 'day' | 'month' | 'year' | 'custom';

export interface DashboardFilterState {
  type: DashboardFilterType;
  date: string; // YYYY-MM-DD
  month: string; // YYYY-MM
  year: string; // YYYY
  customFrom: string; // YYYY-MM-DD
  customTo: string; // YYYY-MM-DD
}

/**
 * Typed model biểu diễn chỉ số KPI dùng chung giữa Dashboard và Báo cáo Doanh thu
 */
export interface KpiCardItem {
  title: string;
  value: string | number;
  subtext: string;
  icon: LucideIcon;
  iconColor: string;
  iconBg: string;
  isWarning?: boolean;
}
