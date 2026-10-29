using System;
using System.Collections.Generic;

namespace HPParking.Api.DTOs.Statistics
{
    /// <summary>
    /// Báo cáo Doanh thu tổng hợp theo kỳ lọc (ADR 0024, ADR 0043)
    /// </summary>
    public class RevenueReportDto
    {
        public decimal TotalRevenue { get; set; }
        public long PaidSessionsCount { get; set; }
        public long CompletedSessionsCount { get; set; }
        public decimal AverageFeePerSession { get; set; }
        public double PaidRatio { get; set; }
        public List<RevenueDailyItemDto> Timeline { get; set; } = new();
    }

    /// <summary>
    /// Dữ liệu thống kê doanh thu và lượt xe theo từng ngày (Timeline).
    /// Đóng gói DateOnly chuẩn định dạng ngày, loại bỏ Primitive Obsession (chuỗi yyyy-MM-dd) và tránh phụ thuộc văn hóa khi định dạng.
    /// </summary>
    public class RevenueDailyItemDto
    {
        public DateOnly Date { get; set; }
        public decimal Revenue { get; set; }
        public int PaidCount { get; set; }
        public int CompletedCount { get; set; }
        public decimal AverageFee { get; set; }
    }
}
