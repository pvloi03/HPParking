import { useQuery } from '@tanstack/react-query';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import {
  CreditCard,
  Car,
  User,
  Phone,
  Calendar,
  Clock,
  Edit,
  AlertCircle,
  Lock,
  CheckCircle2,
  Copy,
  Check,
  FileText,
} from 'lucide-react';
import { useState } from 'react';
import { cardApi } from '@/api/cardApi';
import { clientApi } from '@/api/clientApi';
import { vehicleApi } from '@/api/vehicleApi';
import { CardTargetType, CardStatus, type CardDto } from '@/types/card';
import { getVehicleTypeLabel } from '@/types/vehicle';
import { formatDateTimeVi } from '@/utils/formatters';
import { usePermissions } from '@/hooks/usePermissions';

export interface CardDetailDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  cardId: string | null;
  onEdit?: (card: CardDto) => void;
}

export function CardDetailDialog({
  open,
  onOpenChange,
  cardId,
  onEdit,
}: CardDetailDialogProps) {
  const { canWrite } = usePermissions();
  const [isCopied, setIsCopied] = useState(false);

  // Fetch chi tiết thẻ
  const {
    data: card,
    isLoading: isLoadingCard,
    isError: isCardError,
  } = useQuery({
    queryKey: ['card-detail', cardId],
    queryFn: () => (cardId ? cardApi.getById(cardId) : Promise.reject('No ID')),
    enabled: Boolean(open && cardId),
  });

  // Fetch thông tin Nhân sự nếu thẻ gán cho Client
  const { data: client, isLoading: isLoadingClient } = useQuery({
    queryKey: ['card-client', card?.clientId],
    queryFn: () => (card?.clientId ? clientApi.getById(card.clientId) : null),
    enabled: Boolean(open && card?.clientId),
  });

  // Fetch thông tin Phương tiện nếu thẻ gán cho Vehicle
  const { data: vehicle, isLoading: isLoadingVehicle } = useQuery({
    queryKey: ['card-vehicle', card?.vehicleId],
    queryFn: () => (card?.vehicleId ? vehicleApi.getById(card.vehicleId) : null),
    enabled: Boolean(open && card?.vehicleId),
  });

  const handleCopyCardNumber = async () => {
    if (!card?.cardNumber) return;
    try {
      if (typeof navigator !== 'undefined' && navigator.clipboard?.writeText) {
        await navigator.clipboard.writeText(card.cardNumber);
        setIsCopied(true);
        setTimeout(() => setIsCopied(false), 2000);
      }
    } catch (err) {
      console.warn('Không thể sao chép mã thẻ vào clipboard:', err);
    }
  };

  const getStatusBadge = (status?: CardStatus) => {
    switch (status) {
      case CardStatus.InUse:
        return (
          <Badge className="bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300 gap-1 text-xs">
            <CheckCircle2 className="h-3 w-3" />
            <span>Đang sử dụng</span>
          </Badge>
        );
      case CardStatus.Available:
        return (
          <Badge variant="secondary" className="gap-1 text-xs">
            <span>Trong kho (Chưa gán)</span>
          </Badge>
        );
      case CardStatus.Locked:
        return (
          <Badge variant="destructive" className="gap-1 text-xs">
            <Lock className="h-3 w-3" />
            <span>Tạm khóa</span>
          </Badge>
        );
      case CardStatus.Lost:
        return (
          <Badge variant="destructive" className="gap-1 text-xs">
            <AlertCircle className="h-3 w-3" />
            <span>Báo mất</span>
          </Badge>
        );
      default:
        return null;
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <div className="flex items-center justify-between pr-6">
            <DialogTitle className="flex items-center gap-2 text-base font-bold">
              <CreditCard className="h-5 w-5 text-indigo-600" />
              <span>Hồ Sơ Thẻ Định Danh</span>
            </DialogTitle>
            {card && getStatusBadge(card.status)}
          </div>
          <DialogDescription className="text-xs">
            Mã định danh RFID/Wiegand và thông tin phân bổ quyền ra vào hệ thống.
          </DialogDescription>
        </DialogHeader>

        {isLoadingCard ? (
          <div className="space-y-3 py-4">
            <Skeleton className="h-14 w-full rounded-lg" />
            <Skeleton className="h-20 w-full rounded-lg" />
            <Skeleton className="h-16 w-full rounded-lg" />
          </div>
        ) : isCardError || !card ? (
          <div className="py-6 text-center text-xs text-destructive flex flex-col items-center gap-1.5">
            <AlertCircle className="h-6 w-6" />
            <span>Không thể tải thông tin thẻ định danh. Vui lòng thử lại sau.</span>
          </div>
        ) : (
          <div className="space-y-4 py-1 text-xs">
            {/* Khối Mã Thẻ */}
            <div className="p-3.5 rounded-xl bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-800 flex items-center justify-between">
              <div>
                <span className="text-[11px] font-medium text-muted-foreground uppercase tracking-wider block">
                  Mã thẻ 10 số (RFID / Wiegand)
                </span>
                <span className="font-mono text-lg font-bold text-foreground tracking-widest mt-0.5 block">
                  {card.cardNumber}
                </span>
              </div>
              <Button
                size="sm"
                variant="outline"
                onClick={handleCopyCardNumber}
                className="h-8 px-2 text-xs gap-1 cursor-pointer"
                title="Sao chép mã thẻ"
              >
                {isCopied ? (
                  <>
                    <Check className="h-3.5 w-3.5 text-emerald-600" />
                    <span className="text-emerald-600">Đã chép</span>
                  </>
                ) : (
                  <>
                    <Copy className="h-3.5 w-3.5" />
                    <span>Sao chép</span>
                  </>
                )}
              </Button>
            </div>

            {/* Thông tin Đối tượng Gán */}
            <div className="border border-border rounded-xl p-3.5 space-y-2.5">
              <div className="flex items-center justify-between border-b pb-2">
                <span className="font-semibold text-foreground flex items-center gap-1.5">
                  {card.targetType === CardTargetType.Vehicle ? (
                    <>
                      <Car className="h-4 w-4 text-blue-600" />
                      <span>Phương tiện nội bộ (Gán xe)</span>
                    </>
                  ) : (
                    <>
                      <User className="h-4 w-4 text-purple-600" />
                      <span>Nhân sự nội bộ (Gán người)</span>
                    </>
                  )}
                </span>
                <Badge variant="outline" className="text-[10px]">
                  {card.targetType === CardTargetType.Vehicle ? 'Thẻ xe' : 'Thẻ người'}
                </Badge>
              </div>

              {card.targetType === CardTargetType.Person && (
                <>
                  {isLoadingClient ? (
                    <Skeleton className="h-12 w-full" />
                  ) : card.clientId ? (
                    <div className="space-y-1.5 pt-1">
                      <div className="flex justify-between items-center">
                        <span className="text-muted-foreground">Họ và tên:</span>
                        <span className="font-semibold text-foreground">
                          {client?.name || card.clientName || '—'}
                        </span>
                      </div>
                      {client?.code && (
                        <div className="flex justify-between items-center">
                          <span className="text-muted-foreground">Mã nhân sự:</span>
                          <span className="font-mono font-medium text-foreground">
                            {client.code}
                          </span>
                        </div>
                      )}
                      <div className="flex justify-between items-center">
                        <span className="text-muted-foreground flex items-center gap-1">
                          <Phone className="h-3 w-3" /> Số điện thoại:
                        </span>
                        <span className="font-mono text-foreground">
                          {client?.phoneNumber || '—'}
                        </span>
                      </div>
                    </div>
                  ) : (
                    <p className="text-muted-foreground italic py-1">
                      Thẻ hiện chưa được gán cho nhân sự nào (đang lưu kho).
                    </p>
                  )}
                </>
              )}

              {card.targetType === CardTargetType.Vehicle && (
                <>
                  {isLoadingVehicle ? (
                    <Skeleton className="h-12 w-full" />
                  ) : card.vehicleId ? (
                    <div className="space-y-1.5 pt-1">
                      <div className="flex justify-between items-center">
                        <span className="text-muted-foreground">Biển số xe:</span>
                        <span className="font-mono font-bold text-foreground">
                          {vehicle?.plateNumber || card.plateNumber || '—'}
                        </span>
                      </div>
                      <div className="flex justify-between items-center">
                        <span className="text-muted-foreground">Loại phương tiện:</span>
                        <span className="font-medium text-foreground">
                          {getVehicleTypeLabel(vehicle?.type)}
                        </span>
                      </div>
                      <div className="flex justify-between items-center">
                        <span className="text-muted-foreground">Phân loại sử dụng:</span>
                        <Badge variant="secondary" className="text-[10px]">
                          {vehicle?.isShared ? 'Xe dùng chung / Công vụ' : 'Xe cá nhân'}
                        </Badge>
                      </div>
                    </div>
                  ) : (
                    <p className="text-muted-foreground italic py-1">
                      Thẻ hiện chưa được gán cho phương tiện nào (đang lưu kho).
                    </p>
                  )}
                </>
              )}
            </div>

            {/* Thông tin Ghi chú & Thời gian */}
            <div className="border border-border rounded-xl p-3.5 space-y-2">
              <div className="flex items-start gap-2">
                <FileText className="h-4 w-4 text-muted-foreground shrink-0 mt-0.5" />
                <div className="space-y-0.5">
                  <span className="text-muted-foreground font-medium block">Ghi chú:</span>
                  <p className="text-foreground whitespace-pre-wrap">
                    {card.note || <span className="text-muted-foreground italic">Không có ghi chú</span>}
                  </p>
                </div>
              </div>

              <div className="pt-2 border-t border-border/60 grid grid-cols-2 gap-2 text-[11px]">
                <div className="flex items-center gap-1.5 text-muted-foreground">
                  <Calendar className="h-3.5 w-3.5" />
                  <span>Tạo: {formatDateTimeVi(card.createdAt)}</span>
                </div>
                <div className="flex items-center gap-1.5 text-muted-foreground justify-end">
                  <Clock className="h-3.5 w-3.5" />
                  <span>Sửa: {formatDateTimeVi(card.updatedAt)}</span>
                </div>
              </div>
            </div>
          </div>
        )}

        <DialogFooter className="pt-3">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => onOpenChange(false)}
            className="text-xs h-9 cursor-pointer"
          >
            Đóng
          </Button>
          {canWrite && card && onEdit && (
            <Button
              type="button"
              size="sm"
              onClick={() => {
                onOpenChange(false);
                onEdit(card);
              }}
              className="text-xs h-9 bg-indigo-600 hover:bg-indigo-700 text-white cursor-pointer gap-1.5"
            >
              <Edit className="h-3.5 w-3.5" />
              <span>Chỉnh sửa thẻ</span>
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
