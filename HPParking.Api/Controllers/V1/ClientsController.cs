using Asp.Versioning;
using HPParking.Api.DTOs.Clients;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Vehicles;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Models.Enums;
using HPParking.Core.Models.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
            var terminals = created.FaceIdTerminals ?? [];
            var total = terminals.Count;
            var failure = terminals.Count(t => !t.IsOnline || !t.UserExists);
            var success = total - failure;

            string msg;
            var approval = created.LatestApprovalRequest;
            if (approval != null && !approval.EmailSent)
            {
                var emailErr = !string.IsNullOrWhiteSpace(approval.EmailError)
                    ? $" nhưng gửi email xác nhận thất bại: {approval.EmailError} (có thể gửi lại email sau)."
                    : " nhưng gửi email xác nhận thất bại (có thể gửi lại email sau).";

                if (total > 0 && failure > 0)
                {
                    msg = $"Tạo mới khách hàng thành công{emailErr} Đồng bộ FaceID: {success}/{total} thành công, {failure} thiết bị lỗi.";
                }
                else if (total > 0)
                {
                    msg = $"Tạo mới khách hàng thành công{emailErr} Đồng bộ {total} thiết bị FaceID thành công.";
                }
                else
                {
                    msg = $"Tạo mới khách hàng thành công{emailErr}";
                }
            }
            else if (approval != null && approval.EmailSent)
            {
                var emailNotice = $" Đã gửi email xác nhận kích hoạt tới {approval.ApprovingDepartmentEmail}.";
                if (total > 0 && failure > 0)
                {
                    msg = $"Tạo mới khách hàng thành công.{emailNotice} (Đồng bộ FaceID: {success}/{total} thành công, {failure} thiết bị mất kết nối hoặc lỗi).";
                }
                else if (total > 0)
                {
                    msg = $"Tạo mới khách hàng thành công.{emailNotice} Đồng bộ toàn bộ {total} thiết bị FaceID thành công.";
                }
                else
                {
                    msg = $"Tạo mới khách hàng thành công.{emailNotice}";
                }
            }
            else
            {
                msg = total > 0 && failure > 0
                    ? $"Tạo mới khách hàng thành công (Đồng bộ FaceID: {success}/{total} thành công, {failure} thiết bị mất kết nối hoặc lỗi)."
                    : total > 0
                        ? $"Tạo mới khách hàng và đồng bộ toàn bộ {total} thiết bị FaceID thành công."
                        : "Tạo mới khách hàng thành công.";
            }

            return CreatedApiResponse($"/api/v1/clients/{created.Id}", created, msg);
        }

        /// <summary>
        /// Kiểm tra live trạng thái Client trên tất cả các đầu đọc FaceID đang hoạt động
        /// </summary>
        [HttpGet("{id}/faceid-status")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<ClientFaceIdStatusResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> CheckFaceIdStatus(string id)
        {
            var result = await _clientService.CheckFaceIdStatusAsync(id);
            return OkApiResponse(result, "Kiểm tra trạng thái FaceID thành công.");
        }

        /// <summary>
        /// Cập nhật thông tin khách hàng
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<ClientDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> UpdateClient(string id, [FromBody] UpdateClientRequest request)
        {
            var updated = await _clientService.UpdateClientAsync(id, request);
            var terminals = updated.FaceIdTerminals ?? [];
            var total = terminals.Count;
            var failure = terminals.Count(t => !t.IsOnline || !t.UserExists);
            var success = total - failure;

            var msg = total > 0 && failure > 0
                ? $"Cập nhật khách hàng thành công (Đồng bộ FaceID: {success}/{total} thành công, {failure} thiết bị mất kết nối hoặc lỗi)."
                : total > 0
                    ? $"Cập nhật khách hàng và đồng bộ toàn bộ {total} thiết bị FaceID thành công."
                    : "Cập nhật thông tin khách hàng thành công.";

            return OkApiResponse(updated, msg);
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
            var msg = syncResult.FailureCount > 0
                ? $"Hoàn tất đồng bộ FaceID: {syncResult.SuccessCount}/{syncResult.TotalDevices} thiết bị thành công (Có {syncResult.FailureCount} thiết bị lỗi)."
                : $"Hoàn tất đồng bộ FaceID ({syncResult.SuccessCount}/{syncResult.TotalDevices} thiết bị thành công).";
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

        /// <summary>
        /// Xác nhận kích hoạt hoặc khóa hồ sơ nhân sự qua mã Token bảo mật trong email (ADR 0042)
        /// Endpoint công khai không yêu cầu đăng nhập tài khoản
        /// </summary>
        [HttpPost("confirm-status")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<ConfirmClientStatusResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> ConfirmClientStatus([FromQuery] ApprovalToken token, CancellationToken cancellationToken)
        {
            var result = await _clientService.ConfirmStatusByTokenAsync(token, cancellationToken);
            return OkApiResponse(result, result.Message);
        }

        /// <summary>
        /// Gửi yêu cầu Khóa hoặc Kích hoạt lại hồ sơ nhân sự đến phòng ban phê duyệt qua email (ADR 0042)
        /// </summary>
        [HttpPost("{id}/request-status-change")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<ClientApprovalRequestDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> RequestStatusChange(
            string id,
            [FromBody] RequestClientStatusChangeRequest request,
            CancellationToken cancellationToken)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var result = await _clientService.RequestStatusChangeAsync(id, request, currentUserId, cancellationToken);

            var actionText = request.ActionType == ClientApprovalActionType.Activate ? "kích hoạt" : "khóa";
            var msg = result.EmailSent
                ? $"Đã gửi email yêu cầu {actionText} hồ sơ tới {result.ApprovingDepartmentEmail} thành công."
                : $"Đã ghi nhận yêu cầu {actionText} hồ sơ nhưng gửi email thất bại: {result.EmailError ?? "lỗi máy chủ thư"} (có thể gửi lại sau).";

            return OkApiResponse(result, msg);
        }

        /// <summary>
        /// Gửi lại email phê duyệt kích hoạt / khóa hồ sơ nếu email thất lạc hoặc token hết hạn (ADR 0042)
        /// </summary>
        [HttpPost("{id}/resend-approval")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<ClientApprovalRequestDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> ResendApprovalEmail(string id, CancellationToken cancellationToken)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var result = await _clientService.ResendApprovalEmailAsync(id, currentUserId, cancellationToken);

            var msg = result.EmailSent
                ? $"Đã gửi lại email phê duyệt tới {result.ApprovingDepartmentEmail} thành công."
                : $"Gửi lại email phê duyệt thất bại: {result.EmailError ?? "lỗi máy chủ thư"} (vui lòng thử lại sau).";

            return OkApiResponse(result, msg);
        }
    }
}
