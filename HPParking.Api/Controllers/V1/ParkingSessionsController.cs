using Asp.Versioning;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.ParkingSessions;
using HPParking.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HPParking.Api.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/parking-sessions")]
    public class ParkingSessionsController : BaseApiController
    {
        private readonly IParkingSessionService _parkingSessionService;

        public ParkingSessionsController(
            IParkingSessionService parkingSessionService,
            ILogger<ParkingSessionsController> logger)
            : base(logger)
        {
            _parkingSessionService = parkingSessionService;
        }

        /// <summary>
        /// Tra cứu danh sách phiên đỗ xe có phân trang và bộ lọc đa tiêu chí
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<ParkingSessionDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        public async Task<IActionResult> GetParkingSessions([FromQuery] ParkingSessionFilterQuery query, CancellationToken cancellationToken)
        {
            var result = await _parkingSessionService.GetParkingSessionsPagedAsync(query, cancellationToken);
            return OkApiResponse(result, "Tra cứu danh sách phiên đỗ xe thành công.");
        }

        /// <summary>
        /// Lấy chi tiết phiên đỗ xe theo Id kèm ảnh bằng chứng và thời lượng đỗ xe
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<ParkingSessionDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetParkingSessionById(string id, CancellationToken cancellationToken)
        {
            var result = await _parkingSessionService.GetParkingSessionByIdAsync(id, cancellationToken);
            return OkApiResponse(result, "Lấy chi tiết phiên đỗ xe thành công.");
        }

        /// <summary>
        /// Đóng phiên đỗ xe cưỡng bức do mất thẻ hoặc xử lý sự cố (Manager, Admin)
        /// </summary>
        [HttpPost("{id}/force-close")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<ParkingSessionDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> ForceClose(
            string id,
            [FromBody] ForceCloseParkingSessionRequest request,
            CancellationToken cancellationToken)
        {
            var username = User.Identity?.Name ?? "Unknown";
            var result = await _parkingSessionService.ForceCloseSessionAsync(id, request, username, cancellationToken);
            return OkApiResponse(result, "Đóng phiên đỗ xe cưỡng bức thành công.");
        }
    }
}
