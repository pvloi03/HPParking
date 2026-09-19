using System.Threading.Tasks;
using Asp.Versioning;
using HPParking.Api.DTOs.AuditLogs;
using HPParking.Api.DTOs.Common;
using HPParking.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HPParking.Api.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/audit-logs")]
    public class AuditLogsController : BaseApiController
    {
        private readonly IAuditLogService _auditLogService;

        public AuditLogsController(
            IAuditLogService auditLogService,
            ILogger<AuditLogsController> logger)
            : base(logger)
        {
            _auditLogService = auditLogService;
        }

        /// <summary>
        /// Tra cứu danh sách nhật ký kiểm toán hệ thống (Chỉ dành riêng cho vai trò Admin)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<AuditLogDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        public async Task<IActionResult> GetAuditLogs([FromQuery] AuditLogFilterQuery query)
        {
            var result = await _auditLogService.GetAuditLogsPagedAsync(query);
            return OkApiResponse(result, "Tra cứu danh sách nhật ký kiểm toán thành công.");
        }

        /// <summary>
        /// Xem chi tiết một bản ghi nhật ký kiểm toán kèm chuỗi diff thay đổi dữ liệu
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<AuditLogDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetAuditLogById(string id)
        {
            var result = await _auditLogService.GetAuditLogByIdAsync(id);
            return OkApiResponse(result, "Lấy chi tiết nhật ký kiểm toán thành công.");
        }
    }
}
