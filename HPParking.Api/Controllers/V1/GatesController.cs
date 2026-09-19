using System.Threading.Tasks;
using Asp.Versioning;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Gates;
using HPParking.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HPParking.Api.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class GatesController : BaseApiController
    {
        private readonly IGateService _gateService;

        public GatesController(
            IGateService gateService,
            ILogger<GatesController> logger)
            : base(logger)
        {
            _gateService = gateService;
        }

        /// <summary>
        /// Lấy danh sách cổng kiểm soát có phân trang và lọc theo công ty, trạng thái, từ khóa
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<GateDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        public async Task<IActionResult> GetGates([FromQuery] GateFilterQuery query)
        {
            var result = await _gateService.GetGatesPagedAsync(query);
            return OkApiResponse(result, "Lấy danh sách cổng kiểm soát thành công.");
        }

        /// <summary>
        /// Xem chi tiết thông tin một cổng kiểm soát theo Id
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<GateDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetGateById(string id)
        {
            var gate = await _gateService.GetGateByIdAsync(id);
            return OkApiResponse(gate, "Lấy thông tin cổng kiểm soát thành công.");
        }

        /// <summary>
        /// Tạo mới cổng kiểm soát liên kết với công ty và mã trạm bốt bảo vệ (MachineCode)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<GateDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> CreateGate([FromBody] CreateGateRequest request)
        {
            var created = await _gateService.CreateGateAsync(request);
            return CreatedApiResponse($"/api/v1/gates/{created.Id}", created, "Tạo mới cổng kiểm soát thành công.");
        }

        /// <summary>
        /// Cập nhật thông tin cổng kiểm soát, bảo vệ trạng thái hoạt động (Active State Protection)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<GateDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> UpdateGate(string id, [FromBody] UpdateGateRequest request)
        {
            var updated = await _gateService.UpdateGateAsync(id, request);
            return OkApiResponse(updated, "Cập nhật thông tin cổng kiểm soát thành công.");
        }

        /// <summary>
        /// Xóa cổng kiểm soát (Chặn xóa nếu còn làn xe trực thuộc theo ADR 0030 &amp; ADR 0034)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> DeleteGate(string id, [FromQuery] bool hardDelete = false)
        {
            await _gateService.DeleteGateAsync(id, hardDelete);
            var msg = hardDelete
                ? "Đã xóa vĩnh viễn cổng kiểm soát khỏi cơ sở dữ liệu."
                : "Đã xóa mềm cổng kiểm soát thành công.";

            return OkApiResponse(true, msg);
        }

        /// <summary>
        /// Khôi phục cổng kiểm soát từ thùng rác (ADR 0031 Recycle Bin &amp; Restore)
        /// </summary>
        [HttpPost("{id}/restore")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<GateDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> RestoreGate(string id)
        {
            var restored = await _gateService.RestoreGateAsync(id);
            return OkApiResponse(restored, "Khôi phục cổng kiểm soát thành công.");
        }
    }
}
