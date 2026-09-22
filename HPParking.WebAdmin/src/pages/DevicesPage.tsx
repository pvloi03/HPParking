import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import {
  Cpu,
  Trash2,
  Camera,
  ScanFace,
  HelpCircle,
  Network,
} from 'lucide-react';
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
import { DeviceFormDialog } from '@/components/infrastructure/DeviceFormDialog';
import { devicesApi, extractErrorMessage } from '@/api/infrastructureApi';
import {
  DeviceType,
  type DeviceDto,
  type CreateDeviceRequest,
  type UpdateDeviceRequest,
} from '@/types/infrastructure';

export function DevicesPage() {
  const queryClient = useQueryClient();

  // State bộ lọc và phân trang
  const [pageIndex, setPageIndex] = useState(1);
  const pageSize = 15;
  const [searchKeyword, setSearchKeyword] = useState('');
  const [statusFilter, setStatusFilter] = useState<boolean | 'all'>('all');
  const [typeFilter, setTypeFilter] = useState<string>('all');
  const [isTrashMode, setIsTrashMode] = useState(false);

  // State Modal Form & Confirm Delete
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [selectedDevice, setSelectedDevice] = useState<DeviceDto | null>(null);
  const [deleteCandidate, setDeleteCandidate] = useState<DeviceDto | null>(null);

  // Query: Lấy danh sách thiết bị
  const { data, isLoading } = useQuery({
    queryKey: [
      'devices',
      pageIndex,
      pageSize,
      searchKeyword,
      statusFilter,
      typeFilter,
      isTrashMode,
    ],
    queryFn: () =>
      devicesApi.getPaged({
        pageIndex,
        pageSize,
        keyword: searchKeyword.trim() || undefined,
        isActive: statusFilter === 'all' ? undefined : statusFilter,
        type: typeFilter === 'all' ? undefined : (Number(typeFilter) as DeviceType),
        onlyDeleted: isTrashMode,
      }),
  });

  // Mutation: Thêm mới thiết bị
  const createMutation = useMutation({
    mutationFn: (payload: CreateDeviceRequest) => devicesApi.create(payload),
    onSuccess: (newDevice) => {
      toast.success(`Đã thêm mới thiết bị "${newDevice.name}" thành công`);
      setIsFormOpen(false);
      void queryClient.invalidateQueries({ queryKey: ['devices'] });
      void queryClient.invalidateQueries({ queryKey: ['devices-all'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Mutation: Cập nhật thiết bị
  const updateMutation = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateDeviceRequest }) =>
      devicesApi.update(id, payload),
    onSuccess: (updated) => {
      toast.success(`Đã cập nhật thiết bị "${updated.name}" thành công`);
      setIsFormOpen(false);
      setSelectedDevice(null);
      void queryClient.invalidateQueries({ queryKey: ['devices'] });
      void queryClient.invalidateQueries({ queryKey: ['devices-all'] });
      void queryClient.invalidateQueries({ queryKey: ['lanes'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Mutation: Xóa mềm thiết bị
  const deleteMutation = useMutation({
    mutationFn: (id: string) => devicesApi.delete(id, false),
    onSuccess: () => {
      toast.success('Đã chuyển thiết bị vào thùng rác thành công');
      setDeleteCandidate(null);
      void queryClient.invalidateQueries({ queryKey: ['devices'] });
      void queryClient.invalidateQueries({ queryKey: ['devices-all'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Mutation: Khôi phục thiết bị
  const restoreMutation = useMutation({
    mutationFn: (id: string) => devicesApi.restore(id),
    onSuccess: (restored) => {
      toast.success(`Đã khôi phục thiết bị "${restored.name}" thành công`);
      void queryClient.invalidateQueries({ queryKey: ['devices'] });
      void queryClient.invalidateQueries({ queryKey: ['devices-all'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Xử lý submit form
  const handleFormSubmit = async (
    payload: CreateDeviceRequest | UpdateDeviceRequest
  ) => {
    if (selectedDevice) {
      await updateMutation.mutateAsync({
        id: selectedDevice.id,
        payload: payload as UpdateDeviceRequest,
      });
    } else {
      await createMutation.mutateAsync(payload as CreateDeviceRequest);
    }
  };

  const getDeviceTypeBadge = (type: DeviceType) => {
    switch (type) {
      case DeviceType.Camera:
        return (
          <Badge className="bg-blue-100 text-blue-800 hover:bg-blue-100 dark:bg-blue-950 dark:text-blue-300 gap-1 text-[11px] font-medium">
            <Camera className="h-3 w-3" />
            <span>Camera</span>
          </Badge>
        );
      case DeviceType.Controller:
        return (
          <Badge className="bg-amber-100 text-amber-800 hover:bg-amber-100 dark:bg-amber-950 dark:text-amber-300 gap-1 text-[11px] font-medium">
            <Cpu className="h-3 w-3" />
            <span>Controller</span>
          </Badge>
        );
      case DeviceType.FaceId:
        return (
          <Badge className="bg-emerald-100 text-emerald-800 hover:bg-emerald-100 dark:bg-emerald-950 dark:text-emerald-300 gap-1 text-[11px] font-medium">
            <ScanFace className="h-3 w-3" />
            <span>FaceID</span>
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
  const columns: ColumnDef<DeviceDto>[] = [
    {
      header: 'Mã thiết bị',
      accessorKey: 'code',
      className: 'font-mono font-medium text-xs text-blue-600 dark:text-blue-400 w-36',
      mobileLabel: 'Mã',
    },
    {
      header: 'Tên thiết bị ngoại vi',
      accessorKey: 'name',
      className: 'font-semibold min-w-[200px]',
      mobileLabel: 'Tên thiết bị',
    },
    {
      header: 'Phân loại',
      accessorKey: 'type',
      cell: (item) => getDeviceTypeBadge(item.type),
      className: 'w-36',
      mobileLabel: 'Loại',
    },
    {
      header: 'Địa chỉ IP & Cổng',
      cell: (item) => (
        <span className="font-mono text-xs flex items-center gap-1 text-foreground">
          <Network className="h-3.5 w-3.5 text-muted-foreground" />
          {item.ipAddress}:{item.port}
        </span>
      ),
      className: 'w-44',
      mobileLabel: 'IP:Port',
    },
    {
      header: 'Tài khoản',
      accessorKey: 'userName',
      cell: (item) => item.userName || <span className="text-muted-foreground">—</span>,
      className: 'w-32',
      mobileLabel: 'User',
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
              <Cpu className="h-5 w-5" />
            </div>
            <div>
              <h1 className="text-xl font-bold tracking-tight text-foreground">
                Quản Lý Thiết Bị Ngoại Vi
              </h1>
              <p className="text-xs text-muted-foreground">
                Cấu hình thông số kết nối IP, Port và xác thực cho Camera, Bộ điều khiển Barrier và FaceID.
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
        searchPlaceholder="Tìm kiếm mã, tên thiết bị, địa chỉ IP..."
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
                  <SelectValue placeholder="Lọc theo loại thiết bị" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all" className="text-xs">
                    Tất cả loại thiết bị
                  </SelectItem>
                  <SelectItem value={String(DeviceType.Camera)} className="text-xs">
                    Camera (Biển số / Toàn cảnh)
                  </SelectItem>
                  <SelectItem value={String(DeviceType.Controller)} className="text-xs">
                    Controller (Barrier)
                  </SelectItem>
                  <SelectItem value={String(DeviceType.FaceId)} className="text-xs">
                    FaceID (Khuôn mặt)
                  </SelectItem>
                  <SelectItem value={String(DeviceType.Other)} className="text-xs">
                    Thiết bị khác
                  </SelectItem>
                </SelectContent>
              </Select>
            </div>
          )
        }
        onAddNew={() => {
          setSelectedDevice(null);
          setIsFormOpen(true);
        }}
        addNewLabel="Thêm mới thiết bị"
        actions={{
          onEdit: (item) => {
            setSelectedDevice(item);
            setIsFormOpen(true);
          },
          onDelete: (item) => {
            setDeleteCandidate(item);
          },
          onRestore: (item) => {
            restoreMutation.mutate(item.id);
          },
        }}
        emptyTitle={isTrashMode ? 'Thùng rác trống' : 'Không có thiết bị nào'}
        emptyDescription={
          isTrashMode
            ? 'Hiện tại không có thiết bị ngoại vi nào nằm trong thùng rác.'
            : 'Chưa có dữ liệu thiết bị hoặc không có bản ghi nào khớp với điều kiện tìm kiếm.'
        }
      />

      {/* Modal Form Thêm/Sửa Thiết Bị */}
      <DeviceFormDialog
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        initialData={selectedDevice}
        onSubmit={handleFormSubmit}
        isSubmitting={createMutation.isPending || updateMutation.isPending}
      />

      {/* Confirm Xóa Mềm Thiết Bị */}
      <ConfirmDialog
        open={Boolean(deleteCandidate)}
        onOpenChange={(open) => !open && setDeleteCandidate(null)}
        title="Xác Nhận Xóa Thiết Bị Ngoại Vi"
        description={`Bạn có chắc chắn muốn chuyển thiết bị "${deleteCandidate?.name}" vào thùng rác không? Lưu ý: Hệ thống sẽ từ chối xóa nếu thiết bị này đang được liên kết cấu hình trong bất kỳ làn xe nào (Toàn vẹn tham chiếu ADR 0030 & ADR 0034).`}
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
