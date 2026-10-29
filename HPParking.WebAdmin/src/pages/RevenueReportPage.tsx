import { useState, useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Download, RefreshCw, FileSpreadsheet } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { statisticsApi } from '@/api/statisticsApi';
import { downloadBlob } from '@/utils/downloadBlob';
import { toast } from '@/hooks/use-toast';
import { extractErrorMessage } from '@/api/clientApi';
import { cn } from '@/lib/utils';
import type { RevenueStatisticsQuery } from '@/types/statistics';
import {
  getLocalToday,
  getLocalPastDate,
  getLocalMonthStart,
  getLocalMonthEnd,
  toCompactDate,
} from '@/utils/dateUtils';
import {
  RevenueFilterBar,
  RevenueKpiGrid,
  RevenueBarChart,
  RevenueDailyTable,
  type DatePreset,
} from '@/components/revenue';

export function RevenueReportPage() {
  const today = getLocalToday();
  const [preset, setPreset] = useState<DatePreset>('today');
  const [fromDate, setFromDate] = useState<string>(today);
  const [toDate, setToDate] = useState<string>(today);
  const [isExporting, setIsExporting] = useState<boolean>(false);

  const handlePresetChange = (newPreset: DatePreset) => {
    setPreset(newPreset);
    const curToday = getLocalToday();
    if (newPreset === 'today') {
      setFromDate(curToday);
      setToDate(curToday);
    } else if (newPreset === 'last7days') {
      setFromDate(getLocalPastDate(6));
      setToDate(curToday);
    } else if (newPreset === 'thisMonth') {
      setFromDate(getLocalMonthStart());
      setToDate(getLocalMonthEnd());
    }
  };

  const handleFromDateChange = (val: string) => {
    setFromDate(val);
    setPreset('custom');
  };

  const handleToDateChange = (val: string) => {
    setToDate(val);
    setPreset('custom');
  };

  const queryParams: RevenueStatisticsQuery = useMemo(() => {
    return {
      fromDate: `${fromDate}T00:00:00`,
      toDate: `${toDate}T23:59:59`,
    };
  }, [fromDate, toDate]);

  const {
    data,
    isLoading,
    isRefetching,
    refetch,
  } = useQuery({
    queryKey: ['revenue-report', queryParams.fromDate, queryParams.toDate],
    queryFn: () => statisticsApi.getRevenueReport(queryParams),
    staleTime: 30 * 1000,
  });

  const handleExportExcel = async () => {
    setIsExporting(true);
    try {
      const blob = await statisticsApi.exportRevenueReportExcel(queryParams);
      // ADR 0023 (ngoại lệ date-range): tên theo khoảng ngày compact, khớp tên do backend sinh cho audit log
      const fileName = `Bao_Cao_Doanh_Thu_${toCompactDate(fromDate)}_${toCompactDate(toDate)}.xlsx`;
      downloadBlob(blob, fileName);

      if (blob.isTruncated) {
        toast.warning(
          'Dữ liệu chi tiết vượt quá 10.000 dòng. Hệ thống chỉ xuất tối đa 10.000 giao dịch đầu tiên.',
          { duration: 5000 }
        );
      } else {
        toast.success('Đã xuất báo cáo doanh thu thành công');
      }
    } catch (err) {
      toast.error(extractErrorMessage(err) || 'Xuất báo cáo doanh thu thất bại');
    } finally {
      setIsExporting(false);
    }
  };

  return (
    <div className="space-y-6">
      {/* Header bar: Title & Export / Refresh Actions */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-xl font-bold tracking-tight text-foreground flex items-center gap-2">
            <FileSpreadsheet className="h-5 w-5 text-emerald-600 dark:text-emerald-400" />
            Báo Cáo Doanh Thu
          </h1>
          <p className="text-xs text-muted-foreground mt-0.5">
            Thống kê doanh thu, lưu lượng thu phí và tổng hợp chi tiết theo mốc thời gian xuất bãi (OutTime)
          </p>
        </div>

        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => refetch()}
            disabled={isLoading || isRefetching}
            className="h-9 gap-1.5 text-xs font-medium"
          >
            <RefreshCw className={cn('h-3.5 w-3.5', (isLoading || isRefetching) && 'animate-spin')} />
            Làm mới
          </Button>

          <Button
            size="sm"
            onClick={handleExportExcel}
            disabled={isExporting || isLoading}
            className="h-9 gap-1.5 text-xs font-medium bg-emerald-600 hover:bg-emerald-700 text-white dark:bg-emerald-600 dark:hover:bg-emerald-700"
          >
            <Download className={cn('h-3.5 w-3.5', isExporting && 'animate-spin')} />
            {isExporting ? 'Đang xuất...' : 'Xuất Excel'}
          </Button>
        </div>
      </div>

      {/* Date Filter Bar */}
      <RevenueFilterBar
        preset={preset}
        fromDate={fromDate}
        toDate={toDate}
        onPresetChange={handlePresetChange}
        onFromDateChange={handleFromDateChange}
        onToDateChange={handleToDateChange}
      />

      {/* 4 KPI Cards */}
      <RevenueKpiGrid data={data} isLoading={isLoading} />

      {/* Recharts BarChart Card */}
      <RevenueBarChart
        timeline={data?.timeline}
        totalRevenue={data?.totalRevenue}
        fromDate={fromDate}
        toDate={toDate}
        isLoading={isLoading}
      />

      {/* Daily Breakdown Table */}
      <RevenueDailyTable data={data} isLoading={isLoading} />
    </div>
  );
}

export default RevenueReportPage;
