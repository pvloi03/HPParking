import { useState, useMemo, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query';
import { toast } from 'sonner';
import {
  Users,
  Trash2,
  Loader2,
  CreditCard,
  Phone,
  Mail,
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
import { ClientFormDialog } from '@/components/clients/ClientFormDialog';
import { ClientDetailDialog } from '@/components/clients/ClientDetailDialog';
import { ExcelImportDialog } from '@/components/common/ExcelImportDialog';
import { clientApi, extractErrorMessage } from '@/api/clientApi';
import { vehicleApi } from '@/api/vehicleApi';
import { companiesApi, departmentsApi, contractorsApi } from '@/api/masterDataApi';
import { excelApi } from '@/api/excelApi';
import { downloadBlob } from '@/utils/downloadBlob';
import { usePermissions } from '@/hooks/usePermissions';
import { formatAvatarUrl } from '@/utils/formatAvatarUrl';
import { cn } from '@/lib/utils';
import { hn212Service, type Hn212CardData, type Hn212ReaderStatus } from '@/services/hn212Service';
import type {
  ClientDto,
  CreateClientRequest,
  UpdateClientRequest,
} from '@/types/client';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';

export function ClientsPage() {
  const queryClient = useQueryClient();
  const { canWrite } = usePermissions();

  // State bộ lọc và phân trang
  const [pageIndex, setPageIndex] = useState(1);
  const pageSize = 15;
  const [searchKeyword, setSearchKeyword] = useState('');
  const [statusFilter, setStatusFilter] = useState<boolean | 'all'>('all');
  const [companyFilter, setCompanyFilter] = useState<string>('all');
  const [departmentFilter, setDepartmentFilter] = useState<string>('all');

  // State chọn hàng loạt qua checkbox
  const [selectedRowIds, setSelectedRowIds] = useState<string[]>([]);
  const [isBulkDeleteOpen, setIsBulkDeleteOpen] = useState(false);
  const [isBulkLoading, setIsBulkLoading] = useState(false);

  // State Modals
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [selectedClient, setSelectedClient] = useState<ClientDto | null>(null);
  const [detailClientId, setDetailClientId] = useState<string | null>(null);
  const [isDetailOpen, setIsDetailOpen] = useState(false);
  const [deleteCandidate, setDeleteCandidate] = useState<ClientDto | null>(null);
  const [isExcelImportOpen, setIsExcelImportOpen] = useState(false);
  const [isExportingExcel, setIsExportingExcel] = useState(false);

  // State đầu đọc CCCD HN212
  const [hn212CardData, setHn212CardData] = useState<Hn212CardData | null>(null);
  const [isReaderConnected, setIsReaderConnected] = useState<boolean>(false);
  const [isReadingCard, setIsReadingCard] = useState<boolean>(false);
  const [readingCardMessage, setReadingCardMessage] = useState<string>(
    'Vui lòng giữ nguyên thẻ trên thiết bị đầu đọc HN212.'
  );

  // Tự động tắt Backdrop đọc thẻ nếu quá 15 giây không có phản hồi
  useEffect(() => {
    if (isReadingCard) {
      const timer = setTimeout(() => {
        setIsReadingCard(false);
      }, 15000);
      return () => clearTimeout(timer);
    }
  }, [isReadingCard]);

  // Lắng nghe sự kiện cắm thẻ CCCD từ đầu đọc HN212
  useEffect(() => {
    hn212Service.connect();

    // Lắng nghe trạng thái đặt thẻ/đang đọc/rút thẻ/lỗi đọc từ đầu đọc
    const unsubscribeCardStatus = hn212Service.on(
      'CardStatusChanged',
      ({ status, message }: { status: string; message: string }) => {
        if (status === 'Present' || status === 'Reading') {
          setIsReadingCard(true);
          setReadingCardMessage(message || 'Đang đọc dữ liệu từ chip CCCD...');
        } else if (status === 'Absent') {
          setIsReadingCard(false);
        } else if (status === 'Error' || status === 'ReadError') {
          setIsReadingCard(false);
          toast.error(message || 'Đọc thẻ CCCD thất bại. Vui lòng kiểm tra lại vị trí đặt thẻ.');
        }
      }
    );

    // Lắng nghe khi có thẻ CCCD quét từ đầu đọc HN212 hoàn tất
    const unsubscribeCard = hn212Service.on('CardReadCompleted', async (cardData: Hn212CardData) => {
      setIsReadingCard(false);
      const docNum = (cardData.DocumentNumber || cardData.documentNumber || '').trim();
      const name = (cardData.FullName || cardData.fullName || '').trim();

      if (!docNum && !name) {
        return;
      }

      // Kiểm tra xem CCCD này đã tồn tại trong hệ thống hay chưa
      let existingClient: ClientDto | null = null;
      if (docNum) {
        try {
          const res = await clientApi.getPaged({ keyword: docNum, pageSize: 5 });
          existingClient = res.items.find((c) => c.code?.trim() === docNum) || null;
        } catch (err) {
          console.error('Lỗi kiểm tra CCCD tồn tại:', err);
        }
      }

      if (existingClient) {
        toast.success(
          `Đã tìm thấy hồ sơ: ${existingClient.name} (${docNum}). Mở màn hình cập nhật...`,
          { duration: 4000 }
        );
        setSelectedClient(existingClient);
        setHn212CardData(cardData);
        setIsFormOpen(true);
      } else {
        toast.info(
          `Đã phát hiện thẻ CCCD: ${name || 'Công dân'} (${docNum}). Mở màn hình thêm mới...`,
          { duration: 4000 }
        );
        setSelectedClient(null);
        setHn212CardData(cardData);
        setIsFormOpen(true);
      }
    });

    // Lắng nghe thay đổi trạng thái thiết bị
    const unsubscribeStatus = hn212Service.on('DeviceStatusChanged', (status: Hn212ReaderStatus) => {
      setIsReaderConnected(Boolean(status?.isReaderConnected));
    });

    const unsubscribeConn = hn212Service.on('connectionChanged', (connected: boolean) => {
      if (connected) {
        void hn212Service.getStatus().then((st) => {
          setIsReaderConnected(Boolean(st?.isReaderConnected));
        });
      }
    });

    // Lấy trạng thái thiết bị lúc khởi tạo
    void hn212Service.getStatus().then((st) => {
      if (st) {
        setIsReaderConnected(Boolean(st.isReaderConnected));
      }
    });

    return () => {
      unsubscribeCard();
      unsubscribeCardStatus();
      unsubscribeStatus();
      unsubscribeConn();
    };
  }, []);

  // Tự động bỏ chọn checkbox khi đổi trang hoặc đổi bộ lọc
  useEffect(() => {
    setSelectedRowIds([]);
  }, [pageIndex, searchKeyword, statusFilter, companyFilter, departmentFilter]);

  const handleExportExcel = async () => {
    try {
      setIsExportingExcel(true);
      const blob = await excelApi.exportData('clients', {
        keyword: searchKeyword.trim() || undefined,
        isActive: statusFilter === 'all' ? undefined : statusFilter,
        companyId: companyFilter === 'all' ? undefined : companyFilter,
        departmentId: departmentFilter === 'all' ? undefined : departmentFilter,
      });
      downloadBlob(blob, 'danh_sach_khach_hang.xlsx');
      toast.success('Đã xuất dữ liệu Excel thành công');
    } catch (err) {
      toast.error(extractErrorMessage(err));
    } finally {
      setIsExportingExcel(false);
    }
  };

  // Query: Lấy danh sách công ty, phòng ban, nhà thầu
  const { data: companiesData } = useQuery({
    queryKey: ['companies-all'],
    queryFn: () => companiesApi.getPaged({ pageIndex: 1, pageSize: 100, isActive: true }),
  });
  const companies = companiesData?.items || [];

  const { data: departmentsData } = useQuery({
    queryKey: ['departments-all'],
    queryFn: () => departmentsApi.getPaged({ pageIndex: 1, pageSize: 200, isActive: true }),
  });
  const departments = departmentsData?.items || [];

  const { data: contractorsData } = useQuery({
    queryKey: ['contractors-all'],
    queryFn: () => contractorsApi.getPaged({ pageIndex: 1, pageSize: 100, isActive: true }),
  });
  const contractors = contractorsData?.items || [];

  const companyMap = useMemo(() => {
    const map = new Map<string, string>();
    companies.forEach((c) => map.set(c.id, c.name));
    return map;
  }, [companies]);

  const departmentMap = useMemo(() => {
    const map = new Map<string, string>();
    departments.forEach((d) => map.set(d.id, d.name));
    return map;
  }, [departments]);

  const contractorMap = useMemo(() => {
    const map = new Map<string, string>();
    contractors.forEach((c) => map.set(c.id, c.name));
    return map;
  }, [contractors]);

  // Lọc phòng ban cho dropdown lọc
  const filterDepartmentsList =
    companyFilter && companyFilter !== 'all'
      ? departments.filter((d) => d.companyId === companyFilter)
      : departments;

  // TanStack Query v5: Lấy danh sách khách hàng với placeholderData: keepPreviousData
  const { data, isLoading } = useQuery({
    queryKey: [
      'clients',
      pageIndex,
      pageSize,
      searchKeyword,
      statusFilter,
      companyFilter,
      departmentFilter,
    ],
    queryFn: () =>
      clientApi.getPaged({
        pageIndex,
        pageSize,
        keyword: searchKeyword.trim() || undefined,
        isActive: statusFilter === 'all' ? undefined : statusFilter,
        companyId: companyFilter === 'all' ? undefined : companyFilter,
        departmentId: departmentFilter === 'all' ? undefined : departmentFilter,
      }),
    placeholderData: keepPreviousData,
  });

  // Mutation: Thêm mới khách hàng
  const createMutation = useMutation({
    mutationFn: async ({
      payload,
      avatarFile,
    }: {
      payload: CreateClientRequest;
      avatarFile: File | null;
    }) => {
      const newClient = await clientApi.create(payload);
      if (avatarFile) {
        try {
          await clientApi.uploadAvatar(newClient.id, avatarFile);
        } catch {
          toast.warning('Tạo khách hàng thành công nhưng tải ảnh đại diện thất bại.');
        }
      }
      return newClient;
    },
    onSuccess: (newClient) => {
      const terminals = newClient.faceIdTerminals ?? [];
      const total = terminals.length;
      const failure = terminals.filter((t) => !t.isOnline || !t.userExists).length;
      const success = total - failure;

      if (total > 0 && failure > 0) {
        toast.warning(
          `Thêm mới thành công. Đồng bộ FaceID hoàn tất: ${success}/${total} thành công (${failure} thiết bị mất kết nối hoặc lỗi).`
        );
      } else if (total > 0) {
        toast.success(
          `Đã thêm mới khách hàng "${newClient.name}" và đồng bộ FaceID (${total}/${total} thiết bị thành công).`
        );
      } else {
        toast.success(`Đã thêm mới khách hàng "${newClient.name}" thành công.`);
      }

      setIsFormOpen(false);
      void queryClient.invalidateQueries({ queryKey: ['clients'] });
      void queryClient.invalidateQueries({ queryKey: ['client-detail'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Mutation: Cập nhật khách hàng
  const updateMutation = useMutation({
    mutationFn: async ({
      id,
      payload,
      avatarFile,
    }: {
      id: string;
      payload: UpdateClientRequest;
      avatarFile: File | null;
    }) => {
      const updated = await clientApi.update(id, payload);
      if (avatarFile) {
        try {
          await clientApi.uploadAvatar(id, avatarFile);
        } catch {
          toast.warning('Cập nhật thông tin thành công nhưng tải ảnh đại diện thất bại.');
        }
      }
      return updated;
    },
    onSuccess: (updated) => {
      const terminals = updated.faceIdTerminals ?? [];
      const total = terminals.length;
      const failure = terminals.filter((t) => !t.isOnline || !t.userExists).length;
      const success = total - failure;

      if (total > 0 && failure > 0) {
        toast.warning(
          `Cập nhật thành công. Đồng bộ FaceID hoàn tất: ${success}/${total} thành công (${failure} thiết bị mất kết nối hoặc lỗi).`
        );
      } else if (total > 0) {
        toast.success(
          `Đã cập nhật khách hàng "${updated.name}" và đồng bộ FaceID (${total}/${total} thiết bị thành công).`
        );
      } else {
        toast.success(`Đã cập nhật khách hàng "${updated.name}" thành công.`);
      }

      setIsFormOpen(false);
      setSelectedClient(null);
      void queryClient.invalidateQueries({ queryKey: ['clients'] });
      void queryClient.invalidateQueries({ queryKey: ['client-detail'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Mutation: Xóa mềm khách hàng (Chuyển vào thùng rác)
  const deleteMutation = useMutation({
    mutationFn: (id: string) => clientApi.delete(id, false),
    onSuccess: () => {
      toast.success('Đã chuyển khách hàng vào thùng rác thành công');
      setDeleteCandidate(null);
      void queryClient.invalidateQueries({ queryKey: ['clients'] });
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
        selectedRowIds.map((id) => clientApi.delete(id, false))
      );
      const succeeded = results.filter((r) => r.status === 'fulfilled').length;
      const failed = results.length - succeeded;
      if (failed > 0) {
        toast.warning(
          `Đã chuyển ${succeeded}/${results.length} khách hàng vào thùng rác (${failed} bản ghi không thể xóa do ràng buộc dữ liệu xe/lượt đỗ).`
        );
      } else {
        toast.success(`Đã chuyển thành công ${succeeded} khách hàng vào thùng rác.`);
      }
      setSelectedRowIds([]);
      setIsBulkDeleteOpen(false);
      void queryClient.invalidateQueries({ queryKey: ['clients'] });
    } catch (err) {
      toast.error(extractErrorMessage(err));
    } finally {
      setIsBulkLoading(false);
    }
  };

  // Xử lý submit form
  const handleFormSubmit = async (
    payload: CreateClientRequest | UpdateClientRequest,
    avatarFile: File | null
  ) => {
    if (selectedClient) {
      await updateMutation.mutateAsync({
        id: selectedClient.id,
        payload: payload as UpdateClientRequest,
        avatarFile,
      });

      // Nếu người dùng có bổ sung phương tiện mới trong form cập nhật:
      const newVehicles = (payload as UpdateClientRequest).vehicles;
      if (newVehicles && newVehicles.length > 0) {
        for (const v of newVehicles) {
          try {
            await vehicleApi.create({
              clientId: selectedClient.id,
              plateNumber: v.plateNumber,
              type: v.type,
              isActive: v.isActive,
              note: v.note,
            });
          } catch (vehErr) {
            console.warn('Gán phương tiện bổ sung:', vehErr);
          }
        }
        void queryClient.invalidateQueries({ queryKey: ['vehicles'] });
      }
    } else {
      await createMutation.mutateAsync({
        payload: payload as CreateClientRequest,
        avatarFile,
      });
      if ((payload as CreateClientRequest).vehicles?.length) {
        void queryClient.invalidateQueries({ queryKey: ['vehicles'] });
      }
    }
  };

  // Định nghĩa các cột
  const columns: ColumnDef<ClientDto>[] = [
    {
      header: 'Khách hàng',
      cell: (item) => (
        <div
          className="flex items-center gap-2.5 cursor-pointer group"
          onClick={() => {
            setDetailClientId(item.id);
            setIsDetailOpen(true);
          }}
          title="Nhấn để xem hồ sơ chi tiết khách hàng"
        >
          <Avatar className="h-9 w-9 border border-border/80 shrink-0 group-hover:ring-2 group-hover:ring-blue-500 transition-all">
            <AvatarImage
              src={formatAvatarUrl(item.avatar, item.updatedAt || item.createdAt)}
              alt={item.name || 'Khách hàng'}
              className="object-cover"
            />
            <AvatarFallback className="font-bold text-[11px] text-blue-600 dark:text-blue-400 bg-muted">
              {(item.name || '')
                .trim()
                .split(/\s+/)
                .filter(Boolean)
                .map((n) => n[0])
                .slice(-2)
                .join('')
                .toUpperCase() || 'KH'}
            </AvatarFallback>
          </Avatar>
          <div className="min-w-0">
            <span className="font-semibold text-foreground text-xs block truncate group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors">
              {item.name || '—'}
            </span>
            <span className="font-mono text-[11px] text-muted-foreground block">
              {item.code || '—'}
            </span>
          </div>
        </div>
      ),
      className: 'min-w-[190px]',
      mobileLabel: 'Khách hàng',
    },
    {
      header: 'Liên Hệ',
      cell: (item) => (
        <div className="flex flex-col gap-1 text-xs">
          <div className="flex items-center gap-1.5 text-foreground">
            <Phone className="h-3.5 w-3.5 text-muted-foreground shrink-0" />
            <span className="font-mono text-xs">{item.phoneNumber || '—'}</span>
          </div>
          {item.email ? (
            <div className="flex items-center gap-1.5 text-muted-foreground">
              <Mail className="h-3.5 w-3.5 text-muted-foreground shrink-0" />
              <span className="truncate max-w-[200px] text-xs">{item.email}</span>
            </div>
          ) : null}
        </div>
      ),
      className: 'min-w-[180px]',
      mobileLabel: 'Liên Hệ',
    },
    {
      header: 'Đơn vị trực thuộc',
      cell: (item) => {
        const companyName = item.companyId ? companyMap.get(item.companyId) : null;
        const departmentName = item.departmentId ? departmentMap.get(item.departmentId) : null;
        const contractorName = item.contractorId ? contractorMap.get(item.contractorId) : null;

        return (
          <div className="text-xs">
            {companyName && (
              <div className="font-medium text-foreground truncate">{companyName}</div>
            )}
            {departmentName && (
              <div className="text-[11px] text-muted-foreground truncate">
                {departmentName}
              </div>
            )}
            {contractorName && (
              <div className="text-[11px] text-amber-600 dark:text-amber-400 font-medium truncate">
                Nhà thầu: {contractorName}
              </div>
            )}
            {!companyName && !contractorName && (
              <span className="text-muted-foreground">—</span>
            )}
          </div>
        );
      },
      className: 'min-w-[180px]',
      mobileLabel: 'Đơn vị',
    },
    {
      header: 'Trạng thái',
      accessorKey: 'isActive',
      cell: (item) => (
        <Badge
          variant={item.isActive ? 'success' : 'secondary'}
          className='text-[12px]'
        >
          {item.isActive ? 'Đang hoạt động' : 'Ngừng hoạt động'}
        </Badge>
      ),
      className: 'w-max',
      mobileLabel: 'Trạng thái',
    },
  ];

  return (
    <div className="space-y-6">
      {/* Header trang */}
      {/* Tiêu đề & thanh trạng thái đầu đọc */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2 border-b border-border pb-4">
        <div className="flex items-center justify-between w-full">
          <div className="flex items-center gap-2">
            <div className="p-2 rounded-xl bg-blue-50 dark:bg-blue-950 text-blue-600 dark:text-blue-400">
              <Users className="h-5 w-5" />
            </div>
            <div>
              <h1 className="text-xl font-bold tracking-tight text-foreground">
                Quản Lý Hồ Sơ Khách Hàng &amp; FaceID
              </h1>
              <p className="text-xs text-muted-foreground">
                Hồ sơ định danh khách hàng, số điện thoại Single Source of Truth, ảnh avatar và đồng bộ khuôn mặt sinh trắc học.
              </p>
            </div>
          </div>

          {/* Trạng thái đầu đọc thẻ CCCD HN212 */}
          <div className="hidden sm:flex items-center gap-2 px-3 py-1.5 rounded-xl border border-border/80 bg-card/80 shadow-2xs">
            <CreditCard className="h-4 w-4 text-blue-600 dark:text-blue-400" />
            <div className="flex flex-col">
              <span className="text-[10px] text-muted-foreground font-medium uppercase tracking-wider">
                Đầu đọc CCCD HN212
              </span>
              <div className="flex items-center gap-1.5">
                <span
                  className={cn(
                    'h-2 w-2 rounded-full',
                    isReaderConnected ? 'bg-emerald-500 animate-pulse' : 'bg-neutral-400'
                  )}
                />
                <span
                  className={cn(
                    'text-xs font-semibold',
                    isReaderConnected
                      ? 'text-emerald-600 dark:text-emerald-400'
                      : 'text-muted-foreground'
                  )}
                >
                  {isReaderConnected ? 'Sẵn sàng quẹt thẻ' : 'Chưa kết nối thiết bị'}
                </span>
              </div>
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
        searchPlaceholder="Tìm kiếm mã, tên, SĐT, CCCD..."
        statusFilter={statusFilter}
        onStatusFilterChange={(st) => {
          setStatusFilter(st);
          setPageIndex(1);
        }}
        extraFilters={
          <div className="flex items-center gap-2 flex-wrap">
            {/* Lọc theo công ty */}
            <div className="w-[170px]">
              <Select
                value={companyFilter}
                onValueChange={(val) => {
                  setCompanyFilter(val);
                  setDepartmentFilter('all');
                  setPageIndex(1);
                }}
              >
                <SelectTrigger className="h-9 text-xs">
                  <SelectValue placeholder="Tất cả công ty" />
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

            {/* Lọc theo phòng ban */}
            <div className="w-[170px]">
              <Select
                value={departmentFilter}
                onValueChange={(val) => {
                  setDepartmentFilter(val);
                  setPageIndex(1);
                }}
              >
                <SelectTrigger className="h-9 text-xs">
                  <SelectValue placeholder="Tất cả phòng ban" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all" className="text-xs">
                    Tất cả phòng ban
                  </SelectItem>
                  {filterDepartmentsList.map((d) => (
                    <SelectItem key={d.id} value={d.id} className="text-xs">
                      {d.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
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
                setSelectedClient(null);
                setHn212CardData(null);
                setIsFormOpen(true);
              }
            : undefined
        }
        addNewLabel="Thêm mới khách hàng"
        onImportExcel={canWrite ? () => setIsExcelImportOpen(true) : undefined}
        onExportExcel={handleExportExcel}
        isExportingExcel={isExportingExcel}
        actions={{
          onView: (item) => {
            setDetailClientId(item.id);
            setIsDetailOpen(true);
          },
          onEdit: canWrite
            ? (item) => {
                setSelectedClient(item);
                setHn212CardData(null);
                setIsFormOpen(true);
              }
            : undefined,
          onDelete: canWrite
            ? (item) => {
                setDeleteCandidate(item);
              }
            : undefined,
        }}
        emptyTitle="Không có khách hàng nào"
        emptyDescription="Chưa có hồ sơ khách hàng hoặc không có bản ghi nào khớp với điều kiện tìm kiếm."
      />

      {/* Modal Xem Chi Tiết Khách Hàng */}
      <ClientDetailDialog
        open={isDetailOpen}
        onOpenChange={(open) => {
          setIsDetailOpen(open);
          if (!open) setDetailClientId(null);
        }}
        clientId={detailClientId}
        companies={companies}
        departments={departments}
        contractors={contractors}
        onEdit={(client) => {
          setSelectedClient(client);
          setHn212CardData(null);
          setIsFormOpen(true);
        }}
      />

      {/* Modal Form Thêm/Sửa Khách Hàng */}
      <ClientFormDialog
        open={isFormOpen}
        onOpenChange={(open) => {
          setIsFormOpen(open);
          if (!open) {
            setHn212CardData(null);
            setSelectedClient(null);
          }
        }}
        initialData={selectedClient}
        hn212CardData={hn212CardData}
        companies={companies}
        departments={departments}
        contractors={contractors}
        onSubmit={handleFormSubmit}
        isSubmitting={createMutation.isPending || updateMutation.isPending}
      />

      {/* Modal Nhập Dữ Liệu Excel */}
      <ExcelImportDialog
        open={isExcelImportOpen}
        onOpenChange={setIsExcelImportOpen}
        entity="clients"
        onSuccess={() => {
          void queryClient.invalidateQueries({ queryKey: ['clients'] });
        }}
      />

      {/* Confirm Xóa Mềm Đơn Lẻ Khách Hàng */}
      <ConfirmDialog
        open={Boolean(deleteCandidate)}
        onOpenChange={(open) => !open && setDeleteCandidate(null)}
        title="Xác Nhận Xóa Khách Hàng (Xóa Mềm)"
        description={`Bạn có chắc chắn muốn chuyển khách hàng "${deleteCandidate?.name || 'này'}" vào thùng rác không? Dữ liệu này có thể được khôi phục sau tại mục Thùng Rác Hệ Thống. Lưu ý: Hệ thống sẽ từ chối xóa nếu khách hàng vẫn còn phương tiện xe đang liên kết hoặc đang gửi trong bãi đỗ.`}
        confirmText="Chuyển Vào Thùng Rác"
        cancelText="Hủy Bỏ"
        variant="destructive"
        isLoading={deleteMutation.isPending}
        icon={<Trash2 className="h-5 w-5 text-amber-600" />}
        confirmIcon={<Trash2 className="h-3.5 w-3.5" />}
        onConfirm={() => {
          if (deleteCandidate) {
            deleteMutation.mutate(deleteCandidate.id);
          }
        }}
      />

      {/* Confirm Xóa Mềm Hàng Loạt Khách Hàng */}
      <ConfirmDialog
        open={isBulkDeleteOpen}
        onOpenChange={(open) => !open && setIsBulkDeleteOpen(false)}
        title="Xác Nhận Xóa Mềm Hàng Loạt"
        description={`Bạn có chắc chắn muốn chuyển ${selectedRowIds.length} khách hàng đã chọn vào thùng rác không? Toàn bộ các bản ghi bị xóa mềm có thể được xem và khôi phục tập trung tại màn hình Thùng Rác Hệ Thống.`}
        confirmText={`Chuyển Vào Thùng Rác (${selectedRowIds.length})`}
        cancelText="Hủy Bỏ"
        variant="destructive"
        isLoading={isBulkLoading}
        icon={<Trash2 className="h-5 w-5 text-amber-600" />}
        confirmIcon={<Trash2 className="h-3.5 w-3.5" />}
        onConfirm={handleExecuteBulkDelete}
      />

      {/* Backdrop & Vòng xoay khi cắm thẻ CCCD đang đọc dữ liệu */}
      {isReadingCard && (
        <div className="fixed inset-0 z-[100] bg-black/60 backdrop-blur-xs flex items-center justify-center p-4 animate-in fade-in duration-200">
          <div className="bg-card border border-border/80 rounded-2xl p-6 sm:p-8 max-w-sm w-full shadow-2xl flex flex-col items-center text-center space-y-4">
            <div className="relative flex items-center justify-center">
              {/* Vòng tròn hiệu ứng quay xung quanh */}
              <div className="absolute h-20 w-20 rounded-full border-4 border-blue-500/20 border-t-blue-500 animate-spin" />
              <div className="h-14 w-14 rounded-full bg-blue-50 dark:bg-blue-950/60 border border-blue-200 dark:border-blue-800 flex items-center justify-center text-blue-600 dark:text-blue-400 shadow-inner">
                <CreditCard className="h-7 w-7 animate-pulse" />
              </div>
            </div>

            <div className="space-y-1.5">
              <h3 className="text-base font-bold text-foreground">
                Đang đọc dữ liệu từ thẻ CCCD...
              </h3>
              <p className="text-xs text-muted-foreground leading-relaxed">
                {readingCardMessage || 'Vui lòng giữ nguyên thẻ trên thiết bị đầu đọc HN212.'}
              </p>
            </div>

            <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-muted/80 border border-border text-[11px] text-muted-foreground font-medium">
              <Loader2 className="h-3.5 w-3.5 animate-spin text-blue-500" />
              <span>Đầu đọc HN212 đang xử lý</span>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
