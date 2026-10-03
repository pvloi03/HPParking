using Asp.Versioning;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.GateRoutes;
using HPParking.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HPParking.Api.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/gate-routes")]
    public class GateRoutesController : BaseApiController
    {
        private readonly IGateRouteService _gateRouteService;

        public GateRoutesController(
            IGateRouteService gateRouteService,
            ILogger<GateRoutesController> logger)
            : base(logger)
        {
            _gateRouteService = gateRouteService;
        }

        /// <summary>
        /// Lấy danh sách tuyến đường phương tiện nội bộ có phân trang và tìm kiếm
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<GateRouteDto>>), 200)]
        public async Task<IActionResult> GetRoutes(
            [FromQuery] PaginationQuery query,
            [FromQuery] string? search,
            [FromQuery] bool? isActive,
            CancellationToken cancellationToken)
        {
            var result = await _gateRouteService.GetRoutesPagedAsync(query, search, isActive, cancellationToken);
            return OkApiResponse(result, "Lấy danh sách tuyến đường thành công.");
        }

        /// <summary>
        /// Lấy thông tin chi tiết một tuyến đường
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<GateRouteDto>), 200)]
        public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
        {
            var result = await _gateRouteService.GetByIdAsync(id, cancellationToken);
            return OkApiResponse(result, "Lấy thông tin tuyến đường thành công.");
        }

        /// <summary>
        /// Tạo mới cấu hình tuyến đường đa cổng
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<GateRouteDto>), 201)]
        public async Task<IActionResult> Create([FromBody] CreateGateRouteRequest request, CancellationToken cancellationToken)
        {
            var result = await _gateRouteService.CreateAsync(request, cancellationToken);
            return CreatedApiResponse(result, "Tạo mới tuyến đường thành công.");
        }

        /// <summary>
        /// Cập nhật cấu hình tuyến đường đa cổng
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<GateRouteDto>), 200)]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateGateRouteRequest request, CancellationToken cancellationToken)
        {
            var result = await _gateRouteService.UpdateAsync(id, request, cancellationToken);
            return OkApiResponse(result, "Cập nhật tuyến đường thành công.");
        }

        /// <summary>
        /// Xóa tuyến đường (Xóa mềm, có kiểm tra chặn xóa nếu còn xe gán tuyến)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
        {
            await _gateRouteService.DeleteAsync(id, cancellationToken);
            return OkApiResponse(new { id }, "Xóa tuyến đường thành công.");
        }
    }
}
