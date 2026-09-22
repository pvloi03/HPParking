import { useState, useRef, useEffect, type ChangeEvent } from 'react';
import { Camera, Trash2, Upload, User } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { toast } from 'sonner';

interface AvatarUploadFieldProps {
  currentUrl?: string | null;
  onFileSelected: (file: File | null) => void;
  disabled?: boolean;
}

const MAX_FILE_SIZE_BYTES = 5 * 1024 * 1024; // 5MB
const ACCEPTED_IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/webp'];

export function AvatarUploadField({
  currentUrl,
  onFileSelected,
  disabled = false,
}: AvatarUploadFieldProps) {
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    // Thu dọn object URL nếu có
    return () => {
      if (previewUrl && previewUrl.startsWith('blob:')) {
        URL.revokeObjectURL(previewUrl);
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
    onFileSelected(file);
  };

  const handleRemove = () => {
    if (previewUrl && previewUrl.startsWith('blob:')) {
      URL.revokeObjectURL(previewUrl);
    }
    setPreviewUrl(null);
    onFileSelected(null);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const displaySrc = previewUrl || currentUrl || null;

  return (
    <div className="flex items-center gap-4 p-3 rounded-xl border border-border/70 bg-card/60">
      <div className="relative shrink-0">
        <div className="h-16 w-16 rounded-full border-2 border-border overflow-hidden bg-muted flex items-center justify-center shadow-xs">
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
          onClick={() => fileInputRef.current?.click()}
          disabled={disabled}
          className="absolute -bottom-1 -right-1 p-1 rounded-full bg-blue-600 text-white hover:bg-blue-700 shadow-md cursor-pointer disabled:opacity-50"
          title="Tải ảnh lên"
        >
          <Camera className="h-3.5 w-3.5" />
        </button>
      </div>

      <div className="flex-1 min-w-0 space-y-1">
        <div className="flex items-center gap-2">
          <span className="text-xs font-semibold text-foreground">
            Ảnh chân dung nhận diện FaceID
          </span>
          <span className="text-[10px] bg-blue-100 text-blue-700 dark:bg-blue-950 dark:text-blue-300 px-1.5 py-0.5 rounded font-medium">
            Tối đa 5MB
          </span>
        </div>
        <p className="text-[11px] text-muted-foreground">
          Chụp thẳng mặt rõ nét, không đeo kính đen hoặc khẩu trang để đảm bảo tỉ lệ nhận diện tốt nhất.
        </p>
        <div className="flex items-center gap-2 pt-1">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => fileInputRef.current?.click()}
            disabled={disabled}
            className="h-7 text-xs gap-1 cursor-pointer"
          >
            <Upload className="h-3.5 w-3.5" />
            <span>Chọn ảnh</span>
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
    </div>
  );
}
