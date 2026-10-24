import type { ColumnDef } from '@/components/common/DataTable';
import type { ParkingSessionDto } from '@/types/parkingSession';
import { formatInOrderNumber } from '@/utils/formatters';

/**
 * Cột chuẩn hiển thị số thứ tự 'Lượt Vào' (#<InOrderNumber>) trên bảng danh sách phiên vào ra.
 */
export const inOrderNumberColumn: ColumnDef<ParkingSessionDto> = {
  header: 'Lượt Vào',
  className: 'w-[90px]',
  cell: (item) => (
    <span className="font-mono font-bold text-xs text-primary">
      {formatInOrderNumber(item)}
    </span>
  ),
};
