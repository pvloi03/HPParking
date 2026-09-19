using System.Threading.Tasks;
using Asp.Versioning;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Vehicles;
using HPParking.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HPParking.Api.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class VehiclesController : BaseApiController
    {
        private readonly IVehicleService _vehicleService;

        public VehiclesController(
            IVehicleService vehicleService,
            ILogger<VehiclesController> logger)
            : base(logger)
        {
            _vehicleService = vehicleService;
        }

        /// <summary>
        /// Lấy danh sách toàn bộ phương tiện trong hệ thống có phân trang và lọc theo biển số, loại xe, chủ xe
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<VehicleDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        public async Task<IActionResult> GetVehicles([FromQuery] VehicleFilterQuery query)
        {
            var result = await _vehicleService.GetVehiclesPagedAsync(query);
            return OkApiResponse(result, "Lấy danh sách phương tiện thành công.");
        }

        /// <summary>
        /// Xem chi tiết thông tin một phương tiện theo Id
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<VehicleDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetVehicleById(string id)
        {
            var vehicle = await _vehicleService.GetVehicleByIdAsync(id);
            return OkApiResponse(vehicle, "Lấy thông tin phương tiện thành công.");
        }

        /// <summary>
        /// Cập nhật thông tin phương tiện (biển số xe, loại xe, trạng thái kích hoạt, ghi chú)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<VehicleDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> UpdateVehicle(string id, [FromBody] UpdateVehicleRequest request)
        {
            var updated = await _vehicleService.UpdateVehicleAsync(id, request);
            return OkApiResponse(updated, "Cập nhật phương tiện thành công.");
        }

        /// <summary>
        /// Hủy / Xóa phương tiện (Mặc định xóa mềm, nếu ?hardDelete=true sẽ xóa vĩnh viễn khỏi CSDL)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> DeleteVehicle(string id, [FromQuery] bool hardDelete = false)
        {
            await _vehicleService.DeleteVehicleAsync(id, hardDelete);
            var msg = hardDelete
                ? "Đã xóa vĩnh viễn phương tiện khỏi cơ sở dữ liệu."
                : "Đã xóa mềm phương tiện thành công.";

            return OkApiResponse(true, msg);
        }

        /// <summary>
        /// Khôi phục phương tiện từ thùng rác (ADR 0031 Recycle Bin &amp; Restore)
        /// </summary>
        [HttpPost("{id}/restore")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<VehicleDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> RestoreVehicle(string id)
        {
            var restored = await _vehicleService.RestoreVehicleAsync(id);
            return OkApiResponse(restored, "Khôi phục phương tiện thành công.");
        }
    }
}
