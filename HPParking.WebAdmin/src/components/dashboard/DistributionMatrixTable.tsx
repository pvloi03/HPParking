import { Building, Users, Car, DoorOpen, Route } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import type { DistributionStatisticsDto } from '@/types/statistics';

interface DistributionMatrixTableProps {
  data?: DistributionStatisticsDto;
  isLoading?: boolean;
}

export function DistributionMatrixTable({
  data,
  isLoading,
}: DistributionMatrixTableProps) {
  const items = data?.items ?? [];

  return (
    <Card className="border-border/80 shadow-xs">
      <CardHeader className="pb-3 sm:flex-row sm:items-center sm:justify-between space-y-2 sm:space-y-0">
        <div>
          <CardTitle className="text-sm font-bold text-foreground flex items-center gap-2">
            <Building className="h-4 w-4 text-blue-600 dark:text-blue-400" />
            Ma Trận Phân Bổ Hạ Tầng & Đơn Vị
          </CardTitle>
          <CardDescription className="text-xs text-muted-foreground mt-0.5">
            Thống kê cư dân, phương tiện và lưu lượng cổng làn theo từng Công ty & Phòng ban
          </CardDescription>
        </div>

        {data && (
          <div className="flex items-center gap-3 text-xs text-muted-foreground bg-muted/40 px-3 py-1.5 rounded-lg border border-border/50">
            <span>
              Tổng đơn vị: <strong className="text-foreground">{items.length}</strong>
            </span>
            <span>•</span>
            <span>
              Tổng cư dân:{' '}
              <strong className="text-foreground">
                {data.totalFilteredClients.toLocaleString()}
              </strong>
            </span>
          </div>
        )}
      </CardHeader>

      <CardContent className="p-0">
        {isLoading ? (
          <div className="p-4 space-y-3">
            {Array.from({ length: 4 }).map((_, i) => (
              <div key={i} className="flex items-center gap-4">
                <Skeleton className="h-4 w-1/4" />
                <Skeleton className="h-4 w-1/4" />
                <Skeleton className="h-4 w-1/6" />
                <Skeleton className="h-4 w-1/6" />
                <Skeleton className="h-4 w-1/6" />
              </div>
            ))}
          </div>
        ) : items.length === 0 ? (
          <div className="py-12 text-center text-xs text-muted-foreground">
            Chưa có dữ liệu phân bổ theo đơn vị tổ chức.
          </div>
        ) : (
          <>
            {/* Mobile Stacked Cards View (< 640px) */}
            <div className="block sm:hidden divide-y divide-border">
              {items.map((item, idx) => (
                <div key={idx} className="p-4 space-y-2.5 hover:bg-muted/20 transition-colors">
                  <div>
                    <div className="font-semibold text-xs text-foreground">
                      {item.companyName || 'Công ty chung'}
                    </div>
                    <div className="text-[11px] text-muted-foreground">
                      Phòng ban: {item.departmentName || 'Toàn công ty'}
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-2 text-xs pt-1 border-t border-border/40">
                    <div className="flex items-center gap-1.5">
                      <Users className="h-3.5 w-3.5 text-blue-500 shrink-0" />
                      <span className="text-[11px] text-muted-foreground">Khách:</span>
                      <strong className="text-foreground font-mono">
                        {item.clientCount.toLocaleString()}
                      </strong>
                    </div>

                    <div className="flex items-center gap-1.5">
                      <Car className="h-3.5 w-3.5 text-emerald-500 shrink-0" />
                      <span className="text-[11px] text-muted-foreground">Xe:</span>
                      <strong className="text-foreground font-mono">
                        {item.vehicleCount.toLocaleString()}
                      </strong>
                    </div>

                    <div className="flex items-center gap-1.5">
                      <DoorOpen className="h-3.5 w-3.5 text-indigo-500 shrink-0" />
                      <span className="text-[11px] text-muted-foreground">Cổng:</span>
                      <strong className="text-foreground font-mono">
                        {item.gateCount.toLocaleString()}
                      </strong>
                    </div>

                    <div className="flex items-center gap-1.5">
                      <Route className="h-3.5 w-3.5 text-purple-500 shrink-0" />
                      <span className="text-[11px] text-muted-foreground">Làn:</span>
                      <strong className="text-foreground font-mono">
                        {item.laneCount.toLocaleString()}
                      </strong>
                    </div>
                  </div>
                </div>
              ))}
            </div>

            {/* Desktop & Tablet Table View (>= 640px) */}
            <div className="hidden sm:block overflow-x-auto">
              <table className="w-full text-left border-collapse text-xs">
                <thead>
                  <tr className="border-y border-border bg-muted/40 text-[11px] uppercase tracking-wider text-muted-foreground font-semibold">
                    <th className="py-2.5 px-4">Công Ty</th>
                    <th className="py-2.5 px-4">Phòng Ban</th>
                    <th className="py-2.5 px-4 text-center">Số Cư Dân</th>
                    <th className="py-2.5 px-4 text-center">Phương Tiện</th>
                    <th className="py-2.5 px-4 text-center">Cổng Gán</th>
                    <th className="py-2.5 px-4 text-center">Làn Phụ Trách</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {items.map((item, idx) => (
                    <tr
                      key={idx}
                      className="hover:bg-muted/30 transition-colors"
                    >
                      <td className="py-3 px-4 font-medium text-foreground">
                        {item.companyName || '—'}
                      </td>
                      <td className="py-3 px-4 text-muted-foreground">
                        {item.departmentName || 'Toàn công ty'}
                      </td>
                      <td className="py-3 px-4 text-center font-mono font-semibold text-foreground">
                        {item.clientCount.toLocaleString()}
                      </td>
                      <td className="py-3 px-4 text-center font-mono text-muted-foreground">
                        {item.vehicleCount.toLocaleString()}
                      </td>
                      <td className="py-3 px-4 text-center font-mono text-muted-foreground">
                        {item.gateCount.toLocaleString()}
                      </td>
                      <td className="py-3 px-4 text-center font-mono text-muted-foreground">
                        {item.laneCount.toLocaleString()}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </>
        )}
      </CardContent>
    </Card>
  );
}
