using System.Threading.Tasks;
using Asp.Versioning;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Lanes;
using HPParking.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HPParking.Api.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class LanesController : BaseApiController
    {
        private readonly ILaneService _laneService;

        public LanesController(
            ILaneService laneService,
            ILogger<LanesController> logger)
            : base(logger)
        {
            _laneService = laneService;
        }

        /// <summary>
        /// Lấy danh sách làn xe có phân trang và lọc theo cổng, hướng di chuyển, trạng thái, từ khóa
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<LaneDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        public async Task<IActionResult> GetLanes([FromQuery] LaneFilterQuery query)
        {
            var result = await _laneService.GetLanesPagedAsync(query);
            return OkApiResponse(result, "Lấy danh sách làn xe thành công.");
        }

        /// <summary>
        /// Xem thông tin làn xe cơ bản theo Id
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<LaneDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetLaneById(string id)
        {
            var lane = await _laneService.GetLaneByIdAsync(id);
            return OkApiResponse(lane, "Lấy thông tin làn xe thành công.");
        }

        /// <summary>
        /// Xem chi tiết cấu hình làn xe kèm thông tin tóm tắt của Cổng và 4 thiết bị ngoại vi liên kết (ADR 0034)
        /// </summary>
        [HttpGet("{id}/detail")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<LaneDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetLaneDetail(string id)
        {
            var detail = await _laneService.GetLaneDetailByIdAsync(id);
            return OkApiResponse(detail, "Lấy thông tin chi tiết cấu hình ngoại vi làn xe thành công.");
        }

        /// <summary>
        /// Tạo mới làn xe kèm xác thực cổng và ràng buộc các thiết bị ngoại vi
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<LaneDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> CreateLane([FromBody] CreateLaneRequest request)
        {
            var created = await _laneService.CreateLaneAsync(request);
            return CreatedApiResponse($"/api/v1/lanes/{created.Id}", created, "Tạo mới làn xe thành công.");
        }

        /// <summary>
        /// Cập nhật thông tin làn xe và cấu hình thiết bị phần cứng
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<LaneDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> UpdateLane(string id, [FromBody] UpdateLaneRequest request)
        {
            var updated = await _laneService.UpdateLaneAsync(id, request);
            return OkApiResponse(updated, "Cập nhật thông tin làn xe thành công.");
        }

        /// <summary>
        /// Xóa làn xe (Hỗ trợ xóa mềm và xóa vĩnh viễn)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> DeleteLane(string id, [FromQuery] bool hardDelete = false)
        {
            await _laneService.DeleteLaneAsync(id, hardDelete);
            var msg = hardDelete
                ? "Đã xóa vĩnh viễn làn xe khỏi cơ sở dữ liệu."
                : "Đã xóa mềm làn xe thành công.";

            return OkApiResponse(true, msg);
        }

        /// <summary>
        /// Khôi phục làn xe từ thùng rác (ADR 0031 Recycle Bin &amp; Restore)
        /// </summary>
        [HttpPost("{id}/restore")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<LaneDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> RestoreLane(string id)
        {
            var restored = await _laneService.RestoreLaneAsync(id);
            return OkApiResponse(restored, "Khôi phục làn xe thành công.");
        }
    }
}
