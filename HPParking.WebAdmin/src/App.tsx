import { useState } from 'react';
import { AppLayout } from '@/components/layout/AppLayout';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import {
  Car,
  ArrowDownRight,
  ArrowUpRight,
  AlertTriangle,
  RefreshCw,
} from 'lucide-react';

const mockRecentSessions = [
  {
    id: 'SES-9081',
    plateNumber: '29A-888.99',
    vehicleType: 'Ô tô 4 chỗ',
    laneIn: 'Làn Vào 01 (Cổng Chính)',
    timeIn: '10:02:15 22/09/2026',
    timeOut: '—',
    duration: '6 phút',
    status: 'Đang đỗ',
    mismatch: false,
  },
  {
    id: 'SES-9080',
    plateNumber: '51F-123.45',
    vehicleType: 'Ô tô 7 chỗ',
    laneIn: 'Làn Vào 02 (Cổng Phụ)',
    timeIn: '09:45:10 22/09/2026',
    timeOut: '—',
    duration: '23 phút',
    status: 'Đang đỗ',
    mismatch: false,
  },
  {
    id: 'SES-9079',
    plateNumber: '30E-654.32',
    vehicleType: 'Xe máy',
    laneIn: 'Làn Vào 03 (Xe Máy)',
    timeIn: '08:15:00 22/09/2026',
    timeOut: '10:05:40 22/09/2026',
    duration: '1 giờ 50 phút',
    status: 'Hoàn tất',
    mismatch: false,
  },
  {
    id: 'SES-9078',
    plateNumber: '15A-999.88',
    vehicleType: 'Xe bán tải',
    laneIn: 'Làn Vào 01 (Cổng Chính)',
    timeIn: '07:30:22 22/09/2026',
    timeOut: '09:55:12 22/09/2026',
    duration: '2 giờ 24 phút',
    status: 'Hoàn tất',
    mismatch: true,
  },
];

