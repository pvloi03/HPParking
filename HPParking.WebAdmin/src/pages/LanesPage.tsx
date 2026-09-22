import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import {
  Route,
  Trash2,
  SlidersHorizontal,
  ArrowDownLeft,
  ArrowUpRight,
  ArrowLeftRight,
} from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { DataTable, type ColumnDef } from '@/components/common/DataTable';
import { ConfirmDialog } from '@/components/common/ConfirmDialog';
import { LaneFormDialog } from '@/components/infrastructure/LaneFormDialog';
import { LaneDetailDialog } from '@/components/infrastructure/LaneDetailDialog';
import { ExcelImportDialog } from '@/components/common/ExcelImportDialog';
import {
  lanesApi,
  gatesApi,
  devicesApi,
  extractErrorMessage,
} from '@/api/infrastructureApi';
import { excelApi } from '@/api/excelApi';
import { downloadBlob } from '@/utils/downloadBlob';
import {
  LaneDirection,
  type LaneDto,
  type CreateLaneRequest,
  type UpdateLaneRequest,
} from '@/types/infrastructure';

export function LanesPage() {
  const queryClient = useQueryClient();

  // State bộ lọc và phân trang
  const [pageIndex, setPageIndex] = useState(1);
  const pageSize = 15;
  const [searchKeyword, setSearchKeyword] = useState('');
  const [statusFilter, setStatusFilter] = useState<boolean | 'all'>('all');
  const [gateFilter, setGateFilter] = useState<string>('all');
  const [directionFilter, setDirectionFilter] = useState<string>('all');
  const [isTrashMode, setIsTrashMode] = useState(false);

  // State Modals
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [selectedLane, setSelectedLane] = useState<LaneDto | null>(null);
  const [deleteCandidate, setDeleteCandidate] = useState<LaneDto | null>(null);
  const [detailLaneId, setDetailLaneId] = useState<string | null>(null);
  const [isExcelImportOpen, setIsExcelImportOpen] = useState(false);
  const [isExportingExcel, setIsExportingExcel] = useState(false);

  const handleExportExcel = async () => {
    try {
      setIsExportingExcel(true);
      const blob = await excelApi.exportData('lanes', {
        keyword: searchKeyword.trim() || undefined,
        isActive: statusFilter === 'all' ? undefined : statusFilter,
        gateId: gateFilter === 'all' ? undefined : gateFilter,
        laneDirection: directionFilter === 'all' ? undefined : Number(directionFilter),
      });
      downloadBlob(blob, 'danh_sach_lan_xe.xlsx');
      toast.success('Đã xuất dữ liệu Excel thành công');
    } catch (err) {
      toast.error(extractErrorMessage(err));
    } finally {
      setIsExportingExcel(false);
    }
  };

  // Query: Lấy danh sách cổng kiểm soát
  const { data: gatesData } = useQuery({
    queryKey: ['gates-all'],
    queryFn: () => gatesApi.getPaged({ pageIndex: 1, pageSize: 100, isActive: true }),
  });
  const gates = gatesData?.items || [];

  // Query: Lấy danh sách thiết bị ngoại vi để gán vào làn
  const { data: devicesData } = useQuery({
    queryKey: ['devices-all'],
    queryFn: () => devicesApi.getPaged({ pageIndex: 1, pageSize: 100, isActive: true }),
  });
  const devices = devicesData?.items || [];

  // Query: Lấy danh sách làn xe
  const { data, isLoading } = useQuery({
    queryKey: [
      'lanes',
      pageIndex,
      pageSize,
      searchKeyword,
      statusFilter,
      gateFilter,
      directionFilter,
      isTrashMode,
    ],
    queryFn: () =>
      lanesApi.getPaged({
        pageIndex,
        pageSize,
        keyword: searchKeyword.trim() || undefined,
        isActive: statusFilter === 'all' ? undefined : statusFilter,
        gateId: gateFilter === 'all' ? undefined : gateFilter,
        direction:
          directionFilter === 'all' ? undefined : (Number(directionFilter) as LaneDirection),
        onlyDeleted: isTrashMode,
      }),
  });

  // Mutation: Thêm mới làn xe
  const createMutation = useMutation({
    mutationFn: (payload: CreateLaneRequest) => lanesApi.create(payload),
    onSuccess: (newLane) => {
      toast.success(`Đã thêm mới làn xe "${newLane.name}" thành công`);
      setIsFormOpen(false);
      void queryClient.invalidateQueries({ queryKey: ['lanes'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Mutation: Cập nhật làn xe
  const updateMutation = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateLaneRequest }) =>
      lanesApi.update(id, payload),
    onSuccess: (updated) => {
      toast.success(`Đã cập nhật cấu hình làn "${updated.name}" thành công`);
      setIsFormOpen(false);
      setSelectedLane(null);
      void queryClient.invalidateQueries({ queryKey: ['lanes'] });
      void queryClient.invalidateQueries({ queryKey: ['lane-detail', updated.id] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Mutation: Xóa mềm làn xe
  const deleteMutation = useMutation({
    mutationFn: (id: string) => lanesApi.delete(id, false),
    onSuccess: () => {
      toast.success('Đã chuyển làn xe vào thùng rác thành công');
      setDeleteCandidate(null);
      void queryClient.invalidateQueries({ queryKey: ['lanes'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Mutation: Khôi phục làn xe
  const restoreMutation = useMutation({
    mutationFn: (id: string) => lanesApi.restore(id),
    onSuccess: (restored) => {
      toast.success(`Đã khôi phục làn xe "${restored.name}" thành công`);
      void queryClient.invalidateQueries({ queryKey: ['lanes'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Xử lý submit form
  const handleFormSubmit = async (
    payload: CreateLaneRequest | UpdateLaneRequest
  ) => {
    if (selectedLane) {
      await updateMutation.mutateAsync({
        id: selectedLane.id,
        payload: payload as UpdateLaneRequest,
      });
    } else {
      await createMutation.mutateAsync(payload as CreateLaneRequest);
    }
  };

  // Định nghĩa các cột
  const columns: ColumnDef<LaneDto>[] = [
    {
      header: 'Mã làn',
      accessorKey: 'code',
      className: 'font-mono font-medium text-xs text-blue-600 dark:text-blue-400 w-32',
      mobileLabel: 'Mã',
    },
    {
      header: 'Tên làn xe',
      accessorKey: 'name',
      className: 'font-semibold min-w-[170px]',
      mobileLabel: 'Tên làn',
    },
    {
      header: 'Cổng trực thuộc',
      accessorKey: 'gateName',
      cell: (item) => item.gateName || <span className="text-muted-foreground">—</span>,
      className: 'min-w-[160px]',
      mobileLabel: 'Cổng',
    },
    {
      header: 'Hướng di chuyển',
      accessorKey: 'direction',
      cell: (item) => {
        if (item.direction === LaneDirection.In) {
          return (
            <Badge className="bg-emerald-100 text-emerald-800 hover:bg-emerald-100 dark:bg-emerald-950 dark:text-emerald-300 gap-1 text-[11px] font-medium">
              <ArrowDownLeft className="h-3 w-3" />
              <span>Làn Vào</span>
            </Badge>
          );
        }
        if (item.direction === LaneDirection.Out) {
          return (
            <Badge className="bg-amber-100 text-amber-800 hover:bg-amber-100 dark:bg-amber-950 dark:text-amber-300 gap-1 text-[11px] font-medium">
              <ArrowUpRight className="h-3 w-3" />
              <span>Làn Ra</span>
            </Badge>
          );
        }
        return (
          <Badge className="bg-blue-100 text-blue-800 hover:bg-blue-100 dark:bg-blue-950 dark:text-blue-300 gap-1 text-[11px] font-medium">
            <ArrowLeftRight className="h-3 w-3" />
            <span>Hai Chiều</span>
          </Badge>
        );
      },
      className: 'w-36',
      mobileLabel: 'Hướng',
    },
    {
      header: 'Cấu hình ngoại vi',
      cell: (item) => (
        <Button
          variant="outline"
          size="sm"
          onClick={() => setDetailLaneId(item.id)}
          className="h-7 text-xs gap-1.5 px-2.5 text-blue-600 hover:text-blue-700 hover:bg-blue-50 dark:hover:bg-blue-950/50 border-blue-200 dark:border-blue-900 cursor-pointer"
        >
          <SlidersHorizontal className="h-3.5 w-3.5 text-blue-500" />
          <span>Xem chi tiết</span>
        </Button>
      ),
      className: 'w-36',
      mobileLabel: 'Cấu hình',
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
              <Route className="h-5 w-5" />
            </div>
            <div>
              <h1 className="text-xl font-bold tracking-tight text-foreground">
                Quản Lý Làn Xe &amp; Cấu Hình Ngoại Vi
              </h1>
              <p className="text-xs text-muted-foreground">
                Định tuyến chiều xe, phân bổ cổng rơ-le barrier và gán 4 thiết bị ngoại vi theo chuẩn ADR 0034.
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
        searchPlaceholder="Tìm kiếm mã, tên làn xe..."
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
            <div className="flex flex-wrap items-center gap-2">
              {/* Lọc theo cổng */}
              <div className="w-[180px]">
                <Select
                  value={gateFilter}
                  onValueChange={(val) => {
                    setGateFilter(val);
                    setPageIndex(1);
                  }}
                >
                  <SelectTrigger className="h-9 text-xs">
                    <SelectValue placeholder="Lọc theo cổng" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all" className="text-xs">
                      Tất cả cổng
                    </SelectItem>
                    {gates.map((g) => (
                      <SelectItem key={g.id} value={g.id} className="text-xs">
                        {g.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              {/* Lọc theo hướng */}
              <div className="w-[160px]">
                <Select
                  value={directionFilter}
                  onValueChange={(val) => {
                    setDirectionFilter(val);
                    setPageIndex(1);
                  }}
                >
                  <SelectTrigger className="h-9 text-xs">
                    <SelectValue placeholder="Lọc theo hướng" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all" className="text-xs">
                      Tất cả hướng
                    </SelectItem>
                    <SelectItem value={String(LaneDirection.In)} className="text-xs">
                      Làn Vào (In)
                    </SelectItem>
                    <SelectItem value={String(LaneDirection.Out)} className="text-xs">
                      Làn Ra (Out)
                    </SelectItem>
                    <SelectItem value={String(LaneDirection.Bidirectional)} className="text-xs">
                      Hai Chiều
                    </SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
          )
        }
        onAddNew={() => {
          setSelectedLane(null);
          setIsFormOpen(true);
        }}
        addNewLabel="Thêm mới làn xe"
        onImportExcel={() => setIsExcelImportOpen(true)}
        onExportExcel={handleExportExcel}
        isExportingExcel={isExportingExcel}
        actions={{
          onEdit: (item) => {
            setSelectedLane(item);
            setIsFormOpen(true);
          },
          onDelete: (item) => {
            setDeleteCandidate(item);
          },
          onRestore: (item) => {
            restoreMutation.mutate(item.id);
          },
        }}
        emptyTitle={isTrashMode ? 'Thùng rác trống' : 'Không có làn xe nào'}
        emptyDescription={
          isTrashMode
            ? 'Hiện tại không có làn xe nào nằm trong thùng rác.'
            : 'Chưa có dữ liệu làn xe hoặc không có bản ghi nào khớp với điều kiện tìm kiếm.'
        }
      />

      {/* Modal Form Thêm/Sửa Làn xe */}
      <LaneFormDialog
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        initialData={selectedLane}
        gates={gates}
        devices={devices}
        onSubmit={handleFormSubmit}
        isSubmitting={createMutation.isPending || updateMutation.isPending}
      />

      {/* Modal Chi tiết Cấu hình Ngoại vi của Làn */}
      <LaneDetailDialog
        open={Boolean(detailLaneId)}
        onOpenChange={(open) => !open && setDetailLaneId(null)}
        laneId={detailLaneId}
      />

      {/* Modal Nhập Dữ Liệu Excel */}
      <ExcelImportDialog
        open={isExcelImportOpen}
        onOpenChange={setIsExcelImportOpen}
        entity="lanes"
        onSuccess={() => {
          void queryClient.invalidateQueries({ queryKey: ['lanes'] });
        }}
      />

      {/* Confirm Xóa Mềm Làn xe */}
      <ConfirmDialog
        open={Boolean(deleteCandidate)}
        onOpenChange={(open) => !open && setDeleteCandidate(null)}
        title="Xác Nhận Xóa Làn Xe"
        description={`Bạn có chắc chắn muốn chuyển làn xe "${deleteCandidate?.name}" vào thùng rác không? Lưu ý: Các phiên đỗ xe đang diễn ra tại làn này có thể bị ảnh hưởng.`}
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
