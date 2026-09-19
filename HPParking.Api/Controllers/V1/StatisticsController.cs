using System.Threading.Tasks;
using Asp.Versioning;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Statistics;
using HPParking.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
        /// Tổng hợp toàn bộ các chỉ số KPIs vận hành thời gian thực cho trang Dashboard
        /// </summary>
        [HttpGet("dashboard")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<DashboardStatisticsDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        public async Task<IActionResult> GetDashboardStatistics()
        {
            var result = await _statisticsService.GetDashboardStatisticsAsync();
            return OkApiResponse(result, "Lấy số liệu thống kê Dashboard thành công.");
        }

        /// <summary>
        /// Báo cáo thống kê số lượng khách hàng và phương tiện phân bổ theo đơn vị tổ chức
        /// </summary>
        [HttpGet("distribution")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<DistributionStatisticsDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        public async Task<IActionResult> GetDistributionStatistics([FromQuery] DistributionFilterQuery query)
        {
            var result = await _statisticsService.GetDistributionStatisticsAsync(query);
            return OkApiResponse(result, "Lấy báo cáo phân bổ thống kê thành công.");
        }
    }
}
