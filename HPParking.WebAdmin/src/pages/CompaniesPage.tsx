import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { Building2, Trash2 } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { DataTable, type ColumnDef } from '@/components/common/DataTable';
import { ConfirmDialog } from '@/components/common/ConfirmDialog';
import { CompanyFormDialog } from '@/components/organizations/CompanyFormDialog';
import { companiesApi, extractErrorMessage } from '@/api/masterDataApi';
import type {
  CompanyDto,
  CreateCompanyRequest,
  UpdateCompanyRequest,
} from '@/types/masterData';

export function CompaniesPage() {
  const queryClient = useQueryClient();

  // State bộ lọc và phân trang
  const [pageIndex, setPageIndex] = useState(1);
  const pageSize = 15;
  const [searchKeyword, setSearchKeyword] = useState('');
  const [statusFilter, setStatusFilter] = useState<boolean | 'all'>('all');
  const [isTrashMode, setIsTrashMode] = useState(false);

  // State Modal Form & Confirm Delete
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [selectedCompany, setSelectedCompany] = useState<CompanyDto | null>(null);
  const [deleteCandidate, setDeleteCandidate] = useState<CompanyDto | null>(null);

  // TanStack Query: Lấy danh sách công ty
  const { data, isLoading } = useQuery({
    queryKey: [
      'companies',
      pageIndex,
      pageSize,
      searchKeyword,
      statusFilter,
      isTrashMode,
    ],
    queryFn: () =>
      companiesApi.getPaged({
        pageIndex,
        pageSize,
        keyword: searchKeyword.trim() || undefined,
        isActive: statusFilter === 'all' ? undefined : statusFilter,
        onlyDeleted: isTrashMode,
      }),
  });

  // Mutation: Thêm mới công ty
  const createMutation = useMutation({
    mutationFn: (payload: CreateCompanyRequest) => companiesApi.create(payload),
    onSuccess: (newCompany) => {
      toast.success(`Đã thêm mới công ty "${newCompany.name}" thành công`);
      setIsFormOpen(false);
      void queryClient.invalidateQueries({ queryKey: ['companies'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Mutation: Cập nhật công ty
  const updateMutation = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateCompanyRequest }) =>
      companiesApi.update(id, payload),
    onSuccess: (updated) => {
      toast.success(`Đã cập nhật công ty "${updated.name}" thành công`);
      setIsFormOpen(false);
      setSelectedCompany(null);
      void queryClient.invalidateQueries({ queryKey: ['companies'] });
      void queryClient.invalidateQueries({ queryKey: ['departments'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Mutation: Xóa mềm công ty
  const deleteMutation = useMutation({
    mutationFn: (id: string) => companiesApi.delete(id, false),
    onSuccess: () => {
      toast.success('Đã chuyển công ty vào thùng rác thành công');
      setDeleteCandidate(null);
      void queryClient.invalidateQueries({ queryKey: ['companies'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Mutation: Khôi phục công ty
  const restoreMutation = useMutation({
    mutationFn: (id: string) => companiesApi.restore(id),
    onSuccess: (restored) => {
      toast.success(`Đã khôi phục công ty "${restored.name}" thành công`);
      void queryClient.invalidateQueries({ queryKey: ['companies'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Xử lý submit form
  const handleFormSubmit = async (
    payload: CreateCompanyRequest | UpdateCompanyRequest
  ) => {
    if (selectedCompany) {
      await updateMutation.mutateAsync({
        id: selectedCompany.id,
        payload: payload as UpdateCompanyRequest,
      });
    } else {
      await createMutation.mutateAsync(payload as CreateCompanyRequest);
    }
  };

  // Định nghĩa các cột hiển thị
  const columns: ColumnDef<CompanyDto>[] = [
    {
      header: 'Mã công ty',
      accessorKey: 'code',
      className: 'font-mono font-medium text-xs text-blue-600 dark:text-blue-400 w-36',
      mobileLabel: 'Mã',
    },
    {
      header: 'Tên công ty',
      accessorKey: 'name',
      className: 'font-semibold min-w-[200px]',
      mobileLabel: 'Tên đơn vị',
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
              <Building2 className="h-5 w-5" />
            </div>
            <div>
              <h1 className="text-xl font-bold tracking-tight text-foreground">
                Quản Lý Công Ty &amp; Đơn Vị Thành Viên
              </h1>
              <p className="text-xs text-muted-foreground">
                Danh mục các đơn vị thành viên, cơ quan và công ty đối tác trong hệ thống bãi đỗ xe.
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
        searchPlaceholder="Tìm kiếm mã, tên công ty..."
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
        onAddNew={() => {
          setSelectedCompany(null);
          setIsFormOpen(true);
        }}
        addNewLabel="Thêm mới công ty"
        actions={{
          onEdit: (item) => {
            setSelectedCompany(item);
            setIsFormOpen(true);
          },
          onDelete: (item) => {
            setDeleteCandidate(item);
          },
          onRestore: (item) => {
            restoreMutation.mutate(item.id);
          },
        }}
        emptyTitle={isTrashMode ? 'Thùng rác trống' : 'Không có công ty nào'}
        emptyDescription={
          isTrashMode
            ? 'Hiện tại không có công ty nào nằm trong thùng rác.'
            : 'Chưa có dữ liệu công ty hoặc không có bản ghi nào khớp với điều kiện tìm kiếm.'
        }
      />

      {/* Modal Form Thêm/Sửa */}
      <CompanyFormDialog
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        initialData={selectedCompany}
        onSubmit={handleFormSubmit}
        isSubmitting={createMutation.isPending || updateMutation.isPending}
      />

      {/* Confirm Xóa Mềm */}
      <ConfirmDialog
        open={Boolean(deleteCandidate)}
        onOpenChange={(open) => !open && setDeleteCandidate(null)}
        title="Xác Nhận Xóa Công Ty"
        description={`Bạn có chắc chắn muốn chuyển công ty "${deleteCandidate?.name}" vào thùng rác không? Lưu ý: Hệ thống sẽ từ chối xóa nếu công ty này vẫn còn phòng ban hoặc cổng kiểm soát trực thuộc.`}
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
