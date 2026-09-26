import { useQuery } from '@tanstack/react-query';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from '@/components/ui/dialog';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import {
  User,
  Clock,
  Globe,
  Database,
  CheckCircle2,
  XCircle,
  Info,
} from 'lucide-react';
import { auditApi, extractErrorMessage } from '@/api/auditApi';
import {
  AUDIT_ACTION_BADGES,
  AUDIT_ACTION_LABELS,
  type AuditLogDto,
  type AuditLogDetailDto,
} from '@/types/auditLog';

interface AuditPayloadViewerProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  auditLogId: string | null;
  initialLog?: AuditLogDto | null;
}

export function AuditPayloadViewer({
  open,
  onOpenChange,
  auditLogId,
  initialLog,
}: AuditPayloadViewerProps) {
  const { data: detail, isLoading, error } = useQuery<AuditLogDetailDto>({
    queryKey: ['audit-log', auditLogId],
    queryFn: () => auditApi.getById(auditLogId!),
    enabled: Boolean(auditLogId && open),
  });

  const log = detail || initialLog;

  const formatDate = (dateStr?: string) => {
    if (!dateStr) return '---';
    try {
      const d = new Date(dateStr);
      return d.toLocaleString('vi-VN', {
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit',
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
      });
    } catch {
      return dateStr;
    }
  };

  const actionBadge: { label: string; variant: 'default' | 'secondary' | 'outline' | 'destructive' } =
    log?.actionType
      ? AUDIT_ACTION_BADGES[log.actionType] || { label: 'Khác', variant: 'outline' }
      : { label: 'Sự kiện', variant: 'outline' };

  const actionLabel = log?.actionType
    ? AUDIT_ACTION_LABELS[log.actionType] || 'Không xác định'
    : 'Không xác định';

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <div className="flex flex-wrap items-center justify-between gap-2 mr-6">
            <div className="flex items-center gap-2">
              <DialogTitle className="text-base sm:text-lg font-bold">
                Chi Tiết Nhật Ký Kiểm Toán
              </DialogTitle>
              {log && (
                <Badge variant={actionBadge.variant} className="text-xs">
                  {actionBadge.label}
                </Badge>
              )}
            </div>
          </div>
          <DialogDescription className="text-xs text-muted-foreground pt-1">
            Ghi nhận kiểm toán hành động {actionLabel} trên thực thể{' '}
            <span className="font-semibold text-foreground">
              {log?.targetEntity || 'Hệ thống'}
            </span>
            .
          </DialogDescription>
        </DialogHeader>

        {isLoading ? (
          <div className="space-y-4 py-4">
            <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
              <Skeleton className="h-14 rounded-lg" />
              <Skeleton className="h-14 rounded-lg" />
              <Skeleton className="h-14 rounded-lg" />
            </div>
            <Skeleton className="h-24 rounded-lg" />
          </div>
        ) : error ? (
          <div className="p-4 rounded-lg border border-destructive/20 bg-destructive/10 text-destructive text-sm flex items-center gap-2">
            <XCircle className="h-5 w-5 shrink-0" />
            <span>{extractErrorMessage(error)}</span>
          </div>
        ) : log ? (
          <div className="space-y-4 py-2">
            {/* Metadata Grid */}
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3 rounded-lg border bg-muted/20 p-3.5 text-xs">
              <div className="space-y-1">
                <span className="text-muted-foreground flex items-center gap-1 font-medium">
                  <User className="h-3.5 w-3.5" /> Người thực hiện
                </span>
                <p className="font-semibold text-foreground">
                  {log.actorUsername}
                  {log.actorRole && (
                    <span className="ml-1 text-[11px] text-muted-foreground font-normal">
                      ({log.actorRole})
                    </span>
                  )}
                </p>
              </div>

              <div className="space-y-1">
                <span className="text-muted-foreground flex items-center gap-1 font-medium">
                  <Globe className="h-3.5 w-3.5" /> Nguồn
                </span>
                <p className="font-semibold text-foreground font-mono">
                  {log.source || 'WebAdmin'}
                </p>
              </div>

              <div className="space-y-1">
                <span className="text-muted-foreground flex items-center gap-1 font-medium">
                  <Clock className="h-3.5 w-3.5" /> Thời gian
                </span>
                <p className="font-semibold text-foreground">
                  {formatDate(log.createdAt)}
                </p>
              </div>

              <div className="space-y-1">
                <span className="text-muted-foreground flex items-center gap-1 font-medium">
                  <Database className="h-3.5 w-3.5" /> Đối tượng (Entity)
                </span>
                <p className="font-semibold text-foreground">
                  {log.targetEntity || 'N/A'}
                  {log.targetId && (
                    <span className="block text-[11px] text-muted-foreground font-mono truncate max-w-[180px]">
                      ID: {log.targetId}
                    </span>
                  )}
                </p>
              </div>

              <div className="space-y-1">
                <span className="text-muted-foreground flex items-center gap-1 font-medium">
                  <Info className="h-3.5 w-3.5" /> Tên hiển thị
                </span>
                <p className="font-semibold text-foreground truncate">
                  {log.targetDisplay || 'N/A'}
                </p>
              </div>

              <div className="space-y-1">
                <span className="text-muted-foreground flex items-center gap-1 font-medium">
                  Trạng thái tác vụ
                </span>
                <div className="flex items-center gap-1 font-semibold">
                  {log.isSuccess ? (
                    <>
                      <CheckCircle2 className="h-3.5 w-3.5 text-emerald-500" />
                      <span className="text-emerald-600 dark:text-emerald-400">
                        Thành công
                      </span>
                    </>
                  ) : (
                    <>
                      <XCircle className="h-3.5 w-3.5 text-rose-500" />
                      <span className="text-rose-600 dark:text-rose-400">
                        Thất bại
                      </span>
                    </>
                  )}
                </div>
              </div>
            </div>

            {/* Error message if failed */}
            {!log.isSuccess && detail?.errorMessage && (
              <div className="p-3 rounded-lg border border-destructive/30 bg-destructive/10 text-xs text-destructive flex items-start gap-2">
                <XCircle className="h-4 w-4 mt-0.5 shrink-0" />
                <div>
                  <span className="font-bold">Lý do thất bại: </span>
                  {detail.errorMessage}
                </div>
              </div>
            )}

            {/* Reason / Note if provided */}
            {detail?.reason && (
              <div className="p-3.5 rounded-lg border bg-muted/40 text-xs flex items-start gap-2.5">
                <Info className="h-4 w-4 text-primary mt-0.5 shrink-0" />
                <div>
                  <span className="font-semibold text-foreground">Ghi chú / Lý do thao tác: </span>
                  <span className="text-muted-foreground">{detail.reason}</span>
                </div>
              </div>
            )}
          </div>
        ) : null}
      </DialogContent>
    </Dialog>
  );
}
