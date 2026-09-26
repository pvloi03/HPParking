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
  | 'companies'
  | 'departments'
  | 'contractors'
  | 'gates'
  | 'lanes'
  | 'devices'
  | 'clients'
  | 'vehicles';

export const EXCEL_ENTITY_LABELS: Record<ExcelEntity, string> = {
  companies: 'Công ty & Đơn vị thành viên',
  departments: 'Phòng ban trực thuộc',
  contractors: 'Nhà thầu đối tác',
  gates: 'Cổng kiểm soát',
  lanes: 'Làn xe kiểm soát',
  devices: 'Thiết bị ngoại vi',
  clients: 'Hồ sơ khách hàng',
  vehicles: 'Phương tiện & Biển số xe',
};
