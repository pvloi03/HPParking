import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from '@/hooks/use-toast';
import { Receipt, Plus, Trash2, Edit } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { ActiveStatusBadge } from '@/components/common/ActiveStatusBadge';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { DataTable, type ColumnDef } from '@/components/common/DataTable';
import { ConfirmDialog } from '@/components/common/ConfirmDialog';
import { TariffFormDialog } from '@/components/tariffs/TariffFormDialog';
import { tariffApi, extractErrorMessage } from '@/api/tariffApi';
import { VehicleType, getVehicleTypeLabel } from '@/types/vehicle';
import {
  TariffPricingMode,
  getPricingModeLabel,
  type TariffDto,
  type CreateTariffRequest,
  type UpdateTariffRequest,
} from '@/types/tariff';
import { DEFAULT_PAGE_SIZE } from '@/types/masterData';
import { usePermissions } from '@/hooks/usePermissions';

export function TariffsPage() {
  const queryClient = useQueryClient();
  const { canWrite } = usePermissions();

  const [pageIndex, setPageIndex] = useState(1);
  const pageSize = DEFAULT_PAGE_SIZE;
  const [searchKeyword, setSearchKeyword] = useState('');
  const [vehicleTypeFilter, setVehicleTypeFilter] = useState<string>('all');
  const [pricingModeFilter, setPricingModeFilter] = useState<string>('all');
  const [statusFilter, setStatusFilter] = useState<boolean | 'all'>('all');

  const [isFormOpen, setIsFormOpen] = useState(false);
  const [selectedTariff, setSelectedTariff] = useState<TariffDto | null>(null);
  const [deleteCandidate, setDeleteCandidate] = useState<TariffDto | null>(null);

  const { data, isLoading } = useQuery({
    queryKey: [
      'tariffs',
      pageIndex,
      pageSize,
      searchKeyword,
      vehicleTypeFilter,
      pricingModeFilter,
      statusFilter,
    ],
    queryFn: () =>
      tariffApi.getPaged({
        pageIndex,
        pageSize,
        search: searchKeyword.trim() || undefined,
        vehicleType: vehicleTypeFilter === 'all' ? undefined : (Number(vehicleTypeFilter) as VehicleType),
        pricingMode: pricingModeFilter === 'all' ? undefined : (Number(pricingModeFilter) as TariffPricingMode),
        isActive: statusFilter === 'all' ? undefined : statusFilter,
      }),
  });

  const createMutation = useMutation({
    mutationFn: (payload: CreateTariffRequest) => tariffApi.create(payload),
    onSuccess: (newTariff) => {
      toast.success(`Đã thêm mới biểu phí "${newTariff.name}" thành công`);
      setIsFormOpen(false);
      void queryClient.invalidateQueries({ queryKey: ['tariffs'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateTariffRequest }) =>
      tariffApi.update(id, payload),
    onSuccess: (updated) => {
      toast.success(`Đã cập nhật biểu phí "${updated.name}" thành công`);
      setIsFormOpen(false);
      setSelectedTariff(null);
      void queryClient.invalidateQueries({ queryKey: ['tariffs'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => tariffApi.delete(id),
    onSuccess: () => {
      toast.success(`Đã xóa biểu phí "${deleteCandidate?.name}" thành công`);
      setDeleteCandidate(null);
      void queryClient.invalidateQueries({ queryKey: ['tariffs'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  const handleFormSubmit = async (payload: CreateTariffRequest | UpdateTariffRequest) => {
    if (selectedTariff) {
      await updateMutation.mutateAsync({
        id: selectedTariff.id,
        payload: payload as UpdateTariffRequest,
      });
    } else {
      await createMutation.mutateAsync(payload as CreateTariffRequest);
    }
  };

  const columns: ColumnDef<TariffDto>[] = [
    {
      header: 'Mã biểu phí',
      accessorKey: 'code',
      cell: (tariff) => (
        <span className="font-mono text-xs font-semibold px-2 py-0.5 rounded bg-muted text-foreground">
          {tariff.code}
        </span>
      ),
    },
    {
      header: 'Tên biểu phí',
      accessorKey: 'name',
      cell: (tariff) => (
        <div>
          <span className="font-medium text-xs text-foreground block">{tariff.name}</span>
          <span className="text-[11px] text-muted-foreground">
            Ân hạn: {tariff.gracePeriodMinutes > 0 ? `${tariff.gracePeriodMinutes} phút` : '0 phút'}
          </span>
        </div>
      ),
    },
    {
      header: 'Loại xe',
      accessorKey: 'vehicleType',
      cell: (tariff) => (
        <Badge variant="outline" className="text-xs">
          {getVehicleTypeLabel(tariff.vehicleType)}
        </Badge>
      ),
    },
    {
      header: 'Chế độ tính phí',
      accessorKey: 'pricingMode',
      cell: (tariff) => (
        <Badge
          variant={tariff.pricingMode === TariffPricingMode.Flat ? 'secondary' : 'default'}
          className="text-xs"
        >
          {getPricingModeLabel(tariff.pricingMode)}
        </Badge>
      ),
    },
    {
      header: 'Đơn giá & Quy tắc',
      accessorKey: 'flatPrice',
      cell: (tariff) => {
        if (tariff.pricingMode === TariffPricingMode.Flat) {
          return (
            <div className="font-mono text-xs font-semibold text-emerald-600 dark:text-emerald-400">
              {tariff.flatPrice.toLocaleString('vi-VN')} đ / lượt
            </div>
          );
        }
        return (
          <div className="space-y-0.5 text-xs">
            <div className="font-mono text-indigo-600 dark:text-indigo-400 font-medium">
              {tariff.baseBlockMinutes}p đầu: {tariff.basePrice.toLocaleString('vi-VN')} đ
            </div>
            <div className="font-mono text-muted-foreground text-[11px]">
              +{tariff.additionalBlockMinutes}p tiếp: {tariff.additionalBlockPrice.toLocaleString('vi-VN')} đ
            </div>
          </div>
        );
      },
    },
    {
      header: 'Phụ thu / Mức trần',
      accessorKey: 'overnightSurcharge',
      cell: (tariff) => (
        <div className="space-y-0.5 text-xs">
          <div className="text-[11px] text-muted-foreground">
            Qua đêm:{' '}
            <span className="font-mono font-medium text-foreground">
              {tariff.overnightSurcharge > 0 ? `${tariff.overnightSurcharge.toLocaleString('vi-VN')} đ` : '—'}
            </span>
          </div>
          <div className="text-[11px] text-muted-foreground">
            Trần 24h:{' '}
            <span className="font-mono font-medium text-foreground">
              {tariff.maxDailyCap > 0 ? `${tariff.maxDailyCap.toLocaleString('vi-VN')} đ` : 'Không giới hạn'}
            </span>
          </div>
        </div>
      ),
    },
    {
      header: 'Trạng thái',
      accessorKey: 'isActive',
      cell: (tariff) => <ActiveStatusBadge isActive={tariff.isActive} />,
    },
    {
      header: 'Thao tác',
      cell: (tariff) => (
        <div className="flex items-center gap-1">
          {canWrite && (
            <>
              <Button
                variant="ghost"
                size="sm"
                className="h-8 px-2 text-xs text-indigo-600 hover:text-indigo-700 hover:bg-indigo-50"
                onClick={() => {
                  setSelectedTariff(tariff);
                  setIsFormOpen(true);
                }}
                title="Chỉnh sửa biểu phí"
              >
                <Edit className="h-3.5 w-3.5 mr-1" />
                Sửa
              </Button>
              <Button
                variant="ghost"
                size="sm"
                className="h-8 px-2 text-xs text-destructive hover:bg-destructive/10"
                onClick={() => setDeleteCandidate(tariff)}
                title="Xóa biểu phí"
              >
                <Trash2 className="h-3.5 w-3.5 mr-1" />
                Xóa
              </Button>
            </>
          )}
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-4 p-4 lg:p-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-3 border-b">
        <div>
          <h1 className="text-xl font-bold tracking-tight text-foreground flex items-center gap-2">
            <Receipt className="h-6 w-6 text-indigo-600" />
            <span>Quản Lý Biểu Phí Đỗ Xe</span>
          </h1>
          <p className="text-xs text-muted-foreground mt-0.5">
            Cấu hình đơn giá lượt, block lũy tiến, phụ thu qua đêm và trần phí theo chu kỳ.
          </p>
        </div>
        {canWrite && (
          <Button
            size="sm"
            onClick={() => {
              setSelectedTariff(null);
              setIsFormOpen(true);
            }}
            className="text-xs h-9 bg-indigo-600 hover:bg-indigo-700 text-white cursor-pointer"
          >
            <Plus className="h-3.5 w-3.5 mr-1" />
            <span>Thêm biểu phí</span>
          </Button>
        )}
      </div>

      {/* Filter toolbar */}
      <div className="grid grid-cols-1 sm:grid-cols-4 gap-2.5">
        <Select
          value={vehicleTypeFilter}
          onValueChange={(val) => {
            setVehicleTypeFilter(val);
            setPageIndex(1);
          }}
        >
          <SelectTrigger className="text-xs h-9">
            <SelectValue placeholder="Tất cả loại xe" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all" className="text-xs">
              Tất cả loại xe
            </SelectItem>
            <SelectItem value={String(VehicleType.Car)} className="text-xs">
              {getVehicleTypeLabel(VehicleType.Car)}
            </SelectItem>
            <SelectItem value={String(VehicleType.Motorbike)} className="text-xs">
              {getVehicleTypeLabel(VehicleType.Motorbike)}
            </SelectItem>
            <SelectItem value={String(VehicleType.Bicycle)} className="text-xs">
              {getVehicleTypeLabel(VehicleType.Bicycle)}
            </SelectItem>
            <SelectItem value={String(VehicleType.Other)} className="text-xs">
              {getVehicleTypeLabel(VehicleType.Other)}
            </SelectItem>
          </SelectContent>
        </Select>

        <Select
          value={pricingModeFilter}
          onValueChange={(val) => {
            setPricingModeFilter(val);
            setPageIndex(1);
          }}
        >
          <SelectTrigger className="text-xs h-9">
            <SelectValue placeholder="Tất cả chế độ tính" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all" className="text-xs">
              Tất cả chế độ tính
            </SelectItem>
            <SelectItem value={String(TariffPricingMode.Flat)} className="text-xs">
              Cố định theo lượt (Flat)
            </SelectItem>
            <SelectItem value={String(TariffPricingMode.Block)} className="text-xs">
              Lũy tiến block (Block)
            </SelectItem>
          </SelectContent>
        </Select>

        <Select
          value={String(statusFilter)}
          onValueChange={(val) => {
            if (val === 'all') setStatusFilter('all');
            else setStatusFilter(val === 'true');
            setPageIndex(1);
          }}
        >
          <SelectTrigger className="text-xs h-9">
            <SelectValue placeholder="Tất cả trạng thái" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all" className="text-xs">
              Tất cả trạng thái
            </SelectItem>
            <SelectItem value="true" className="text-xs">
              Đang hoạt động
            </SelectItem>
            <SelectItem value="false" className="text-xs">
              Ngưng hoạt động
            </SelectItem>
          </SelectContent>
        </Select>
      </div>

      {/* Data Table */}
      <DataTable<TariffDto>
        columns={columns}
        data={data?.items || []}
        isLoading={isLoading}
        pagination={data?.pagination}
        onPageChange={setPageIndex}
        searchPlaceholder="Tìm theo tên hoặc mã biểu phí..."
        searchKeyword={searchKeyword}
        onSearchChange={(val) => {
          setSearchKeyword(val);
          setPageIndex(1);
        }}
      />

      {/* Form Dialog */}
      <TariffFormDialog
        open={isFormOpen}
        onOpenChange={(open) => {
          setIsFormOpen(open);
          if (!open) setSelectedTariff(null);
        }}
        initialData={selectedTariff}
        onSubmit={handleFormSubmit}
        isSubmitting={createMutation.isPending || updateMutation.isPending}
      />

      {/* Confirm Delete Dialog */}
      <ConfirmDialog
        open={Boolean(deleteCandidate)}
        onOpenChange={(open) => {
          if (!open) setDeleteCandidate(null);
        }}
        title="Xác nhận xóa biểu phí"
        description={`Bạn có chắc chắn muốn xóa biểu phí "${deleteCandidate?.name}" (${deleteCandidate?.code})? Hành động này có thể đưa vào thùng rác nếu chưa được gắn vào thẻ nào.`}
        confirmText="Xác nhận xóa"
        variant="destructive"
        isLoading={deleteMutation.isPending}
        onConfirm={async () => {
          if (deleteCandidate) {
            await deleteMutation.mutateAsync(deleteCandidate.id);
          }
        }}
      />
    </div>
  );
}
