import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { Briefcase, Trash2 } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { DataTable, type ColumnDef } from '@/components/common/DataTable';
import { ConfirmDialog } from '@/components/common/ConfirmDialog';
import { ContractorFormDialog } from '@/components/organizations/ContractorFormDialog';
import { contractorsApi, extractErrorMessage } from '@/api/masterDataApi';
import type {
  ContractorDto,
  CreateContractorRequest,
  UpdateContractorRequest,
} from '@/types/masterData';

export function ContractorsPage() {
  const queryClient = useQueryClient();

  // State bộ lọc và phân trang
  const [pageIndex, setPageIndex] = useState(1);
  const pageSize = 15;
  const [searchKeyword, setSearchKeyword] = useState('');
  const [statusFilter, setStatusFilter] = useState<boolean | 'all'>('all');
  const [isTrashMode, setIsTrashMode] = useState(false);

  // State Modal Form & Confirm Delete
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [selectedContractor, setSelectedContractor] = useState<ContractorDto | null>(null);
  const [deleteCandidate, setDeleteCandidate] = useState<ContractorDto | null>(null);

  // TanStack Query: Lấy danh sách nhà thầu
  const { data, isLoading } = useQuery({
    queryKey: [
      'contractors',
      pageIndex,
      pageSize,
      searchKeyword,
      statusFilter,
      isTrashMode,
    ],
    queryFn: () =>
      contractorsApi.getPaged({
        pageIndex,
        pageSize,
        keyword: searchKeyword.trim() || undefined,
        isActive: statusFilter === 'all' ? undefined : statusFilter,
        onlyDeleted: isTrashMode,
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

  // Mutation: Khôi phục nhà thầu
  const restoreMutation = useMutation({
    mutationFn: (id: string) => contractorsApi.restore(id),
    onSuccess: (restored) => {
      toast.success(`Đã khôi phục nhà thầu "${restored.name}" thành công`);
      void queryClient.invalidateQueries({ queryKey: ['contractors'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

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
      className: 'font-mono font-medium text-xs text-blue-600 dark:text-blue-400 w-36',
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
        isTrashMode={isTrashMode}
        onTrashModeToggle={() => {
          setIsTrashMode(!isTrashMode);
          setPageIndex(1);
        }}
        onAddNew={() => {
          setSelectedContractor(null);
          setIsFormOpen(true);
        }}
        addNewLabel="Thêm mới nhà thầu"
        actions={{
          onEdit: (item) => {
            setSelectedContractor(item);
            setIsFormOpen(true);
          },
          onDelete: (item) => {
            setDeleteCandidate(item);
          },
          onRestore: (item) => {
            restoreMutation.mutate(item.id);
          },
        }}
        emptyTitle={isTrashMode ? 'Thùng rác trống' : 'Không có nhà thầu nào'}
        emptyDescription={
          isTrashMode
            ? 'Hiện tại không có nhà thầu nào nằm trong thùng rác.'
            : 'Chưa có nhà thầu hoặc không có bản ghi nào khớp với điều kiện tìm kiếm.'
        }
      />

      {/* Modal Form Thêm/Sửa */}
      <ContractorFormDialog
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        initialData={selectedContractor}
        onSubmit={handleFormSubmit}
        isSubmitting={createMutation.isPending || updateMutation.isPending}
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
    </div>
  );
}
