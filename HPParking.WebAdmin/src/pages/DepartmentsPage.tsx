import { useState, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { Building, Trash2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { DataTable, type ColumnDef } from '@/components/common/DataTable';
import { ConfirmDialog } from '@/components/common/ConfirmDialog';
import { DepartmentFormDialog } from '@/components/organizations/DepartmentFormDialog';
import { ExcelImportDialog } from '@/components/common/ExcelImportDialog';
import {
  departmentsApi,
  companiesApi,
  extractErrorMessage,
} from '@/api/masterDataApi';
import { excelApi } from '@/api/excelApi';
import { downloadBlob } from '@/utils/downloadBlob';
import type {
  DepartmentDto,
  CreateDepartmentRequest,
  UpdateDepartmentRequest,
} from '@/types/masterData';
import { usePermissions } from '@/hooks/usePermissions';

export function DepartmentsPage() {
  const queryClient = useQueryClient();
  const { canWrite } = usePermissions();

  // State bộ lọc và phân trang
  const [pageIndex, setPageIndex] = useState(1);
  const pageSize = 15;
  const [searchKeyword, setSearchKeyword] = useState('');
  const [selectedCompanyFilter, setSelectedCompanyFilter] = useState<string>('all');
  const [statusFilter, setStatusFilter] = useState<boolean | 'all'>('all');

  // State Checkbox selection & Bulk Delete
  const [selectedRowIds, setSelectedRowIds] = useState<string[]>([]);
  const [isBulkDeleteOpen, setIsBulkDeleteOpen] = useState(false);
  const [isBulkLoading, setIsBulkLoading] = useState(false);

  // Tự động bỏ chọn checkbox khi đổi trang hoặc bộ lọc
  useEffect(() => {
    setSelectedRowIds([]);
  }, [pageIndex, searchKeyword, selectedCompanyFilter, statusFilter]);

  // State Modal Form & Confirm Delete
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [selectedDepartment, setSelectedDepartment] = useState<DepartmentDto | null>(null);
  const [deleteCandidate, setDeleteCandidate] = useState<DepartmentDto | null>(null);
  const [isExcelImportOpen, setIsExcelImportOpen] = useState(false);
  const [isExportingExcel, setIsExportingExcel] = useState(false);

  const handleExportExcel = async () => {
    try {
      setIsExportingExcel(true);
      const blob = await excelApi.exportData('departments', {
        keyword: searchKeyword.trim() || undefined,
        companyId: selectedCompanyFilter === 'all' ? undefined : selectedCompanyFilter,
        isActive: statusFilter === 'all' ? undefined : statusFilter,
      });
      downloadBlob(blob, 'danh_sach_phong_ban.xlsx');
      toast.success('Đã xuất dữ liệu Excel thành công');
    } catch (err) {
      toast.error(extractErrorMessage(err));
    } finally {
      setIsExportingExcel(false);
    }
  };

  // Nạp danh sách công ty để đổ vào dropdown bộ lọc và Form
  const { data: companiesData } = useQuery({
    queryKey: ['companies-all'],
    queryFn: () => companiesApi.getPaged({ pageSize: 100, isActive: true }),
    staleTime: 5 * 60 * 1000,
  });

  const companies = companiesData?.items || [];

  // TanStack Query: Lấy danh sách phòng ban
  const { data, isLoading } = useQuery({
    queryKey: [
      'departments',
      pageIndex,
      pageSize,
      searchKeyword,
      selectedCompanyFilter,
      statusFilter,
    ],
    queryFn: () =>
      departmentsApi.getPaged({
        pageIndex,
        pageSize,
        companyId: selectedCompanyFilter === 'all' ? undefined : selectedCompanyFilter,
        keyword: searchKeyword.trim() || undefined,
        isActive: statusFilter === 'all' ? undefined : statusFilter,
      }),
  });

  // Mutation: Thêm mới phòng ban
  const createMutation = useMutation({
    mutationFn: (payload: CreateDepartmentRequest) => departmentsApi.create(payload),
    onSuccess: (newDept) => {
      toast.success(`Đã thêm mới phòng ban "${newDept.name}" thành công`);
      setIsFormOpen(false);
      void queryClient.invalidateQueries({ queryKey: ['departments'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Mutation: Cập nhật phòng ban
  const updateMutation = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateDepartmentRequest }) =>
      departmentsApi.update(id, payload),
    onSuccess: (updated) => {
      toast.success(`Đã cập nhật phòng ban "${updated.name}" thành công`);
      setIsFormOpen(false);
      setSelectedDepartment(null);
      void queryClient.invalidateQueries({ queryKey: ['departments'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Mutation: Xóa mềm phòng ban
  const deleteMutation = useMutation({
    mutationFn: (id: string) => departmentsApi.delete(id, false),
    onSuccess: () => {
      toast.success('Đã chuyển phòng ban vào thùng rác thành công');
      setDeleteCandidate(null);
      void queryClient.invalidateQueries({ queryKey: ['departments'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Xử lý thực thi xóa mềm hàng loạt qua checkbox (chuyển vào thùng rác)
  const handleExecuteBulkDelete = async () => {
    if (selectedRowIds.length === 0) return;
    setIsBulkLoading(true);
    try {
      const results = await Promise.allSettled(
        selectedRowIds.map((id) => departmentsApi.delete(id, false))
      );
      const succeeded = results.filter((r) => r.status === 'fulfilled').length;
      const failed = results.length - succeeded;
      if (failed > 0) {
        toast.warning(
          `Đã chuyển ${succeeded}/${results.length} phòng ban vào thùng rác (${failed} bản ghi không thể xóa do có khách hàng trực thuộc).`
        );
      } else {
        toast.success(`Đã chuyển thành công ${succeeded} phòng ban vào thùng rác.`);
      }
      setSelectedRowIds([]);
      setIsBulkDeleteOpen(false);
      void queryClient.invalidateQueries({ queryKey: ['departments'] });
    } catch (err) {
      toast.error(extractErrorMessage(err));
    } finally {
      setIsBulkLoading(false);
    }
  };


  // Xử lý submit form
  const handleFormSubmit = async (
    payload: CreateDepartmentRequest | UpdateDepartmentRequest
  ) => {
    if (selectedDepartment) {
      await updateMutation.mutateAsync({
        id: selectedDepartment.id,
        payload: payload as UpdateDepartmentRequest,
      });
    } else {
      await createMutation.mutateAsync(payload as CreateDepartmentRequest);
    }
  };

  // Định nghĩa các cột hiển thị
  const columns: ColumnDef<DepartmentDto>[] = [
    {
      header: 'Mã phòng ban',
      accessorKey: 'code',
      className: 'font-semibold w-36',
      mobileLabel: 'Mã',
    },
    {
      header: 'Tên phòng ban',
      accessorKey: 'name',
      className: 'font-semibold min-w-[180px]',
      mobileLabel: 'Tên phòng ban',
    },
    {
      header: 'Công ty trực thuộc',
      accessorKey: 'companyName',
      cell: (item) => (
        <span className="inline-flex items-center px-2 py-0.5 rounded-md text-[11px] font-medium bg-blue-50 text-blue-700 dark:bg-blue-950 dark:text-blue-300 border border-blue-200 dark:border-blue-900">
          {item.companyName || '—'}
        </span>
      ),
      className: 'min-w-[160px]',
      mobileLabel: 'Công ty',
    },
    {
      header: 'Trưởng phòng / Quản lý',
      accessorKey: 'managerName',
      cell: (item) => item.managerName || <span className="text-muted-foreground">—</span>,
      className: 'w-40',
      mobileLabel: 'Quản lý',
    },
    {
      header: 'Số điện thoại',
      accessorKey: 'phoneNumber',
      cell: (item) => item.phoneNumber || <span className="text-muted-foreground">—</span>,
      className: 'w-36',
      mobileLabel: 'SĐT',
    },
    {
      header: 'Trạng thái',
      accessorKey: 'isActive',
      cell: (item) => (
        <Badge
          variant={item.isActive ? 'default' : 'secondary'}
          className={`text-[11px] font-medium ${item.isActive
              ? 'bg-emerald-100 text-emerald-800 hover:bg-emerald-100 dark:bg-emerald-950 dark:text-emerald-300'
              : 'bg-muted text-muted-foreground'
            }`}
        >
          {item.isActive ? 'Đang hoạt động' : 'Ngừng hoạt động'}
        </Badge>
      ),
      className: 'w-36',
      mobileLabel: 'Trạng thái',
    },
  ];

  return (
    <div className="space-y-6">
      {/* Header trang */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2 border-b border-border pb-4">
        <div>
          <div className="flex items-center gap-2">
            <div className="p-2 rounded-xl bg-blue-50 dark:bg-blue-950 text-blue-600 dark:text-blue-400">
              <Building className="h-5 w-5" />
            </div>
            <div>
              <h1 className="text-xl font-bold tracking-tight text-foreground">
                Quản Lý Phòng Ban Trực Thuộc
              </h1>
              <p className="text-xs text-muted-foreground">
                Danh mục các phòng ban, bộ phận chuyên trách trực thuộc các công ty thành viên.
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Bảng dữ liệu dùng chung */}
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
        onPageChange={(p) => setPageIndex(p)}
        isLoading={isLoading}
        searchKeyword={searchKeyword}
        onSearchChange={(kw) => {
          setSearchKeyword(kw);
          setPageIndex(1);
        }}
        searchPlaceholder="Tìm kiếm mã, tên phòng ban..."
        extraFilters={
          <div className="w-48">
            <Select
              value={selectedCompanyFilter}
              onValueChange={(val) => {
                setSelectedCompanyFilter(val);
                setPageIndex(1);
              }}
            >
              <SelectTrigger className="h-9 text-xs">
                <SelectValue placeholder="Lọc theo công ty..." />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all" className="text-xs">
                  Tất cả công ty
                </SelectItem>
                {companies.map((c) => (
                  <SelectItem key={c.id} value={c.id} className="text-xs">
                    {c.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        }
        statusFilter={statusFilter}
        onStatusFilterChange={(st) => {
          setStatusFilter(st);
          setPageIndex(1);
        }}
        selectable={canWrite}
        selectedRowIds={selectedRowIds}
        onSelectedRowIdsChange={setSelectedRowIds}
        bulkActions={
          canWrite ? (
            <div className="flex items-center gap-1.5 ml-1">
              <Button
                size="sm"
                variant="outline"
                onClick={() => setIsBulkDeleteOpen(true)}
                className="h-7 px-2.5 text-xs text-amber-700 dark:text-amber-300 border-amber-300 dark:border-amber-800 hover:bg-amber-50 dark:hover:bg-amber-950/50 cursor-pointer"
              >
                <Trash2 className="h-3.5 w-3.5 mr-1 text-amber-600" />
                <span>Xóa vào thùng rác ({selectedRowIds.length})</span>
              </Button>
            </div>
          ) : undefined
        }
        onAddNew={
          canWrite
            ? () => {
                setSelectedDepartment(null);
                setIsFormOpen(true);
              }
            : undefined
        }
        addNewLabel="Thêm mới phòng ban"
        onImportExcel={canWrite ? () => setIsExcelImportOpen(true) : undefined}
        onExportExcel={handleExportExcel}
        isExportingExcel={isExportingExcel}
        actions={{
          onEdit: canWrite
            ? (item) => {
                setSelectedDepartment(item);
                setIsFormOpen(true);
              }
            : undefined,
          onDelete: canWrite
            ? (item) => {
                setDeleteCandidate(item);
              }
            : undefined,
        }}
        emptyTitle="Không có phòng ban nào"
        emptyDescription="Chưa có phòng ban hoặc không có bản ghi nào khớp với điều kiện tìm kiếm."
      />

      {/* Modal Form Thêm/Sửa */}
      <DepartmentFormDialog
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        initialData={selectedDepartment}
        companies={companies}
        onSubmit={handleFormSubmit}
        isSubmitting={createMutation.isPending || updateMutation.isPending}
      />

      {/* Modal Nhập Dữ Liệu Excel */}
      <ExcelImportDialog
        open={isExcelImportOpen}
        onOpenChange={setIsExcelImportOpen}
        entity="departments"
        onSuccess={() => {
          void queryClient.invalidateQueries({ queryKey: ['departments'] });
        }}
      />

      {/* Confirm Xóa Mềm */}
      <ConfirmDialog
        open={Boolean(deleteCandidate)}
        onOpenChange={(open) => !open && setDeleteCandidate(null)}
        title="Xác Nhận Xóa Phòng Ban"
        description={`Bạn có chắc chắn muốn chuyển phòng ban "${deleteCandidate?.name}" vào thùng rác không? Lưu ý: Hệ thống sẽ từ chối xóa nếu phòng ban này vẫn còn nhân sự hoặc khách hàng trực thuộc.`}
        confirmText="Chuyển Vào Thùng Rác"
        cancelText="Hủy Bỏ"
        variant="destructive"
        icon={<Trash2 className="h-5 w-5 text-destructive" />}
        confirmIcon={<Trash2 className="h-3.5 w-3.5" />}
        onConfirm={() => {
          if (deleteCandidate) {
            deleteMutation.mutate(deleteCandidate.id);
          }
        }}
      />

      {/* Confirm Xóa Mềm Hàng Loạt Phòng Ban */}
      <ConfirmDialog
        open={isBulkDeleteOpen}
        onOpenChange={(open) => !open && setIsBulkDeleteOpen(false)}
        title="Xác Nhận Xóa Mềm Hàng Loạt"
        description={`Bạn có chắc chắn muốn chuyển ${selectedRowIds.length} phòng ban đã chọn vào thùng rác không? Toàn bộ các bản ghi bị xóa mềm có thể được xem và khôi phục tập trung tại màn hình Thùng Rác Hệ Thống.`}
        confirmText={`Chuyển Vào Thùng Rác (${selectedRowIds.length})`}
        cancelText="Hủy Bỏ"
        variant="destructive"
        isLoading={isBulkLoading}
        icon={<Trash2 className="h-5 w-5 text-amber-600" />}
        confirmIcon={<Trash2 className="h-3.5 w-3.5" />}
        onConfirm={handleExecuteBulkDelete}
      />
    </div>
  );
}
