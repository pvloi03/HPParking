import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  ShieldAlert,
  Clock,
  User,
  Globe,
  Database,
  Eye,
  RotateCcw,
  CheckCircle2,
  XCircle,
  Download,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { DataTable, type ColumnDef } from '@/components/common/DataTable';
import { AuditPayloadViewer } from '@/components/auditLogs/AuditPayloadViewer';
import { auditApi, extractErrorMessage } from '@/api/auditApi';
import { downloadBlob } from '@/utils/downloadBlob';
import { toast } from 'sonner';
import {
  AuditActionType,
  AUDIT_ACTION_BADGES,
  AUDIT_ACTION_LABELS,
  type AuditLogDto,
} from '@/types/auditLog';

export function AuditLogsPage() {
  // Phân trang
  const [pageIndex, setPageIndex] = useState(1);
  const pageSize = 15;

  // Bộ lọc
  const [searchKeyword, setSearchKeyword] = useState('');
  const [actionTypeFilter, setActionTypeFilter] = useState<AuditActionType | 'all'>('all');
  const [targetEntityFilter, setTargetEntityFilter] = useState<string>('all');
  const [statusFilter, setStatusFilter] = useState<boolean | 'all'>('all');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [isExportingExcel, setIsExportingExcel] = useState(false);

  // Dialog xem chi tiết
  const [selectedLogId, setSelectedLogId] = useState<string | null>(null);
  const [selectedLog, setSelectedLog] = useState<AuditLogDto | null>(null);
  const [isViewerOpen, setIsViewerOpen] = useState(false);

  // TanStack Query
  const { data, isLoading } = useQuery({
    queryKey: [
      'audit-logs',
      pageIndex,
      pageSize,
      searchKeyword,
      actionTypeFilter,
      targetEntityFilter,
      statusFilter,
      fromDate,
      toDate,
    ],
    queryFn: () =>
      auditApi.getPaged({
        pageIndex,
        pageSize,
        actorUsername: searchKeyword.trim() || undefined,
        actionType: actionTypeFilter === 'all' ? undefined : actionTypeFilter,
        targetEntity: targetEntityFilter === 'all' ? undefined : targetEntityFilter,
        isSuccess: statusFilter === 'all' ? undefined : statusFilter,
        fromDate: fromDate ? new Date(`${fromDate}T00:00:00`).toISOString() : undefined,
        toDate: toDate ? new Date(`${toDate}T23:59:59.999`).toISOString() : undefined,
      }),
  });

  const handleResetFilters = () => {
    setSearchKeyword('');
    setActionTypeFilter('all');
    setTargetEntityFilter('all');
    setStatusFilter('all');
    setFromDate('');
    setToDate('');
    setPageIndex(1);
  };

  const handleExportExcel = async () => {
    try {
      setIsExportingExcel(true);
      toast.info('Đang khởi tạo tệp Excel sổ cái kiểm toán...');
      const blob = await auditApi.exportExcel({
        actorUsername: searchKeyword.trim() || undefined,
        actionType: actionTypeFilter === 'all' ? undefined : actionTypeFilter,
        targetEntity: targetEntityFilter === 'all' ? undefined : targetEntityFilter,
        isSuccess: statusFilter === 'all' ? undefined : statusFilter,
        fromDate: fromDate ? new Date(`${fromDate}T00:00:00`).toISOString() : undefined,
        toDate: toDate ? new Date(`${toDate}T23:59:59.999`).toISOString() : undefined,
      });
      const fileName = `nhat_ky_kiem_toan_${new Date().toISOString().slice(0, 10)}.xlsx`;
      downloadBlob(blob, fileName);
      toast.success('Xuất báo cáo Excel sổ cái kiểm toán thành công!');
    } catch (error) {
      toast.error(extractErrorMessage(error) || 'Xuất báo cáo Excel thất bại');
    } finally {
      setIsExportingExcel(false);
    }
  };

  const handleOpenDetail = (item: AuditLogDto) => {
    setSelectedLogId(item.id);
    setSelectedLog(item);
    setIsViewerOpen(true);
  };

  const formatDate = (dateStr: string) => {
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

  // Cấu hình các cột hiển thị trong bảng DataTable
  const columns: ColumnDef<AuditLogDto>[] = [
    {
      header: 'Thời điểm',
      accessorKey: 'createdAt',
      cell: (item) => (
        <div className="flex items-center gap-1.5 text-xs font-medium text-foreground whitespace-nowrap">
          <Clock className="h-3.5 w-3.5 text-muted-foreground shrink-0" />
          <span>{formatDate(item.createdAt)}</span>
        </div>
      ),
    },
    {
      header: 'Người thực hiện',
      accessorKey: 'actorUsername',
      cell: (item) => (
        <div className="flex flex-col">
          <div className="flex items-center gap-1.5 font-semibold text-xs text-foreground">
            <User className="h-3.5 w-3.5 text-primary shrink-0" />
            <span>{item.actorUsername || 'Hệ thống'}</span>
          </div>
          {item.actorRole && (
            <span className="text-[11px] text-muted-foreground ml-5">
              {item.actorRole}
            </span>
          )}
        </div>
      ),
    },
    {
      header: 'Nguồn',
      accessorKey: 'source',
      cell: (item) => (
        <div className="flex items-center gap-1.5 font-mono text-xs text-muted-foreground whitespace-nowrap">
          <Globe className="h-3.5 w-3.5 shrink-0" />
          <span>{item.source || 'N/A'}</span>
        </div>
      ),
    },
    {
      header: 'Hành động',
      accessorKey: 'actionType',
      cell: (item) => {
        const badge = AUDIT_ACTION_BADGES[item.actionType] || {
          label: 'Khác',
          variant: 'outline',
        };
        const label = AUDIT_ACTION_LABELS[item.actionType] || 'Không xác định';
        return <Badge variant={badge.variant as any}>{label}</Badge>;
      },
    },
    {
      header: 'Thực thể tác động',
      accessorKey: 'targetEntity',
      cell: (item) => (
        <div className="flex flex-col max-w-[200px]">
          <div className="flex items-center gap-1.5 text-xs font-medium text-foreground truncate">
            <Database className="h-3.5 w-3.5 text-muted-foreground shrink-0" />
            <span className="font-semibold">{item.targetEntity}</span>
          </div>
          {item.targetDisplay && (
            <span
              className="text-[11px] text-muted-foreground ml-5 truncate"
              title={item.targetDisplay}
            >
              {item.targetDisplay}
            </span>
          )}
        </div>
      ),
    },
    {
      header: 'Kết quả',
      accessorKey: 'isSuccess',
      cell: (item) =>
        item.isSuccess ? (
          <Badge
            variant="outline"
            className="text-emerald-600 border-emerald-300 dark:border-emerald-800 bg-emerald-50 dark:bg-emerald-950/40 flex items-center gap-1 w-fit"
          >
            <CheckCircle2 className="h-3 w-3" />
            Thành công
          </Badge>
        ) : (
          <Badge variant="destructive" className="flex items-center gap-1 w-fit">
            <XCircle className="h-3 w-3" />
            Thất bại
          </Badge>
        ),
    },
    {
      header: 'Thao tác',
      cell: (item) => (
        <Button
          variant="outline"
          size="sm"
          className="h-8 px-2.5 text-xs gap-1.5 font-medium hover:border-primary hover:text-primary transition-colors"
          onClick={() => handleOpenDetail(item)}
          title="Xem chi tiết"
        >
          <Eye className="h-3.5 w-3.5" />
          <span>Chi tiết</span>
        </Button>
      ),
    },
  ];

  // Khối bộ lọc phụ bổ sung
  const extraFilters = (
    <div className="flex flex-wrap items-center gap-2">
      {/* Lọc loại hành động */}
      <select
        value={actionTypeFilter}
        onChange={(e) => {
          const val = e.target.value;
          setActionTypeFilter(val === 'all' ? 'all' : (Number(val) as AuditActionType));
          setPageIndex(1);
        }}
        aria-label="Lọc loại hành động"
        className="h-9 rounded-md border border-input bg-background px-3 py-1 text-xs shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring cursor-pointer"
      >
        <option value="all">Tất cả hành động</option>
        <option value={AuditActionType.Login}>Đăng nhập</option>
        <option value={AuditActionType.Logout}>Đăng xuất</option>
        <option value={AuditActionType.Create}>Thêm mới</option>
        <option value={AuditActionType.Update}>Cập nhật</option>
        <option value={AuditActionType.Delete}>Xóa</option>
        <option value={AuditActionType.ChangePassword}>Đổi mật khẩu</option>
        <option value={AuditActionType.ChangeRole}>Đổi vai trò</option>
        <option value={AuditActionType.LicenseUpdate}>Bản quyền</option>
        <option value={AuditActionType.Export}>Xuất dữ liệu</option>
        <option value={AuditActionType.ManualOverride}>Can thiệp</option>
        <option value={AuditActionType.PermanentDelete}>Xóa vĩnh viễn</option>
        <option value={AuditActionType.Restore}>Khôi phục</option>
        <option value={AuditActionType.FaceIdSync}>Đồng bộ FaceID</option>
      </select>

      {/* Lọc theo thực thể tác động */}
      <select
        value={targetEntityFilter}
        onChange={(e) => {
          setTargetEntityFilter(e.target.value);
          setPageIndex(1);
        }}
        aria-label="Lọc theo thực thể"
        className="h-9 rounded-md border border-input bg-background px-3 py-1 text-xs shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring cursor-pointer"
      >
        <option value="all">Tất cả thực thể</option>
        <option value="Client">Khách hàng (Client)</option>
        <option value="Vehicle">Phương tiện (Vehicle)</option>
        <option value="User">Tài khoản (User)</option>
        <option value="Company">Công ty (Company)</option>
        <option value="Department">Phòng ban (Department)</option>
        <option value="Contractor">Nhà thầu (Contractor)</option>
        <option value="Gate">Cổng (Gate)</option>
        <option value="Lane">Làn xe (Lane)</option>
        <option value="Device">Thiết bị (Device)</option>
        <option value="Reports">Báo cáo (Reports)</option>
        <option value="Auth">Xác thực (Auth)</option>
      </select>

      {/* Lọc kết quả */}
      <select
        value={statusFilter === 'all' ? 'all' : statusFilter ? 'success' : 'failed'}
        onChange={(e) => {
          const val = e.target.value;
          setStatusFilter(val === 'all' ? 'all' : val === 'success');
          setPageIndex(1);
        }}
        aria-label="Lọc kết quả thực hiện"
        className="h-9 rounded-md border border-input bg-background px-3 py-1 text-xs shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring cursor-pointer"
      >
        <option value="all">Tất cả kết quả</option>
        <option value="success">Thành công</option>
        <option value="failed">Thất bại</option>
      </select>

      {/* Lọc từ ngày */}
      <div className="flex items-center gap-1">
        <span className="text-[11px] text-muted-foreground whitespace-nowrap">Từ:</span>
        <Input
          type="date"
          value={fromDate}
          onChange={(e) => {
            setFromDate(e.target.value);
            setPageIndex(1);
          }}
          className="h-9 w-36 text-xs px-2"
        />
      </div>

      {/* Lọc đến ngày */}
      <div className="flex items-center gap-1">
        <span className="text-[11px] text-muted-foreground whitespace-nowrap">Đến:</span>
        <Input
          type="date"
          value={toDate}
          onChange={(e) => {
            setToDate(e.target.value);
            setPageIndex(1);
          }}
          className="h-9 w-36 text-xs px-2"
        />
      </div>

      {/* Đặt lại bộ lọc */}
      {(searchKeyword ||
        actionTypeFilter !== 'all' ||
        targetEntityFilter !== 'all' ||
        statusFilter !== 'all' ||
        fromDate ||
        toDate) && (
          <Button
            variant="ghost"
            size="sm"
            onClick={handleResetFilters}
            className="h-9 px-2 text-xs text-muted-foreground hover:text-foreground"
            title="Đặt lại bộ lọc"
          >
            <RotateCcw className="h-3.5 w-3.5 mr-1" />
            Đặt lại
          </Button>
        )}
    </div>
  );

  return (
    <div className="space-y-6">
      {/* Header trang */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-foreground flex items-center gap-2.5">
            <ShieldAlert className="h-7 w-7 text-primary" />
            Nhật Ký Kiểm Toán
          </h1>
          <p className="text-sm text-muted-foreground mt-1">
            Vết sự kiện kiểm toán hệ thống bất biến (Immutable Audit Trail), ghi nhận mọi hoạt động đăng nhập, thay đổi và tác vụ quản trị.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={handleExportExcel}
            disabled={isExportingExcel || (data?.items?.length === 0 && !isLoading)}
            className="h-9 gap-1.5"
            title="Xuất danh sách sổ cái kiểm toán ra Excel"
          >
            <Download className="h-4 w-4" />
            {isExportingExcel ? 'Đang xuất...' : 'Xuất Excel'}
          </Button>
        </div>
      </div>

      {/* Bảng sự kiện kiểm toán */}
      <DataTable
        data={data?.items || []}
        columns={columns}
        pagination={
          data?.pagination || {
            pageIndex: 1,
            pageSize: 15,
            totalCount: 0,
            totalPages: 1,
            hasPreviousPage: false,
            hasNextPage: false,
          }
        }
        onPageChange={setPageIndex}
        isLoading={isLoading}
        searchKeyword={searchKeyword}
        onSearchChange={(val) => {
          setSearchKeyword(val);
          setPageIndex(1);
        }}
        searchPlaceholder="Tìm theo tên tài khoản hoặc thực thể..."
        extraFilters={extraFilters}
        emptyTitle="Chưa có dữ liệu nhật ký kiểm toán"
        emptyDescription="Không tìm thấy sự kiện kiểm toán nào phù hợp với bộ lọc tìm kiếm hiện tại."
      />

      {/* Drawer / Dialog xem chi tiết payload */}
      <AuditPayloadViewer
        open={isViewerOpen}
        onOpenChange={setIsViewerOpen}
        auditLogId={selectedLogId}
        initialLog={selectedLog}
      />
    </div>
  );
}
export default AuditLogsPage;
