import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { DoorOpen, Trash2 } from 'lucide-react';
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
import { GateFormDialog } from '@/components/infrastructure/GateFormDialog';
import { ExcelImportDialog } from '@/components/common/ExcelImportDialog';
import { gatesApi, extractErrorMessage } from '@/api/infrastructureApi';
import { companiesApi } from '@/api/masterDataApi';
import { excelApi } from '@/api/excelApi';
import { downloadBlob } from '@/utils/downloadBlob';
import type {
  GateDto,
  CreateGateRequest,
  UpdateGateRequest,
} from '@/types/infrastructure';

export function GatesPage() {
  const queryClient = useQueryClient();

  // State bộ lọc và phân trang
  const [pageIndex, setPageIndex] = useState(1);
  const pageSize = 15;
  const [searchKeyword, setSearchKeyword] = useState('');
  const [statusFilter, setStatusFilter] = useState<boolean | 'all'>('all');
  const [companyFilter, setCompanyFilter] = useState<string>('all');
  const [isTrashMode, setIsTrashMode] = useState(false);

  // State Modal Form & Confirm Delete
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [selectedGate, setSelectedGate] = useState<GateDto | null>(null);
  const [deleteCandidate, setDeleteCandidate] = useState<GateDto | null>(null);
  const [isExcelImportOpen, setIsExcelImportOpen] = useState(false);
  const [isExportingExcel, setIsExportingExcel] = useState(false);

  const handleExportExcel = async () => {
    try {
      setIsExportingExcel(true);
      const blob = await excelApi.exportData('gates', {
        keyword: searchKeyword.trim() || undefined,
        isActive: statusFilter === 'all' ? undefined : statusFilter,
        companyId: companyFilter === 'all' ? undefined : companyFilter,
      });
      downloadBlob(blob, 'danh_sach_cong.xlsx');
      toast.success('Đã xuất dữ liệu Excel thành công');
    } catch (err) {
      toast.error(extractErrorMessage(err));
    } finally {
      setIsExportingExcel(false);
    }
  };

  // Query: Lấy danh sách công ty để chọn & lọc
  const { data: companiesData } = useQuery({
    queryKey: ['companies-all'],
    queryFn: () => companiesApi.getPaged({ pageIndex: 1, pageSize: 100, isActive: true }),
  });
  const companies = companiesData?.items || [];

  // Query: Lấy danh sách cổng kiểm soát
  const { data, isLoading } = useQuery({
    queryKey: [
      'gates',
      pageIndex,
      pageSize,
      searchKeyword,
      statusFilter,
      companyFilter,
      isTrashMode,
    ],
    queryFn: () =>
      gatesApi.getPaged({
        pageIndex,
        pageSize,
        keyword: searchKeyword.trim() || undefined,
        isActive: statusFilter === 'all' ? undefined : statusFilter,
        companyId: companyFilter === 'all' ? undefined : companyFilter,
        onlyDeleted: isTrashMode,
      }),
  });

  // Mutation: Thêm mới cổng
  const createMutation = useMutation({
    mutationFn: (payload: CreateGateRequest) => gatesApi.create(payload),
    onSuccess: (newGate) => {
      toast.success(`Đã thêm mới cổng "${newGate.name}" thành công`);
      setIsFormOpen(false);
      void queryClient.invalidateQueries({ queryKey: ['gates'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Mutation: Cập nhật cổng
  const updateMutation = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateGateRequest }) =>
      gatesApi.update(id, payload),
    onSuccess: (updated) => {
      toast.success(`Đã cập nhật cổng "${updated.name}" thành công`);
      setIsFormOpen(false);
      setSelectedGate(null);
      void queryClient.invalidateQueries({ queryKey: ['gates'] });
      void queryClient.invalidateQueries({ queryKey: ['lanes'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Mutation: Xóa mềm cổng
  const deleteMutation = useMutation({
    mutationFn: (id: string) => gatesApi.delete(id, false),
    onSuccess: () => {
      toast.success('Đã chuyển cổng bãi xe vào thùng rác thành công');
      setDeleteCandidate(null);
      void queryClient.invalidateQueries({ queryKey: ['gates'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Mutation: Khôi phục cổng
  const restoreMutation = useMutation({
    mutationFn: (id: string) => gatesApi.restore(id),
    onSuccess: (restored) => {
      toast.success(`Đã khôi phục cổng "${restored.name}" thành công`);
      void queryClient.invalidateQueries({ queryKey: ['gates'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Xử lý submit form
  const handleFormSubmit = async (
    payload: CreateGateRequest | UpdateGateRequest
  ) => {
    if (selectedGate) {
      await updateMutation.mutateAsync({
        id: selectedGate.id,
        payload: payload as UpdateGateRequest,
      });
    } else {
      await createMutation.mutateAsync(payload as CreateGateRequest);
    }
  };

  // Định nghĩa các cột
  const columns: ColumnDef<GateDto>[] = [
    {
      header: 'Mã cổng',
      accessorKey: 'code',
      className: 'font-mono font-medium text-xs text-blue-600 dark:text-blue-400 w-36',
      mobileLabel: 'Mã',
    },
    {
      header: 'Tên cổng kiểm soát',
      accessorKey: 'name',
      className: 'font-semibold min-w-[180px]',
      mobileLabel: 'Tên cổng',
    },
    {
      header: 'Công ty quản lý',
      accessorKey: 'companyName',
      cell: (item) => item.companyName || <span className="text-muted-foreground">—</span>,
      className: 'min-w-[180px]',
      mobileLabel: 'Công ty',
    },
    {
      header: 'Mã máy bốt trực',
      accessorKey: 'machineCode',
      cell: (item) => (
        <span className="font-mono text-xs bg-muted/60 px-2 py-0.5 rounded text-foreground">
          {item.machineCode || '—'}
        </span>
      ),
      className: 'w-44',
      mobileLabel: 'MachineCode',
    },
    {
      header: 'Trạng thái',
      accessorKey: 'isActive',
      cell: (item) => (
        <Badge
          variant={item.isActive ? 'default' : 'secondary'}
          className={`text-[11px] font-medium ${
            item.isActive
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
              <DoorOpen className="h-5 w-5" />
            </div>
            <div>
              <h1 className="text-xl font-bold tracking-tight text-foreground">
                Quản Lý Cổng Kiểm Soát
              </h1>
              <p className="text-xs text-muted-foreground">
                Danh mục cổng bãi xe, cấu hình liên kết công ty cha và mã định danh máy tính bốt trực.
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Bảng dữ liệu */}
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
        searchPlaceholder="Tìm kiếm mã, tên cổng, machine code..."
        statusFilter={statusFilter}
        onStatusFilterChange={(st) => {
          setStatusFilter(st);
          setPageIndex(1);
        }}
        isTrashMode={isTrashMode}
        onTrashModeToggle={() => {
          setIsTrashMode(!isTrashMode);
          setPageIndex(1);
        }}
        extraFilters={
          !isTrashMode && (
            <div className="w-[200px]">
              <Select
                value={companyFilter}
                onValueChange={(val) => {
                  setCompanyFilter(val);
                  setPageIndex(1);
                }}
              >
                <SelectTrigger className="h-9 text-xs">
                  <SelectValue placeholder="Lọc theo công ty" />
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
          )
        }
        onAddNew={() => {
          setSelectedGate(null);
          setIsFormOpen(true);
        }}
        addNewLabel="Thêm mới cổng"
        onImportExcel={() => setIsExcelImportOpen(true)}
        onExportExcel={handleExportExcel}
        isExportingExcel={isExportingExcel}
        actions={{
          onEdit: (item) => {
            setSelectedGate(item);
            setIsFormOpen(true);
          },
          onDelete: (item) => {
            setDeleteCandidate(item);
          },
          onRestore: (item) => {
            restoreMutation.mutate(item.id);
          },
        }}
        emptyTitle={isTrashMode ? 'Thùng rác trống' : 'Không có cổng kiểm soát nào'}
        emptyDescription={
          isTrashMode
            ? 'Hiện tại không có cổng bãi xe nào nằm trong thùng rác.'
            : 'Chưa có dữ liệu cổng kiểm soát hoặc không có bản ghi nào khớp với điều kiện tìm kiếm.'
        }
      />

      {/* Modal Form Thêm/Sửa Cổng */}
      <GateFormDialog
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        initialData={selectedGate}
        companies={companies}
        onSubmit={handleFormSubmit}
        isSubmitting={createMutation.isPending || updateMutation.isPending}
      />

      {/* Modal Nhập Dữ Liệu Excel */}
      <ExcelImportDialog
        open={isExcelImportOpen}
        onOpenChange={setIsExcelImportOpen}
        entity="gates"
        onSuccess={() => {
          void queryClient.invalidateQueries({ queryKey: ['gates'] });
        }}
      />

      {/* Confirm Xóa Mềm Cổng */}
      <ConfirmDialog
        open={Boolean(deleteCandidate)}
        onOpenChange={(open) => !open && setDeleteCandidate(null)}
        title="Xác Nhận Xóa Cổng Kiểm Soát"
        description={`Bạn có chắc chắn muốn chuyển cổng "${deleteCandidate?.name}" vào thùng rác không? Lưu ý: Hệ thống sẽ từ chối xóa nếu cổng này vẫn còn làn xe trực thuộc (Toàn vẹn tham chiếu ADR 0030).`}
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
    </div>
  );
}
