import { useState, useEffect, useMemo } from 'react';
import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query';
import { toast } from 'sonner';
import {
  Trash2,
  RotateCcw,
  ShieldAlert,
  Layers,
  Users,
  Car,
  Building,
  Building2,
  Briefcase,
  DoorOpen,
  Route,
  Cpu,
  UserCog,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { DataTable, type ColumnDef } from '@/components/common/DataTable';
import { ConfirmDialog } from '@/components/common/ConfirmDialog';
import { clientApi } from '@/api/clientApi';
import { vehicleApi } from '@/api/vehicleApi';
import { companiesApi, departmentsApi, contractorsApi } from '@/api/masterDataApi';
import { gatesApi, lanesApi, devicesApi } from '@/api/infrastructureApi';
import { usersApi } from '@/api/userApi';
import { extractErrorMessage } from '@/api/clientApi';
import { usePermissions } from '@/hooks/usePermissions';
import { UserRole } from '@/types/user';
import { cn } from '@/lib/utils';
import type { PaginationMetadata } from '@/types/masterData';

type EntityType =
  | 'all'
  | 'clients'
  | 'vehicles'
  | 'companies'
  | 'departments'
  | 'contractors'
  | 'gates'
  | 'lanes'
  | 'devices'
  | 'users';

interface EntityConfig {
  id: EntityType;
  label: string;
  icon: React.ComponentType<{ className?: string }>;
  description: string;
  roles?: (UserRole | string | number)[];
}

const ENTITY_CONFIGS: EntityConfig[] = [
  { id: 'all', label: 'Tất cả', icon: Layers, description: 'Toàn bộ bản ghi đã bị xóa trong hệ thống' },
  { id: 'clients', label: 'Khách hàng', icon: Users, description: 'Hồ sơ định danh khách hàng & FaceID' },
  { id: 'vehicles', label: 'Phương tiện', icon: Car, description: 'Biển số xe & quyền sở hữu' },
  { id: 'companies', label: 'Công ty', icon: Building, description: 'Danh mục công ty & đơn vị gốc' },
  { id: 'departments', label: 'Phòng ban', icon: Building2, description: 'Phòng ban trực thuộc công ty' },
  { id: 'contractors', label: 'Nhà thầu', icon: Briefcase, description: 'Nhà thầu & đối tác thi công' },
  { id: 'gates', label: 'Cổng bãi xe', icon: DoorOpen, description: 'Cổng kiểm soát ra vào' },
  { id: 'lanes', label: 'Làn xe', icon: Route, description: 'Làn kiểm soát luồng xe ra/vào' },
  { id: 'devices', label: 'Thiết bị', icon: Cpu, description: 'Camera, Barie, LED, Đầu đọc FaceID' },
  { id: 'users', label: 'Tài khoản', icon: UserCog, description: 'Tài khoản người dùng & phân quyền', roles: [UserRole.Admin] },
];

export function RecycleBinPage() {
  const queryClient = useQueryClient();

  const [selectedEntity, setSelectedEntity] = useState<EntityType>('all');
  const [pageIndex, setPageIndex] = useState(1);
  const pageSize = 15;
  const [searchKeyword, setSearchKeyword] = useState('');
  const [selectedRowIds, setSelectedRowIds] = useState<string[]>([]);

  // Dialog states
  const [restoreCandidate, setRestoreCandidate] = useState<{ id: string; name: string; entity: EntityType } | null>(null);
  const [hardDeleteCandidate, setHardDeleteCandidate] = useState<{ id: string; name: string; entity: EntityType } | null>(null);
  const [isBulkRestoreOpen, setIsBulkRestoreOpen] = useState(false);
  const [isBulkHardDeleteOpen, setIsBulkHardDeleteOpen] = useState(false);
  const [isBulkLoading, setIsBulkLoading] = useState(false);

  // Tự động bỏ chọn checkbox khi đổi tab đối tượng, trang hoặc từ khóa tìm kiếm
  useEffect(() => {
    setSelectedRowIds([]);
  }, [selectedEntity, pageIndex, searchKeyword]);

  // Quyền truy cập
  const { isAdmin } = usePermissions();

  const availableConfigs = useMemo(() => {
    return ENTITY_CONFIGS.filter(
      (cfg) => !cfg.roles || (isAdmin && cfg.roles.includes(UserRole.Admin))
    );
  }, [isAdmin]);

  // Nếu người dùng không phải Admin mà đang ở tab 'users', tự động chuyển về 'all'
  useEffect(() => {
    if (!isAdmin && selectedEntity === 'users') {
      setSelectedEntity('all');
    }
  }, [isAdmin, selectedEntity]);

  const activeConfig = availableConfigs.find((e) => e.id === selectedEntity) || availableConfigs[0];

  // TanStack Query: Lấy số lượng bản ghi bị xóa của từng loại thực thể để hiển thị badge
  const { data: countsData } = useQuery({
    queryKey: ['recycle-bin-counts'],
    queryFn: async () => {
      const promises: Promise<any>[] = [
        clientApi.getPaged({ pageSize: 1, onlyDeleted: true }),
        vehicleApi.getPaged({ pageSize: 1, onlyDeleted: true }),
        companiesApi.getPaged({ pageSize: 1, onlyDeleted: true }),
        departmentsApi.getPaged({ pageSize: 1, onlyDeleted: true }),
        contractorsApi.getPaged({ pageSize: 1, onlyDeleted: true }),
        gatesApi.getPaged({ pageSize: 1, onlyDeleted: true }),
        lanesApi.getPaged({ pageSize: 1, onlyDeleted: true }),
        devicesApi.getPaged({ pageSize: 1, onlyDeleted: true }),
      ];

      // Chỉ Admin mới có quyền truy vấn số lượng tài khoản đã xóa
      if (isAdmin) {
        promises.push(usersApi.getPaged({ pageSize: 1, onlyDeleted: true }));
      }

      const results = await Promise.allSettled(promises);

      const getCount = (res: PromiseSettledResult<any>) =>
        res.status === 'fulfilled' ? res.value?.pagination?.totalCount || 0 : 0;

      const counts: Record<string, number> = {
        clients: getCount(results[0]),
        vehicles: getCount(results[1]),
        companies: getCount(results[2]),
        departments: getCount(results[3]),
        contractors: getCount(results[4]),
        gates: getCount(results[5]),
        lanes: getCount(results[6]),
        devices: getCount(results[7]),
        users: isAdmin && results[8] ? getCount(results[8]) : 0,
      };

      counts.all = Object.values(counts).reduce((sum, val) => sum + val, 0);

      return counts;
    },
    refetchOnWindowFocus: true,
  });

  // TanStack Query: Lấy danh sách bản ghi đã bị xóa (onlyDeleted: true)
  const { data, isLoading } = useQuery({
    queryKey: ['recycle-bin', selectedEntity, pageIndex, pageSize, searchKeyword],
    queryFn: async () => {
      const keyword = searchKeyword.trim() || undefined;

      if (selectedEntity === 'all') {
        const promises: Promise<any>[] = [
          clientApi.getPaged({ pageSize: 50, keyword, onlyDeleted: true }),
          vehicleApi.getPaged({ pageSize: 50, keyword, onlyDeleted: true }),
          companiesApi.getPaged({ pageSize: 50, keyword, onlyDeleted: true }),
          departmentsApi.getPaged({ pageSize: 50, keyword, onlyDeleted: true }),
          contractorsApi.getPaged({ pageSize: 50, keyword, onlyDeleted: true }),
          gatesApi.getPaged({ pageSize: 50, keyword, onlyDeleted: true }),
          lanesApi.getPaged({ pageSize: 50, keyword, onlyDeleted: true }),
          devicesApi.getPaged({ pageSize: 50, keyword, onlyDeleted: true }),
        ];

        if (isAdmin) {
          promises.push(usersApi.getPaged({ pageSize: 50, keyword, onlyDeleted: true }));
        }

        const results = await Promise.allSettled(promises);

        const tagItems = (res: PromiseSettledResult<any>, entity: EntityType) => {
          if (res.status !== 'fulfilled' || !res.value?.items) return [];
          return res.value.items.map((item: any) => ({
            ...item,
            _entityType: entity,
          }));
        };

        const allItems = [
          ...tagItems(results[0], 'clients'),
          ...tagItems(results[1], 'vehicles'),
          ...tagItems(results[2], 'companies'),
          ...tagItems(results[3], 'departments'),
          ...tagItems(results[4], 'contractors'),
          ...tagItems(results[5], 'gates'),
          ...tagItems(results[6], 'lanes'),
          ...tagItems(results[7], 'devices'),
          ...(isAdmin && results[8] ? tagItems(results[8], 'users') : []),
        ];

        // Sắp xếp giảm dần theo thời gian cập nhật/xóa
        allItems.sort((a, b) => {
          const dateA = new Date(a.updatedAt || a.createdAt || 0).getTime();
          const dateB = new Date(b.updatedAt || b.createdAt || 0).getTime();
          return dateB - dateA;
        });

        const totalCount = allItems.length;
        const totalPages = Math.ceil(totalCount / pageSize) || 1;
        const startIndex = (pageIndex - 1) * pageSize;
        const pagedItems = allItems.slice(startIndex, startIndex + pageSize);

        return {
          items: pagedItems,
          pagination: {
            pageIndex,
            pageSize,
            totalCount,
            totalPages,
            hasPreviousPage: pageIndex > 1,
            hasNextPage: pageIndex < totalPages,
          },
        };
      }

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
        case 'users':
          return usersApi.getPaged({ pageIndex, pageSize, keyword, onlyDeleted: true });
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
      case 'users':
        return {
          restore: (id: string) => usersApi.restore(id),
          hardDelete: (id: string) => usersApi.delete(id, true),
        };
      default:
        return {
          restore: (id: string) => clientApi.restore(id),
          hardDelete: (id: string) => clientApi.delete(id, true),
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
      toast.success('Đã khôi phục bản ghi thành công.');
      setRestoreCandidate(null);
      void queryClient.invalidateQueries({ queryKey: ['recycle-bin'] });
      void queryClient.invalidateQueries({ queryKey: ['recycle-bin-counts'] });
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
      toast.success('Đã xóa vĩnh viễn bản ghi khỏi cơ sở dữ liệu.');
      setHardDeleteCandidate(null);
      void queryClient.invalidateQueries({ queryKey: ['recycle-bin'] });
      void queryClient.invalidateQueries({ queryKey: ['recycle-bin-counts'] });
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

    const itemMap = new Map<string, EntityType>();
    data?.items?.forEach((item: any) => {
      itemMap.set(item.id, item._entityType || selectedEntity);
    });

    try {
      const results = await Promise.allSettled(
        selectedRowIds.map((id) => {
          const entity = itemMap.get(id) || (selectedEntity === 'all' ? 'clients' : selectedEntity);
          return getEntityApi(entity).restore(id);
        })
      );
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
      void queryClient.invalidateQueries({ queryKey: ['recycle-bin'] });
      void queryClient.invalidateQueries({ queryKey: ['recycle-bin-counts'] });
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

    const itemMap = new Map<string, EntityType>();
    data?.items?.forEach((item: any) => {
      itemMap.set(item.id, item._entityType || selectedEntity);
    });

    try {
      const results = await Promise.allSettled(
        selectedRowIds.map((id) => {
          const entity = itemMap.get(id) || (selectedEntity === 'all' ? 'clients' : selectedEntity);
          return getEntityApi(entity).hardDelete(id);
        })
      );
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
      void queryClient.invalidateQueries({ queryKey: ['recycle-bin'] });
      void queryClient.invalidateQueries({ queryKey: ['recycle-bin-counts'] });
      void queryClient.invalidateQueries({ queryKey: [selectedEntity] });
    } catch (err) {
      toast.error(extractErrorMessage(err));
    } finally {
      setIsBulkLoading(false);
    }
  };

  // Cấu hình cột hiển thị theo từng loại thực thể
  const getColumns = (): ColumnDef<any>[] => {
    switch (selectedEntity) {
      case 'all':
        return [
          {
            header: 'Phân loại',
            cell: (item: any) => {
              const entityConfig = ENTITY_CONFIGS.find((c) => c.id === item._entityType);
              const Icon = entityConfig?.icon || Layers;
              return (
                <Badge
                  variant="outline"
                  className="gap-1 text-[11px] font-medium py-0.5 px-2 bg-amber-50/50 dark:bg-amber-950/30 text-amber-800 dark:text-amber-300 border-amber-300 dark:border-amber-800"
                >
                  <Icon className="h-3 w-3" />
                  <span>{entityConfig?.label || 'Khác'}</span>
                </Badge>
              );
            },
            className: 'w-36',
            mobileLabel: 'Phân loại',
          },
          {
            header: 'Mã đối tượng',
            cell: (item: any) => (
              <span className="font-mono text-xs font-semibold">
                {item.plateNumber || item.code || item.username || '—'}
              </span>
            ),
            className: 'w-44',
            mobileLabel: 'Định danh',
          },
          {
            header: 'Tên đối tượng',
            cell: (item: any) => (
              <span className="text-xs font-semibold text-foreground">
                {item.name || item.fullName || item.plateNumber || item.username || '—'}
              </span>
            ),
            className: 'min-w-[180px]',
            mobileLabel: 'Tên',
          },
          {
            header: 'Thông tin bổ sung',
            cell: (item: any) => {
              const subInfo =
                item.email ||
                item.phoneNumber ||
                item.taxCode ||
                item.ipAddress ||
                item.description ||
                item.idCardNumber;
              return (
                <span className="text-xs text-muted-foreground truncate max-w-[200px] inline-block">
                  {subInfo || '—'}
                </span>
              );
            },
            className: 'w-48',
            mobileLabel: 'Thông tin',
          },
        ];

      case 'clients':
        return [
          {
            header: 'Mã khách hàng',
            accessorKey: 'code',
            cell: (item: any) => (
              <span className="font-mono text-xs font-semibold">{item.code || '—'}</span>
            ),
          },
          {
            header: 'Họ và tên',
            accessorKey: 'name',
            cell: (item: any) => (
              <span className="text-xs font-semibold text-foreground">{item.name || '—'}</span>
            ),
          },
          {
            header: 'Loại khách hàng',
            cell: (item: any) => {
              const types = ['Cán bộ NV', 'Nhà thầu', 'Khách vãng lai', 'VIP', 'Khác'];
              return (
                <Badge variant="outline" className="text-[11px]">
                  {types[item.type] || 'Khác'}
                </Badge>
              );
            },
          },
          {
            header: 'Số CCCD',
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

      case 'users':
        return [
          {
            header: 'Tên đăng nhập',
            accessorKey: 'username',
            cell: (item: any) => (
              <span className="font-semibold text-xs text-foreground">{item.username || '—'}</span>
            ),
          },
          {
            header: 'Họ và tên',
            accessorKey: 'fullName',
            cell: (item: any) => (
              <span className="text-xs text-foreground">{item.fullName || '—'}</span>
            ),
          },
          {
            header: 'Vai trò',
            cell: (item: any) => {
              const role =
                item.role === 1 || item.role === 'Admin'
                  ? 'Quản trị viên'
                  : item.role === 2 || item.role === 'Manager'
                    ? 'Quản lý'
                    : 'Người xem';
              return (
                <Badge variant="outline" className="text-[11px]">
                  {role}
                </Badge>
              );
            },
          },
          {
            header: 'Email / SĐT',
            cell: (item: any) => (
              <span className="text-xs text-muted-foreground">
                {item.email || item.phoneNumber || '—'}
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
    return item.name || item.fullName || item.username || item.plateNumber || item.code || item.id;
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
        {availableConfigs.map((config) => {
          const Icon = config.icon;
          const isActive = selectedEntity === config.id;
          const count = countsData?.[config.id];

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
                'group flex items-center gap-1.5 px-3 py-2 rounded-lg text-xs font-medium transition-all shrink-0 cursor-pointer',
                isActive
                  ? 'bg-amber-500/15 text-amber-700 dark:text-amber-300 font-semibold shadow-2xs border border-amber-500/30'
                  : 'text-muted-foreground hover:text-foreground hover:bg-muted/60'
              )}
            >
              <Icon className={cn('h-4 w-4', isActive ? 'text-amber-600' : 'text-muted-foreground')} />
              <span>{config.label}</span>
              {count !== undefined && (
                <span
                  className={cn(
                    'ml-0.5 px-1.5 py-0.5 text-[10px] font-bold rounded-full transition-colors min-w-[20px] text-center leading-none',
                    isActive
                      ? 'bg-amber-500/25 text-amber-900 dark:text-amber-100 border border-amber-500/30'
                      : 'bg-muted text-muted-foreground group-hover:bg-muted-foreground/20'
                  )}
                >
                  {count >= 100 ? '99+' : count}
                </span>
              )}
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
        searchPlaceholder={
          selectedEntity === 'all'
            ? 'Tìm kiếm trong tất cả bản ghi đã xóa...'
            : `Tìm kiếm ${activeConfig.label.toLowerCase()} trong thùng rác...`
        }
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
            setRestoreCandidate({
              id: item.id,
              name: getRecordDisplayName(item),
              entity: item._entityType || selectedEntity,
            });
          },
          onHardDelete: (item: any) => {
            setHardDeleteCandidate({
              id: item.id,
              name: getRecordDisplayName(item),
              entity: item._entityType || selectedEntity,
            });
          },
        }}
        emptyTitle={
          selectedEntity === 'all'
            ? 'Thùng rác trống'
            : `Thùng rác ${activeConfig.label.toLowerCase()} trống`
        }
        emptyDescription={
          selectedEntity === 'all'
            ? 'Không có bản ghi nào bị xóa trong toàn bộ hệ thống.'
            : `Không có bản ghi ${activeConfig.label.toLowerCase()} nào nằm trong thùng rác hệ thống.`
        }
      />

      {/* Confirm Khôi Phục Đơn Lẻ */}
      <ConfirmDialog
        open={Boolean(restoreCandidate)}
        onOpenChange={(open) => !open && setRestoreCandidate(null)}
        title="Khôi Phục Bản Ghi"
        description={`Bạn có chắc chắn muốn khôi phục bản ghi "${restoreCandidate?.name || 'này'}" về trạng thái hoạt động bình thường không?`}
        confirmText="Khôi Phục"
        cancelText="Hủy Bỏ"
        variant="default"
        isLoading={restoreMutation.isPending}
        icon={<RotateCcw className="h-5 w-5 text-blue-600" />}
        confirmIcon={<RotateCcw className="h-3.5 w-3.5" />}
        onConfirm={() => {
          if (restoreCandidate) {
            restoreMutation.mutate({
              id: restoreCandidate.id,
              entity: restoreCandidate.entity || (selectedEntity === 'all' ? 'clients' : selectedEntity),
            });
          }
        }}
      />

      {/* Confirm Xóa Cứng (Xóa Vĩnh Viễn) Đơn Lẻ */}
      <ConfirmDialog
        open={Boolean(hardDeleteCandidate)}
        onOpenChange={(open) => !open && setHardDeleteCandidate(null)}
        title="CẢNH BÁO: Xóa Vĩnh Viễn Bản Ghi (Xóa Cứng)"
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
                {hardDeleteCandidate?.entity === 'clients' && (
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
            hardDeleteMutation.mutate({
              id: hardDeleteCandidate.id,
              entity: hardDeleteCandidate.entity || (selectedEntity === 'all' ? 'clients' : selectedEntity),
            });
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
              khỏi cơ sở dữ liệu?
            </p>
            <div className="p-2.5 rounded-lg bg-destructive/10 border border-destructive/20 text-destructive leading-relaxed">
              <strong>Hành động này mang tính chất phá hủy và không thể hoàn tác:</strong>
              <ul className="list-disc pl-4 mt-1 space-y-0.5">
                <li>Toàn bộ dữ liệu của {selectedRowIds.length} bản ghi sẽ bị xóa vĩnh viễn.</li>
                <li>Lệnh thu hồi FaceID/đồng bộ thiết bị sẽ được kích hoạt tương ứng.</li>
                <li>Các bản ghi có quan hệ dữ liệu ràng buộc sẽ bị hệ thống từ chối xóa.</li>
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
