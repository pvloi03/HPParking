export const DuplicateMode = {
  Skip: 1,
  Update: 2,
  Error: 3,
} as const;
export type DuplicateMode = (typeof DuplicateMode)[keyof typeof DuplicateMode];

export interface ExcelRowErrorDto {
  row: number;
  column: string;
  value?: string;
  errorMessage: string;
}

export interface ExcelImportResultDto {
  totalRows: number;
  successCount: number;
  failedCount: number;
  skippedCount: number;
  isDryRun: boolean;
  errors: ExcelRowErrorDto[];
}

export type ExcelEntity =
  | 'clients'
  | 'vehicles';

export const EXCEL_ENTITY_LABELS: Record<ExcelEntity, string> = {
  clients: 'Hồ sơ nhân sự',
  vehicles: 'Phương tiện & Biển số xe',
};
