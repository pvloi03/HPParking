import { useEffect, useRef, useState } from 'react';
import { useSearchParams, Link } from 'react-router-dom';
import axios from 'axios';
import { clientApi, extractErrorMessage } from '@/api/clientApi';
import type { ConfirmClientStatusResponse } from '@/types/client';
import {
  AlertCircle,
  Loader2,
  Clock,
  Building2,
  ArrowRight,
  ShieldCheck,
  ShieldAlert,
  CheckCheck,
} from 'lucide-react';
import { Button } from '@/components/ui/button';

export type ConfirmPageStatus =
  | 'loading'
  | 'success'
  | 'expired'
  | 'already_processed'
  | 'error';

export function ConfirmClientStatusPage() {
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token');

  const [status, setStatus] = useState<ConfirmPageStatus>('loading');
  const [result, setResult] = useState<ConfirmClientStatusResponse | null>(null);
  const [errorMessage, setErrorMessage] = useState<string>('');
  const executedTokenRef = useRef<string | null>(null);

  useEffect(() => {
    if (!token || token.trim() === '') {
      setStatus('error');
      setErrorMessage('Không tìm thấy mã xác thực token hợp lệ trên đường dẫn liên kết.');
      return;
    }

    const cleanToken = token.trim();
    if (executedTokenRef.current === cleanToken) {
      return;
    }
    executedTokenRef.current = cleanToken;

    const executeConfirmation = async () => {
      try {
        setStatus('loading');
        const res = await clientApi.confirmStatus(cleanToken);
        setResult(res);
        setStatus('success');
      } catch (err: unknown) {

        const msg = extractErrorMessage(err);
        setErrorMessage(msg);

        // Phân loại chính xác trạng thái UI riêng biệt:
        // 'expired' (hết hạn 24h) vs 'already_processed' (đã duyệt trước đó) vs 'error' (lỗi kỹ thuật/không tìm thấy)
        let isExpired = false;
        let isAlreadyProcessed = false;

        if (axios.isAxiosError(err)) {
          const resData = err.response?.data as { message?: string; errors?: string[] } | undefined;
          const rawErrors = [resData?.message, ...(resData?.errors || [])].filter(Boolean).join(' ');

          if (
            rawErrors.includes('APPROVAL_TOKEN_EXPIRED') ||
            rawErrors.toLowerCase().includes('hết hạn') ||
            rawErrors.includes('quá 24 giờ')
          ) {
            isExpired = true;
          } else if (
            rawErrors.includes('APPROVAL_REQUEST_ALREADY_PROCESSED') ||
            rawErrors.toLowerCase().includes('đã được phê duyệt') ||
            rawErrors.toLowerCase().includes('đã được xử lý') ||
            err.response?.status === 409
          ) {
            isAlreadyProcessed = true;
          }
        }

        // Fallback kiểm tra chuỗi thông báo lỗi
        if (!isExpired && !isAlreadyProcessed) {
          const lowerMsg = msg.toLowerCase();
          if (lowerMsg.includes('hết hạn') || lowerMsg.includes('quá 24 giờ')) {
            isExpired = true;
          } else if (lowerMsg.includes('đã được phê duyệt') || lowerMsg.includes('đã được xử lý')) {
            isAlreadyProcessed = true;
          }
        }

        if (isExpired) {
          setStatus('expired');
        } else if (isAlreadyProcessed) {
          setStatus('already_processed');
        } else {
          setStatus('error');
        }
      }
    };

    void executeConfirmation();
  }, [token]);

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 via-blue-50/30 to-indigo-50/40 dark:from-slate-950 dark:via-slate-900 dark:to-blue-950/20 flex flex-col justify-center items-center p-4 sm:p-6">
      {/* Main Card */}
      <div className="w-full max-w-lg bg-card/90 backdrop-blur-md rounded-2xl border border-border/80 shadow-xl overflow-hidden transition-all duration-300">
        {/* 1. Loading State */}
        {status === 'loading' && (
          <div className="p-8 sm:p-10 text-center space-y-4">
            <div className="relative mx-auto w-16 h-16 flex items-center justify-center">
              <div className="absolute inset-0 rounded-full border-4 border-blue-200 dark:border-blue-900 animate-ping opacity-40" />
              <Loader2 className="h-10 w-10 text-blue-600 animate-spin" />
            </div>
            <div className="space-y-1">
              <h2 className="text-lg font-bold text-foreground">Đang xác thực yêu cầu...</h2>
              <p className="text-xs text-muted-foreground">
                Hệ thống đang kiểm tra mã bảo mật và cập nhật trạng thái hồ sơ nhân sự. Vui lòng giữ nguyên màn hình.
              </p>
            </div>
          </div>
        )}

        {/* 2. Success State */}
        {status === 'success' && result && (
          <div className="divide-y divide-border/60">
            <div className="p-6 sm:p-8 text-center space-y-3 bg-gradient-to-b from-emerald-50/60 to-transparent dark:from-emerald-950/30">
              <div className="mx-auto w-14 h-14 rounded-2xl bg-emerald-100 dark:bg-emerald-950 text-emerald-600 dark:text-emerald-400 flex items-center justify-center shadow-md shadow-emerald-600/10">
                {result.newActiveStatus ? (
                  <ShieldCheck className="h-8 w-8" />
                ) : (
                  <ShieldAlert className="h-8 w-8" />
                )}
              </div>
              <div className="space-y-1">
                <span className="inline-block text-[11px] font-bold uppercase tracking-wider px-2.5 py-0.5 rounded-full bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300">
                  Xác nhận thành công
                </span>
                <h2 className="text-xl font-bold tracking-tight text-foreground">
                  {result.newActiveStatus ? 'Đã Kích Hoạt Hồ Sơ Nhân Sự' : 'Đã Khóa Hồ Sơ Nhân Sự'}
                </h2>
                <p className="text-xs text-muted-foreground max-w-sm mx-auto">
                  {result.message}
                </p>
              </div>
            </div>

            <div className="p-6 space-y-3 text-xs">
              <div className="p-3.5 rounded-xl bg-muted/40 border border-border/60 space-y-2">
                <div className="flex justify-between items-center">
                  <span className="text-muted-foreground">Họ và tên nhân sự:</span>
                  <span className="font-bold text-foreground text-sm">{result.clientName}</span>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-muted-foreground">Mã định danh:</span>
                  <span className="font-mono font-semibold text-foreground">{result.clientCode}</span>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-muted-foreground">Trạng thái mới:</span>
                  <span
                    className={`font-bold px-2 py-0.5 rounded-md ${result.newActiveStatus
                      ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300'
                      : 'bg-red-100 text-red-800 dark:bg-red-950 dark:text-red-300'
                      }`}
                  >
                    {result.newActiveStatus ? '✓ HOẠT ĐỘNG (ĐÃ KÍCH HOẠT)' : '✕ VÔ HIỆU HÓA (ĐÃ KHÓA)'}
                  </span>
                </div>
                {result.approvingDepartmentName && (
                  <div className="flex justify-between items-center">
                    <span className="text-muted-foreground flex items-center gap-1">
                      <Building2 className="h-3 w-3" />
                      Phòng ban phê duyệt:
                    </span>
                    <span className="font-medium text-foreground">{result.approvingDepartmentName}</span>
                  </div>
                )}
                <div className="flex justify-between items-center">
                  <span className="text-muted-foreground flex items-center gap-1">
                    <Clock className="h-3 w-3" />
                    Thời điểm xác nhận:
                  </span>
                  <span className="font-mono text-foreground">
                    {new Date(result.processedAt).toLocaleString('vi-VN')}
                  </span>
                </div>
              </div>
            </div>

            <div className="p-4 bg-muted/20 flex justify-center">
              <Button asChild size="sm" className="text-xs h-9 bg-blue-600 hover:bg-blue-700 text-white gap-1.5">
                <Link to="/dashboard">
                  <span>Truy cập hệ thống quản trị</span>
                  <ArrowRight className="h-3.5 w-3.5" />
                </Link>
              </Button>
            </div>
          </div>
        )}

        {/* 3. Expired State (Riêng biệt cho token hết hạn 24 giờ) */}
        {status === 'expired' && (
          <div className="divide-y divide-border/60">
            <div className="p-6 sm:p-8 text-center space-y-3 bg-gradient-to-b from-amber-50/70 to-transparent dark:from-amber-950/30">
              <div className="mx-auto w-14 h-14 rounded-2xl bg-amber-100 dark:bg-amber-950 text-amber-600 dark:text-amber-400 flex items-center justify-center shadow-md shadow-amber-600/10">
                <Clock className="h-8 w-8" />
              </div>
              <div className="space-y-1">
                <span className="inline-block text-[11px] font-bold uppercase tracking-wider px-2.5 py-0.5 rounded-full bg-amber-100 text-amber-800 dark:bg-amber-950 dark:text-amber-300">
                  Liên kết đã hết hiệu lực
                </span>
                <h2 className="text-xl font-bold tracking-tight text-foreground">
                  Yêu Cầu Phê Duyệt Đã Hết Hạn
                </h2>
                <p className="text-xs text-amber-900 dark:text-amber-200 max-w-sm mx-auto font-medium">
                  {errorMessage || 'Liên kết xác nhận này chỉ có hiệu lực trong vòng 24 giờ kể từ thời điểm gửi email.'}
                </p>
              </div>
            </div>
            <div className="p-4 bg-muted/20 flex justify-center">
              <Button asChild variant="outline" size="sm" className="text-xs h-9">
                <Link to="/login">
                  <span>Quay về trang đăng nhập</span>
                </Link>
              </Button>
            </div>
          </div>
        )}

        {/* 4. Already Processed State (Riêng biệt cho yêu cầu đã được xác nhận trước đó) */}
        {status === 'already_processed' && (
          <div className="divide-y divide-border/60">
            <div className="p-6 sm:p-8 text-center space-y-3 bg-gradient-to-b from-sky-50/70 to-transparent dark:from-sky-950/30">
              <div className="mx-auto w-14 h-14 rounded-2xl bg-sky-100 dark:bg-sky-950 text-sky-600 dark:text-sky-400 flex items-center justify-center shadow-md shadow-sky-600/10">
                <CheckCheck className="h-8 w-8" />
              </div>
              <div className="space-y-1">
                <span className="inline-block text-[11px] font-bold uppercase tracking-wider px-2.5 py-0.5 rounded-full bg-sky-100 text-sky-800 dark:bg-sky-950 dark:text-sky-300">
                  Yêu cầu đã được xác nhận
                </span>
                <h2 className="text-xl font-bold tracking-tight text-foreground">
                  Yêu Cầu Này Đã Được Xử Lý
                </h2>
                <p className="text-xs text-sky-900 dark:text-sky-200 max-w-sm mx-auto font-medium">
                  {errorMessage || 'Yêu cầu phê duyệt trạng thái hồ sơ nhân sự này đã được xác nhận hoàn tất trước đó.'}
                </p>
              </div>
            </div>
            <div className="p-4 bg-muted/20 flex justify-center">
              <Button asChild size="sm" className="text-xs h-9 bg-blue-600 hover:bg-blue-700 text-white gap-1.5">
                <Link to="/dashboard">
                  <span>Truy cập hệ thống quản trị</span>
                  <ArrowRight className="h-3.5 w-3.5" />
                </Link>
              </Button>
            </div>
          </div>
        )}

        {/* 5. General Error State (Mã không hợp lệ, lỗi kết nối hoặc vi phạm toàn vẹn) */}
        {status === 'error' && (
          <div className="divide-y divide-border/60">
            <div className="p-6 sm:p-8 text-center space-y-3 bg-gradient-to-b from-red-50/60 to-transparent dark:from-red-950/30">
              <div className="mx-auto w-14 h-14 rounded-2xl bg-red-100 dark:bg-red-950 text-red-600 dark:text-red-400 flex items-center justify-center shadow-md shadow-red-600/10">
                <AlertCircle className="h-8 w-8" />
              </div>
              <div className="space-y-1">
                <span className="inline-block text-[11px] font-bold uppercase tracking-wider px-2.5 py-0.5 rounded-full bg-red-100 text-red-800 dark:bg-red-950 dark:text-red-300">
                  Xác nhận không thành công
                </span>
                <h2 className="text-xl font-bold tracking-tight text-foreground">
                  Không Thể Hoàn Tất Phê Duyệt
                </h2>
                <p className="text-xs text-destructive max-w-sm mx-auto font-medium">
                  {errorMessage}
                </p>
              </div>
            </div>
            <div className="p-4 bg-muted/20 flex justify-center">
              <Button asChild variant="outline" size="sm" className="text-xs h-9">
                <Link to="/login">
                  <span>Quay về trang đăng nhập</span>
                </Link>
              </Button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
