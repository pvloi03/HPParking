using Asp.Versioning;
using HPParking.Api.DTOs.AuditLogs;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.ParkingSessions;
using HPParking.Api.DTOs.Statistics;
using HPParking.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HPParking.Api.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/excel/reports")]
    [Route("api/v{version:apiVersion}/excel")]
    public class ReportsExcelController : BaseApiController
    {
        private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private readonly IReportExcelService _reportExcelService;

        public ReportsExcelController(
            IReportExcelService reportExcelService,
            ILogger<ReportsExcelController> logger)
            : base(logger)
        {
            _reportExcelService = reportExcelService;
        }

        // =========================================================================
        // --- 1. XUẤT LỊCH SỬ PHIÊN ĐỖ XE (PARKING SESSIONS) ---
        // =========================================================================

        /// <summary>
        /// Xuất lịch sử phiên đỗ xe ra tệp Excel (POST kèm tiêu chí lọc nâng cao)
        /// </summary>
        [HttpPost("parking-sessions/export")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        public async Task<IActionResult> ExportParkingSessionsPost([FromBody] ParkingSessionFilterQuery query)
        {
            var (content, fileName, isTruncated) = await _reportExcelService.ExportParkingSessionsAsync(query ?? new ParkingSessionFilterQuery());
            if (isTruncated)
            {
                Response.Headers.Append("X-Export-Truncated", "true");
            }
            return File(content, ExcelContentType, fileName);
        }

        /// <summary>
        /// Xuất lịch sử phiên đỗ xe ra tệp Excel (GET tải trực tiếp từ trình duyệt)
        /// </summary>
        [HttpGet("parking-sessions/export")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        public async Task<IActionResult> ExportParkingSessionsGet([FromQuery] ParkingSessionFilterQuery query)
        {
            var (content, fileName, isTruncated) = await _reportExcelService.ExportParkingSessionsAsync(query);
            if (isTruncated)
            {
                Response.Headers.Append("X-Export-Truncated", "true");
            }
            return File(content, ExcelContentType, fileName);
        }

        // =========================================================================
        // --- 2. XUẤT NHẬT KÝ KIỂM TOÁN HỆ THỐNG (AUDIT LOGS - CHỈ DÀNH CHO ADMIN) ---
        // =========================================================================

        /// <summary>
        /// Xuất nhật ký kiểm toán hệ thống ra tệp Excel (POST - Phân quyền duy nhất Admin)
        /// </summary>
        [HttpPost("audit-logs/export")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        public async Task<IActionResult> ExportAuditLogsPost([FromBody] AuditLogFilterQuery query)
        {
            var (content, fileName, isTruncated) = await _reportExcelService.ExportAuditLogsAsync(query ?? new AuditLogFilterQuery());
            if (isTruncated)
            {
                Response.Headers.Append("X-Export-Truncated", "true");
            }
            return File(content, ExcelContentType, fileName);
        }

        /// <summary>
        /// Xuất nhật ký kiểm toán hệ thống ra tệp Excel (GET - Phân quyền duy nhất Admin)
        /// </summary>
        [HttpGet("audit-logs/export")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        public async Task<IActionResult> ExportAuditLogsGet([FromQuery] AuditLogFilterQuery query)
        {
            var (content, fileName, isTruncated) = await _reportExcelService.ExportAuditLogsAsync(query);
            if (isTruncated)
            {
                Response.Headers.Append("X-Export-Truncated", "true");
            }
            return File(content, ExcelContentType, fileName);
        }

        // =========================================================================
        // --- 3. XUẤT BÁO CÁO TỔNG HỢP LƯỢT RA VÀO (TRAFFIC SUMMARY) ---
        // =========================================================================

        /// <summary>
        /// Xuất báo cáo tổng hợp lưu lượng lượt ra vào theo từng người và phương tiện ra Excel (POST kèm bộ lọc)
        /// </summary>
        [HttpPost("traffic-summary/export")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        public async Task<IActionResult> ExportTrafficSummaryPost([FromBody] TrafficSummaryFilterQuery query)
        {
            var (content, fileName, isTruncated) = await _reportExcelService.ExportTrafficSummaryAsync(query ?? new TrafficSummaryFilterQuery());
            if (isTruncated)
            {
                Response.Headers.Append("X-Export-Truncated", "true");
            }
            return File(content, ExcelContentType, fileName);
        }

        /// <summary>
        /// Xuất báo cáo tổng hợp lưu lượng lượt ra vào theo từng người và phương tiện ra Excel (GET tải trực tiếp)
        /// </summary>
        [HttpGet("traffic-summary/export")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        public async Task<IActionResult> ExportTrafficSummaryGet([FromQuery] TrafficSummaryFilterQuery query)
        {
            var (content, fileName, isTruncated) = await _reportExcelService.ExportTrafficSummaryAsync(query);
            if (isTruncated)
            {
                Response.Headers.Append("X-Export-Truncated", "true");
            }
            return File(content, ExcelContentType, fileName);
        }
    }
}
