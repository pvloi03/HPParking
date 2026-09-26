import { Badge } from '@/components/ui/badge';
import { Camera, Cpu, ScanFace, HelpCircle } from 'lucide-react';
import { DeviceType, LaneDirection } from '@/types/infrastructure';

/**
 * Trả về Badge hiển thị phân loại thiết bị ngoại vi đồng bộ toàn hệ thống
 */
export function getDeviceTypeBadge(type: DeviceType) {
  switch (type) {
    case DeviceType.Camera:
      return (
        <Badge className="bg-blue-100 text-blue-800 hover:bg-blue-100 dark:bg-blue-950 dark:text-blue-300 gap-1.5 text-xs font-medium py-0.5 px-2">
          <Camera className="h-3.5 w-3.5" />
          <span>Camera Giám Sát</span>
        </Badge>
      );
    case DeviceType.Controller:
      return (
        <Badge className="bg-amber-100 text-amber-800 hover:bg-amber-100 dark:bg-amber-950 dark:text-amber-300 gap-1.5 text-xs font-medium py-0.5 px-2">
          <Cpu className="h-3.5 w-3.5" />
          <span>Bộ Điều Khiển Barrier</span>
        </Badge>
      );
    case DeviceType.FaceId:
      return (
        <Badge className="bg-emerald-100 text-emerald-800 hover:bg-emerald-100 dark:bg-emerald-950 dark:text-emerald-300 gap-1.5 text-xs font-medium py-0.5 px-2">
          <ScanFace className="h-3.5 w-3.5" />
          <span>Nhận Diện Khuôn Mặt (FaceID)</span>
        </Badge>
      );
    default:
      return (
        <Badge className="bg-muted text-muted-foreground gap-1.5 text-xs font-medium py-0.5 px-2">
          <HelpCircle className="h-3.5 w-3.5" />
          <span>Thiết Bị Khác</span>
        </Badge>
      );
  }
}

/**
 * Trả về Badge hiển thị hướng làn xe đồng bộ toàn hệ thống
 */
export function getLaneDirectionBadge(dir: LaneDirection) {
  switch (dir) {
    case LaneDirection.In:
      return (
        <Badge
          variant="outline"
          className="text-[10px] font-medium text-blue-600 dark:text-blue-400 border-blue-200 dark:border-blue-900"
        >
          Làn Vào
        </Badge>
      );
    case LaneDirection.Out:
      return (
        <Badge
          variant="outline"
          className="text-[10px] font-medium text-amber-600 dark:text-amber-400 border-amber-200 dark:border-amber-900"
        >
          Làn Ra
        </Badge>
      );
    case LaneDirection.Bidirectional:
      return (
        <Badge
          variant="outline"
          className="text-[10px] font-medium text-purple-600 dark:text-purple-400 border-purple-200 dark:border-purple-900"
        >
          Hai Chiều
        </Badge>
      );
    default:
      return null;
  }
}
