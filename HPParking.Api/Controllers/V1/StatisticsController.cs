using Asp.Versioning;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Statistics;
using HPParking.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HPParking.Api.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/statistics")]
    public class StatisticsController : BaseApiController
    {
        private readonly IStatisticsService _statisticsService;

        public StatisticsController(
            IStatisticsService statisticsService,
            ILogger<StatisticsController> logger)
            : base(logger)
        {
            _statisticsService = statisticsService;
        }

        /// <summary>
        /// Tổng hợp toàn bộ các chỉ số KPIs vận hành mới nhất cho trang Dashboard (ADR 0024, ADR 0043).
        /// Chỉ số danh mục/hạ tầng phản ánh toàn cục tức thời; chỉ số tài chính (TotalRevenue, PaidParkingSessionsCount)
        /// lọc theo thời điểm ra OutTime (mặc định trọn vẹn Hôm nay nếu không truyền fromDate/toDate).
        /// </summary>
        /// <param name="fromDate">Thời điểm bắt đầu (chuỗi ISO Local Date-Time ...T00:00:00, mặc định đầu ngày hôm nay nếu null)</param>
        /// <param name="toDate">Thời điểm kết thúc (chuỗi ISO Local Date-Time ...T23:59:59, mặc định cuối ngày hôm nay nếu null)</param>
        [HttpGet("dashboard")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<DashboardStatisticsDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        public async Task<IActionResult> GetDashboardStatistics(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var result = await _statisticsService.GetDashboardStatisticsAsync(fromDate, toDate, HttpContext.RequestAborted);
            return OkApiResponse(result, "Lấy số liệu thống kê Dashboard thành công.");
        }

        /// <summary>
        /// Báo cáo thống kê doanh thu và lượt xe theo từng ngày (Timeline Zero-Fill) (ADR 0024, ADR 0043)
        /// </summary>
        [HttpGet("revenue")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<RevenueReportDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        public async Task<IActionResult> GetRevenueReport([FromQuery] RevenueStatisticsQuery query)
        {
            var result = await _statisticsService.GetRevenueReportAsync(query ?? new RevenueStatisticsQuery(), HttpContext.RequestAborted);
            return OkApiResponse(result, "Lấy báo cáo doanh thu thành công.");
        }
    }
}
