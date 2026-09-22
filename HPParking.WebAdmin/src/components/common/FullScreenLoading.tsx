import { Loader2 } from 'lucide-react';

interface FullScreenLoadingProps {
  message?: string;
}

export function FullScreenLoading({
  message = 'Đang xác thực phiên làm việc...',
}: FullScreenLoadingProps) {
  return (
    <div
      data-testid="auth-loading"
      className="fixed inset-0 z-50 flex flex-col items-center justify-center bg-background/80 backdrop-blur-xs text-foreground"
    >
      <div className="flex flex-col items-center gap-3 p-6 rounded-2xl bg-card border border-border shadow-xl">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
        <p className="text-xs font-medium text-muted-foreground">{message}</p>
      </div>
    </div>
  );
}
