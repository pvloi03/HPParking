namespace HPParking.Api.DTOs.Statistics
{
    /// <summary>
    /// Bộ lọc thời gian cho Báo cáo Doanh thu &amp; Thống kê (ADR 0024, ADR 0043)
    /// </summary>
    public class RevenueStatisticsQuery
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
