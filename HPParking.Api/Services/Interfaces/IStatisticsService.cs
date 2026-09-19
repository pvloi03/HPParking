using System.Threading;
using System.Threading.Tasks;
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
        /// Báo cáo thống kê phân bổ số lượng khách hàng và phương tiện theo đơn vị tổ chức
        /// </summary>
        Task<DistributionStatisticsDto> GetDistributionStatisticsAsync(DistributionFilterQuery query, CancellationToken cancellationToken = default);
    }
}
