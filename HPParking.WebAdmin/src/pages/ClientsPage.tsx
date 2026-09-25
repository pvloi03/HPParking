import { useState, useMemo, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query';
import { toast } from 'sonner';
import {
  Users,
  Trash2,
  ScanFace,
  Loader2,
  CheckCircle2,
  AlertCircle,
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
import { ExcelImportDialog } from '@/components/common/ExcelImportDialog';
import { clientApi, extractErrorMessage } from '@/api/clientApi';
import { companiesApi, departmentsApi, contractorsApi } from '@/api/masterDataApi';
import { excelApi } from '@/api/excelApi';
import { downloadBlob } from '@/utils/downloadBlob';
import type {
  ClientDto,
  CreateClientRequest,
  UpdateClientRequest,
} from '@/types/client';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';

export function ClientsPage() {
  const queryClient = useQueryClient();

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
  const [deleteCandidate, setDeleteCandidate] = useState<ClientDto | null>(null);
  const [syncingClientId, setSyncingClientId] = useState<string | null>(null);
  const [isExcelImportOpen, setIsExcelImportOpen] = useState(false);
  const [isExportingExcel, setIsExportingExcel] = useState(false);

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

  // Mutation: Đồng bộ FaceID thủ công
  const syncFaceIdMutation = useMutation({
    mutationFn: (id: string) => clientApi.syncFaceId(id),
    onMutate: (id) => {
      setSyncingClientId(id);
    },
    onSuccess: (res) => {
      if (res.totalDevices === 0) {
        toast.info('Không có thiết bị FaceID nào đang hoạt động trên các làn xe.');
      } else if (res.failureCount > 0) {
        toast.warning(
          `Đồng bộ FaceID hoàn tất: ${res.successCount}/${res.totalDevices} thành công (${res.failureCount} thiết bị mất kết nối hoặc lỗi).`
        );
      } else {
        toast.success(`Đã hoàn tất đồng bộ FaceID lên toàn bộ ${res.totalDevices} thiết bị.`);
      }
      void queryClient.invalidateQueries({ queryKey: ['clients'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
    onSettled: () => {
      setSyncingClientId(null);
    },
  });

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
    } else {
      await createMutation.mutateAsync({
        payload: payload as CreateClientRequest,
        avatarFile,
      });
    }
  };

  // Chuẩn hóa đường dẫn Avatar để luôn tải được ảnh qua proxy
  const formatAvatarUrl = (url?: string) => {
    if (!url) return '';
    const clean = url.trim();
    if (clean.startsWith('http://') || clean.startsWith('https://') || clean.startsWith('blob:')) return clean;
    return clean.startsWith('/') ? clean : `/${clean}`;
  };

  // Định nghĩa các cột
  const columns: ColumnDef<ClientDto>[] = [
    {
      header: 'Khách hàng',
      cell: (item) => (
        <div className="flex items-center gap-2.5">
          <Avatar className="h-9 w-9 border border-border/80 shrink-0">
            <AvatarImage
              src={formatAvatarUrl(item.avatar)}
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
            <span className="font-semibold text-foreground text-xs block truncate">
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
      header: 'Số điện thoại',
      accessorKey: 'phoneNumber',
      cell: (item) => (
        <span className="font-mono text-xs text-foreground font-medium">
          {item.phoneNumber}
        </span>
      ),
      className: 'w-32',
      mobileLabel: 'SĐT',
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
      header: 'Sinh trắc FaceID',
      cell: (item) => (
        <div className="flex items-center gap-2">
          {item.avatar ? (
            <Badge className="bg-emerald-100 text-emerald-800 hover:bg-emerald-100 dark:bg-emerald-950 dark:text-emerald-300 gap-1 text-[11px] font-medium">
              <CheckCircle2 className="h-3 w-3 text-emerald-600" />
              <span>Đã có ảnh</span>
            </Badge>
          ) : (
            <Badge variant="outline" className="text-muted-foreground gap-1 text-[11px]">
              <AlertCircle className="h-3 w-3 text-muted-foreground" />
              <span>Chưa có ảnh</span>
            </Badge>
          )}

          <Button
            variant="ghost"
            size="sm"
            onClick={() => syncFaceIdMutation.mutate(item.id)}
            disabled={syncingClientId === item.id}
            className="h-7 w-7 p-0 text-blue-600 hover:text-blue-700 hover:bg-blue-50 dark:hover:bg-blue-950 cursor-pointer"
            title="Phát lệnh đồng bộ FaceID lên thiết bị làn xe"
          >
            {syncingClientId === item.id ? (
              <Loader2 className="h-3.5 w-3.5 animate-spin text-blue-600" />
            ) : (
              <ScanFace className="h-3.5 w-3.5" />
            )}
          </Button>
        </div>
      ),
      className: 'w-44',
      mobileLabel: 'FaceID',
    },
    {
      header: 'Trạng thái',
      accessorKey: 'isActive',
      cell: (item) => (
        <Badge
          variant={item.isActive ? 'default' : 'secondary'}
          className={`text-[11px] font-medium ${item.isActive
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
        selectable={true}
        selectedRowIds={selectedRowIds}
        onSelectedRowIdsChange={setSelectedRowIds}
        bulkActions={
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
        }
        onAddNew={() => {
          setSelectedClient(null);
          setIsFormOpen(true);
        }}
        addNewLabel="Thêm mới khách hàng"
        onImportExcel={() => setIsExcelImportOpen(true)}
        onExportExcel={handleExportExcel}
        isExportingExcel={isExportingExcel}
        actions={{
          onEdit: (item) => {
            setSelectedClient(item);
            setIsFormOpen(true);
          },
          onDelete: (item) => {
            setDeleteCandidate(item);
          },
        }}
        emptyTitle="Không có khách hàng nào"
        emptyDescription="Chưa có hồ sơ khách hàng hoặc không có bản ghi nào khớp với điều kiện tìm kiếm."
      />

      {/* Modal Form Thêm/Sửa Khách Hàng */}
      <ClientFormDialog
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        initialData={selectedClient}
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
    </div>
  );
}
