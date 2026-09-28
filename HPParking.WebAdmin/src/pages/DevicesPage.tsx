import { useState, useEffect, useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from '@/hooks/use-toast';
import {
  Cpu,
  Trash2,
  Activity,
  RefreshCw,
  Loader2,
  Clock,
} from 'lucide-react';
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
import { DeviceFormDialog } from '@/components/infrastructure/DeviceFormDialog';
import { DeviceDetailDialog } from '@/components/infrastructure/DeviceDetailDialog';
import { ExcelImportDialog } from '@/components/common/ExcelImportDialog';
import { usePermissions } from '@/hooks/usePermissions';
import { devicesApi, extractErrorMessage } from '@/api/infrastructureApi';
import { excelApi } from '@/api/excelApi';
import { downloadBlob } from '@/utils/downloadBlob';
import {
  DeviceType,
  type DeviceDto,
  type CreateDeviceRequest,
  type UpdateDeviceRequest,
  type DevicePingResultDto,
} from '@/types/infrastructure';
import { useDevicePingStore, isPingRecordFresh } from '@/stores/devicePingStore';
import { getDeviceTypeBadge } from '@/components/infrastructure/deviceBadges';

export function DevicesPage() {
  const queryClient = useQueryClient();
  const { canWrite } = usePermissions();

  // State bộ lọc và phân trang
  const [pageIndex, setPageIndex] = useState(1);
  const pageSize = 15;
  const [searchKeyword, setSearchKeyword] = useState('');
  const [statusFilter, setStatusFilter] = useState<boolean | 'all'>('all');
  const [typeFilter, setTypeFilter] = useState<string>('all');

  // State Checkbox selection & Bulk Delete
  const [selectedRowIds, setSelectedRowIds] = useState<string[]>([]);
  const [isBulkDeleteOpen, setIsBulkDeleteOpen] = useState(false);
  const [isBulkLoading, setIsBulkLoading] = useState(false);

  // Tự động bỏ chọn checkbox khi đổi trang hoặc bộ lọc
  useEffect(() => {
    setSelectedRowIds([]);
  }, [pageIndex, searchKeyword, statusFilter, typeFilter]);

  // State Modal Form & Confirm Delete
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [selectedDevice, setSelectedDevice] = useState<DeviceDto | null>(null);
  const [deleteCandidate, setDeleteCandidate] = useState<DeviceDto | null>(null);
  const [detailDeviceId, setDetailDeviceId] = useState<string | null>(null);
  const [isExcelImportOpen, setIsExcelImportOpen] = useState(false);
  const [isExportingExcel, setIsExportingExcel] = useState(false);

  // Query: Lấy danh sách thiết bị
  const { data, isLoading } = useQuery({
    queryKey: [
      'devices',
      pageIndex,
      pageSize,
      searchKeyword,
      statusFilter,
      typeFilter,
    ],
    queryFn: () =>
      devicesApi.getPaged({
        pageIndex,
        pageSize,
        keyword: searchKeyword.trim() || undefined,
        isActive: statusFilter === 'all' ? undefined : statusFilter,
        type: typeFilter === 'all' ? undefined : (Number(typeFilter) as DeviceType),
      }),
  });

  // Zustand Ping Store & Actions
  const pingRecords = useDevicePingStore((s) => s.records);
  const pingingDeviceIds = useDevicePingStore((s) => s.pingingDeviceIds);
  const isAutoPingEnabled = useDevicePingStore((s) => s.isAutoPingEnabled);
  const isBatchPinging = useDevicePingStore((s) => s.isBatchPinging);
  const setPingResult = useDevicePingStore((s) => s.setPingResult);
  const setBatchPingResults = useDevicePingStore((s) => s.setBatchPingResults);
  const setPinging = useDevicePingStore((s) => s.setPinging);
  const setBatchPinging = useDevicePingStore((s) => s.setBatchPinging);
  const setAutoPingEnabled = useDevicePingStore((s) => s.setAutoPingEnabled);

  // Xử lý Ping đơn lẻ cho 1 thiết bị
  const handlePingSingle = useCallback(
    async (device: DeviceDto) => {
      if (!device.ipAddress?.trim()) {
        toast.error(`Thiết bị "${device.name}" chưa được cấu hình địa chỉ IP.`);
        return;
      }

      setPinging(device.id, true);

      try {
        const res = await devicesApi.pingDeviceIp(device.ipAddress);
        setPingResult(device.id, res);

        if (res.isAlive) {
          toast.success(
            `Thiết bị "${device.name}" (${res.ipAddress}): Online (${res.roundtripTimeMs}ms via ${res.method})`
          );
        } else {
          toast.error(`Thiết bị "${device.name}" (${res.ipAddress}): ${res.message}`);
        }
      } catch (err) {
        setPingResult(device.id, {
          ipAddress: device.ipAddress,
          isAlive: false,
          roundtripTimeMs: 2000,
          method: 'ERROR',
          message: extractErrorMessage(err),
          timestamp: new Date().toISOString(),
        });
        toast.error(extractErrorMessage(err));
      } finally {
        setPinging(device.id, false);
      }
    },
    [setPinging, setPingResult]
  );

  // Xử lý Ping hàng loạt (tất cả hoặc danh sách đã chọn qua checkbox)
  // isSilent: true khi chạy tự động ngầm theo chu kỳ 15s (không toast thông báo)
  const handlePingBatch = useCallback(
    async (targetDevices?: DeviceDto[], isSilent = false) => {
      const devicesToPing =
        targetDevices && targetDevices.length > 0
          ? targetDevices
          : data?.items || [];

      const validDevices = devicesToPing.filter((d) => Boolean(d.ipAddress?.trim()));
      if (validDevices.length === 0) {
        if (!isSilent) {
          toast.warning('Không có thiết bị nào có địa chỉ IP để kiểm tra kết nối.');
        }
        return;
      }

      setBatchPinging(true);
      validDevices.forEach((d) => setPinging(d.id, true));

      try {
        const distinctIps = Array.from(
          new Set(validDevices.map((d) => d.ipAddress.trim()))
        );
        const results = await devicesApi.pingBatchDeviceIps(distinctIps);

        const resultMap = new Map<string, DevicePingResultDto>();
        results.forEach((r) => {
          resultMap.set(r.ipAddress.toLowerCase(), r);
        });

        const storeUpdates: { deviceId: string; result: DevicePingResultDto }[] = [];
        validDevices.forEach((d) => {
          const ip = d.ipAddress.trim().toLowerCase();
          const hostOnly = ip.includes(':') ? ip.split(':')[0] : ip;
          const matched = resultMap.get(ip) || resultMap.get(hostOnly);
          if (matched) {
            storeUpdates.push({ deviceId: d.id, result: matched });
          } else {
            storeUpdates.push({
              deviceId: d.id,
              result: {
                ipAddress: d.ipAddress,
                isAlive: false,
                roundtripTimeMs: 2000,
                method: 'NONE',
                message: 'Không nhận được phản hồi từ thiết bị',
                timestamp: new Date().toISOString(),
              },
            });
          }
        });

        setBatchPingResults(storeUpdates);

        if (!isSilent) {
          const aliveCount = results.filter((r) => r.isAlive).length;
          const deadCount = results.length - aliveCount;
          if (deadCount === 0) {
            toast.success(
              `Kiểm tra hoàn tất: Toàn bộ ${aliveCount} thiết bị đang Online ổn định.`
            );
          } else {
            toast.warning(
              `Kiểm tra kết nối: ${aliveCount} thiết bị Online, ${deadCount} thiết bị Mất kết nối (Offline).`
            );
          }
        }
      } catch (err) {
        if (!isSilent) {
          toast.error(extractErrorMessage(err));
        }
      } finally {
        validDevices.forEach((d) => setPinging(d.id, false));
        setBatchPinging(false);
      }
    },
    [data?.items, setBatchPinging, setPinging, setBatchPingResults]
  );

  // Polling tự động chu kỳ 15s cho danh sách thiết bị trên trang hiện tại khi isAutoPingEnabled = true
  useEffect(() => {
    if (!isAutoPingEnabled || !data?.items?.length) return;

    const intervalId = window.setInterval(() => {
      if (!document.hidden && !isBatchPinging) {
        void handlePingBatch(data.items, true);
      }
    }, 15000);

    const handleVisibilityChange = () => {
      if (!document.hidden && isAutoPingEnabled && !isBatchPinging && data?.items?.length) {
        void handlePingBatch(data.items, true);
      }
    };

    document.addEventListener('visibilitychange', handleVisibilityChange);

    return () => {
      window.clearInterval(intervalId);
      document.removeEventListener('visibilitychange', handleVisibilityChange);
    };
  }, [isAutoPingEnabled, data?.items, isBatchPinging, handlePingBatch]);

  const renderPingCell = (device: DeviceDto) => {
    const isPinging = Boolean(pingingDeviceIds[device.id]);
    const storedRecord = pingRecords[device.id];
    const result = storedRecord?.result;
    const isFresh = isPingRecordFresh(storedRecord);

    if (isPinging) {
      return (
        <Badge
          variant="outline"
          className="gap-1.5 py-1 px-2 text-[11px] font-medium border-blue-300 bg-blue-50/70 text-blue-700 dark:border-blue-800 dark:bg-blue-950/50 dark:text-blue-300 animate-pulse"
        >
          <Loader2 className="h-3 w-3 animate-spin text-blue-600" />
          <span>Đang ping...</span>
        </Badge>
      );
    }

    if (!result) {
      return (
        <Button
          variant="outline"
          size="sm"
          onClick={() => void handlePingSingle(device)}
          disabled={isBatchPinging || !device.ipAddress}
          className="h-7 px-2.5 text-[11px] gap-1 text-slate-700 dark:text-slate-300 hover:text-blue-600 hover:border-blue-300 hover:bg-blue-50/50 dark:hover:bg-blue-950/30 cursor-pointer"
          title={`Kiểm tra kết nối tới ${device.ipAddress || 'thiết bị'}`}
        >
          <Activity className="h-3 w-3 text-blue-500" />
          <span>Ping</span>
        </Button>
      );
    }

    if (!isFresh) {
      return (
        <div
          className="flex items-center gap-1.5"
          title={`Kết quả đo kiểm đã quá 5 phút (${new Date(storedRecord.lastPingedAt).toLocaleTimeString('vi-VN')}) - Nhấn để đo lại`}
        >
          <Badge
            variant="outline"
            className="gap-1.5 py-1 px-2 text-[11px] font-medium border-dashed border-amber-300 bg-amber-50/50 text-amber-800 dark:border-amber-800 dark:bg-amber-950/30 dark:text-amber-300"
          >
            <Clock className="h-3 w-3 text-amber-600" />
            <span>Chờ kiểm tra</span>
          </Badge>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => void handlePingSingle(device)}
            disabled={isBatchPinging}
            className="h-6 w-6 p-0 text-muted-foreground hover:text-foreground cursor-pointer"
            title="Đo kiểm tra kết nối ngay"
          >
            <RefreshCw className="h-3 w-3" />
          </Button>
        </div>
      );
    }

    if (result.isAlive) {
      return (
        <div
          className="flex items-center gap-1.5"
          title={`${result.message} - Phương thức: ${result.method}`}
        >
          <Badge className="gap-1.5 py-1 px-2 text-[11px] font-medium bg-emerald-100 text-emerald-800 hover:bg-emerald-100 dark:bg-emerald-950/80 dark:text-emerald-300 border border-emerald-300/60 dark:border-emerald-800">
            <span className="relative flex h-2 w-2">
              <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75" />
              <span className="relative inline-flex rounded-full h-2 w-2 bg-emerald-500" />
            </span>
            <span>Online</span>
            <span className="text-[10px] opacity-75 font-mono">
              ({result.roundtripTimeMs}ms)
            </span>
          </Badge>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => void handlePingSingle(device)}
            disabled={isBatchPinging}
            className="h-6 w-6 p-0 text-muted-foreground hover:text-foreground cursor-pointer"
            title="Kiểm tra lại kết nối"
          >
            <RefreshCw className="h-3 w-3" />
          </Button>
        </div>
      );
    }

    return (
      <div
        className="flex items-center gap-1.5"
        title={`${result.message} (${result.method})`}
      >
        <Badge
          variant="destructive"
          className="gap-1.5 py-1 px-2 text-[11px] font-medium bg-rose-100 text-rose-800 hover:bg-rose-100 dark:bg-rose-950/80 dark:text-rose-300 border border-rose-300/60 dark:border-rose-800"
        >
          <span className="h-2 w-2 rounded-full bg-rose-500" />
          <span>Offline</span>
        </Badge>
        <Button
          variant="ghost"
          size="sm"
          onClick={() => void handlePingSingle(device)}
          disabled={isBatchPinging}
          className="h-6 w-6 p-0 text-muted-foreground hover:text-foreground cursor-pointer"
          title="Thử lại kết nối"
        >
          <RefreshCw className="h-3 w-3" />
        </Button>
      </div>
    );
  };

  const handleExportExcel = async () => {
    try {
      setIsExportingExcel(true);
      const blob = await excelApi.exportData('devices', {
        keyword: searchKeyword.trim() || undefined,
        isActive: statusFilter === 'all' ? undefined : statusFilter,
        deviceType: typeFilter === 'all' ? undefined : Number(typeFilter),
      });
      downloadBlob(blob, 'danh_sach_thiet_bi.xlsx');
      toast.success('Đã xuất dữ liệu Excel thành công');
    } catch (err) {
      toast.error(extractErrorMessage(err));
    } finally {
      setIsExportingExcel(false);
    }
  };

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

  // Xử lý thực thi xóa mềm hàng loạt qua checkbox (chuyển vào thùng rác)
  const handleExecuteBulkDelete = async () => {
    if (selectedRowIds.length === 0) return;
    setIsBulkLoading(true);
    try {
      const results = await Promise.allSettled(
        selectedRowIds.map((id) => devicesApi.delete(id, false))
      );
      const succeeded = results.filter((r) => r.status === 'fulfilled').length;
      const failed = results.length - succeeded;
      if (failed > 0) {
        toast.warning(
          `Đã chuyển ${succeeded}/${results.length} thiết bị vào thùng rác (${failed} bản ghi không thể xóa do đang gắn vào làn xe).`
        );
      } else {
        toast.success(`Đã chuyển thành công ${succeeded} thiết bị vào thùng rác.`);
      }
      setSelectedRowIds([]);
      setIsBulkDeleteOpen(false);
      void queryClient.invalidateQueries({ queryKey: ['devices'] });
      void queryClient.invalidateQueries({ queryKey: ['devices-all'] });
    } catch (err) {
      toast.error(extractErrorMessage(err));
    } finally {
      setIsBulkLoading(false);
    }
  };



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



  // Định nghĩa các cột
  const columns: ColumnDef<DeviceDto>[] = [
    {
      header: 'Mã thiết bị',
      accessorKey: 'code',
      cell: (item) => (
        <button
          type="button"
          onClick={() => setDetailDeviceId(item.id)}
          className="font-mono font-semibold text-blue-600 dark:text-blue-400 hover:underline cursor-pointer text-left"
          title={`Xem chi tiết thiết bị ${item.code}`}
        >
          {item.code}
        </button>
      ),
      className: 'font-semibold w-36',
      mobileLabel: 'Mã',
    },
    {
      header: 'Tên thiết bị ngoại vi',
      accessorKey: 'name',
      cell: (item) => (
        <button
          type="button"
          onClick={() => setDetailDeviceId(item.id)}
          className="font-semibold text-foreground hover:text-blue-600 dark:hover:text-blue-400 hover:underline cursor-pointer text-left block truncate max-w-xs"
          title={`Xem chi tiết thiết bị ${item.name}`}
        >
          {item.name}
        </button>
      ),
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
      header: 'Kết nối (Ping)',
      cell: (item) => renderPingCell(item),
      className: 'w-48',
      mobileLabel: 'Kết nối',
    },
    {
      header: 'Trạng thái',
      accessorKey: 'isActive',
      cell: (item) => <ActiveStatusBadge isActive={item.isActive} />,
      className: 'w-36',
      mobileLabel: 'Trạng thái',
    },
  ];

  return (
    <div className="space-y-6">
      {/* Header trang */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 border-b border-border pb-4">
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

        {/* Nút Ping Tất Cả */}
        <div className="flex items-center gap-2 self-start sm:self-auto">
          <Button
            variant="outline"
            size="sm"
            onClick={() => void handlePingBatch()}
            disabled={isBatchPinging || !data?.items?.length}
            className="h-9 gap-1.5 text-xs font-semibold cursor-pointer border-blue-200 dark:border-blue-900 text-blue-700 dark:text-blue-300 hover:bg-blue-50 dark:hover:bg-blue-950/60 shadow-xs"
            title="Kiểm tra kết nối song song toàn bộ thiết bị đang hiển thị trên trang"
          >
            {isBatchPinging ? (
              <Loader2 className="h-3.5 w-3.5 animate-spin text-blue-600" />
            ) : (
              <Activity className="h-3.5 w-3.5 text-blue-600" />
            )}
            <span>
              {isBatchPinging
                ? 'Đang kiểm tra...'
                : `Ping tất cả (${data?.items?.length || 0})`}
            </span>
          </Button>
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
        extraFilters={
          <div className="flex items-center gap-2 flex-wrap">
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

            {/* Công tắc Bật/Tắt Tự động ping 15s */}
            <Button
              type="button"
              variant={isAutoPingEnabled ? 'default' : 'outline'}
              size="sm"
              onClick={() => setAutoPingEnabled(!isAutoPingEnabled)}
              className={`h-9 px-2.5 text-xs gap-1.5 transition-all cursor-pointer ${isAutoPingEnabled
                  ? 'bg-emerald-600 hover:bg-emerald-700 text-white shadow-xs'
                  : 'text-muted-foreground hover:text-foreground'
                }`}
              title={
                isAutoPingEnabled
                  ? 'Đang bật tự động kiểm tra mỗi 15s. Nhấn để tạm dừng.'
                  : 'Đang tắt tự động kiểm tra 15s. Nhấn để bật.'
              }
            >
              <span className="relative flex h-2 w-2">
                {isAutoPingEnabled && (
                  <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-300 opacity-75" />
                )}
                <span
                  className={`relative inline-flex rounded-full h-2 w-2 ${isAutoPingEnabled ? 'bg-white' : 'bg-muted-foreground'
                    }`}
                />
              </span>
              <span>Tự động ping (15s)</span>
            </Button>
          </div>
        }
        selectable={canWrite}
        selectedRowIds={selectedRowIds}
        onSelectedRowIdsChange={setSelectedRowIds}
        bulkActions={
          canWrite ? (
            <div className="flex items-center gap-1.5 ml-1">
              <Button
                size="sm"
                variant="outline"
                disabled={isBatchPinging}
                onClick={() => {
                  const selected = (data?.items || []).filter((d) =>
                    selectedRowIds.includes(d.id)
                  );
                  void handlePingBatch(selected);
                }}
                className="h-7 px-2.5 text-xs text-blue-700 dark:text-blue-300 border-blue-300 dark:border-blue-800 hover:bg-blue-50 dark:hover:bg-blue-950/50 cursor-pointer"
              >
                {isBatchPinging ? (
                  <Loader2 className="h-3 w-3 mr-1 animate-spin text-blue-600" />
                ) : (
                  <Activity className="h-3 w-3 mr-1 text-blue-600" />
                )}
                <span>Ping đã chọn ({selectedRowIds.length})</span>
              </Button>

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
              setSelectedDevice(null);
              setIsFormOpen(true);
            }
            : undefined
        }
        addNewLabel="Thêm mới thiết bị"
        onImportExcel={canWrite ? () => setIsExcelImportOpen(true) : undefined}
        onExportExcel={handleExportExcel}
        isExportingExcel={isExportingExcel}
        actions={{
          onView: (item) => {
            setDetailDeviceId(item.id);
          },
          onEdit: canWrite
            ? (item) => {
              setSelectedDevice(item);
              setIsFormOpen(true);
            }
            : undefined,
          onDelete: canWrite
            ? (item) => {
              setDeleteCandidate(item);
            }
            : undefined,
        }}
        emptyTitle="Không có thiết bị nào"
        emptyDescription="Chưa có dữ liệu thiết bị hoặc không có bản ghi nào khớp với điều kiện tìm kiếm."
      />

      {/* Modal Form Thêm/Sửa Thiết Bị */}
      <DeviceFormDialog
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        initialData={selectedDevice}
        onSubmit={handleFormSubmit}
        isSubmitting={createMutation.isPending || updateMutation.isPending}
      />

      {/* Modal Chi Tiết Cấu Hình & Kết Nối Thiết Bị Ngoại Vi */}
      <DeviceDetailDialog
        open={Boolean(detailDeviceId)}
        onOpenChange={(open) => !open && setDetailDeviceId(null)}
        deviceId={detailDeviceId}
        onEdit={
          canWrite
            ? (device) => {
              setDetailDeviceId(null);
              setSelectedDevice(device);
              setIsFormOpen(true);
            }
            : undefined
        }
      />

      {/* Modal Nhập Dữ Liệu Excel */}
      <ExcelImportDialog
        open={isExcelImportOpen}
        onOpenChange={setIsExcelImportOpen}
        entity="devices"
        onSuccess={() => {
          void queryClient.invalidateQueries({ queryKey: ['devices'] });
        }}
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

      {/* Confirm Xóa Mềm Hàng Loạt Thiết Bị */}
      <ConfirmDialog
        open={isBulkDeleteOpen}
        onOpenChange={(open) => !open && setIsBulkDeleteOpen(false)}
        title="Xác Nhận Xóa Mềm Hàng Loạt"
        description={`Bạn có chắc chắn muốn chuyển ${selectedRowIds.length} thiết bị ngoại vi đã chọn vào thùng rác không? Toàn bộ các bản ghi bị xóa mềm có thể được xem và khôi phục tập trung tại màn hình Thùng Rác Hệ Thống.`}
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