export default function App() {
  const [period, setPeriod] = useState<'today' | 'week' | 'month'>('today');
  const [isRefreshing, setIsRefreshing] = useState(false);

  const handleRefresh = () => {
    setIsRefreshing(true);
    setTimeout(() => setIsRefreshing(false), 600);
  };

  return (
    <AppLayout>
      <div className="space-y-6">
        {/* Top Control Bar: Period Filter & Refresh */}
        <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
          <div>
            <h1 className="text-xl font-bold tracking-tight text-foreground">
              Tổng Quan Hoạt Động Bãi Xe
            </h1>
            <p className="text-xs text-muted-foreground mt-0.5">
              Dữ liệu giám sát tự động cập nhật thời gian thực từ các đầu đọc làn xe
            </p>
          </div>

          <div className="flex items-center gap-2">
            {/* Period Selector Tabs */}
            <div className="flex items-center rounded-lg border border-border bg-card p-1 text-xs shadow-xs">
              <button
                type="button"
                onClick={() => setPeriod('today')}
                className={`px-3 py-1 rounded-md font-medium transition-colors cursor-pointer ${
                  period === 'today'
                    ? 'bg-blue-600 text-white shadow-xs'
                    : 'text-muted-foreground hover:text-foreground'
                }`}
              >
                Hôm Nay
              </button>
              <button
                type="button"
                onClick={() => setPeriod('week')}
                className={`px-3 py-1 rounded-md font-medium transition-colors cursor-pointer ${
                  period === 'week'
                    ? 'bg-blue-600 text-white shadow-xs'
                    : 'text-muted-foreground hover:text-foreground'
                }`}
              >
                Tuần Này
              </button>
              <button
                type="button"
                onClick={() => setPeriod('month')}
                className={`px-3 py-1 rounded-md font-medium transition-colors cursor-pointer ${
                  period === 'month'
                    ? 'bg-blue-600 text-white shadow-xs'
                    : 'text-muted-foreground hover:text-foreground'
                }`}
              >
                Tháng Này
              </button>
            </div>

            <Button
              variant="outline"
              size="sm"
              onClick={handleRefresh}
              className="h-8 gap-1.5 text-xs cursor-pointer"
            >
              <RefreshCw
                className={`h-3.5 w-3.5 ${isRefreshing ? 'animate-spin' : ''}`}
              />
              <span className="hidden sm:inline">Làm mới</span>
            </Button>
          </div>
        </div>

        {/* 4 Primary KPI Cards */}
        <div className="grid gap-4 grid-cols-1 sm:grid-cols-2 lg:grid-cols-4">
          {/* KPI 1: Xe đang đỗ */}
          <Card className="hover:shadow-md transition-shadow">
            <CardContent className="p-5">
              <div className="flex items-center justify-between">
                <span className="text-xs font-semibold text-muted-foreground">
                  Xe Đang Đỗ Trong Bãi
                </span>
                <div className="h-9 w-9 rounded-xl bg-blue-50 dark:bg-blue-950/50 text-blue-600 dark:text-blue-400 flex items-center justify-center">
                  <Car className="h-4.5 w-4.5" />
                </div>
              </div>
              <div className="mt-3">
                <div className="text-2xl font-bold tracking-tight text-foreground">
                  142
                </div>
                <div className="flex items-center gap-1.5 text-xs text-emerald-600 mt-1">
                  <ArrowUpRight className="h-3.5 w-3.5" />
                  <span>Sức chứa: 142 / 200 chỗ (71%)</span>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* KPI 2: Lượt vào */}
          <Card className="hover:shadow-md transition-shadow">
            <CardContent className="p-5">
              <div className="flex items-center justify-between">
                <span className="text-xs font-semibold text-muted-foreground">
                  Lượt Xe Vào Hôm Nay
                </span>
                <div className="h-9 w-9 rounded-xl bg-emerald-50 dark:bg-emerald-950/50 text-emerald-600 dark:text-emerald-400 flex items-center justify-center">
                  <ArrowDownRight className="h-4.5 w-4.5" />
                </div>
              </div>
              <div className="mt-3">
                <div className="text-2xl font-bold tracking-tight text-foreground">
                  584
                </div>
                <div className="flex items-center gap-1.5 text-xs text-muted-foreground mt-1">
                  <span>Cao điểm: 07:30 - 08:30</span>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* KPI 3: Lượt ra */}
          <Card className="hover:shadow-md transition-shadow">
            <CardContent className="p-5">
              <div className="flex items-center justify-between">
                <span className="text-xs font-semibold text-muted-foreground">
                  Lượt Xe Ra Hôm Nay
                </span>
                <div className="h-9 w-9 rounded-xl bg-indigo-50 dark:bg-indigo-950/50 text-indigo-600 dark:text-indigo-400 flex items-center justify-center">
                  <ArrowUpRight className="h-4.5 w-4.5" />
                </div>
              </div>
              <div className="mt-3">
                <div className="text-2xl font-bold tracking-tight text-foreground">
                  442
                </div>
                <div className="flex items-center gap-1.5 text-xs text-muted-foreground mt-1">
                  <span>Trung bình: 48 lượt/giờ</span>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* KPI 4: Cảnh báo lệch biển số */}
          <Card className="hover:shadow-md transition-shadow">
            <CardContent className="p-5">
              <div className="flex items-center justify-between">
                <span className="text-xs font-semibold text-muted-foreground">
                  Cảnh Báo Lệch Biển Số
                </span>
                <div className="h-9 w-9 rounded-xl bg-amber-50 dark:bg-amber-950/50 text-amber-600 dark:text-amber-400 flex items-center justify-center">
                  <AlertTriangle className="h-4.5 w-4.5" />
                </div>
              </div>
              <div className="mt-3">
                <div className="text-2xl font-bold tracking-tight text-foreground">
                  2
                </div>
                <div className="flex items-center gap-1.5 text-xs text-amber-600 mt-1">
                  <span>Cần bảo vệ kiểm tra đối soát</span>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Lane Status Monitoring Bar */}
        <Card>
          <CardHeader className="pb-3">
            <div className="flex items-center justify-between">
              <div>
                <CardTitle className="text-sm font-bold">
                  Trạng Thái Làn Xe & Cổng Kiểm Soát
                </CardTitle>
                <CardDescription className="text-xs">
                  Giám sát tình trạng kết nối thiết bị ngoại vi tại các làn xe
                </CardDescription>
              </div>
              <Badge variant="outline" className="text-xs gap-1.5">
                <span className="h-2 w-2 rounded-full bg-emerald-500" />
                8/8 Làn Trực Tuyến
              </Badge>
            </div>
          </CardHeader>
          <CardContent>
            <div className="grid gap-3 grid-cols-2 sm:grid-cols-4 lg:grid-cols-8">
              {[
                { name: 'Làn Vào 01', type: 'Vào', gate: 'Cổng 1', active: true },
                { name: 'Làn Ra 01', type: 'Ra', gate: 'Cổng 1', active: true },
                { name: 'Làn Vào 02', type: 'Vào', gate: 'Cổng 1', active: true },
                { name: 'Làn Ra 02', type: 'Ra', gate: 'Cổng 1', active: true },
                { name: 'Làn Vào 03', type: 'Vào', gate: 'Cổng 2', active: true },
                { name: 'Làn Ra 03', type: 'Ra', gate: 'Cổng 2', active: true },
                { name: 'Làn Xe Máy 01', type: 'Vào', gate: 'Cổng 3', active: true },
                { name: 'Làn Xe Máy 02', type: 'Ra', gate: 'Cổng 3', active: true },
              ].map((lane, index) => (
                <div
                  key={index}
                  className="rounded-lg border border-border p-3 bg-muted/20 hover:bg-muted/40 transition-colors text-center space-y-1"
                >
                  <div className="flex items-center justify-center gap-1.5">
                    <span className="h-1.5 w-1.5 rounded-full bg-emerald-500 animate-pulse" />
                    <span className="text-[11px] font-bold text-foreground">
                      {lane.name}
                    </span>
                  </div>
                  <div className="text-[10px] text-muted-foreground">
                    {lane.gate} • {lane.type}
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>

        {/* Recent Parking Activity Table */}
        <Card>
          <CardHeader className="pb-3">
            <div className="flex items-center justify-between">
              <div>
                <CardTitle className="text-sm font-bold">
                  Hoạt Động Ra Vào Gần Nhất
                </CardTitle>
                <CardDescription className="text-xs">
                  Nhật ký phương tiện vừa quét thẻ / nhận diện biển số tại các làn
                </CardDescription>
              </div>
              <Button variant="ghost" size="sm" className="text-xs text-blue-600 dark:text-blue-400">
                Xem tất cả lịch sử →
              </Button>
            </div>
          </CardHeader>
          <CardContent className="p-0">
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse">
                <thead>
                  <tr className="border-y border-border bg-muted/40 text-[11px] uppercase tracking-wider text-muted-foreground font-semibold">
                    <th className="py-2.5 px-4">Biển Số Xe</th>
                    <th className="py-2.5 px-4">Loại Xe</th>
                    <th className="py-2.5 px-4">Làn Vào</th>
                    <th className="py-2.5 px-4">Thời Điểm Vào</th>
                    <th className="py-2.5 px-4">Thời Lượng</th>
                    <th className="py-2.5 px-4">Trạng Thái</th>
                    <th className="py-2.5 px-4 text-right">Hành Động</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border text-xs">
                  {mockRecentSessions.map((session) => (
                    <tr
                      key={session.id}
                      className="hover:bg-muted/30 transition-colors"
                    >
                      <td className="py-3 px-4 font-mono font-bold text-foreground">
                        {session.plateNumber}
                      </td>
                      <td className="py-3 px-4 text-muted-foreground">
                        {session.vehicleType}
                      </td>
                      <td className="py-3 px-4 text-muted-foreground">
                        {session.laneIn}
                      </td>
                      <td className="py-3 px-4 font-mono text-muted-foreground">
                        {session.timeIn}
                      </td>
                      <td className="py-3 px-4 text-muted-foreground">
                        {session.duration}
                      </td>
                      <td className="py-3 px-4">
                        {session.mismatch ? (
                          <Badge variant="warning" className="text-[10px]">
                            Lệch biển số
                          </Badge>
                        ) : session.status === 'Đang đỗ' ? (
                          <Badge variant="success" className="text-[10px]">
                            Đang đỗ
                          </Badge>
                        ) : (
                          <Badge variant="secondary" className="text-[10px]">
                            Hoàn tất
                          </Badge>
                        )}
                      </td>
                      <td className="py-3 px-4 text-right">
                        <button
                          type="button"
                          className="text-xs font-semibold text-blue-600 dark:text-blue-400 hover:underline cursor-pointer"
                        >
                          Chi tiết ảnh
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </CardContent>
        </Card>
      </div>
    </AppLayout>
  );
}
