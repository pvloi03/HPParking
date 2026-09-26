import { useState, useEffect, useRef } from 'react';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Camera, RefreshCw, X, AlertCircle, Scan } from 'lucide-react';
import { hn212Service, base64ToFile } from '@/services/hn212Service';
import { toast } from 'sonner';

interface Hn212CameraDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onCaptureSuccess: (file: File, base64: string) => void;
}

export function Hn212CameraDialog({
  open,
  onOpenChange,
  onCaptureSuccess,
}: Hn212CameraDialogProps) {
  const [isInitializing, setIsInitializing] = useState(true);
  const [isCapturing, setIsCapturing] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [streamTimestamp, setStreamTimestamp] = useState<number>(Date.now());
  const hasCapturedRef = useRef<boolean>(false);

  // Lưu callbacks vào ref để tránh re-trigger effect khi component cha re-render
  const onCaptureSuccessRef = useRef(onCaptureSuccess);
  onCaptureSuccessRef.current = onCaptureSuccess;

  const onOpenChangeRef = useRef(onOpenChange);
  onOpenChangeRef.current = onOpenChange;

  useEffect(() => {
    if (!open) {
      // Khi đóng dialog, hủy tiến trình và tắt camera
      hasCapturedRef.current = false;
      void hn212Service.cancelCaptureFace();
      setIsInitializing(false);
      setIsCapturing(false);
      setErrorMessage(null);
      return;
    }

    let isMounted = true;
    hasCapturedRef.current = false;
    setErrorMessage(null);
    setIsInitializing(true);
    setIsCapturing(true);
    setStreamTimestamp(Date.now());

    // 1. Kết nối websocket nếu chưa kết nối
    hn212Service.connect();

    // 2. Đăng ký lắng nghe sự kiện ảnh khuôn mặt chụp thành công từ HN212
    const unsubscribeCaptured = hn212Service.on('FaceCaptured', (base64: string) => {
      if (!isMounted || hasCapturedRef.current) return;
      hasCapturedRef.current = true; // Đánh dấu đã chụp xong

      setIsCapturing(false);
      toast.success('Đã chụp ảnh khuôn mặt thành công từ thiết bị HN212!');

      try {
        const file = base64ToFile(base64, 'avatar_hn212.jpg');
        onCaptureSuccessRef.current(file, base64);
      } catch (err) {
        console.error('Lỗi chuyển đổi base64 ảnh chân dung:', err);
      }

      // Đóng dialog ngay lập tức không cần chờ
      onOpenChangeRef.current(false);
    });

    // 3. Đăng ký lắng nghe khi camera bị hủy
    const unsubscribeCancelled = hn212Service.on('FaceCaptureCancelled', (reason?: string) => {
      if (!isMounted || hasCapturedRef.current) return;
      setIsCapturing(false);
      setErrorMessage(reason || 'Quá trình chụp khuôn mặt đã kết thúc hoặc quá thời gian chờ.');
    });

    // 4. Kích hoạt lệnh bật camera trên thiết bị
    const startCamera = async () => {
      // Kiểm tra trạng thái thiết bị trước
      const status = await hn212Service.getStatus();
      if (!status || !status.isReaderConnected) {
        if (isMounted) {
          setIsInitializing(false);
          setIsCapturing(false);
          setErrorMessage('Chưa phát hiện đầu đọc HN212 được kết nối với máy tính hoặc dịch vụ chưa khởi chạy.');
        }
        return;
      }

      const ok = await hn212Service.startCaptureFace();
      if (isMounted) {
        setIsInitializing(false);
        if (!ok) {
          setIsCapturing(false);
          setErrorMessage('Không thể gửi lệnh bật camera tới đầu đọc HN212. Vui lòng kiểm tra lại thiết bị.');
        }
      }
    };

    void startCamera();

    return () => {
      isMounted = false;
      unsubscribeCaptured();
      unsubscribeCancelled();
      void hn212Service.cancelCaptureFace();
    };
  }, [open]);

  const handleRetry = async () => {
    setErrorMessage(null);
    setIsInitializing(true);
    setIsCapturing(true);
    setStreamTimestamp(Date.now());

    const ok = await hn212Service.startCaptureFace();
    setIsInitializing(false);
    if (!ok) {
      setIsCapturing(false);
      setErrorMessage('Không thể bật camera. Kiểm tra lại kết nối thiết bị.');
    }
  };

  const handleClose = () => {
    void hn212Service.cancelCaptureFace();
    onOpenChange(false);
  };

  const streamUrl = `${hn212Service.getCameraStreamUrl()}?t=${streamTimestamp}`;

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="sm:max-w-lg p-0 overflow-hidden bg-background border-border shadow-2xl">
        <DialogHeader className="p-4 pb-2 border-b border-border/70">
          <div className="flex items-center gap-2">
            <div className="p-2 rounded-xl bg-blue-100 dark:bg-blue-950 text-blue-600 dark:text-blue-400">
              <Camera className="h-5 w-5" />
            </div>
            <div>
              <DialogTitle className="text-base font-bold">
                Chụp Ảnh Khuôn Mặt Bằng Đầu Đọc HN212
              </DialogTitle>
              <DialogDescription className="text-xs text-muted-foreground">
                Camera tự động phát hiện khuôn mặt đạt chuẩn và lưu làm ảnh Avatar / FaceID.
              </DialogDescription>
            </div>
          </div>
        </DialogHeader>

        <div className="p-4 space-y-3">
          {/* Khung Stream Video từ Camera */}
          <div className="relative aspect-4/3 w-full rounded-2xl overflow-hidden bg-neutral-950 border border-neutral-800 flex items-center justify-center shadow-inner">
            {errorMessage ? (
              // Hiển thị thông báo lỗi
              <div className="p-6 text-center space-y-3 max-w-xs text-neutral-300">
                <div className="inline-flex p-3 rounded-full bg-destructive/20 text-destructive border border-destructive/30">
                  <AlertCircle className="h-7 w-7" />
                </div>
                <p className="text-xs text-neutral-300 leading-relaxed">{errorMessage}</p>
                <Button
                  size="sm"
                  variant="outline"
                  onClick={handleRetry}
                  className="text-xs gap-1.5 h-8 bg-neutral-900 text-white border-neutral-700 hover:bg-neutral-800"
                >
                  <RefreshCw className="h-3.5 w-3.5" />
                  <span>Thử lại</span>
                </Button>
              </div>
            ) : isInitializing ? (
              // Đang bật camera
              <div className="flex flex-col items-center gap-2.5 text-neutral-400">
                <RefreshCw className="h-7 w-7 animate-spin text-blue-500" />
                <span className="text-xs font-medium">Đang khởi động camera thiết bị...</span>
              </div>
            ) : (
              // Live Stream Camera từ HN212
              <div className="relative w-full h-full flex items-center justify-center bg-neutral-950">
                <img
                  src={streamUrl}
                  alt="HN212 Camera Stream"
                  className="w-full h-full object-cover"
                  onError={() => {
                    setErrorMessage('Không thể tải luồng video từ camera đầu đọc.');
                  }}
                />

                {/* Khung viền ngắm nhận diện khuôn mặt chuyên nghiệp */}
                <div className="absolute inset-0 pointer-events-none flex items-center justify-center p-8">
                  <div className="relative w-52 h-64 rounded-3xl border-2 border-dashed border-blue-400/80 bg-blue-500/5 shadow-[0_0_20px_rgba(59,130,246,0.25)] flex flex-col items-center justify-between p-3">
                    <div className="flex justify-between w-full">
                      <div className="w-4 h-4 border-t-2 border-l-2 border-blue-400 rounded-tl-lg" />
                      <div className="w-4 h-4 border-t-2 border-r-2 border-blue-400 rounded-tr-lg" />
                    </div>

                    <div className="flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-neutral-950/80 border border-blue-400/40 text-[11px] text-blue-300 font-medium tracking-wide">
                      <Scan className="h-3.5 w-3.5 animate-pulse text-blue-400" />
                      <span>{isCapturing ? 'Đang quét khuôn mặt...' : 'Đang xử lý ảnh'}</span>
                    </div>

                    <div className="flex justify-between w-full">
                      <div className="w-4 h-4 border-b-2 border-l-2 border-blue-400 rounded-bl-lg" />
                      <div className="w-4 h-4 border-b-2 border-r-2 border-blue-400 rounded-br-lg" />
                    </div>
                  </div>
                </div>

                {/* Badge trạng thái live */}
                <div className="absolute top-3 left-3 flex items-center gap-1.5 px-2 py-0.5 rounded-full bg-red-600/90 text-white text-[10px] font-bold uppercase tracking-wider shadow-sm">
                  <span className="h-1.5 w-1.5 rounded-full bg-white animate-ping" />
                  <span>HN212 LIVE</span>
                </div>
              </div>
            )}
          </div>

          {/* Hướng dẫn người dùng */}
          <div className="p-2.5 rounded-xl bg-muted/60 border border-border/80 flex items-center gap-2.5 text-xs text-muted-foreground">
            <Scan className="h-4 w-4 text-blue-600 dark:text-blue-400 shrink-0" />
            <span>
              Giữ thẳng mặt trong khung ngắm, bỏ kính râm và khẩu trang. Thiết bị sẽ tự động chụp khi ảnh đạt chất lượng tối ưu.
            </span>
          </div>
        </div>

        <DialogFooter className="p-4 pt-2 border-t border-border/70 flex sm:justify-between items-center gap-2">
          <div className="text-[11px] text-muted-foreground flex items-center gap-1">
            <span className="inline-block h-2 w-2 rounded-full bg-emerald-500" />
            <span>HN212 Camera Service (Port 5000)</span>
          </div>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={handleClose}
            className="text-xs h-8 cursor-pointer"
          >
            <X className="h-3.5 w-3.5 mr-1" />
            <span>Hủy / Đóng</span>
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
