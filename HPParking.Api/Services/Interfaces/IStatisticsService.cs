using HPParking.Api.DTOs.Statistics;

namespace HPParking.Api.Services.Interfaces
{
    /// <summary>
    /// Giao diện dịch vụ báo cáo thống kê đa chiều và KPIs mới nhất
    /// </summary>
    public interface IStatisticsService
    {
        /// <summary>
        /// Tổng hợp toàn bộ các chỉ số KPIs vận hành mới nhất cho trang Dashboard
        /// </summary>
        Task<DashboardStatisticsDto> GetDashboardStatisticsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Báo cáo tổng hợp lưu lượng lượt ra vào theo từng người và phương tiện
        /// </summary>
        Task<List<TrafficSummaryItemDto>> GetTrafficSummaryAsync(TrafficSummaryFilterQuery query, CancellationToken cancellationToken = default);
    }
}
