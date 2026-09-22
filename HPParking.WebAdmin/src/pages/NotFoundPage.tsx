import { useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { FileQuestion, Home } from 'lucide-react';

export function NotFoundPage() {
  const navigate = useNavigate();

  return (
    <div className="min-h-[70vh] flex flex-col items-center justify-center p-6 text-center space-y-4">
      <div className="h-16 w-16 rounded-2xl bg-muted flex items-center justify-center text-muted-foreground">
        <FileQuestion className="h-8 w-8" />
      </div>
      <h1 className="text-2xl font-bold tracking-tight text-foreground">
        404 - Không Tìm Thấy Trang
      </h1>
      <p className="text-xs text-muted-foreground max-w-md">
        Đường dẫn bạn truy cập không tồn tại hoặc đã được di chuyển sang địa chỉ mới.
      </p>
      <Button
        onClick={() => navigate('/')}
        className="gap-2 text-xs cursor-pointer"
      >
        <Home className="h-4 w-4" />
        Quay về Bảng điều khiển
      </Button>
    </div>
  );
}
