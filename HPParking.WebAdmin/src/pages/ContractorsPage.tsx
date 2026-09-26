import { useState, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { Briefcase, Trash2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { DataTable, type ColumnDef } from '@/components/common/DataTable';
import { ConfirmDialog } from '@/components/common/ConfirmDialog';
import { ContractorFormDialog } from '@/components/organizations/ContractorFormDialog';
import { ExcelImportDialog } from '@/components/common/ExcelImportDialog';
import { contractorsApi, extractErrorMessage } from '@/api/masterDataApi';
import { excelApi } from '@/api/excelApi';
import { downloadBlob } from '@/utils/downloadBlob';
import type {
  ContractorDto,
  CreateContractorRequest,
  UpdateContractorRequest,
} from '@/types/masterData';
import { usePermissions } from '@/hooks/usePermissions';

export function ContractorsPage() {
  const queryClient = useQueryClient();
  const { canWrite } = usePermissions();

  // State bộ lọc và phân trang
  const [pageIndex, setPageIndex] = useState(1);
  const pageSize = 15;
  const [searchKeyword, setSearchKeyword] = useState('');
  const [statusFilter, setStatusFilter] = useState<boolean | 'all'>('all');

  // State Checkbox selection & Bulk Delete
  const [selectedRowIds, setSelectedRowIds] = useState<string[]>([]);
  const [isBulkDeleteOpen, setIsBulkDeleteOpen] = useState(false);
  const [isBulkLoading, setIsBulkLoading] = useState(false);

  // Tự động bỏ chọn checkbox khi đổi trang hoặc bộ lọc
  useEffect(() => {
    setSelectedRowIds([]);
  }, [pageIndex, searchKeyword, statusFilter]);

  // State Modal Form & Confirm Delete
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [selectedContractor, setSelectedContractor] = useState<ContractorDto | null>(null);
  const [deleteCandidate, setDeleteCandidate] = useState<ContractorDto | null>(null);
  const [isExcelImportOpen, setIsExcelImportOpen] = useState(false);
  const [isExportingExcel, setIsExportingExcel] = useState(false);

  const handleExportExcel = async () => {
    try {
      setIsExportingExcel(true);
      const blob = await excelApi.exportData('contractors', {
        keyword: searchKeyword.trim() || undefined,
        isActive: statusFilter === 'all' ? undefined : statusFilter,
      });
      downloadBlob(blob, 'danh_sach_nha_thau.xlsx');
      toast.success('Đã xuất dữ liệu Excel thành công');
    } catch (err) {
      toast.error(extractErrorMessage(err));
    } finally {
      setIsExportingExcel(false);
    }
  };

  // TanStack Query: Lấy danh sách nhà thầu
  const { data, isLoading } = useQuery({
    queryKey: [
      'contractors',
      pageIndex,
      pageSize,
      searchKeyword,
      statusFilter,
    ],
    queryFn: () =>
      contractorsApi.getPaged({
        pageIndex,
        pageSize,
        keyword: searchKeyword.trim() || undefined,
        isActive: statusFilter === 'all' ? undefined : statusFilter,
      }),
  });

  // Mutation: Thêm mới nhà thầu
  const createMutation = useMutation({
    mutationFn: (payload: CreateContractorRequest) => contractorsApi.create(payload),
    onSuccess: (newContractor) => {
      toast.success(`Đã thêm mới nhà thầu "${newContractor.name}" thành công`);
      setIsFormOpen(false);
      void queryClient.invalidateQueries({ queryKey: ['contractors'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Mutation: Cập nhật nhà thầu
  const updateMutation = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateContractorRequest }) =>
      contractorsApi.update(id, payload),
    onSuccess: (updated) => {
      toast.success(`Đã cập nhật nhà thầu "${updated.name}" thành công`);
      setIsFormOpen(false);
      setSelectedContractor(null);
      void queryClient.invalidateQueries({ queryKey: ['contractors'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Mutation: Xóa mềm nhà thầu
  const deleteMutation = useMutation({
    mutationFn: (id: string) => contractorsApi.delete(id, false),
    onSuccess: () => {
      toast.success('Đã chuyển nhà thầu vào thùng rác thành công');
      setDeleteCandidate(null);
      void queryClient.invalidateQueries({ queryKey: ['contractors'] });
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
        selectedRowIds.map((id) => contractorsApi.delete(id, false))
      );
      const succeeded = results.filter((r) => r.status === 'fulfilled').length;
      const failed = results.length - succeeded;
      if (failed > 0) {
        toast.warning(
          `Đã chuyển ${succeeded}/${results.length} nhà thầu vào thùng rác (${failed} bản ghi không thể xóa do có khách hàng trực thuộc).`
        );
      } else {
        toast.success(`Đã chuyển thành công ${succeeded} nhà thầu vào thùng rác.`);
      }
      setSelectedRowIds([]);
      setIsBulkDeleteOpen(false);
      void queryClient.invalidateQueries({ queryKey: ['contractors'] });
    } catch (err) {
      toast.error(extractErrorMessage(err));
    } finally {
      setIsBulkLoading(false);
    }
  };



  // Xử lý submit form
  const handleFormSubmit = async (
    payload: CreateContractorRequest | UpdateContractorRequest
  ) => {
    if (selectedContractor) {
      await updateMutation.mutateAsync({
        id: selectedContractor.id,
        payload: payload as UpdateContractorRequest,
      });
    } else {
      await createMutation.mutateAsync(payload as CreateContractorRequest);
    }
  };

  // Định nghĩa các cột hiển thị
  const columns: ColumnDef<ContractorDto>[] = [
    {
      header: 'Mã nhà thầu',
      accessorKey: 'code',
      className: 'font-semibold w-36',
      mobileLabel: 'Mã',
    },
    {
      header: 'Tên nhà thầu / Đơn vị',
      accessorKey: 'name',
      className: 'font-semibold min-w-[200px]',
      mobileLabel: 'Tên nhà thầu',
    },
    {
      header: 'Người đại diện liên hệ',
      accessorKey: 'contactPerson',
      cell: (item) => item.contactPerson || <span className="text-muted-foreground">—</span>,
      className: 'w-44',
      mobileLabel: 'Người liên hệ',
    },
    {
      header: 'Số điện thoại',
      accessorKey: 'phoneNumber',
      cell: (item) => item.phoneNumber || <span className="text-muted-foreground">—</span>,
      className: 'w-36',
      mobileLabel: 'SĐT',
    },
    {
      header: 'Email',
      accessorKey: 'email',
      cell: (item) => item.email || <span className="text-muted-foreground">—</span>,
      className: 'w-48',
      mobileLabel: 'Email',
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
              <Briefcase className="h-5 w-5" />
            </div>
            <div>
              <h1 className="text-xl font-bold tracking-tight text-foreground">
                Quản Lý Nhà Thầu &amp; Đối Tác Thi Công
              </h1>
              <p className="text-xs text-muted-foreground">
                Danh mục các đơn vị nhà thầu, đối tác cung cấp dịch vụ và nhà xe bên ngoài hoạt động trong khuôn viên.
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
        searchPlaceholder="Tìm kiếm mã, tên nhà thầu, người liên hệ..."
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
                setSelectedContractor(null);
                setIsFormOpen(true);
              }
            : undefined
        }
        addNewLabel="Thêm mới nhà thầu"
        onImportExcel={canWrite ? () => setIsExcelImportOpen(true) : undefined}
        onExportExcel={handleExportExcel}
        isExportingExcel={isExportingExcel}
        actions={{
          onEdit: canWrite
            ? (item) => {
                setSelectedContractor(item);
                setIsFormOpen(true);
              }
            : undefined,
          onDelete: canWrite
            ? (item) => {
                setDeleteCandidate(item);
              }
            : undefined,
        }}
        emptyTitle="Không có nhà thầu nào"
        emptyDescription="Chưa có nhà thầu hoặc không có bản ghi nào khớp với điều kiện tìm kiếm."
      />

      {/* Modal Form Thêm/Sửa */}
      <ContractorFormDialog
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        initialData={selectedContractor}
        onSubmit={handleFormSubmit}
        isSubmitting={createMutation.isPending || updateMutation.isPending}
      />

      {/* Modal Nhập Dữ Liệu Excel */}
      <ExcelImportDialog
        open={isExcelImportOpen}
        onOpenChange={setIsExcelImportOpen}
        entity="contractors"
        onSuccess={() => {
          void queryClient.invalidateQueries({ queryKey: ['contractors'] });
        }}
      />

      {/* Confirm Xóa Mềm */}
      <ConfirmDialog
        open={Boolean(deleteCandidate)}
        onOpenChange={(open) => !open && setDeleteCandidate(null)}
        title="Xác Nhận Xóa Nhà Thầu"
        description={`Bạn có chắc chắn muốn chuyển nhà thầu "${deleteCandidate?.name}" vào thùng rác không? Lưu ý: Hệ thống sẽ từ chối xóa nếu nhà thầu này vẫn còn khách hàng/nhân sự trực thuộc.`}
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

      {/* Confirm Xóa Mềm Hàng Loạt Nhà Thầu */}
      <ConfirmDialog
        open={isBulkDeleteOpen}
        onOpenChange={(open) => !open && setIsBulkDeleteOpen(false)}
        title="Xác Nhận Xóa Mềm Hàng Loạt"
        description={`Bạn có chắc chắn muốn chuyển ${selectedRowIds.length} nhà thầu đã chọn vào thùng rác không? Toàn bộ các bản ghi bị xóa mềm có thể được xem và khôi phục tập trung tại màn hình Thùng Rác Hệ Thống.`}
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
