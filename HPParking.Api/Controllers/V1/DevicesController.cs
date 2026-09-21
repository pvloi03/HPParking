using Asp.Versioning;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Devices;
using HPParking.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HPParking.Api.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class DevicesController : BaseApiController
    {
        private readonly IDeviceService _deviceService;

        public DevicesController(
            IDeviceService deviceService,
            ILogger<DevicesController> logger)
            : base(logger)
        {
            _deviceService = deviceService;
        }

        /// <summary>
        /// Lấy danh sách thiết bị ngoại vi có phân trang, lọc theo loại (Type), trạng thái (IsActive) và từ khóa
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<DeviceDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        public async Task<IActionResult> GetDevices([FromQuery] DeviceFilterQuery query)
        {
            var result = await _deviceService.GetDevicesPagedAsync(query);
            return OkApiResponse(result, "Lấy danh sách thiết bị thành công.");
        }

        /// <summary>
        /// Xem chi tiết thông tin một thiết bị ngoại vi theo Id
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<DeviceDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetDeviceById(string id)
        {
            var device = await _deviceService.GetDeviceByIdAsync(id);
            return OkApiResponse(device, "Lấy thông tin thiết bị thành công.");
        }

        /// <summary>
        /// Tạo mới thiết bị ngoại vi, kiểm tra trùng mã Code và địa chỉ kết nối IpAddress:Port
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<DeviceDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> CreateDevice([FromBody] CreateDeviceRequest request)
        {
            var created = await _deviceService.CreateDeviceAsync(request);
            return CreatedApiResponse($"/api/v1/devices/{created.Id}", created, "Tạo mới thiết bị thành công.");
        }

        /// <summary>
        /// Cập nhật thông tin thiết bị ngoại vi, bảo vệ trạng thái hoạt động theo ADR 0030
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<DeviceDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> UpdateDevice(string id, [FromBody] UpdateDeviceRequest request)
        {
            var updated = await _deviceService.UpdateDeviceAsync(id, request);
            return OkApiResponse(updated, "Cập nhật thông tin thiết bị thành công.");
        }

        /// <summary>
        /// Xóa thiết bị ngoại vi (Chặn xóa nếu đang được gán vào làn xe theo ADR 0030 &amp; ADR 0031)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> DeleteDevice(string id, [FromQuery] bool hardDelete = false)
        {
            // Nếu hardDelete = true, yêu cầu quyền Admin
            if (hardDelete && !User.IsInRole("Admin"))
            {
                return StatusCode(403, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Bạn không có quyền thực hiện xóa vĩnh viễn (yêu cầu quyền Admin).",
                    Errors = ["FORBIDDEN_HARD_DELETE"]
                });
            }

            await _deviceService.DeleteDeviceAsync(id, hardDelete);
            var msg = hardDelete
                ? "Đã xóa vĩnh viễn thiết bị khỏi cơ sở dữ liệu."
                : "Đã xóa mềm thiết bị thành công.";

            return OkApiResponse(true, msg);
        }

        /// <summary>
        /// Khôi phục thiết bị ngoại vi từ thùng rác (ADR 0031 Recycle Bin &amp; Restore)
        /// </summary>
        [HttpPost("{id}/restore")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<DeviceDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> RestoreDevice(string id)
        {
            var restored = await _deviceService.RestoreDeviceAsync(id);
            return OkApiResponse(restored, "Khôi phục thiết bị thành công.");
        }
    }
}
