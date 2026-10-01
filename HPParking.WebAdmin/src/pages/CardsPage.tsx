import { useState, useMemo } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from '@/hooks/use-toast';
import {
  CreditCard,
  Car,
  User,
  Lock,
  AlertCircle,
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
import { CardFormDialog } from '@/components/cards/CardFormDialog';
import { CardDetailDialog } from '@/components/cards/CardDetailDialog';
import { cardApi, extractErrorMessage } from '@/api/cardApi';
import {
  CardTargetType,
  CardStatus,
  type CardDto,
  type CreateCardRequest,
  type UpdateCardRequest,
} from '@/types/card';
import { usePermissions } from '@/hooks/usePermissions';
import { DEFAULT_PAGE_SIZE } from '@/types/masterData';

export function CardsPage() {
  const queryClient = useQueryClient();
  const { canWrite } = usePermissions();

  const [pageIndex, setPageIndex] = useState(1);
  const pageSize = DEFAULT_PAGE_SIZE;
  const [searchKeyword, setSearchKeyword] = useState('');
  const [targetTypeFilter, setTargetTypeFilter] = useState<string>('all');
  const [statusFilter, setStatusFilter] = useState<string>('all');

  const [isFormOpen, setIsFormOpen] = useState(false);
  const [selectedCard, setSelectedCard] = useState<CardDto | null>(null);
  const [detailCardId, setDetailCardId] = useState<string | null>(null);
  const [deleteCandidate, setDeleteCandidate] = useState<CardDto | null>(null);

  const { data: pagedData, isLoading } = useQuery({
    queryKey: ['cards', pageIndex, pageSize, searchKeyword, targetTypeFilter, statusFilter],
    queryFn: () =>
      cardApi.getCards({
        pageIndex,
        pageSize,
        search: searchKeyword.trim() || undefined,
        targetType: targetTypeFilter !== 'all' ? (Number(targetTypeFilter) as CardTargetType) : undefined,
        status: statusFilter !== 'all' ? (Number(statusFilter) as CardStatus) : undefined,
      }),
  });

  const createMutation = useMutation({
    mutationFn: (data: CreateCardRequest) => cardApi.create(data),
    onSuccess: () => {
      toast.success('Đã khởi tạo thẻ định danh mới thành công');
      setIsFormOpen(false);
      setSelectedCard(null);
      void queryClient.invalidateQueries({ queryKey: ['cards'] });
      void queryClient.invalidateQueries({ queryKey: ['clients'] });
      void queryClient.invalidateQueries({ queryKey: ['vehicles'] });
    },
    onError: (err: unknown) => {
      toast.error(extractErrorMessage(err));
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateCardRequest }) => cardApi.update(id, data),
    onSuccess: () => {
      toast.success('Cập nhật thông tin thẻ định danh thành công');
      setIsFormOpen(false);
      setSelectedCard(null);
      void queryClient.invalidateQueries({ queryKey: ['cards'] });
      void queryClient.invalidateQueries({ queryKey: ['card-detail'] });
      void queryClient.invalidateQueries({ queryKey: ['clients'] });
      void queryClient.invalidateQueries({ queryKey: ['vehicles'] });
    },
    onError: (err: unknown) => {
      toast.error(extractErrorMessage(err));
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => cardApi.delete(id),
    onSuccess: () => {
      toast.success('Đã xóa thẻ định danh khỏi hệ thống');
      setDeleteCandidate(null);
      void queryClient.invalidateQueries({ queryKey: ['cards'] });
      void queryClient.invalidateQueries({ queryKey: ['clients'] });
      void queryClient.invalidateQueries({ queryKey: ['vehicles'] });
    },
    onError: (err: unknown) => {
      toast.error(extractErrorMessage(err));
    },
  });

  const handleFormSubmit = async (data: CreateCardRequest | UpdateCardRequest) => {
    if (selectedCard) {
      await updateMutation.mutateAsync({
        id: selectedCard.id,
        data: data as UpdateCardRequest,
      });
    } else {
      await createMutation.mutateAsync(data as CreateCardRequest);
    }
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
              <span>Phương tiện nội bộ</span>
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
    ],
    []
  );

  return (
    <div className="space-y-4 p-4 lg:p-6">
      {/* Header trang (Đã bỏ nút thêm thẻ dư thừa) */}
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
        onAddNew={
          canWrite
            ? () => {
                setSelectedCard(null);
                setIsFormOpen(true);
              }
            : undefined
        }
        addNewLabel="Thêm thẻ mới"
        actions={{
          onView: (item) => setDetailCardId(item.id),
          onEdit: canWrite
            ? (item) => {
                setSelectedCard(item);
                setIsFormOpen(true);
              }
            : undefined,
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
                <SelectItem value={String(CardTargetType.Person)} className="text-xs">Thẻ nhân sự</SelectItem>
                <SelectItem value={String(CardTargetType.Vehicle)} className="text-xs">Thẻ phương tiện nội bộ</SelectItem>
              </SelectContent>
            </Select>

            <Select value={statusFilter} onValueChange={(val) => { setStatusFilter(val); setPageIndex(1); }}>
              <SelectTrigger className="w-[150px] text-xs h-9">
                <SelectValue placeholder="Trạng thái" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all" className="text-xs">Tất cả trạng thái</SelectItem>
                <SelectItem value={String(CardStatus.InUse)} className="text-xs">Đang sử dụng</SelectItem>
                <SelectItem value={String(CardStatus.Available)} className="text-xs">Trong kho</SelectItem>
                <SelectItem value={String(CardStatus.Locked)} className="text-xs">Tạm khóa</SelectItem>
                <SelectItem value={String(CardStatus.Lost)} className="text-xs">Báo mất</SelectItem>
              </SelectContent>
            </Select>
          </div>
        }
      />

      {/* Modal Xem chi tiết thẻ */}
      <CardDetailDialog
        open={Boolean(detailCardId)}
        onOpenChange={(open) => !open && setDetailCardId(null)}
        cardId={detailCardId}
        onEdit={
          canWrite
            ? (card) => {
                setSelectedCard(card);
                setIsFormOpen(true);
              }
            : undefined
        }
      />

      {/* Modal Form Thêm/Sửa Thẻ định danh */}
      <CardFormDialog
        open={isFormOpen}
        onOpenChange={setIsFormOpen}
        initialData={selectedCard}
        onSubmit={handleFormSubmit}
        isSubmitting={createMutation.isPending || updateMutation.isPending}
      />

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
