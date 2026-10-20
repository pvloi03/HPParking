import { useState, useMemo, useEffect } from 'react';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/components/ui/dialog';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { toast } from '@/hooks/use-toast';
import { Mail, ShieldAlert, ShieldCheck, Loader2 } from 'lucide-react';
import { clientApi, extractErrorMessage } from '@/api/clientApi';
import {
  ClientApprovalActionType,
  type ClientDto,
  type ClientDetailDto,
} from '@/types/client';
import type { DepartmentDto } from '@/types/masterData';
import { findDefaultHrDepartmentId } from '@/utils/departmentHelper';


interface RequestStatusChangeDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  client: ClientDto | ClientDetailDto | null;
  targetAction: ClientApprovalActionType;
  departments: DepartmentDto[];
  onSuccess?: () => void;
}

export function RequestStatusChangeDialog({
  open,
  onOpenChange,
  client,
  targetAction,
  departments,
  onSuccess,
}: RequestStatusChangeDialogProps) {
  const isActivate = targetAction === ClientApprovalActionType.Activate;
  const [selectedDeptId, setSelectedDeptId] = useState<string>('');
  const [note, setNote] = useState<string>('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const departmentsWithEmail = useMemo(() => {
    return departments.filter(
      (d) => d.isActive && d.email && d.email.trim() !== ''
    );
  }, [departments]);

  useEffect(() => {
    if (open && departmentsWithEmail.length > 0) {
      setSelectedDeptId(findDefaultHrDepartmentId(departmentsWithEmail));
      setNote('');
    }
  }, [open, departmentsWithEmail]);

  if (!client) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedDeptId) {
      toast.error('Vui lòng chọn phòng ban tiếp nhận email phê duyệt.');
      return;
    }

    try {
      setIsSubmitting(true);
      const res = await clientApi.requestStatusChange(client.id, {
        approvingDepartmentId: selectedDeptId,
        actionType: targetAction,
        note: note.trim() || undefined,
      });

      if (res.emailSent === false) {
        toast.warning(
          `Đã tạo yêu cầu ${isActivate ? 'kích hoạt lại' : 'khóa'} nhưng gửi email thất bại: ${res.emailError || 'Lỗi máy chủ gửi thư'}. Bạn có thể bấm "Gửi lại email" sau.`,
          { duration: 6000 }
        );
      } else {
        toast.success(
          `Đã gửi email yêu cầu ${isActivate ? 'kích hoạt lại' : 'khóa'} hồ sơ tới ${res.approvingDepartmentEmail}.`,
          { duration: 5000 }
        );
      }
      onOpenChange(false);
      onSuccess?.();
    } catch (err) {
      toast.error(extractErrorMessage(err));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <div className="flex items-center gap-2">
            <div
              className={`p-2 rounded-lg ${
                isActivate
                  ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-400'
                  : 'bg-red-100 text-red-700 dark:bg-red-950 dark:text-red-400'
              }`}
            >
              {isActivate ? <ShieldCheck className="h-5 w-5" /> : <ShieldAlert className="h-5 w-5" />}
            </div>
            <div>
              <DialogTitle className="text-base font-bold">
                {isActivate ? 'Yêu Cầu Kích Hoạt Lại Hồ Sơ' : 'Yêu Cầu Khóa Hồ Sơ Nhân Sự'}
              </DialogTitle>
              <DialogDescription className="text-xs">
                Gửi liên kết 1-click tới phòng ban chuyên trách để xác nhận thay đổi trạng thái
              </DialogDescription>
            </div>
          </div>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4 pt-2">
          {/* Thông tin nhân sự tóm tắt */}
          <div className="p-3 rounded-lg bg-muted/40 border border-border space-y-1 text-xs">
            <div className="flex justify-between">
              <span className="text-muted-foreground">Nhân sự:</span>
              <span className="font-semibold">{client.name}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-muted-foreground">Mã định danh:</span>
              <span className="font-mono">{client.code}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-muted-foreground">Hành động:</span>
              <span
                className={`font-bold ${
                  isActivate ? 'text-emerald-600 dark:text-emerald-400' : 'text-red-600 dark:text-red-400'
                }`}
              >
                {isActivate ? 'Chuyển sang HOẠT ĐỘNG' : 'Chuyển sang VÔ HIỆU HÓA'}
              </span>
            </div>
          </div>

          {/* Chọn phòng ban */}
          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-foreground flex items-center gap-1.5">
              <Mail className="h-3.5 w-3.5 text-blue-600" />
              <span>Phòng ban nhận email phê duyệt <span className="text-destructive">*</span></span>
            </label>
            {departmentsWithEmail.length === 0 ? (
              <p className="text-xs text-destructive">
                Chưa có phòng ban nào cấu hình email trong hệ thống.
              </p>
            ) : (
              <Select value={selectedDeptId} onValueChange={setSelectedDeptId}>
                <SelectTrigger className="text-xs bg-background h-9">
                  <SelectValue placeholder="Chọn phòng ban nhận email..." />
                </SelectTrigger>
                <SelectContent>
                  {departmentsWithEmail.map((d) => (
                    <SelectItem key={d.id} value={d.id} className="text-xs">
                      <div className="flex items-center gap-2">
                        <span className="font-medium">{d.name}</span>
                        <span className="text-muted-foreground font-mono text-[11px]">({d.email})</span>
                      </div>
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}
          </div>

          {/* Lý do / Ghi chú */}
          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-foreground">
              Lý do thay đổi trạng thái (Tùy chọn)
            </label>
            <Input
              value={note}
              onChange={(e) => setNote(e.target.value)}
              placeholder={isActivate ? 'VD: Đã hoàn tất bổ sung hợp đồng...' : 'VD: Nhân sự tạm nghỉ việc / vi phạm...'}
              className="text-xs bg-background h-9"
              maxLength={500}
            />
          </div>

          <DialogFooter className="pt-2 gap-2 sm:gap-0">
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => onOpenChange(false)}
              disabled={isSubmitting}
              className="text-xs h-9"
            >
              Hủy bỏ
            </Button>
            <Button
              type="submit"
              size="sm"
              disabled={isSubmitting || departmentsWithEmail.length === 0}
              className={`text-xs h-9 text-white ${
                isActivate
                  ? 'bg-emerald-600 hover:bg-emerald-700'
                  : 'bg-red-600 hover:bg-red-700'
              }`}
            >
              {isSubmitting ? (
                <>
                  <Loader2 className="h-3.5 w-3.5 mr-1.5 animate-spin" />
                  <span>Đang gửi...</span>
                </>
              ) : (
                <>
                  <Mail className="h-3.5 w-3.5 mr-1.5" />
                  <span>Gửi Email Phê Duyệt</span>
                </>
              )}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
