import { useState, useRef, useEffect, useCallback, type ChangeEvent } from 'react';
import { Camera, Trash2, Upload, User, CheckCircle2, AlertTriangle, CreditCard } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Hn212CameraDialog } from './Hn212CameraDialog';
import { toast } from 'sonner';
import { cn } from '@/lib/utils';
import { formatAvatarUrl } from '@/utils/formatAvatarUrl';
import { hn212Service, type Hn212FaceCompareResult } from '@/services/hn212Service';

interface AvatarUploadFieldProps {
  currentUrl?: string | null;
  initialFile?: File | null;
  cccdPhotoFile?: File | null;
  onFileSelected: (file: File | null) => void;
  disabled?: boolean;
  compareResult?: Hn212FaceCompareResult | null;
  onCompareResultChange?: (result: Hn212FaceCompareResult | null) => void;
}

const MAX_FILE_SIZE_BYTES = 5 * 1024 * 1024; // 5MB
const ACCEPTED_IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/webp'];

export function AvatarUploadField({
  currentUrl,
  initialFile,
  cccdPhotoFile,
  onFileSelected,
  disabled = false,
  compareResult: propCompareResult,
  onCompareResultChange,
}: AvatarUploadFieldProps) {
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [isCameraDialogOpen, setIsCameraDialogOpen] = useState(false);
  const [internalCompareResult, setInternalCompareResult] = useState<Hn212FaceCompareResult | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const activeCompareResult =
    propCompareResult !== undefined ? propCompareResult : internalCompareResult;

  // Lắng nghe sự kiện FaceCompared từ đầu đọc HN212
  useEffect(() => {
    const unsubscribe = hn212Service.on('FaceCompared', (result: any) => {
      if (!result) return;
      const score = Number(result.score ?? result.Score ?? 0);
      const isMatch = Boolean(result.isMatch ?? result.IsMatch ?? (score >= 60));
      const message = String(result.message ?? result.Message ?? '');
      const compareData: Hn212FaceCompareResult = {
        score,
        isMatch,
        message,
        capturedFaceBase64: result.capturedFaceBase64 ?? result.CapturedFaceBase64,
      };
      setInternalCompareResult(compareData);
      onCompareResultChange?.(compareData);
    });

    return () => {
      unsubscribe();
    };
  }, [onCompareResultChange]);

  // Cập nhật preview khi có initialFile từ ngoài (ví dụ ảnh chip CCCD)
  useEffect(() => {
    if (initialFile) {
      setInternalCompareResult(null);
      onCompareResultChange?.(null);
      try {
        const objectUrl = URL.createObjectURL(initialFile);
        setPreviewUrl(objectUrl);
        return () => {
          try {
            URL.revokeObjectURL(objectUrl);
          } catch {
            // Safe ignore
          }
        };
      } catch {
        // Safe ignore
      }
    } else {
      setPreviewUrl(null);
    }
  }, [initialFile, onCompareResultChange]);

  useEffect(() => {
    // Thu dọn object URL nếu có
    return () => {
      if (previewUrl && previewUrl.startsWith('blob:')) {
        try {
          URL.revokeObjectURL(previewUrl);
        } catch {
          // Safe ignore
        }
      }
    };
  }, [previewUrl]);

  const handleFileChange = (e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    if (!ACCEPTED_IMAGE_TYPES.includes(file.type)) {
      toast.error('Chỉ chấp nhận tệp ảnh định dạng JPG, PNG hoặc WebP.');
      return;
    }

    if (file.size > MAX_FILE_SIZE_BYTES) {
      toast.error('Kích thước ảnh tối đa cho phép là 5MB.');
      return;
    }

    if (previewUrl && previewUrl.startsWith('blob:')) {
      URL.revokeObjectURL(previewUrl);
    }

    const objectUrl = URL.createObjectURL(file);
    setPreviewUrl(objectUrl);
    setInternalCompareResult(null);
    onCompareResultChange?.(null);
    onFileSelected(file);
  };

  const handleCameraCapture = useCallback((file: File) => {
    if (previewUrl && previewUrl.startsWith('blob:')) {
      try {
        URL.revokeObjectURL(previewUrl);
      } catch {
        // Safe ignore
      }
    }
    try {
      const objectUrl = URL.createObjectURL(file);
      setPreviewUrl(objectUrl);
    } catch {
      // Safe ignore
    }
    onFileSelected(file);
  }, [previewUrl, onFileSelected]);

  const handleSelectCccdPhoto = () => {
    if (!cccdPhotoFile) return;
    if (previewUrl && previewUrl.startsWith('blob:')) {
      try {
        URL.revokeObjectURL(previewUrl);
      } catch {
        // Safe ignore
      }
    }
    try {
      const objectUrl = URL.createObjectURL(cccdPhotoFile);
      setPreviewUrl(objectUrl);
    } catch {
      // Safe ignore
    }
    setInternalCompareResult(null);
    onCompareResultChange?.(null);
    onFileSelected(cccdPhotoFile);
    toast.success('Đã chọn ảnh từ chip thẻ CCCD làm ảnh đại diện!');
  };

  const handleRemove = () => {
    if (previewUrl && previewUrl.startsWith('blob:')) {
      URL.revokeObjectURL(previewUrl);
    }
    setPreviewUrl(null);
    setInternalCompareResult(null);
    onCompareResultChange?.(null);
    onFileSelected(null);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const displaySrc = previewUrl || (currentUrl ? formatAvatarUrl(currentUrl) : null);

  return (
    <div className="flex items-center gap-4 p-3 rounded-xl border border-border/70 bg-card/60">
      <div className="relative shrink-0">
        <div
          className={cn(
            'h-16 w-16 rounded-full border-2 overflow-hidden bg-muted flex items-center justify-center shadow-xs transition-all',
            activeCompareResult
              ? activeCompareResult.isMatch
                ? 'border-emerald-500 ring-2 ring-emerald-500/25'
                : 'border-rose-500 ring-2 ring-rose-500/25'
              : 'border-border'
          )}
        >
          {displaySrc ? (
            <img
              src={displaySrc}
              alt="Avatar khách hàng"
              className="h-full w-full object-cover"
            />
          ) : (
            <User className="h-8 w-8 text-muted-foreground/60" />
          )}
        </div>
        <button
          type="button"
          onClick={() => setIsCameraDialogOpen(true)}
          disabled={disabled}
          className="absolute -bottom-1 -right-1 p-1 rounded-full bg-blue-600 text-white hover:bg-blue-700 shadow-md cursor-pointer disabled:opacity-50 transition-transform active:scale-95"
          title="Chụp ảnh trực tiếp từ đầu đọc HN212"
        >
          <Camera className="h-3.5 w-3.5" />
        </button>
      </div>

      <div className="flex-1 min-w-0 space-y-1">
        <div className="flex items-center gap-2 flex-wrap">
          <span className="text-xs font-semibold text-foreground">
            Ảnh chân dung nhận diện FaceID
          </span>

          {activeCompareResult ? (
            <span
              className={cn(
                'inline-flex items-center gap-1 text-[11px] font-semibold px-2 py-0.5 rounded-full border transition-all animate-in fade-in',
                activeCompareResult.isMatch
                  ? 'bg-emerald-50 text-emerald-700 border-emerald-300 dark:bg-emerald-950/70 dark:text-emerald-300 dark:border-emerald-800'
                  : 'bg-rose-50 text-rose-700 border-rose-300 dark:bg-rose-950/70 dark:text-rose-300 dark:border-rose-800'
              )}
              title={activeCompareResult.message || (activeCompareResult.isMatch ? 'Khuôn mặt khớp' : 'Khuôn mặt không khớp')}
            >
              {activeCompareResult.isMatch ? (
                <CheckCircle2 className="h-3.5 w-3.5 text-emerald-600 dark:text-emerald-400 shrink-0" />
              ) : (
                <AlertTriangle className="h-3.5 w-3.5 text-rose-600 dark:text-rose-400 shrink-0" />
              )}
              <span>
                {activeCompareResult.isMatch
                  ? `Khớp CCCD: ${activeCompareResult.score}%`
                  : `Không khớp CCCD: ${activeCompareResult.score}%`}
              </span>
            </span>
          ) : (
            <span className="text-[10px] bg-blue-100 text-blue-700 dark:bg-blue-950 dark:text-blue-300 px-1.5 py-0.5 rounded font-medium">
              Tối đa 5MB
            </span>
          )}
        </div>
        <p className="text-[11px] text-muted-foreground">
          {activeCompareResult
            ? activeCompareResult.isMatch
              ? 'Ảnh chụp đã được đối soát khớp với ảnh chân dung trong chip thẻ CCCD.'
              : 'Cảnh báo: Ảnh chụp có độ tương đồng thấp với ảnh chân dung trong chip thẻ CCCD.'
            : 'Chụp thẳng mặt rõ nét hoặc bấm vào biểu tượng camera để kích hoạt đầu đọc HN212.'}
        </p>
        <div className="flex items-center gap-2 pt-1 flex-wrap">
          {cccdPhotoFile && (
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={handleSelectCccdPhoto}
              disabled={disabled}
              className="h-7 text-xs gap-1 border-blue-300 dark:border-blue-800 text-blue-700 dark:text-blue-300 hover:bg-blue-50 dark:hover:bg-blue-950 cursor-pointer shadow-2xs"
              title="Sử dụng ảnh chân dung đọc từ thẻ chip CCCD"
            >
              <CreditCard className="h-3.5 w-3.5 text-blue-600 dark:text-blue-400" />
              <span>Dùng ảnh CCCD</span>
            </Button>
          )}

          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => fileInputRef.current?.click()}
            disabled={disabled}
            className="h-7 text-xs gap-1 cursor-pointer"
          >
            <Upload className="h-3.5 w-3.5" />
            <span>Chọn ảnh từ máy</span>
          </Button>

          {displaySrc && (
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={handleRemove}
              disabled={disabled}
              className="h-7 text-xs text-destructive hover:bg-destructive/10 cursor-pointer"
            >
              <Trash2 className="h-3.5 w-3.5 mr-1" />
              <span>Gỡ ảnh</span>
            </Button>
          )}
        </div>
      </div>

      <input
        ref={fileInputRef}
        type="file"
        accept="image/jpeg,image/png,image/webp"
        onChange={handleFileChange}
        className="hidden"
        disabled={disabled}
      />

      {/* Dialog live stream và chụp ảnh khuôn mặt từ đầu đọc HN212 */}
      <Hn212CameraDialog
        open={isCameraDialogOpen}
        onOpenChange={setIsCameraDialogOpen}
        onCaptureSuccess={handleCameraCapture}
      />
    </div>
  );
}
