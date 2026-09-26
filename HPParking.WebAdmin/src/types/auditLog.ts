import type { PaginationQuery } from './masterData';

export const AuditActionType = {
  Login: 1,
  Logout: 2,
  Create: 3,
  Update: 4,
  Delete: 5,
  ChangePassword: 6,
  ChangeRole: 7,
  LicenseUpdate: 8,
  Export: 9,
  ManualOverride: 10,
  PermanentDelete: 11,
  Restore: 12,
  FaceIdSync: 13,
} as const;

export type AuditActionType =
  (typeof AuditActionType)[keyof typeof AuditActionType];

export const AUDIT_ACTION_LABELS: Record<AuditActionType | number, string> = {
  [AuditActionType.Login]: 'Đăng nhập',
  [AuditActionType.Logout]: 'Đăng xuất',
  [AuditActionType.Create]: 'Thêm mới',
  [AuditActionType.Update]: 'Cập nhật',
  [AuditActionType.Delete]: 'Xóa',
  [AuditActionType.ChangePassword]: 'Đổi mật khẩu',
  [AuditActionType.ChangeRole]: 'Đổi vai trò',
  [AuditActionType.LicenseUpdate]: 'Bản quyền',
  [AuditActionType.Export]: 'Xuất dữ liệu',
  [AuditActionType.ManualOverride]: 'Can thiệp thủ công',
  [AuditActionType.PermanentDelete]: 'Xóa vĩnh viễn',
  [AuditActionType.Restore]: 'Khôi phục',
  [AuditActionType.FaceIdSync]: 'Đồng bộ FaceID',
};

export const AUDIT_ACTION_BADGES: Record<
  AuditActionType | number,
  { label: string; variant: 'default' | 'secondary' | 'outline' | 'destructive' }
> = {
  [AuditActionType.Login]: { label: 'Đăng nhập', variant: 'outline' },
  [AuditActionType.Logout]: { label: 'Đăng xuất', variant: 'secondary' },
  [AuditActionType.Create]: { label: 'Thêm mới', variant: 'default' },
  [AuditActionType.Update]: { label: 'Cập nhật', variant: 'default' },
  [AuditActionType.Delete]: { label: 'Xóa', variant: 'destructive' },
  [AuditActionType.ChangePassword]: { label: 'Đổi mật khẩu', variant: 'outline' },
  [AuditActionType.ChangeRole]: { label: 'Đổi vai trò', variant: 'destructive' },
  [AuditActionType.LicenseUpdate]: { label: 'Bản quyền', variant: 'outline' },
  [AuditActionType.Export]: { label: 'Xuất dữ liệu', variant: 'secondary' },
  [AuditActionType.ManualOverride]: { label: 'Can thiệp', variant: 'destructive' },
  [AuditActionType.PermanentDelete]: { label: 'Xóa vĩnh viễn', variant: 'destructive' },
  [AuditActionType.Restore]: { label: 'Khôi phục', variant: 'secondary' },
  [AuditActionType.FaceIdSync]: { label: 'Đồng bộ FaceID', variant: 'outline' },
};

export interface AuditLogDto {
  id: string;
  actorId?: string | null;
  actorUsername: string;
  actorRole: string;
  source: string;
  actionType: AuditActionType | number;
  targetEntity: string;
  targetId?: string | null;
  targetDisplay?: string | null;
  isSuccess: boolean;
  createdAt: string;
}

export interface AuditLogDetailDto extends AuditLogDto {
  reason?: string | null;
  errorMessage?: string | null;
}

export interface AuditLogFilterQuery extends PaginationQuery {
  actorUsername?: string;
  actionType?: AuditActionType | number;
  targetEntity?: string;
  isSuccess?: boolean;
  source?: string;
  fromDate?: string;
  toDate?: string;
}
