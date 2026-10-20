import { useEffect, useRef, useState } from 'react';
import { useSearchParams, Link } from 'react-router-dom';
import { clientApi, extractErrorMessage } from '@/api/clientApi';
import type { ConfirmClientStatusResponse } from '@/types/client';
import {
  CheckCircle2,
  AlertCircle,
  Loader2,
  Car,
  Clock,
  Building2,
  ArrowRight,
  ShieldCheck,
  ShieldAlert,
} from 'lucide-react';
import { Button } from '@/components/ui/button';

export function ConfirmClientStatusPage() {
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token');

  const [status, setStatus] = useState<'loading' | 'success' | 'error'>('loading');
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

    let isMounted = true;
    const executeConfirmation = async () => {
      try {
        setStatus('loading');
        const res = await clientApi.confirmStatus(cleanToken);
        if (isMounted) {
          setResult(res);
          setStatus('success');
        }
      } catch (err) {
        if (isMounted) {
          setStatus('error');
          setErrorMessage(extractErrorMessage(err));
        }
      }
    };

    void executeConfirmation();

    return () => {
      isMounted = false;
    };
  }, [token]);

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 via-blue-50/30 to-indigo-50/40 dark:from-slate-950 dark:via-slate-900 dark:to-blue-950/20 flex flex-col justify-center items-center p-4 sm:p-6">
      {/* Brand Header */}
      <div className="mb-6 text-center space-y-2">
        <div className="inline-flex items-center gap-2.5 px-4 py-2 rounded-2xl bg-white/80 dark:bg-card/80 border border-border shadow-xs backdrop-blur-md">
          <div className="p-2 rounded-xl bg-blue-600 text-white shadow-xs">
            <Car className="h-5 w-5" />
          </div>
          <span className="font-extrabold text-base tracking-tight text-foreground">
            HPPARKING SMART PARKING
          </span>
        </div>
        <p className="text-xs text-muted-foreground">
          Cổng thông tin xác nhận phê duyệt trạng thái hồ sơ nhân sự
        </p>
      </div>

      {/* Main Card */}
      <div className="w-full max-w-lg bg-card/90 backdrop-blur-md rounded-2xl border border-border/80 shadow-xl overflow-hidden transition-all duration-300">
        {/* Loading State */}
        {status === 'loading' && (
          <div className="p-8 sm:p-10 text-center space-y-4">
            <div className="relative mx-auto w-16 h-16 flex items-center justify-center">
              <div className="absolute inset-0 rounded-full border-4 border-blue-200 dark:border-blue-900 animate-ping opacity-40" />
              <Loader2 className="h-10 w-10 text-blue-600 animate-spin" />
            </div>
            <div className="space-y-1">
              <h2 className="text-lg font-bold text-foreground">Đang xác thực yêu cầu...</h2>
              <p className="text-xs text-muted-foreground">
                Hệ thống đang kiểm tra mã token bảo mật và cập nhật trạng thái hồ sơ nhân sự. Vui lòng giữ nguyên màn hình.
              </p>
            </div>
          </div>
        )}

        {/* Success State */}
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
                  Xác nhận 1-Click thành công
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
                  <span className="text-muted-foreground">Mã định danh (CCCD):</span>
                  <span className="font-mono font-semibold text-foreground">{result.clientCode}</span>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-muted-foreground">Trạng thái mới:</span>
                  <span
                    className={`font-bold px-2 py-0.5 rounded-md ${
                      result.newActiveStatus
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

              <div className="p-3 rounded-lg bg-blue-50/60 dark:bg-blue-950/20 border border-blue-200/60 dark:border-blue-900/40 text-[11px] text-blue-800 dark:text-blue-300 space-y-1">
                <p className="font-semibold flex items-center gap-1.5">
                  <CheckCircle2 className="h-3.5 w-3.5 text-blue-600" />
                  Quyền truy cập bãi đỗ xe đã được cập nhật
                </p>
                <p className="text-muted-foreground">
                  Hệ thống kiểm soát cổng barie và các làn tự động nhận diện sẽ áp dụng ngay lập tức trạng thái mới này khi nhân sự quẹt thẻ hoặc nhận diện khuôn mặt.
                </p>
              </div>
            </div>

            <div className="p-4 bg-muted/20 flex justify-center">
              <Button asChild size="sm" className="text-xs h-9 bg-blue-600 hover:bg-blue-700 text-white gap-1.5">
                <Link to="/login">
                  <span>Truy cập hệ thống quản trị</span>
                  <ArrowRight className="h-3.5 w-3.5" />
                </Link>
              </Button>
            </div>
          </div>
        )}

        {/* Error / Expired State */}
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

            <div className="p-6 space-y-3 text-xs text-muted-foreground leading-relaxed">
              <p>
                <strong>Nguyên nhân có thể do:</strong>
              </p>
              <ul className="list-disc pl-5 space-y-1">
                <li>Đường link xác nhận đã quá thời hạn hiệu lực <strong>24 giờ</strong>.</li>
                <li>Yêu cầu này đã được ai đó trong phòng ban xác nhận trước đó.</li>
                <li>Mã token trên đường dẫn không hợp lệ hoặc đã bị thay đổi.</li>
              </ul>
              <p className="pt-2 text-[11px]">
                Vui lòng liên hệ Quản trị viên hệ thống bãi xe để gửi lại đường liên kết phê duyệt mới nếu cần.
              </p>
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

      <div className="mt-8 text-center text-[11px] text-muted-foreground">
        HPParking Management System &copy; 2026. All rights reserved.
      </div>
    </div>
  );
}
