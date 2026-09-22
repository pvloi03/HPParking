import { useState } from 'react';
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

export function ClientsPage() {
  const queryClient = useQueryClient();

  // State bộ lọc và phân trang
  const [pageIndex, setPageIndex] = useState(1);
  const pageSize = 15;
  const [searchKeyword, setSearchKeyword] = useState('');
  const [statusFilter, setStatusFilter] = useState<boolean | 'all'>('all');
  const [companyFilter, setCompanyFilter] = useState<string>('all');
  const [departmentFilter, setDepartmentFilter] = useState<string>('all');
  const [isTrashMode, setIsTrashMode] = useState(false);

  // State Modals
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [selectedClient, setSelectedClient] = useState<ClientDto | null>(null);
  const [deleteCandidate, setDeleteCandidate] = useState<ClientDto | null>(null);
  const [syncingClientId, setSyncingClientId] = useState<string | null>(null);
  const [isExcelImportOpen, setIsExcelImportOpen] = useState(false);
  const [isExportingExcel, setIsExportingExcel] = useState(false);

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
      isTrashMode,
    ],
    queryFn: () =>
      clientApi.getPaged({
        pageIndex,
        pageSize,
        keyword: searchKeyword.trim() || undefined,
        isActive: statusFilter === 'all' ? undefined : statusFilter,
        companyId: companyFilter === 'all' ? undefined : companyFilter,
        departmentId: departmentFilter === 'all' ? undefined : departmentFilter,
        onlyDeleted: isTrashMode,
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
      toast.success(`Đã thêm mới khách hàng "${newClient.fullName}" thành công`);
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
      toast.success(`Đã cập nhật khách hàng "${updated.fullName}" thành công`);
      setIsFormOpen(false);
      setSelectedClient(null);
      void queryClient.invalidateQueries({ queryKey: ['clients'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Mutation: Xóa mềm khách hàng
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

  // Mutation: Khôi phục khách hàng
  const restoreMutation = useMutation({
    mutationFn: (id: string) => clientApi.restore(id),
    onSuccess: (restored) => {
      toast.success(`Đã khôi phục khách hàng "${restored.fullName}" thành công`);
      void queryClient.invalidateQueries({ queryKey: ['clients'] });
    },
    onError: (err) => {
      toast.error(extractErrorMessage(err));
    },
  });

  // Mutation: Đồng bộ FaceID thủ công
  const syncFaceIdMutation = useMutation({
    mutationFn: (id: string) => clientApi.syncFaceId(id),
    onMutate: (id) => {
      setSyncingClientId(id);
    },
    onSuccess: (res) => {
      toast.success(res.message || 'Đã phát lệnh đồng bộ FaceID lên thiết bị bãi xe thành công');
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

  // Định nghĩa các cột
  const columns: ColumnDef<ClientDto>[] = [
    {
      header: 'Khách hàng',
      cell: (item) => (
        <div className="flex items-center gap-2.5">
          <div className="h-9 w-9 rounded-full bg-muted border border-border overflow-hidden shrink-0 flex items-center justify-center">
            {item.avatarUrl ? (
              <img
                src={item.avatarUrl}
                alt={item.fullName}
                className="h-full w-full object-cover"
              />
            ) : (
              <span className="font-bold text-[11px] text-blue-600 dark:text-blue-400">
                {item.fullName
                  .trim()
                  .split(/\s+/)
                  .map((n) => n[0])
                  .slice(-2)
                  .join('')
                  .toUpperCase()}
              </span>
            )}
          </div>
          <div className="min-w-0">
            <span className="font-semibold text-foreground text-xs block truncate">
              {item.fullName}
            </span>
            <span className="font-mono text-[11px] text-muted-foreground block">
              {item.code}
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
      header: 'CCCD/Định danh',
      accessorKey: 'identityNumber',
      cell: (item) => (
        <span className="font-mono text-xs text-muted-foreground">
          {item.identityNumber}
        </span>
      ),
      className: 'w-36',
      mobileLabel: 'CCCD',
    },
    {
      header: 'Đơn vị trực thuộc',
      cell: (item) => (
        <div className="text-xs">
          {item.companyName && (
            <div className="font-medium text-foreground truncate">{item.companyName}</div>
          )}
          {item.departmentName && (
            <div className="text-[11px] text-muted-foreground truncate">
              {item.departmentName}
            </div>
          )}
          {item.contractorName && (
            <div className="text-[11px] text-amber-600 dark:text-amber-400 font-medium truncate">
              Nhà thầu: {item.contractorName}
            </div>
          )}
          {!item.companyName && !item.contractorName && (
            <span className="text-muted-foreground">—</span>
          )}
        </div>
      ),
      className: 'min-w-[180px]',
      mobileLabel: 'Đơn vị',
    },
    {
      header: 'Sinh trắc FaceID',
      cell: (item) => (
        <div className="flex items-center gap-2">
          {item.isFaceIdEnrolled ? (
            <Badge className="bg-emerald-100 text-emerald-800 hover:bg-emerald-100 dark:bg-emerald-950 dark:text-emerald-300 gap-1 text-[11px] font-medium">
              <CheckCircle2 className="h-3 w-3 text-emerald-600" />
              <span>Đã nạp</span>
            </Badge>
          ) : (
            <Badge variant="outline" className="text-muted-foreground gap-1 text-[11px]">
              <AlertCircle className="h-3 w-3 text-muted-foreground" />
              <span>Chưa nạp</span>
            </Badge>
          )}

          {!isTrashMode && (
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
          )}
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
        isTrashMode={isTrashMode}
        onTrashModeToggle={() => {
          setIsTrashMode(!isTrashMode);
          setPageIndex(1);
        }}
        extraFilters={
          !isTrashMode && (
            <div className="flex flex-wrap items-center gap-2">
              {/* Lọc theo công ty */}
              <div className="w-[180px]">
                <Select
                  value={companyFilter}
                  onValueChange={(val) => {
                    setCompanyFilter(val);
                    setDepartmentFilter('all');
                    setPageIndex(1);
                  }}
                >
                  <SelectTrigger className="h-9 text-xs">
                    <SelectValue placeholder="Lọc theo công ty" />
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
              <div className="w-[180px]">
                <Select
                  value={departmentFilter}
                  onValueChange={(val) => {
                    setDepartmentFilter(val);
                    setPageIndex(1);
                  }}
                >
                  <SelectTrigger className="h-9 text-xs">
                    <SelectValue placeholder="Lọc theo phòng ban" />
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
          )
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
          onRestore: (item) => {
            restoreMutation.mutate(item.id);
          },
        }}
        emptyTitle={isTrashMode ? 'Thùng rác trống' : 'Không có khách hàng nào'}
        emptyDescription={
          isTrashMode
            ? 'Hiện tại không có hồ sơ khách hàng nào nằm trong thùng rác.'
            : 'Chưa có hồ sơ khách hàng hoặc không có bản ghi nào khớp với điều kiện tìm kiếm.'
        }
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

      {/* Confirm Xóa Mềm Khách Hàng */}
      <ConfirmDialog
        open={Boolean(deleteCandidate)}
        onOpenChange={(open) => !open && setDeleteCandidate(null)}
        title="Xác Nhận Xóa Khách Hàng"
        description={`Bạn có chắc chắn muốn chuyển khách hàng "${deleteCandidate?.fullName}" vào thùng rác không? Lưu ý: Hệ thống sẽ từ chối xóa nếu khách hàng vẫn còn phương tiện xe đang liên kết hoặc đang gửi trong bãi đỗ.`}
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
