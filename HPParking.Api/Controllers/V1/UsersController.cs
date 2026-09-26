using Asp.Versioning;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Users;
using HPParking.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace HPParking.Api.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Roles = "Admin")]
    public class UsersController : BaseApiController
    {
        private readonly IUserService _userService;

        public UsersController(
            IUserService userService,
            ILogger<UsersController> logger)
            : base(logger)
        {
            _userService = userService;
        }

        private string? CurrentUserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        /// <summary>
        /// Lấy danh sách tài khoản người dùng có phân trang, tìm kiếm và lọc theo vai trò, trạng thái
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<UserDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        public async Task<IActionResult> GetUsers([FromQuery] UserFilterQuery query, CancellationToken cancellationToken)
        {
            var result = await _userService.GetUsersPagedAsync(query, cancellationToken);
            return OkApiResponse(result, "Lấy danh sách người dùng thành công.");
        }

        /// <summary>
        /// Xem chi tiết thông tin người dùng theo Id
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetUserById(string id, CancellationToken cancellationToken)
        {
            var user = await _userService.GetUserByIdAsync(id, cancellationToken);
            return OkApiResponse(user, "Lấy thông tin người dùng thành công.");
        }

        /// <summary>
        /// Tạo mới tài khoản người dùng
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
        {
            var created = await _userService.CreateUserAsync(request, cancellationToken);
            return CreatedApiResponse($"/api/v1/users/{created.Id}", created, "Tạo mới người dùng thành công.");
        }

        /// <summary>
        /// Cập nhật thông tin tài khoản người dùng
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
        {
            var updated = await _userService.UpdateUserAsync(id, request, cancellationToken);
            return OkApiResponse(updated, "Cập nhật thông tin người dùng thành công.");
        }

        /// <summary>
        /// Xóa tài khoản (mặc định xóa mềm, hỗ trợ xóa vĩnh viễn)
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> DeleteUser(string id, [FromQuery] bool permanent = false, CancellationToken cancellationToken = default)
        {
            var result = await _userService.DeleteUserAsync(id, CurrentUserId, permanent, cancellationToken);
            var msg = permanent
                ? "Đã xóa vĩnh viễn tài khoản người dùng."
                : "Đã xóa mềm tài khoản người dùng thành công.";
            return OkApiResponse(result, msg);
        }

        /// <summary>
        /// Khôi phục tài khoản người dùng đã bị xóa mềm
        /// </summary>
        [HttpPost("{id}/restore")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> RestoreUser(string id, CancellationToken cancellationToken)
        {
            var result = await _userService.RestoreUserAsync(id, cancellationToken);
            return OkApiResponse(result, "Khôi phục tài khoản người dùng thành công.");
        }

        /// <summary>
        /// Đặt lại mật khẩu cho tài khoản người dùng (Admin quyền cao)
        /// </summary>
        [HttpPost("{id}/reset-password")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> ResetPassword(string id, [FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
        {
            var result = await _userService.ResetPasswordAsync(id, request, cancellationToken);
            return OkApiResponse(result, "Đặt lại mật khẩu cho tài khoản thành công.");
        }

        /// <summary>
        /// Bật/tắt trạng thái kích hoạt (Active) của tài khoản
        /// </summary>
        [HttpPatch("{id}/toggle-status")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> ToggleStatus(string id, CancellationToken cancellationToken)
        {
            var updated = await _userService.ToggleStatusAsync(id, CurrentUserId, cancellationToken);
            var statusText = updated.IsActive ? "Kích hoạt" : "Vô hiệu hóa";
            return OkApiResponse(updated, $"{statusText} tài khoản thành công.");
        }
    }
}
