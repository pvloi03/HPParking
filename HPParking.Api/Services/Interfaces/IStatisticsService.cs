using HPParking.Api.DTOs.Statistics;

namespace HPParking.Api.Services.Interfaces
{
    /// <summary>
    /// Giao diện dịch vụ báo cáo thống kê đa chiều và KPIs mới nhất (ADR 0024, ADR 0043)
    /// </summary>
    public interface IStatisticsService
    {
        /// <summary>
        /// Tổng hợp toàn bộ các chỉ số KPIs vận hành mới nhất cho trang Dashboard.
        /// Các chỉ số danh mục/hạ tầng phản ánh hiện trạng tức thời toàn cục; các chỉ số tài chính (TotalRevenue, PaidParkingSessionsCount)
        /// được lọc theo khoảng thời gian xuất bãi OutTime (mặc định là Hôm nay nếu fromDate và toDate đều là null) (ADR 0024, ADR 0043).
        /// </summary>
        Task<DashboardStatisticsDto> GetDashboardStatisticsAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Báo cáo thống kê doanh thu và lượt xe theo từng ngày (Timeline Zero-Fill) (ADR 0024, ADR 0043)
        /// </summary>
        Task<RevenueReportDto> GetRevenueReportAsync(
            RevenueStatisticsQuery query,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Báo cáo tổng hợp lưu lượng lượt ra vào theo từng người và phương tiện
        /// </summary>
        Task<List<TrafficSummaryItemDto>> GetTrafficSummaryAsync(TrafficSummaryFilterQuery query, CancellationToken cancellationToken = default);
    }
}
