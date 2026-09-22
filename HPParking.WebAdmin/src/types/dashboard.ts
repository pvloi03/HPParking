export type DashboardFilterType = 'day' | 'month' | 'year' | 'custom';

export interface DashboardFilterState {
  type: DashboardFilterType;
  date: string; // YYYY-MM-DD
  month: string; // YYYY-MM
  year: string; // YYYY
  customFrom: string; // YYYY-MM-DD
  customTo: string; // YYYY-MM-DD
}
