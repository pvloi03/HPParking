import { useState, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query';
import { toast } from 'sonner';
import {
  Trash2,
  RotateCcw,
  ShieldAlert,
  Users,
  Car,
  Building,
  Building2,
  Briefcase,
  DoorOpen,
  Route,
  Cpu,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { DataTable, type ColumnDef } from '@/components/common/DataTable';
import { ConfirmDialog } from '@/components/common/ConfirmDialog';
import { clientApi } from '@/api/clientApi';
import { vehicleApi } from '@/api/vehicleApi';
import { companiesApi, departmentsApi, contractorsApi } from '@/api/masterDataApi';
import { gatesApi, lanesApi, devicesApi } from '@/api/infrastructureApi';
import { extractErrorMessage } from '@/api/clientApi';
import { cn } from '@/lib/utils';
import type { PaginationMetadata } from '@/types/masterData';

type EntityType =
  | 'clients'
  | 'vehicles'
  | 'companies'
  | 'departments'
  | 'contractors'
  | 'gates'
  | 'lanes'
  | 'devices';

interface EntityConfig {
  id: EntityType;
  label: string;
  icon: React.ComponentType<{ className?: string }>;
  description: string;
}

const ENTITY_CONFIGS: EntityConfig[] = [
  { id: 'clients', label: 'Khách hàng', icon: Users, description: 'Hồ sơ định danh khách hàng & FaceID' },
  { id: 'vehicles', label: 'Phương tiện', icon: Car, description: 'Biển số xe & quyền sở hữu' },
  { id: 'companies', label: 'Công ty', icon: Building, description: 'Danh mục công ty & đơn vị gốc' },
  { id: 'departments', label: 'Phòng ban', icon: Building2, description: 'Phòng ban trực thuộc công ty' },
  { id: 'contractors', label: 'Nhà thầu', icon: Briefcase, description: 'Nhà thầu & đối tác thi công' },
  { id: 'gates', label: 'Cổng bãi xe', icon: DoorOpen, description: 'Cổng kiểm soát ra vào' },
  { id: 'lanes', label: 'Làn xe', icon: Route, description: 'Làn kiểm soát luồng xe ra/vào' },
  { id: 'devices', label: 'Thiết bị', icon: Cpu, description: 'Camera, Barie, LED, Đầu đọc FaceID' },
];

export function RecycleBinPage() {
  const queryClient = useQueryClient();

  const [selectedEntity, setSelectedEntity] = useState<EntityType>('clients');
  const [pageIndex, setPageIndex] = useState(1);
  const pageSize = 15;
  const [searchKeyword, setSearchKeyword] = useState('');
  const [selectedRowIds, setSelectedRowIds] = useState<string[]>([]);

  // Dialog states
  const [restoreCandidate, setRestoreCandidate] = useState<{ id: string; name: string } | null>(null);
  const [hardDeleteCandidate, setHardDeleteCandidate] = useState<{ id: string; name: string } | null>(null);
  const [isBulkRestoreOpen, setIsBulkRestoreOpen] = useState(false);
  const [isBulkHardDeleteOpen, setIsBulkHardDeleteOpen] = useState(false);
  const [isBulkLoading, setIsBulkLoading] = useState(false);

  // Tự động bỏ chọn checkbox khi đổi tab đối tượng, trang hoặc từ khóa tìm kiếm
  useEffect(() => {
    setSelectedRowIds([]);
  }, [selectedEntity, pageIndex, searchKeyword]);

  const activeConfig = ENTITY_CONFIGS.find((e) => e.id === selectedEntity) || ENTITY_CONFIGS[0];

  // TanStack Query: Lấy danh sách bản ghi đã bị xóa (onlyDeleted: true)
  const { data, isLoading } = useQuery({
    queryKey: ['recycle-bin', selectedEntity, pageIndex, pageSize, searchKeyword],
    queryFn: async () => {
      const keyword = searchKeyword.trim() || undefined;
      switch (selectedEntity) {
        case 'clients':
          return clientApi.getPaged({ pageIndex, pageSize, keyword, onlyDeleted: true });
        case 'vehicles':
          return vehicleApi.getPaged({ pageIndex, pageSize, keyword, onlyDeleted: true });
        case 'companies':
          return companiesApi.getPaged({ pageIndex, pageSize, keyword, onlyDeleted: true });
        case 'departments':
          return departmentsApi.getPaged({ pageIndex, pageSize, keyword, onlyDeleted: true });
        case 'contractors':
          return contractorsApi.getPaged({ pageIndex, pageSize, keyword, onlyDeleted: true });
        case 'gates':
          return gatesApi.getPaged({ pageIndex, pageSize, keyword, onlyDeleted: true });
        case 'lanes':
          return lanesApi.getPaged({ pageIndex, pageSize, keyword, onlyDeleted: true });
        case 'devices':
          return devicesApi.getPaged({ pageIndex, pageSize, keyword, onlyDeleted: true });
      }
    },
    placeholderData: keepPreviousData,
  });

  const getEntityApi = (type: EntityType) => {
    switch (type) {
      case 'clients':
        return {
          restore: (id: string) => clientApi.restore(id),
          hardDelete: (id: string) => clientApi.delete(id, true),
        };
      case 'vehicles':
        return {
          restore: (id: string) => vehicleApi.restore(id),
          hardDelete: (id: string) => vehicleApi.delete(id, true),
        };
      case 'companies':
        return {
          restore: (id: string) => companiesApi.restore(id),
          hardDelete: (id: string) => companiesApi.delete(id, true),
        };
      case 'departments':
        return {
          restore: (id: string) => departmentsApi.restore(id),
          hardDelete: (id: string) => departmentsApi.delete(id, true),
        };
      case 'contractors':
        return {
          restore: (id: string) => contractorsApi.restore(id),
          hardDelete: (id: string) => contractorsApi.delete(id, true),
        };
      case 'gates':
        return {
          restore: (id: string) => gatesApi.restore(id),
          hardDelete: (id: string) => gatesApi.delete(id, true),
        };
      case 'lanes':
        return {
          restore: (id: string) => lanesApi.restore(id),
          hardDelete: (id: string) => lanesApi.delete(id, true),
        };
      case 'devices':
        return {
          restore: (id: string) => devicesApi.restore(id),
          hardDelete: (id: string) => devicesApi.delete(id, true),
        };
    }
  };

  // Mutation: Khôi phục đơn lẻ
  const restoreMutation = useMutation({
    mutationFn: async ({ id, entity }: { id: string; entity: EntityType }) => {
      const api = getEntityApi(entity);
      return api.restore(id);
    },
    onSuccess: () => {
      toast.success(`Đã khôi phục ${activeConfig.label.toLowerCase()} thành công.`);
      setRestoreCandidate(null);
      void queryClient.invalidateQueries({ queryKey: ['recycle-bin', selectedEntity] });
      void queryClient.invalidateQueries({ queryKey: [selectedEntity] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Mutation: Xóa vĩnh viễn (Xóa cứng) đơn lẻ
  const hardDeleteMutation = useMutation({
    mutationFn: async ({ id, entity }: { id: string; entity: EntityType }) => {
      const api = getEntityApi(entity);
      return api.hardDelete(id);
    },
    onSuccess: () => {
      toast.success(`Đã xóa vĩnh viễn ${activeConfig.label.toLowerCase()} khỏi cơ sở dữ liệu.`);
      setHardDeleteCandidate(null);
      void queryClient.invalidateQueries({ queryKey: ['recycle-bin', selectedEntity] });
      void queryClient.invalidateQueries({ queryKey: [selectedEntity] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Thực thi khôi phục hàng loạt
  const handleExecuteBulkRestore = async () => {
    if (selectedRowIds.length === 0) return;
    setIsBulkLoading(true);
    const api = getEntityApi(selectedEntity);
    try {
      const results = await Promise.allSettled(selectedRowIds.map((id) => api.restore(id)));
      const succeeded = results.filter((r) => r.status === 'fulfilled').length;
      const failed = results.length - succeeded;
      if (failed > 0) {
        toast.warning(
          `Đã khôi phục ${succeeded}/${results.length} bản ghi (${failed} không thành công do ràng buộc cha).`
        );
      } else {
        toast.success(`Đã khôi phục thành công ${succeeded} bản ghi.`);
      }
      setSelectedRowIds([]);
      setIsBulkRestoreOpen(false);
      void queryClient.invalidateQueries({ queryKey: ['recycle-bin', selectedEntity] });
      void queryClient.invalidateQueries({ queryKey: [selectedEntity] });
    } catch (err) {
      toast.error(extractErrorMessage(err));
    } finally {
      setIsBulkLoading(false);
    }
  };

  // Thực thi xóa vĩnh viễn (xóa cứng) hàng loạt
  const handleExecuteBulkHardDelete = async () => {
    if (selectedRowIds.length === 0) return;
    setIsBulkLoading(true);
    const api = getEntityApi(selectedEntity);
    try {
      const results = await Promise.allSettled(selectedRowIds.map((id) => api.hardDelete(id)));
      const succeeded = results.filter((r) => r.status === 'fulfilled').length;
      const failed = results.length - succeeded;
      if (failed > 0) {
        toast.warning(
          `Đã xóa vĩnh viễn ${succeeded}/${results.length} bản ghi (${failed} không thể xóa do ràng buộc dữ liệu).`
        );
      } else {
        toast.success(`Đã xóa vĩnh viễn ${succeeded} bản ghi khỏi cơ sở dữ liệu.`);
      }
      setSelectedRowIds([]);
      setIsBulkHardDeleteOpen(false);
      void queryClient.invalidateQueries({ queryKey: ['recycle-bin', selectedEntity] });
      void queryClient.invalidateQueries({ queryKey: [selectedEntity] });
    } catch (err) {
      toast.error(extractErrorMessage(err));
    } finally {
      setIsBulkLoading(false);
    }
  };

  // Định nghĩa các cột tùy biến theo từng đối tượng
  const getColumns = (): ColumnDef<any>[] => {
    switch (selectedEntity) {
      case 'clients':
        return [
          {
            header: 'Khách hàng',
            cell: (item: any) => (
              <div>
                <span className="font-semibold text-foreground text-xs block">
                  {item.name || '—'}
                </span>
                <span className="font-mono text-[11px] text-muted-foreground block">
                  {item.code || '—'}
                </span>
              </div>
            ),
          },
          {
            header: 'Số điện thoại',
            accessorKey: 'phoneNumber',
            cell: (item: any) => (
              <span className="font-mono text-xs text-foreground">{item.phoneNumber || '—'}</span>
            ),
          },
          {
            header: 'CCCD / Hộ chiếu',
            accessorKey: 'idCardNumber',
            cell: (item: any) => (
              <span className="font-mono text-xs text-muted-foreground">
                {item.idCardNumber || '—'}
              </span>
            ),
          },
        ];

      case 'vehicles':
        return [
          {
            header: 'Biển số xe',
            accessorKey: 'plateNumber',
            cell: (item: any) => (
              <Badge variant="outline" className="font-mono text-xs font-semibold px-2 py-0.5">
                {item.plateNumber || '—'}
              </Badge>
            ),
          },
          {
            header: 'Loại phương tiện',
            cell: (item: any) => {
              const types = ['Xe máy', 'Ô tô', 'Xe đạp điện', 'Xe tải'];
              return <span className="text-xs text-foreground">{types[item.type] || 'Khác'}</span>;
            },
          },
          {
            header: 'Mô tả / Hiệu xe',
            accessorKey: 'description',
            cell: (item: any) => (
              <span className="text-xs text-muted-foreground">{item.description || '—'}</span>
            ),
          },
        ];

      case 'companies':
      case 'contractors':
        return [
          {
            header: 'Mã đơn vị',
            accessorKey: 'code',
            cell: (item: any) => (
              <span className="font-mono text-xs font-semibold">{item.code || '—'}</span>
            ),
          },
          {
            header: 'Tên đơn vị',
            accessorKey: 'name',
            cell: (item: any) => (
              <span className="text-xs font-semibold text-foreground">{item.name || '—'}</span>
            ),
          },
          {
            header: 'Mã số thuế',
            accessorKey: 'taxCode',
            cell: (item: any) => (
              <span className="font-mono text-xs text-muted-foreground">{item.taxCode || '—'}</span>
            ),
          },
        ];

      case 'departments':
        return [
          {
            header: 'Mã phòng ban',
            accessorKey: 'code',
            cell: (item: any) => (
              <span className="font-mono text-xs font-semibold">{item.code || '—'}</span>
            ),
          },
          {
            header: 'Tên phòng ban',
            accessorKey: 'name',
            cell: (item: any) => (
              <span className="text-xs font-semibold text-foreground">{item.name || '—'}</span>
            ),
          },
        ];

      case 'gates':
        return [
          {
            header: 'Mã cổng',
            accessorKey: 'code',
            cell: (item: any) => (
              <span className="font-mono text-xs font-semibold">{item.code || '—'}</span>
            ),
          },
          {
            header: 'Tên cổng kiểm soát',
            accessorKey: 'name',
            cell: (item: any) => (
              <span className="text-xs font-semibold text-foreground">{item.name || '—'}</span>
            ),
          },
        ];

      case 'lanes':
        return [
          {
            header: 'Mã làn',
            accessorKey: 'code',
            cell: (item: any) => (
              <span className="font-mono text-xs font-semibold">{item.code || '—'}</span>
            ),
          },
          {
            header: 'Tên làn xe',
            accessorKey: 'name',
            cell: (item: any) => (
              <span className="text-xs font-semibold text-foreground">{item.name || '—'}</span>
            ),
          },
          {
            header: 'Hướng làn',
            cell: (item: any) => (
              <Badge variant="outline" className="text-[11px]">
                {item.direction === 0 ? 'Làn Vào' : item.direction === 1 ? 'Làn Ra' : 'Hai Chiều'}
              </Badge>
            ),
          },
        ];

      case 'devices':
        return [
          {
            header: 'Mã thiết bị',
            accessorKey: 'code',
            cell: (item: any) => (
              <span className="font-mono text-xs font-semibold">{item.code || '—'}</span>
            ),
          },
          {
            header: 'Tên thiết bị',
            accessorKey: 'name',
            cell: (item: any) => (
              <span className="text-xs font-semibold text-foreground">{item.name || '—'}</span>
            ),
          },
          {
            header: 'Địa chỉ IP',
            accessorKey: 'ipAddress',
            cell: (item: any) => (
              <span className="font-mono text-xs text-muted-foreground">
                {item.ipAddress || '—'}
              </span>
            ),
          },
        ];

      default:
        return [
          {
            header: 'Mã',
            accessorKey: 'code',
            cell: (item: any) => <span className="font-mono text-xs">{item.code || '—'}</span>,
          },
          {
            header: 'Tên',
            accessorKey: 'name',
            cell: (item: any) => <span className="text-xs">{item.name || '—'}</span>,
          },
        ];
    }
  };

  const getRecordDisplayName = (item: any) => {
    return item.name || item.plateNumber || item.code || item.id;
  };

  return (
    <div className="space-y-6">
      {/* Header trang */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2 border-b border-border pb-4">
        <div className="flex items-center gap-3">
          <div className="p-2.5 rounded-xl bg-amber-500/10 text-amber-600 dark:text-amber-400">
            <Trash2 className="h-6 w-6" />
          </div>
          <div>
            <h1 className="text-xl font-bold tracking-tight text-foreground">
              Thùng Rác Hệ Thống
            </h1>
            <p className="text-xs text-muted-foreground">
              Quản lý tập trung các bản ghi đã xóa tạm thời. Khôi phục dữ liệu hoặc xóa vĩnh viễn (xóa cứng) khỏi cơ sở dữ liệu.
            </p>
          </div>
        </div>
      </div>

      {/* Tabs chuyển đổi đối tượng dữ liệu */}
      <div role="tablist" aria-label="Lọc theo đối tượng" className="flex items-center gap-1.5 overflow-x-auto pb-1 border-b border-border">
        {ENTITY_CONFIGS.map((config) => {
          const Icon = config.icon;
          const isActive = selectedEntity === config.id;
          return (
            <button
              key={config.id}
              type="button"
              role="tab"
              aria-selected={isActive}
              onClick={() => {
                setSelectedEntity(config.id);
                setPageIndex(1);
                setSearchKeyword('');
              }}
              className={cn(
                'flex items-center gap-2 px-3 py-2 rounded-lg text-xs font-medium transition-all shrink-0 cursor-pointer',
                isActive
                  ? 'bg-amber-500/15 text-amber-700 dark:text-amber-300 font-semibold shadow-2xs border border-amber-500/30'
                  : 'text-muted-foreground hover:text-foreground hover:bg-muted/60'
              )}
            >
              <Icon className={cn('h-4 w-4', isActive ? 'text-amber-600' : 'text-muted-foreground')} />
              <span>{config.label}</span>
            </button>
          );
        })}
      </div>

      {/* Bảng dữ liệu thùng rác */}
      <DataTable
        data={data?.items || []}
        columns={getColumns()}
        pagination={
          (data?.pagination as PaginationMetadata) || {
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
        searchPlaceholder={`Tìm kiếm ${activeConfig.label.toLowerCase()} trong thùng rác...`}
        isTrashMode={true}
        selectable={true}
        selectedRowIds={selectedRowIds}
        onSelectedRowIdsChange={setSelectedRowIds}
        bulkActions={
          <div className="flex items-center gap-1.5 ml-1">
            <Button
              size="sm"
              variant="outline"
              onClick={() => setIsBulkRestoreOpen(true)}
              className="h-7 px-2.5 text-xs text-blue-600 hover:text-blue-700 hover:bg-blue-50 dark:hover:bg-blue-950 border-blue-200 dark:border-blue-900 cursor-pointer"
            >
              <RotateCcw className="h-3.5 w-3.5 mr-1" />
              <span>Khôi phục ({selectedRowIds.length})</span>
            </Button>
            <Button
              size="sm"
              variant="destructive"
              onClick={() => setIsBulkHardDeleteOpen(true)}
              className="h-7 px-2.5 text-xs cursor-pointer shadow-2xs"
            >
              <ShieldAlert className="h-3.5 w-3.5 mr-1" />
              <span>Xóa vĩnh viễn ({selectedRowIds.length})</span>
            </Button>
          </div>
        }
        actions={{
          onRestore: (item: any) => {
            setRestoreCandidate({ id: item.id, name: getRecordDisplayName(item) });
          },
          onHardDelete: (item: any) => {
            setHardDeleteCandidate({ id: item.id, name: getRecordDisplayName(item) });
          },
        }}
        emptyTitle={`Thùng rác ${activeConfig.label.toLowerCase()} trống`}
        emptyDescription={`Không có bản ghi ${activeConfig.label.toLowerCase()} nào nằm trong thùng rác hệ thống.`}
      />

      {/* Confirm Khôi Phục Đơn Lẻ */}
      <ConfirmDialog
        open={Boolean(restoreCandidate)}
        onOpenChange={(open) => !open && setRestoreCandidate(null)}
        title={`Khôi Phục ${activeConfig.label}`}
        description={`Bạn có chắc chắn muốn khôi phục bản ghi "${restoreCandidate?.name || 'này'}" về trạng thái hoạt động bình thường không?`}
        confirmText="Khôi Phục"
        cancelText="Hủy Bỏ"
        variant="default"
        isLoading={restoreMutation.isPending}
        icon={<RotateCcw className="h-5 w-5 text-blue-600" />}
        confirmIcon={<RotateCcw className="h-3.5 w-3.5" />}
        onConfirm={() => {
          if (restoreCandidate) {
            restoreMutation.mutate({ id: restoreCandidate.id, entity: selectedEntity });
          }
        }}
      />

      {/* Confirm Xóa Cứng (Xóa Vĩnh Viễn) Đơn Lẻ */}
      <ConfirmDialog
        open={Boolean(hardDeleteCandidate)}
        onOpenChange={(open) => !open && setHardDeleteCandidate(null)}
        title={`CẢNH BÁO: Xóa Vĩnh Viễn ${activeConfig.label} (Xóa Cứng)`}
        description={
          <div className="space-y-2 text-xs">
            <p>
              Bạn đang thực hiện xóa vĩnh viễn bản ghi{' '}
              <strong className="text-foreground">"{hardDeleteCandidate?.name}"</strong>.
            </p>
            <div className="p-2.5 rounded-lg bg-destructive/10 border border-destructive/20 text-destructive leading-relaxed">
              <strong>Hành động này mang tính chất phá hủy và không thể hoàn tác:</strong>
              <ul className="list-disc pl-4 mt-1 space-y-0.5">
                <li>Bản ghi sẽ bị xóa hoàn toàn khỏi cơ sở dữ liệu.</li>
                {selectedEntity === 'clients' && (
                  <li>Lệnh thu hồi FaceID sẽ được gửi đến toàn bộ thiết bị nhận diện tại làn xe.</li>
                )}
                <li>Hệ thống sẽ từ chối nếu bản ghi có quan hệ ràng buộc với các thực thể khác.</li>
              </ul>
            </div>
          </div>
        }
        confirmText="Xóa Vĩnh Viễn Ngay"
        cancelText="Hủy Bỏ"
        variant="destructive"
        isLoading={hardDeleteMutation.isPending}
        icon={<ShieldAlert className="h-5 w-5 text-destructive" />}
        confirmIcon={<ShieldAlert className="h-3.5 w-3.5" />}
        onConfirm={() => {
          if (hardDeleteCandidate) {
            hardDeleteMutation.mutate({ id: hardDeleteCandidate.id, entity: selectedEntity });
          }
        }}
      />

      {/* Confirm Khôi Phục Hàng Loạt */}
      <ConfirmDialog
        open={isBulkRestoreOpen}
        onOpenChange={(open) => !open && setIsBulkRestoreOpen(false)}
        title={`Khôi Phục Hàng Loạt ${activeConfig.label}`}
        description={`Bạn có chắc chắn muốn khôi phục ${selectedRowIds.length} bản ghi ${activeConfig.label.toLowerCase()} đã chọn về trạng thái hoạt động bình thường?`}
        confirmText={`Khôi Phục (${selectedRowIds.length})`}
        cancelText="Hủy Bỏ"
        variant="default"
        isLoading={isBulkLoading}
        icon={<RotateCcw className="h-5 w-5 text-blue-600" />}
        confirmIcon={<RotateCcw className="h-3.5 w-3.5" />}
        onConfirm={handleExecuteBulkRestore}
      />

      {/* Confirm Xóa Cứng (Xóa Vĩnh Viễn) Hàng Loạt */}
      <ConfirmDialog
        open={isBulkHardDeleteOpen}
        onOpenChange={(open) => !open && setIsBulkHardDeleteOpen(false)}
        title={`CẢNH BÁO: Xóa Vĩnh Viễn Hàng Loạt (Xóa Cứng)`}
        description={
          <div className="space-y-2 text-xs">
            <p>
              Bạn có chắc chắn muốn{' '}
              <strong className="text-destructive font-semibold">
                xóa vĩnh viễn {selectedRowIds.length} bản ghi {activeConfig.label.toLowerCase()}
              </strong>{' '}
              khỏi hệ thống?
            </p>
            <div className="p-2.5 rounded-lg bg-destructive/10 border border-destructive/20 text-destructive leading-relaxed">
              <strong>Cảnh báo quan trọng:</strong>
              <ul className="list-disc pl-4 mt-1 space-y-0.5">
                <li>Dữ liệu bị xóa vĩnh viễn khỏi CSDL và không thể phục hồi.</li>
                {selectedEntity === 'clients' && (
                  <li>Toàn bộ FaceID liên quan sẽ bị thu hồi khỏi các thiết bị làn xe.</li>
                )}
                <li>Các bản ghi còn quan hệ phụ thuộc sẽ bị hệ thống tự động chặn xóa.</li>
              </ul>
            </div>
          </div>
        }
        confirmText={`Xóa Vĩnh Viễn (${selectedRowIds.length})`}
        cancelText="Hủy Bỏ"
        variant="destructive"
        isLoading={isBulkLoading}
        icon={<ShieldAlert className="h-5 w-5 text-destructive" />}
        confirmIcon={<ShieldAlert className="h-3.5 w-3.5" />}
        onConfirm={handleExecuteBulkHardDelete}
      />
    </div>
  );
}
