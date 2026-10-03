import { useState } from 'react';
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
  Building2,
  Building,
  DoorOpen,
  Phone,
  Mail,
  Calendar,
  Clock,
  Edit,
  AlertCircle,
  Users,
} from 'lucide-react';
import { companiesApi, departmentsApi } from '@/api/masterDataApi';
import { gatesApi } from '@/api/infrastructureApi';
import type { CompanyDto, DepartmentDto } from '@/types/masterData';
import type { GateDto } from '@/types/infrastructure';
import { formatDateTimeVi } from '@/utils/formatters';
import { createEmptyPagedResult } from '@/utils/pagination';

export interface CompanyDetailDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  companyId: string | null;
  onEdit?: (company: CompanyDto) => void;
}

export function CompanyDetailDialog({
  open,
  onOpenChange,
  companyId,
  onEdit,
}: CompanyDetailDialogProps) {
  const [activeTab, setActiveTab] = useState<'info' | 'departments' | 'gates'>('info');

  // Fetch thông tin chi tiết công ty
  const {
    data: company,
    isLoading: isLoadingCompany,
    isError: isCompanyError,
  } = useQuery({
    queryKey: ['company-detail', companyId],
    queryFn: () => (companyId ? companiesApi.getById(companyId) : Promise.reject('No ID')),
    enabled: Boolean(open && companyId),
  });

  // Fetch danh sách phòng ban trực thuộc công ty
  const { data: departmentsData, isLoading: isLoadingDepts } = useQuery({
    queryKey: ['company-departments', companyId],
    queryFn: () =>
      companyId
        ? departmentsApi.getPaged({ companyId, pageSize: 50 })
        : Promise.resolve(createEmptyPagedResult<DepartmentDto>(50)),
    enabled: Boolean(open && companyId && (activeTab === 'departments' || activeTab === 'info')),
  });

  // Fetch danh sách cổng kiểm soát trực thuộc công ty
  const { data: gatesData, isLoading: isLoadingGates } = useQuery({
    queryKey: ['company-gates', companyId],
    queryFn: () =>
      companyId
        ? gatesApi.getPaged({ companyId, pageSize: 50 })
        : Promise.resolve(createEmptyPagedResult<GateDto>(50)),
    enabled: Boolean(open && companyId && (activeTab === 'gates' || activeTab === 'info')),
  });

  const departments = departmentsData?.items || [];
  const gates = gatesData?.items || [];

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <div className="flex items-center gap-3">
            <div className="p-2.5 rounded-xl bg-blue-500/10 text-blue-600 dark:text-blue-400">
              <Building2 className="h-6 w-6" />
            </div>
            <div>
              <DialogTitle className="text-lg font-bold flex items-center gap-2">
                {isLoadingCompany ? (
                  <Skeleton className="h-6 w-48" />
                ) : (
                  <>
                    <span>{company?.name || 'Chi Tiết Công Ty'}</span>
                    {company && <ActiveStatusBadge isActive={company.isActive} />}
                  </>
                )}
              </DialogTitle>
              <DialogDescription className="text-xs text-muted-foreground mt-0.5">
                {company?.code ? `Mã công ty: ${company.code}` : 'Xem hồ sơ dữ liệu tổ chức và cấu trúc cơ sở'}
              </DialogDescription>
            </div>
          </div>
        </DialogHeader>

        {isLoadingCompany ? (
          <div className="space-y-4 py-4">
            <div className="grid grid-cols-2 gap-4">
              <Skeleton className="h-14 rounded-lg" />
              <Skeleton className="h-14 rounded-lg" />
            </div>
            <Skeleton className="h-24 rounded-lg" />
          </div>
        ) : isCompanyError || !company ? (
          <div className="py-8 flex flex-col items-center justify-center text-center">
            <AlertCircle className="h-10 w-10 text-destructive mb-2 opacity-80" />
            <p className="text-sm font-semibold text-foreground">Không thể tải thông tin công ty</p>
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
                onClick={() => setActiveTab('departments')}
                className={`text-xs font-semibold px-3 py-1.5 rounded-lg transition-colors cursor-pointer flex items-center gap-1.5 ${
                  activeTab === 'departments'
                    ? 'bg-primary text-primary-foreground'
                    : 'text-muted-foreground hover:bg-muted'
                }`}
              >
                <Building className="h-3.5 w-3.5" />
                <span>Phòng ban ({departments.length})</span>
              </button>
              <button
                type="button"
                onClick={() => setActiveTab('gates')}
                className={`text-xs font-semibold px-3 py-1.5 rounded-lg transition-colors cursor-pointer flex items-center gap-1.5 ${
                  activeTab === 'gates'
                    ? 'bg-primary text-primary-foreground'
                    : 'text-muted-foreground hover:bg-muted'
                }`}
              >
                <DoorOpen className="h-3.5 w-3.5" />
                <span>Cổng kiểm soát ({gates.length})</span>
              </button>
            </div>

            {/* Tab 1: Thông tin chung */}
            {activeTab === 'info' && (
              <div className="space-y-4">
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  {/* Mã định danh & Tên */}
                  <div className="p-3 rounded-xl border border-border bg-card/60">
                    <span className="text-[11px] font-medium text-muted-foreground block">
                      Mã công ty (Code)
                    </span>
                    <span className="text-sm font-semibold text-foreground font-mono">
                      {company.code}
                    </span>
                  </div>

                  <div className="p-3 rounded-xl border border-border bg-card/60">
                    <span className="text-[11px] font-medium text-muted-foreground block">
                      Trạng thái hoạt động
                    </span>
                    <div className="mt-1">
                      <ActiveStatusBadge isActive={company.isActive} />
                    </div>
                  </div>

                  {/* Điện thoại */}
                  <div className="p-3 rounded-xl border border-border bg-card/60">
                    <span className="text-[11px] font-medium text-muted-foreground flex items-center gap-1">
                      <Phone className="h-3 w-3 text-muted-foreground" />
                      Số điện thoại liên hệ
                    </span>
                    {company.phoneNumber ? (
                      <a
                        href={`tel:${company.phoneNumber}`}
                        className="text-sm font-medium text-blue-600 dark:text-blue-400 hover:underline"
                      >
                        {company.phoneNumber}
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
                    {company.email ? (
                      <a
                        href={`mailto:${company.email}`}
                        className="text-sm font-medium text-blue-600 dark:text-blue-400 hover:underline"
                      >
                        {company.email}
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
                      {formatDateTimeVi(company.createdAt)}
                    </span>
                  </div>

                  {/* Thời gian cập nhật */}
                  <div className="p-3 rounded-xl border border-border bg-card/60">
                    <span className="text-[11px] font-medium text-muted-foreground flex items-center gap-1">
                      <Clock className="h-3 w-3 text-muted-foreground" />
                      Cập nhật lần cuối
                    </span>
                    <span className="text-xs text-foreground mt-0.5 block">
                      {formatDateTimeVi(company.updatedAt)}
                    </span>
                  </div>
                </div>

                {/* Thống kê nhanh cơ cấu trực thuộc */}
                <div className="p-3.5 rounded-xl border border-border bg-muted/40 space-y-2">
                  <h4 className="text-xs font-semibold text-foreground flex items-center gap-1.5">
                    <Users className="h-3.5 w-3.5 text-blue-600 dark:text-blue-400" />
                    Cơ cấu tổ chức trực thuộc
                  </h4>
                  <div className="grid grid-cols-2 gap-2 text-xs">
                    <div className="p-2.5 rounded-lg bg-background border border-border flex items-center justify-between">
                      <span className="text-muted-foreground">Phòng ban:</span>
                      <span className="font-semibold text-foreground font-mono">
                        {departments.length}
                      </span>
                    </div>
                    <div className="p-2.5 rounded-lg bg-background border border-border flex items-center justify-between">
                      <span className="text-muted-foreground">Cổng kiểm soát:</span>
                      <span className="font-semibold text-foreground font-mono">
                        {gates.length}
                      </span>
                    </div>
                  </div>
                </div>
              </div>
            )}

            {/* Tab 2: Danh sách phòng ban trực thuộc */}
            {activeTab === 'departments' && (
              <div className="space-y-2">
                {isLoadingDepts ? (
                  <div className="space-y-2">
                    <Skeleton className="h-12 w-full rounded-lg" />
                    <Skeleton className="h-12 w-full rounded-lg" />
                  </div>
                ) : departments.length === 0 ? (
                  <div className="py-6 text-center border border-dashed border-border rounded-xl">
                    <Building className="h-8 w-8 text-muted-foreground mx-auto mb-1.5 opacity-40" />
                    <p className="text-xs text-muted-foreground">
                      Chưa có phòng ban nào trực thuộc công ty này
                    </p>
                  </div>
                ) : (
                  <div className="max-h-60 overflow-y-auto space-y-2 pr-1">
                    {departments.map((dept) => (
                      <div
                        key={dept.id}
                        className="p-2.5 rounded-lg border border-border bg-card flex items-center justify-between gap-2"
                      >
                        <div className="min-w-0">
                          <div className="flex items-center gap-2">
                            <span className="text-xs font-semibold text-foreground truncate">
                              {dept.name}
                            </span>
                            <Badge variant="outline" className="text-[10px] py-0 px-1 font-mono">
                              {dept.code}
                            </Badge>
                          </div>
                          {dept.managerName && (
                            <p className="text-[11px] text-muted-foreground mt-0.5">
                              Trưởng phòng: <span className="font-medium text-foreground">{dept.managerName}</span>
                            </p>
                          )}
                        </div>
                        <ActiveStatusBadge isActive={dept.isActive} />
                      </div>
                    ))}
                  </div>
                )}
              </div>
            )}

            {/* Tab 3: Danh sách cổng kiểm soát trực thuộc */}
            {activeTab === 'gates' && (
              <div className="space-y-2">
                {isLoadingGates ? (
                  <div className="space-y-2">
                    <Skeleton className="h-12 w-full rounded-lg" />
                    <Skeleton className="h-12 w-full rounded-lg" />
                  </div>
                ) : gates.length === 0 ? (
                  <div className="py-6 text-center border border-dashed border-border rounded-xl">
                    <DoorOpen className="h-8 w-8 text-muted-foreground mx-auto mb-1.5 opacity-40" />
                    <p className="text-xs text-muted-foreground">
                      Chưa có cổng kiểm soát nào được gán cho công ty này
                    </p>
                  </div>
                ) : (
                  <div className="max-h-60 overflow-y-auto space-y-2 pr-1">
                    {gates.map((gate) => (
                      <div
                        key={gate.id}
                        className="p-2.5 rounded-lg border border-border bg-card flex items-center justify-between gap-2"
                      >
                        <div className="min-w-0">
                          <div className="flex items-center gap-2">
                            <span className="text-xs font-semibold text-foreground truncate">
                              {gate.name}
                            </span>
                            <Badge variant="outline" className="text-[10px] py-0 px-1 font-mono">
                              {gate.code}
                            </Badge>
                          </div>
                          <p className="text-[11px] text-muted-foreground mt-0.5 font-mono">
                            Máy trạm: {gate.machineCode}
                          </p>
                        </div>
                        <ActiveStatusBadge isActive={gate.isActive} />
                      </div>
                    ))}
                  </div>
                )}
              </div>
            )}
          </div>
        )}

        <DialogFooter className="flex items-center justify-end gap-2 pt-2 border-t border-border">
          {company && onEdit && (
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => {
                onOpenChange(false);
                onEdit(company);
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
