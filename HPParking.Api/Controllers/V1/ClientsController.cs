using Asp.Versioning;
using HPParking.Api.DTOs.Clients;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Vehicles;
using HPParking.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HPParking.Api.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ClientsController : BaseApiController
    {
        private readonly IClientService _clientService;
        private readonly IVehicleService _vehicleService;

        public ClientsController(
            IClientService clientService,
            IVehicleService vehicleService,
            ILogger<ClientsController> logger)
            : base(logger)
        {
            _clientService = clientService;
            _vehicleService = vehicleService;
        }

        /// <summary>
        /// Lấy danh sách khách hàng có phân trang, hỗ trợ tìm kiếm và lọc đa tiêu chí
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<ClientDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        public async Task<IActionResult> GetClients([FromQuery] ClientFilterQuery query)
        {
            var result = await _clientService.GetClientsPagedAsync(query);
            return OkApiResponse(result, "Lấy danh sách khách hàng thành công.");
        }

        /// <summary>
        /// Lấy thông tin chi tiết hồ sơ một khách hàng kèm danh sách xe sở hữu
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<ClientDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetClientById(string id)
        {
            var client = await _clientService.GetClientByIdAsync(id);
            return OkApiResponse(client, "Lấy thông tin khách hàng thành công.");
        }

        /// <summary>
        /// Tạo mới khách hàng, kiểm tra tính duy nhất của SĐT và CCCD, hỗ trợ đăng ký kèm danh sách xe ban đầu
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<ClientDetailDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> CreateClient([FromBody] CreateClientRequest request)
        {
            var created = await _clientService.CreateClientAsync(request);
            return CreatedApiResponse($"/api/v1/clients/{created.Id}", created, "Tạo mới khách hàng thành công.");
        }

        /// <summary>
        /// Cập nhật thông tin khách hàng
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<ClientDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> UpdateClient(string id, [FromBody] UpdateClientRequest request)
        {
            var updated = await _clientService.UpdateClientAsync(id, request);
            return OkApiResponse(updated, "Cập nhật thông tin khách hàng thành công.");
        }

        /// <summary>
        /// Xóa khách hàng (Mặc định xóa mềm, chặn xóa nếu còn phương tiện theo ADR 0030/0031); nếu ?hardDelete=true sẽ xóa vĩnh viễn khỏi CSDL và phát lệnh xóa FaceID
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> DeleteClient(string id, [FromQuery] bool hardDelete = false)
        {
            await _clientService.DeleteClientAsync(id, hardDelete);
            var message = hardDelete
                ? "Đã xóa vĩnh viễn khách hàng và gửi lệnh thu hồi quyền FaceID."
                : "Đã xóa mềm khách hàng thành công (bảo lưu FaceID).";

            return OkApiResponse(true, message);
        }

        /// <summary>
        /// Khôi phục khách hàng từ thùng rác (ADR 0031 Recycle Bin &amp; Restore)
        /// </summary>
        [HttpPost("{id}/restore")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<ClientDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> RestoreClient(string id)
        {
            var restored = await _clientService.RestoreClientAsync(id);
            return OkApiResponse(restored, "Khôi phục khách hàng thành công.");
        }

        /// <summary>
        /// Tải lên ảnh đại diện / ảnh khuôn mặt cho khách hàng (JPG, PNG, WEBP &lt;= 5MB)
        /// </summary>
        [HttpPost("{id}/avatar")]
        [Authorize(Roles = "Manager,Admin")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<string>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> UploadAvatar(string id, IFormFile file)
        {
            var relativeUrl = await _clientService.UploadAvatarAsync(id, file);
            return OkApiResponse(relativeUrl, "Tải lên ảnh đại diện thành công.");
        }

        /// <summary>
        /// Tải luồng ảnh đại diện chân dung của khách hàng
        /// </summary>
        [HttpGet("{id}/avatar")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetClientAvatar(string id)
        {
            var (bytes, contentType) = await _clientService.GetAvatarAsync(id);
            return File(bytes, contentType);
        }

        /// <summary>
        /// Chủ động đồng bộ hồ sơ, thẻ SĐT và ảnh khuôn mặt lên toàn bộ các thiết bị FaceID của làn xe đang hoạt động
        /// </summary>
        [HttpPost("{id}/sync-faceid")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<SyncFaceIdResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> SyncFaceId(string id)
        {
            var syncResult = await _clientService.SyncFaceIdAsync(id);
            var msg = $"Đã hoàn tất đồng bộ FaceID ({syncResult.SuccessCount}/{syncResult.TotalDevices} thiết bị thành công).";
            return OkApiResponse(syncResult, msg);
        }

        /// <summary>
        /// Lấy danh sách tất cả các xe của một khách hàng cụ thể
        /// </summary>
        [HttpGet("{id}/vehicles")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<VehicleDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetClientVehicles(string id)
        {
            var vehicles = await _vehicleService.GetVehiclesByClientIdAsync(id);
            return OkApiResponse(vehicles, "Lấy danh sách phương tiện của khách hàng thành công.");
        }

        /// <summary>
        /// Đăng ký thêm xe mới trực tiếp cho khách hàng
        /// </summary>
        [HttpPost("{id}/vehicles")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<VehicleDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> AddClientVehicle(string id, [FromBody] CreateVehicleRequest request)
        {
            var createdVehicle = await _vehicleService.CreateVehicleAsync(id, request);
            return CreatedApiResponse($"/api/v1/vehicles/{createdVehicle.Id}", createdVehicle, "Thêm phương tiện mới cho khách hàng thành công.");
        }
    }
}
