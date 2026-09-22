import { useState, useMemo } from 'react';
import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query';
import { toast } from 'sonner';
import { Car, Trash2, Bike, HelpCircle } from 'lucide-react';
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
import { VehicleFormDialog } from '@/components/vehicles/VehicleFormDialog';
import { vehicleApi, extractErrorMessage } from '@/api/vehicleApi';
import { clientApi } from '@/api/clientApi';
import {
  VehicleType,
  type VehicleDto,
  type CreateVehicleRequest,
  type UpdateVehicleRequest,
} from '@/types/vehicle';

export function VehiclesPage() {
  const queryClient = useQueryClient();

  // State bộ lọc và phân trang
  const [pageIndex, setPageIndex] = useState(1);
  const pageSize = 15;
  const [searchKeyword, setSearchKeyword] = useState('');
  const [statusFilter, setStatusFilter] = useState<boolean | 'all'>('all');
  const [typeFilter, setTypeFilter] = useState<string>('all');
  const [isTrashMode, setIsTrashMode] = useState(false);

  // State Modals
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [selectedVehicle, setSelectedVehicle] = useState<VehicleDto | null>(null);
  const [deleteCandidate, setDeleteCandidate] = useState<VehicleDto | null>(null);

  // Query: Lấy danh sách khách hàng để ánh xạ chủ sở hữu và chọn trong Form
  const { data: clientsData } = useQuery({
    queryKey: ['clients-all'],
    queryFn: () => clientApi.getPaged({ pageIndex: 1, pageSize: 200, isActive: true }),
  });
  const clients = clientsData?.items || [];

  const clientMap = useMemo(() => {
    const map = new Map<string, (typeof clients)[0]>();
    clients.forEach((c) => map.set(c.id, c));
    return map;
  }, [clients]);

  // TanStack Query v5: Lấy danh sách phương tiện với placeholderData: keepPreviousData
  const { data, isLoading } = useQuery({
    queryKey: [
      'vehicles',
      pageIndex,
      pageSize,
      searchKeyword,
      statusFilter,
      typeFilter,
      isTrashMode,
    ],
    queryFn: () =>
      vehicleApi.getPaged({
        pageIndex,
        pageSize,
        keyword: searchKeyword.trim() || undefined,
        isActive: statusFilter === 'all' ? undefined : statusFilter,
        type: typeFilter === 'all' ? undefined : (Number(typeFilter) as VehicleType),
        onlyDeleted: isTrashMode,
      }),
    placeholderData: keepPreviousData,
  });

  // Mutation: Thêm mới phương tiện
  const createMutation = useMutation({
    mutationFn: (payload: CreateVehicleRequest) => vehicleApi.create(payload),
    onSuccess: (newVehicle) => {
      toast.success(`Đã thêm mới phương tiện biển số "${newVehicle.plateNumber}" thành công`);
      setIsFormOpen(false);
      void queryClient.invalidateQueries({ queryKey: ['vehicles'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Mutation: Cập nhật phương tiện
  const updateMutation = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateVehicleRequest }) =>
      vehicleApi.update(id, payload),
    onSuccess: (updated) => {
      toast.success(`Đã cập nhật phương tiện biển số "${updated.plateNumber}" thành công`);
      setIsFormOpen(false);
      setSelectedVehicle(null);
      void queryClient.invalidateQueries({ queryKey: ['vehicles'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Mutation: Xóa mềm phương tiện
  const deleteMutation = useMutation({
    mutationFn: (id: string) => vehicleApi.delete(id, false),
    onSuccess: () => {
      toast.success('Đã chuyển phương tiện vào thùng rác thành công');
      setDeleteCandidate(null);
      void queryClient.invalidateQueries({ queryKey: ['vehicles'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Mutation: Khôi phục phương tiện
  const restoreMutation = useMutation({
    mutationFn: (id: string) => vehicleApi.restore(id),
    onSuccess: (restored) => {
      toast.success(`Đã khôi phục phương tiện biển số "${restored.plateNumber}" thành công`);
      void queryClient.invalidateQueries({ queryKey: ['vehicles'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Xử lý submit form
  const handleFormSubmit = async (
    payload: CreateVehicleRequest | UpdateVehicleRequest
  ) => {
    if (selectedVehicle) {
      await updateMutation.mutateAsync({
        id: selectedVehicle.id,
        payload: payload as UpdateVehicleRequest,
      });
    } else {
      await createMutation.mutateAsync(payload as CreateVehicleRequest);
    }
  };

  const getVehicleTypeBadge = (type: VehicleType) => {
    switch (type) {
      case VehicleType.Car:
        return (
          <Badge className="bg-blue-100 text-blue-800 hover:bg-blue-100 dark:bg-blue-950 dark:text-blue-300 gap-1 text-[11px] font-medium">
            <Car className="h-3 w-3" />
            <span>Ô tô</span>
          </Badge>
        );
      case VehicleType.Motorbike:
        return (
          <Badge className="bg-emerald-100 text-emerald-800 hover:bg-emerald-100 dark:bg-emerald-950 dark:text-emerald-300 gap-1 text-[11px] font-medium">
            <Bike className="h-3 w-3" />
            <span>Xe máy</span>
          </Badge>
        );
      case VehicleType.Bicycle:
        return (
          <Badge className="bg-amber-100 text-amber-800 hover:bg-amber-100 dark:bg-amber-950 dark:text-amber-300 gap-1 text-[11px] font-medium">
            <Bike className="h-3 w-3" />
            <span>Xe đạp</span>
          </Badge>
        );
      default:
        return (
          <Badge className="bg-muted text-muted-foreground gap-1 text-[11px] font-medium">
            <HelpCircle className="h-3 w-3" />
            <span>Khác</span>
          </Badge>
        );
    }
  };

  // Định nghĩa các cột
  const columns: ColumnDef<VehicleDto>[] = [
    {
      header: 'Biển số xe',
      accessorKey: 'plateNumber',
      cell: (item) => (
        <div className="inline-flex items-center px-2.5 py-1 rounded-md border-2 border-foreground/20 bg-background shadow-2xs font-mono font-bold text-xs tracking-wider text-blue-600 dark:text-blue-400">
          {item.plateNumber}
        </div>
      ),
      className: 'w-40',
      mobileLabel: 'Biển số',
    },
    {
      header: 'Loại phương tiện',
      accessorKey: 'type',
      cell: (item) => getVehicleTypeBadge(item.type),
      className: 'w-36',
      mobileLabel: 'Loại xe',
    },
    {
      header: 'Chủ sở hữu',
      cell: (item) => {
        const owner = item.ownerClientId ? clientMap.get(item.ownerClientId) : null;
        if (!owner) {
          return <span className="text-muted-foreground text-xs">—</span>;
        }
        return (
          <div className="text-xs">
            <span className="font-semibold text-foreground block">
              {owner.fullName}
            </span>
            <span className="font-mono text-[11px] text-muted-foreground block">
              {owner.phoneNumber}
              {owner.companyName ? ` • ${owner.companyName}` : ''}
            </span>
          </div>
        );
      },
      className: 'min-w-[200px]',
      mobileLabel: 'Chủ xe',
    },
    {
      header: 'Ghi chú',
      accessorKey: 'note',
      cell: (item) => item.note || <span className="text-muted-foreground">—</span>,
      className: 'min-w-[160px]',
      mobileLabel: 'Ghi chú',
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
              <Car className="h-5 w-5" />
            </div>
            <div>
              <h1 className="text-xl font-bold tracking-tight text-foreground">
                Quản Lý Phương Tiện &amp; Biển Số Xe
              </h1>
              <p className="text-xs text-muted-foreground">
                Đăng ký biển số xe chuẩn hóa, phân loại ô tô/xe máy và gán quyền sở hữu với hồ sơ khách hàng.
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
        searchPlaceholder="Tìm kiếm biển số xe..."
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
            <div className="w-[180px]">
              <Select
                value={typeFilter}
                onValueChange={(val) => {
                  setTypeFilter(val);
                  setPageIndex(1);
                }}
              >
                <SelectTrigger className="h-9 text-xs">
                  <SelectValue placeholder="Lọc theo loại xe" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all" className="text-xs">
                    Tất cả loại xe
                  </SelectItem>
                  <SelectItem value={String(VehicleType.Car)} className="text-xs">
                    Ô tô
                  </SelectItem>
                  <SelectItem value={String(VehicleType.Motorbike)} className="text-xs">
                    Xe máy
                  </SelectItem>
                  <SelectItem value={String(VehicleType.Bicycle)} className="text-xs">
                    Xe đạp / Xe điện
                  </SelectItem>
                  <SelectItem value={String(VehicleType.Other)} className="text-xs">
                    Khác
                  </SelectItem>
                </SelectContent>
              </Select>
            </div>
          )
        }
        onAddNew={() => {
          setSelectedVehicle(null);
          setIsFormOpen(true);
        }}
        addNewLabel="Đăng ký phương tiện"
        actions={{
          onEdit: (item) => {
            setSelectedVehicle(item);
            setIsFormOpen(true);
          },
          onDelete: (item) => {
            setDeleteCandidate(item);
          },
          onRestore: (item) => {
            restoreMutation.mutate(item.id);
          },
        }}
        emptyTitle={isTrashMode ? 'Thùng rác trống' : 'Không có phương tiện nào'}
        emptyDescription={
          isTrashMode
            ? 'Hiện tại không có phương tiện nào nằm trong thùng rác.'
            : 'Chưa có dữ liệu phương tiện hoặc không có biển số nào khớp với từ khóa tìm kiếm.'
        }
      />

      {/* Modal Form Thêm/Sửa Phương tiện */}
      <VehicleFormDialog
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        initialData={selectedVehicle}
        clients={clients}
        onSubmit={handleFormSubmit}
        isSubmitting={createMutation.isPending || updateMutation.isPending}
      />

      {/* Confirm Xóa Mềm Phương tiện */}
      <ConfirmDialog
        open={Boolean(deleteCandidate)}
        onOpenChange={(open) => !open && setDeleteCandidate(null)}
        title="Xác Nhận Xóa Phương Tiện"
        description={`Bạn có chắc chắn muốn chuyển phương tiện biển số "${deleteCandidate?.plateNumber}" vào thùng rác không? Lưu ý: Phương tiện đang trong phiên gửi xe tại bãi sẽ không thể xóa.`}
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
