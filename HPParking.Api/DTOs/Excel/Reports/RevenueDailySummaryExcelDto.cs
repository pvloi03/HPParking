using HPParking.Api.Common.Excel;

namespace HPParking.Api.DTOs.Excel.Reports
{
    /// <summary>
    /// DTO đại diện cho một dòng dữ liệu trong Sheet 1 (Tổng hợp theo ngày) của Báo cáo Doanh thu (ADR 0023, ADR 0043)
    /// </summary>
    public class RevenueDailySummaryExcelDto
    {
        public string Date { get; set; } = string.Empty;
        public long PaidCount { get; set; }
        public long CompletedCount { get; set; }
        public decimal Revenue { get; set; }
        public decimal AverageFee { get; set; }
    }

    /// <summary>
    /// Cấu hình Fluent Profile cho Sheet 1 Báo cáo Doanh thu tổng hợp theo ngày (ADR 0023, ADR 0043)
    /// </summary>
    public class RevenueDailySummaryExcelProfile : ExcelProfile<RevenueDailySummaryExcelDto>
    {
        public RevenueDailySummaryExcelProfile()
        {
            Map(x => x.Date).ColumnName("Ngày").Order(1);
            Map(x => x.PaidCount).ColumnName("Số lượt thu tiền").Order(2).Format("#,##0");
            Map(x => x.CompletedCount).ColumnName("Tổng lượt ra").Order(3).Format("#,##0");
            Map(x => x.Revenue).ColumnName("Tổng doanh thu").Order(4).Format("#,##0");
            Map(x => x.AverageFee).ColumnName("Doanh thu TB").Order(5).Format("#,##0");
        }
    }
}
