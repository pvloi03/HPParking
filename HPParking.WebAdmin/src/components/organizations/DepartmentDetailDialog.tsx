import { useState, useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { ActiveStatusBadge } from '@/components/common/ActiveStatusBadge';
import {
  Building,
  Building2,
  Users,
  UserCheck,
  Phone,
  Mail,
  Calendar,
  Clock,
  Edit,
  AlertCircle,
  User,
} from 'lucide-react';
import { departmentsApi } from '@/api/masterDataApi';
import { clientApi } from '@/api/clientApi';
import type { DepartmentDto, CompanyDto } from '@/types/masterData';
import type { ClientDto } from '@/types/client';
import { formatDateTimeVi } from '@/utils/formatters';
import { createEmptyPagedResult } from '@/utils/pagination';

export interface DepartmentDetailDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  departmentId: string | null;
  companies?: CompanyDto[];
  onEdit?: (department: DepartmentDto) => void;
}

export function DepartmentDetailDialog({
  open,
  onOpenChange,
  departmentId,
  companies = [],
  onEdit,
}: DepartmentDetailDialogProps) {
  const [activeTab, setActiveTab] = useState<'info' | 'members'>('info');

  // Tra cứu tên công ty cha
  const companyMap = useMemo(() => {
    const map = new Map<string, string>();
    companies.forEach((c) => map.set(c.id, c.name));
    return map;
  }, [companies]);

  // Fetch chi tiết phòng ban
  const {
    data: department,
    isLoading: isLoadingDept,
    isError: isDeptError,
  } = useQuery({
    queryKey: ['department-detail', departmentId],
    queryFn: () => (departmentId ? departmentsApi.getById(departmentId) : Promise.reject('No ID')),
    enabled: Boolean(open && departmentId),
  });

  // Fetch danh sách nhân viên trực thuộc phòng ban
  const { data: membersData, isLoading: isLoadingMembers } = useQuery({
    queryKey: ['department-members', departmentId],
    queryFn: () =>
      departmentId
        ? clientApi.getPaged({ departmentId, pageSize: 20 })
        : Promise.resolve(createEmptyPagedResult<ClientDto>(20)),
    enabled: Boolean(open && departmentId && (activeTab === 'members' || activeTab === 'info')),
  });

  const members = membersData?.items || [];
  const parentCompanyName =
    department?.companyName ||
    (department?.companyId ? companyMap.get(department.companyId) : undefined) ||
    'Chưa liên kết công ty';

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <div className="flex items-center gap-3">
            <div className="p-2.5 rounded-xl bg-indigo-500/10 text-indigo-600 dark:text-indigo-400">
              <Building className="h-6 w-6" />
            </div>
            <div>
              <DialogTitle className="text-lg font-bold flex items-center gap-2">
                {isLoadingDept ? (
                  <Skeleton className="h-6 w-48" />
                ) : (
                  <>
                    <span>{department?.name || 'Chi Tiết Phòng Ban'}</span>
                    {department && <ActiveStatusBadge isActive={department.isActive} />}
                  </>
                )}
              </DialogTitle>
              <DialogDescription className="text-xs text-muted-foreground mt-0.5">
                {department?.code ? `Mã phòng ban: ${department.code}` : 'Xem hồ sơ phòng ban và danh sách nhân sự'}
              </DialogDescription>
            </div>
          </div>
        </DialogHeader>

        {isLoadingDept ? (
          <div className="space-y-4 py-4">
            <div className="grid grid-cols-2 gap-4">
              <Skeleton className="h-14 rounded-lg" />
              <Skeleton className="h-14 rounded-lg" />
            </div>
            <Skeleton className="h-24 rounded-lg" />
          </div>
        ) : isDeptError || !department ? (
          <div className="py-8 flex flex-col items-center justify-center text-center">
            <AlertCircle className="h-10 w-10 text-destructive mb-2 opacity-80" />
            <p className="text-sm font-semibold text-foreground">Không thể tải thông tin phòng ban</p>
            <p className="text-xs text-muted-foreground mt-1">
              Bản ghi có thể đã bị xóa hoặc không thể kết nối tới máy chủ.
            </p>
          </div>
        ) : (
          <div className="space-y-4 pt-2">
            {/* Tabs điều hướng */}
            <div className="flex items-center gap-2 border-b border-border pb-2">
              <button
                type="button"
                onClick={() => setActiveTab('info')}
                className={`text-xs font-semibold px-3 py-1.5 rounded-lg transition-colors cursor-pointer ${
                  activeTab === 'info'
                    ? 'bg-primary text-primary-foreground'
                    : 'text-muted-foreground hover:bg-muted'
                }`}
              >
                Thông tin chung
              </button>
              <button
                type="button"
                onClick={() => setActiveTab('members')}
                className={`text-xs font-semibold px-3 py-1.5 rounded-lg transition-colors cursor-pointer flex items-center gap-1.5 ${
                  activeTab === 'members'
                    ? 'bg-primary text-primary-foreground'
                    : 'text-muted-foreground hover:bg-muted'
                }`}
              >
                <Users className="h-3.5 w-3.5" />
                <span>Nhân sự trực thuộc ({members.length})</span>
              </button>
            </div>

            {/* Tab 1: Thông tin chung */}
            {activeTab === 'info' && (
              <div className="space-y-4">
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  {/* Mã định danh */}
                  <div className="p-3 rounded-xl border border-border bg-card/60">
                    <span className="text-[11px] font-medium text-muted-foreground block">
                      Mã phòng ban (Code)
                    </span>
                    <span className="text-sm font-semibold text-foreground font-mono">
                      {department.code}
                    </span>
                  </div>

                  {/* Công ty chủ quản */}
                  <div className="p-3 rounded-xl border border-border bg-card/60">
                    <span className="text-[11px] font-medium text-muted-foreground flex items-center gap-1">
                      <Building2 className="h-3 w-3 text-muted-foreground" />
                      Công ty chủ quản
                    </span>
                    <span className="text-sm font-semibold text-foreground block mt-0.5">
                      {parentCompanyName}
                    </span>
                  </div>

                  {/* Trưởng phòng / Quản lý */}
                  <div className="p-3 rounded-xl border border-border bg-card/60">
                    <span className="text-[11px] font-medium text-muted-foreground flex items-center gap-1">
                      <UserCheck className="h-3 w-3 text-muted-foreground" />
                      Người quản lý / Trưởng phòng
                    </span>
                    <span className="text-sm font-medium text-foreground block mt-0.5">
                      {department.managerName || (
                        <span className="text-muted-foreground italic">Chưa phân công</span>
                      )}
                    </span>
                  </div>

                  {/* Trạng thái hoạt động */}
                  <div className="p-3 rounded-xl border border-border bg-card/60">
                    <span className="text-[11px] font-medium text-muted-foreground block">
                      Trạng thái hoạt động
                    </span>
                    <div className="mt-1">
                      <ActiveStatusBadge isActive={department.isActive} />
                    </div>
                  </div>

                  {/* Điện thoại */}
                  <div className="p-3 rounded-xl border border-border bg-card/60">
                    <span className="text-[11px] font-medium text-muted-foreground flex items-center gap-1">
                      <Phone className="h-3 w-3 text-muted-foreground" />
                      Số điện thoại
                    </span>
                    {department.phoneNumber ? (
                      <a
                        href={`tel:${department.phoneNumber}`}
                        className="text-sm font-medium text-blue-600 dark:text-blue-400 hover:underline"
                      >
                        {department.phoneNumber}
                      </a>
                    ) : (
                      <span className="text-sm text-muted-foreground italic">Chưa cập nhật</span>
                    )}
                  </div>

                  {/* Email */}
                  <div className="p-3 rounded-xl border border-border bg-card/60">
                    <span className="text-[11px] font-medium text-muted-foreground flex items-center gap-1">
                      <Mail className="h-3 w-3 text-muted-foreground" />
                      Hộp thư điện tử
                    </span>
                    {department.email ? (
                      <a
                        href={`mailto:${department.email}`}
                        className="text-sm font-medium text-blue-600 dark:text-blue-400 hover:underline"
                      >
                        {department.email}
                      </a>
                    ) : (
                      <span className="text-sm text-muted-foreground italic">Chưa cập nhật</span>
                    )}
                  </div>

                  {/* Thời gian tạo */}
                  <div className="p-3 rounded-xl border border-border bg-card/60">
                    <span className="text-[11px] font-medium text-muted-foreground flex items-center gap-1">
                      <Calendar className="h-3 w-3 text-muted-foreground" />
                      Thời gian tạo bản ghi
                    </span>
                    <span className="text-xs text-foreground mt-0.5 block">
                      {formatDateTimeVi(department.createdAt)}
                    </span>
                  </div>

                  {/* Thời gian cập nhật */}
                  <div className="p-3 rounded-xl border border-border bg-card/60">
                    <span className="text-[11px] font-medium text-muted-foreground flex items-center gap-1">
                      <Clock className="h-3 w-3 text-muted-foreground" />
                      Cập nhật lần cuối
                    </span>
                    <span className="text-xs text-foreground mt-0.5 block">
                      {formatDateTimeVi(department.updatedAt)}
                    </span>
                  </div>
                </div>

                {/* Thống kê nhanh nhân sự */}
                <div className="p-3.5 rounded-xl border border-border bg-muted/40 flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <Users className="h-4 w-4 text-indigo-600 dark:text-indigo-400" />
                    <span className="text-xs font-semibold text-foreground">
                      Tổng số nhân sự trực thuộc:
                    </span>
                  </div>
                  <span className="text-sm font-bold text-foreground font-mono">
                    {members.length} nhân sự
                  </span>
                </div>
              </div>
            )}

            {/* Tab 2: Danh sách nhân viên trực thuộc */}
            {activeTab === 'members' && (
              <div className="space-y-2">
                {isLoadingMembers ? (
                  <div className="space-y-2">
                    <Skeleton className="h-12 w-full rounded-lg" />
                    <Skeleton className="h-12 w-full rounded-lg" />
                  </div>
                ) : members.length === 0 ? (
                  <div className="py-6 text-center border border-dashed border-border rounded-xl">
                    <Users className="h-8 w-8 text-muted-foreground mx-auto mb-1.5 opacity-40" />
                    <p className="text-xs text-muted-foreground">
                      Chưa có nhân sự nào trực thuộc phòng ban này
                    </p>
                  </div>
                ) : (
                  <div className="max-h-60 overflow-y-auto space-y-2 pr-1">
                    {members.map((client) => (
                      <div
                        key={client.id}
                        className="p-2.5 rounded-lg border border-border bg-card flex items-center justify-between gap-2"
                      >
                        <div className="flex items-center gap-2.5 min-w-0">
                          <div className="p-1.5 rounded-lg bg-muted text-muted-foreground">
                            <User className="h-4 w-4" />
                          </div>
                          <div className="min-w-0">
                            <div className="flex items-center gap-2">
                              <span className="text-xs font-semibold text-foreground truncate">
                                {client.name}
                              </span>
                              <Badge variant="outline" className="text-[10px] py-0 px-1 font-mono">
                                {client.code}
                              </Badge>
                            </div>
                            {client.phoneNumber && (
                              <p className="text-[11px] text-muted-foreground mt-0.5">
                                SĐT: {client.phoneNumber}
                              </p>
                            )}
                          </div>
                        </div>
                        <ActiveStatusBadge isActive={client.isActive} />
                      </div>
                    ))}
                  </div>
                )}
              </div>
            )}
          </div>
        )}

        <DialogFooter className="flex items-center justify-end gap-2 pt-2 border-t border-border">
          {department && onEdit && (
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => {
                onOpenChange(false);
                onEdit(department);
              }}
              className="text-xs gap-1.5 text-blue-600 border-blue-200 hover:bg-blue-50 dark:hover:bg-blue-950/40 cursor-pointer"
            >
              <Edit className="h-3.5 w-3.5" />
              <span>Chỉnh sửa</span>
            </Button>
          )}
          <Button
            type="button"
            variant="default"
            size="sm"
            onClick={() => onOpenChange(false)}
            className="text-xs cursor-pointer"
          >
            Đóng
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
