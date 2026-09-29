import { useState, useMemo } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { toast } from '@/hooks/use-toast';
import {
  CreditCard,
  Plus,
  Car,
  User,
  Lock,
  AlertCircle,
  Save,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/components/ui/dialog';
import { DataTable, type ColumnDef } from '@/components/common/DataTable';
import { ConfirmDialog } from '@/components/common/ConfirmDialog';
import { cardApi } from '@/api/cardApi';
import {
  CardTargetType,
  CardStatus,
  type CardDto,
  type CreateCardRequest,
  normalizeCardCode,
} from '@/types/card';
import { usePermissions } from '@/hooks/usePermissions';

const createCardSchema = z.object({
  cardNumber: z
    .string()
    .trim()
    .min(1, 'Vui lòng nhập mã thẻ')
    .max(10, 'Mã thẻ tối đa 10 chữ số')
    .regex(/^\d+$/, 'Mã thẻ chỉ được chứa các chữ số 0-9'),
  targetType: z.enum([CardTargetType.Person, CardTargetType.Vehicle]),
  status: z.enum([
    CardStatus.Available,
    CardStatus.InUse,
    CardStatus.Locked,
    CardStatus.Lost,
  ]),
  note: z.string().trim().optional(),
});

type CreateCardFormData = z.infer<typeof createCardSchema>;

export function CardsPage() {
  const queryClient = useQueryClient();
  const { canWrite } = usePermissions();

  const [pageIndex, setPageIndex] = useState(1);
  const pageSize = 15;
  const [searchKeyword, setSearchKeyword] = useState('');
  const [targetTypeFilter, setTargetTypeFilter] = useState<string>('all');
  const [statusFilter, setStatusFilter] = useState<string>('all');

  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [deleteCandidate, setDeleteCandidate] = useState<CardDto | null>(null);

  const { data: pagedData, isLoading } = useQuery({
    queryKey: ['cards', pageIndex, pageSize, searchKeyword, targetTypeFilter, statusFilter],
    queryFn: () =>
      cardApi.getCards({
        pageIndex,
        pageSize,
        search: searchKeyword.trim() || undefined,
        targetType: targetTypeFilter !== 'all' ? (targetTypeFilter as CardTargetType) : undefined,
        status: statusFilter !== 'all' ? (statusFilter as CardStatus) : undefined,
      }),
  });

  const createMutation = useMutation({
    mutationFn: (data: CreateCardRequest) => cardApi.create(data),
    onSuccess: () => {
      toast.success('Đã khởi tạo thẻ định danh mới thành công');
      setIsCreateOpen(false);
      resetForm();
      queryClient.invalidateQueries({ queryKey: ['cards'] });
    },
    onError: (err: any) => {
      toast.error(err.response?.data?.message || err.message || 'Lỗi khi tạo thẻ mới');
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => cardApi.delete(id),
    onSuccess: () => {
      toast.success('Đã xóa thẻ định danh khỏi hệ thống');
      setDeleteCandidate(null);
      queryClient.invalidateQueries({ queryKey: ['cards'] });
    },
    onError: (err: any) => {
      toast.error(err.response?.data?.message || err.message || 'Lỗi khi xóa thẻ');
    },
  });

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    reset: resetForm,
    formState: { errors, isSubmitting },
  } = useForm<CreateCardFormData>({
    resolver: zodResolver(createCardSchema),
    defaultValues: {
      cardNumber: '',
      targetType: CardTargetType.Person,
      status: CardStatus.Available,
      note: '',
    },
  });

  const watchedCardNumber = watch('cardNumber');
  const watchedTargetType = watch('targetType');
  const watchedStatus = watch('status');

  const handleCreateSubmit = async (data: CreateCardFormData) => {
    const padded = normalizeCardCode(data.cardNumber);
    const payload: CreateCardRequest = {
      cardNumber: padded,
      targetType: data.targetType,
      status: data.status,
      note: data.note || undefined,
    };
    await createMutation.mutateAsync(payload);
  };

  const columns = useMemo<ColumnDef<CardDto>[]>(
    () => [
      {
        accessorKey: 'cardNumber',
        header: 'Mã Thẻ (10 số)',
        cell: (item: CardDto) => (
          <div className="flex items-center gap-2 font-mono font-bold text-xs text-foreground">
            <CreditCard className="h-4 w-4 text-indigo-500 shrink-0" />
            <span>{item.cardNumber}</span>
          </div>
        ),
      },
      {
        accessorKey: 'targetType',
        header: 'Đối Tượng Gán',
        cell: (item: CardDto) => {
          const isVehicle = item.targetType === CardTargetType.Vehicle;
          return isVehicle ? (
            <Badge variant="outline" className="gap-1 border-blue-300 text-blue-700 dark:text-blue-300 bg-blue-50/50 dark:bg-blue-950/30 text-[11px]">
              <Car className="h-3 w-3" />
              <span>Xe công vụ</span>
            </Badge>
          ) : (
            <Badge variant="outline" className="gap-1 border-purple-300 text-purple-700 dark:text-purple-300 bg-purple-50/50 dark:bg-purple-950/30 text-[11px]">
              <User className="h-3 w-3" />
              <span>Nhân sự (Client)</span>
            </Badge>
          );
        },
      },
      {
        header: 'Thông Tin Gán',
        cell: (item: CardDto) => {
          if (item.targetType === CardTargetType.Vehicle) {
            return (
              <span className="font-semibold text-xs text-foreground">
                {item.plateNumber || '—'}
              </span>
            );
          }
          return (
            <span className="font-semibold text-xs text-foreground">
              {item.clientName || '—'}
            </span>
          );
        },
      },
      {
        accessorKey: 'status',
        header: 'Trạng Thái',
        cell: (item: CardDto) => {
          const s = item.status;
          if (s === CardStatus.InUse) {
            return <Badge className="bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300 text-[11px]">Đang sử dụng</Badge>;
          }
          if (s === CardStatus.Available) {
            return <Badge variant="secondary" className="text-[11px]">Trong kho</Badge>;
          }
          if (s === CardStatus.Locked) {
            return <Badge variant="destructive" className="gap-1 text-[11px]"><Lock className="h-3 w-3" /> Tạm khóa</Badge>;
          }
          return <Badge variant="destructive" className="gap-1 text-[11px]"><AlertCircle className="h-3 w-3" /> Báo mất</Badge>;
        },
      },
      {
        accessorKey: 'note',
        header: 'Ghi Chú',
        cell: (item: CardDto) => <span className="text-muted-foreground text-xs">{item.note || '—'}</span>,
      },
    ],
    []
  );

  return (
    <div className="space-y-4 p-4 lg:p-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-3 border-b">
        <div>
          <h1 className="text-xl font-bold tracking-tight text-foreground flex items-center gap-2">
            <CreditCard className="h-6 w-6 text-indigo-600" />
            <span>Quản Lý Thẻ Định Danh (RFID / Wiegand)</span>
          </h1>
          <p className="text-xs text-muted-foreground mt-0.5">
            Mã thẻ luôn được chuẩn hóa 10 chữ số, dùng chung cho quẹt thẻ RFID và nạp FaceID.
          </p>
        </div>

        {canWrite && (
          <Button
            size="sm"
            onClick={() => setIsCreateOpen(true)}
            className="gap-1.5 bg-indigo-600 hover:bg-indigo-700 text-white cursor-pointer self-start sm:self-auto text-xs h-9"
          >
            <Plus className="h-4 w-4" />
            <span>Thêm Thẻ Mới</span>
          </Button>
        )}
      </div>

      {/* Bảng dữ liệu */}
      <DataTable<CardDto>
        data={pagedData?.items || []}
        columns={columns}
        pagination={pagedData?.pagination}
        onPageChange={setPageIndex}
        isLoading={isLoading}
        searchKeyword={searchKeyword}
        onSearchChange={setSearchKeyword}
        searchPlaceholder="Tìm kiếm theo mã thẻ 10 số..."
        onAddNew={canWrite ? () => setIsCreateOpen(true) : undefined}
        addNewLabel="Thêm thẻ mới"
        actions={{
          onDelete: canWrite ? (item) => setDeleteCandidate(item) : undefined,
          canDelete: () => canWrite,
        }}
        extraFilters={
          <div className="flex flex-wrap items-center gap-2">
            <Select value={targetTypeFilter} onValueChange={(val) => { setTargetTypeFilter(val); setPageIndex(1); }}>
              <SelectTrigger className="w-[150px] text-xs h-9">
                <SelectValue placeholder="Loại đối tượng" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all" className="text-xs">Tất cả đối tượng</SelectItem>
                <SelectItem value={CardTargetType.Person} className="text-xs">Thẻ nhân sự</SelectItem>
                <SelectItem value={CardTargetType.Vehicle} className="text-xs">Thẻ xe công vụ</SelectItem>
              </SelectContent>
            </Select>

            <Select value={statusFilter} onValueChange={(val) => { setStatusFilter(val); setPageIndex(1); }}>
              <SelectTrigger className="w-[150px] text-xs h-9">
                <SelectValue placeholder="Trạng thái" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all" className="text-xs">Tất cả trạng thái</SelectItem>
                <SelectItem value={CardStatus.InUse} className="text-xs">Đang sử dụng</SelectItem>
                <SelectItem value={CardStatus.Available} className="text-xs">Trong kho</SelectItem>
                <SelectItem value={CardStatus.Locked} className="text-xs">Tạm khóa</SelectItem>
                <SelectItem value={CardStatus.Lost} className="text-xs">Báo mất</SelectItem>
              </SelectContent>
            </Select>
          </div>
        }
      />

      {/* Modal Thêm thẻ */}
      <Dialog open={isCreateOpen} onOpenChange={setIsCreateOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2 text-base font-bold">
              <CreditCard className="h-5 w-5 text-indigo-600" />
              <span>Thêm Thẻ Định Danh Mới</span>
            </DialogTitle>
            <DialogDescription className="text-xs">
              Mã thẻ sẽ tự động được chuẩn hóa đủ 10 chữ số (thêm các số 0 ở đầu nếu chưa đủ).
            </DialogDescription>
          </DialogHeader>

          <form onSubmit={handleSubmit(handleCreateSubmit)} className="space-y-3.5 py-1">
            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">
                Mã thẻ (tối đa 10 chữ số) <span className="text-destructive">*</span>
              </label>
              <Input
                {...register('cardNumber')}
                placeholder="VD: 12345 hoặc 0000012345"
                className="font-mono text-xs"
                maxLength={10}
                autoFocus
              />
              {watchedCardNumber && (
                <p className="text-[11px] text-muted-foreground">
                  Chuẩn hóa lưu trữ:{' '}
                  <strong className="text-indigo-600 font-mono font-bold">
                    {normalizeCardCode(watchedCardNumber)}
                  </strong>
                </p>
              )}
              {errors.cardNumber && (
                <p className="text-[11px] text-destructive">{errors.cardNumber.message}</p>
              )}
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1">
                <label className="text-xs font-semibold text-foreground">Loại đối tượng</label>
                <Select
                  value={watchedTargetType}
                  onValueChange={(val) => setValue('targetType', val as CardTargetType)}
                >
                  <SelectTrigger className="text-xs">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value={CardTargetType.Person} className="text-xs">
                      Thẻ người (Nhân sự)
                    </SelectItem>
                    <SelectItem value={CardTargetType.Vehicle} className="text-xs">
                      Thẻ xe công vụ
                    </SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-1">
                <label className="text-xs font-semibold text-foreground">Trạng thái ban đầu</label>
                <Select
                  value={watchedStatus}
                  onValueChange={(val) => setValue('status', val as CardStatus)}
                >
                  <SelectTrigger className="text-xs">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value={CardStatus.Available} className="text-xs">
                      Trong kho (Chưa gán)
                    </SelectItem>
                    <SelectItem value={CardStatus.InUse} className="text-xs">
                      Đang sử dụng
                    </SelectItem>
                    <SelectItem value={CardStatus.Locked} className="text-xs">
                      Tạm khóa
                    </SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>

            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">Ghi chú</label>
              <Input
                {...register('note')}
                placeholder="VD: Thẻ Wiegand 26-bit dự phòng kho"
                className="text-xs"
              />
            </div>

            <DialogFooter className="pt-3">
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => setIsCreateOpen(false)}
                className="text-xs h-9 cursor-pointer"
              >
                Hủy bỏ
              </Button>
              <Button
                type="submit"
                size="sm"
                disabled={isSubmitting || createMutation.isPending}
                className="text-xs h-9 bg-indigo-600 hover:bg-indigo-700 text-white cursor-pointer"
              >
                <Save className="h-3.5 w-3.5 mr-1" />
                <span>{createMutation.isPending ? 'Đang lưu...' : 'Thêm thẻ'}</span>
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Confirm Delete */}
      <ConfirmDialog
        open={Boolean(deleteCandidate)}
        onOpenChange={(open) => !open && setDeleteCandidate(null)}
        title="Xóa Thẻ Định Danh"
        description={`Bạn có chắc muốn xóa thẻ "${deleteCandidate?.cardNumber}" khỏi hệ thống không?`}
        confirmText="Xác Nhận Xóa"
        cancelText="Hủy Bỏ"
        variant="destructive"
        onConfirm={() => {
          if (deleteCandidate) {
            deleteMutation.mutate(deleteCandidate.id);
          }
        }}
      />
    </div>
  );
}
