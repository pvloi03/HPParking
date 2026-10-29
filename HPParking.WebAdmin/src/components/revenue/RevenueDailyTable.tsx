import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import type { RevenueReportDto } from '@/types/statistics';
import { formatDateDisplay } from '@/utils/dateUtils';

export interface RevenueDailyTableProps {
  data?: RevenueReportDto;
  isLoading?: boolean;
}

/**
 * Bảng kê chi tiết doanh thu theo từng ngày xuất bãi
 */
export function RevenueDailyTable({ data, isLoading }: RevenueDailyTableProps) {
  return (
    <Card className="shadow-xs border-border/80">
      <CardHeader className="flex flex-row items-center justify-between pb-3">
        <div>
          <CardTitle className="text-base font-semibold">
            Bảng Kê Doanh Thu Chi Tiết Theo Ngày
          </CardTitle>
          <CardDescription className="text-xs text-muted-foreground mt-0.5">
            Chi tiết số lượt xe ra, lượt thu phí và tổng doanh thu thực thu từng ngày
          </CardDescription>
        </div>
      </CardHeader>
      <CardContent className="p-0">
        <div className="overflow-x-auto">
          <table className="w-full text-sm text-left border-collapse">
            <thead className="bg-muted/50 text-xs uppercase text-muted-foreground font-semibold border-b border-border">
              <tr>
                <th className="py-3 px-4">Ngày</th>
                <th className="py-3 px-4 text-right">Lượt Thu Phí</th>
                <th className="py-3 px-4 text-right">Tổng Lượt Ra</th>
                <th className="py-3 px-4 text-right">Tổng Doanh Thu</th>
                <th className="py-3 px-4 text-right">Doanh Thu TB / Lượt</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {isLoading ? (
                Array.from({ length: 5 }).map((_, i) => (
                  <tr key={i}>
                    <td className="py-3 px-4"><Skeleton className="h-4 w-24" /></td>
                    <td className="py-3 px-4 text-right"><Skeleton className="h-4 w-12 ml-auto" /></td>
                    <td className="py-3 px-4 text-right"><Skeleton className="h-4 w-12 ml-auto" /></td>
                    <td className="py-3 px-4 text-right"><Skeleton className="h-4 w-20 ml-auto" /></td>
                    <td className="py-3 px-4 text-right"><Skeleton className="h-4 w-16 ml-auto" /></td>
                  </tr>
                ))
              ) : !data?.timeline || data.timeline.length === 0 ? (
                <tr>
                  <td colSpan={5} className="py-8 text-center text-muted-foreground text-sm">
                    Không có dữ liệu trong khoảng thời gian đã chọn
                  </td>
                </tr>
              ) : (
                data.timeline.map((item) => (
                  <tr key={item.date} className="hover:bg-muted/30 transition-colors">
                    <td className="py-3 px-4 font-medium text-foreground">
                      {formatDateDisplay(item.date)}
                    </td>
                    <td className="py-3 px-4 text-right">
                      <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-blue-50 dark:bg-blue-950/50 text-blue-700 dark:text-blue-300">
                        {item.paidCount.toLocaleString('vi-VN')}
                      </span>
                    </td>
                    <td className="py-3 px-4 text-right text-muted-foreground">
                      {item.completedCount.toLocaleString('vi-VN')}
                    </td>
                    <td className="py-3 px-4 text-right font-semibold text-emerald-600 dark:text-emerald-400">
                      {item.revenue.toLocaleString('vi-VN')} đ
                    </td>
                    <td className="py-3 px-4 text-right text-muted-foreground">
                      {Math.round(item.averageFee).toLocaleString('vi-VN')} đ
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </CardContent>
    </Card>
  );
}
