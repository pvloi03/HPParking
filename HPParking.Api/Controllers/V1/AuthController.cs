using System.Security.Claims;
using System.Threading.Tasks;
using Asp.Versioning;
using HPParking.Api.DTOs.Auth;
using HPParking.Api.DTOs.Common;
using HPParking.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace HPParking.Api.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AuthController : BaseApiController
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
            : base(logger)
        {
            _authService = authService;
        }

        /// <summary>
        /// Đăng nhập hệ thống lấy JWT Bearer Token theo ca làm việc (8-12 tiếng)
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("LoginRateLimitPolicy")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 429)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var response = await _authService.LoginAsync(request);
            return OkApiResponse(response, "Đăng nhập thành công.");
        }

        /// <summary>
        /// Lấy thông tin tài khoản người dùng đang đăng nhập
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<UserInfoDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(userId))
            {
                return ErrorApiResponse("Không xác định được danh tính người dùng trong token.", null, StatusCodes.Status401Unauthorized);
            }

            var userInfo = await _authService.GetCurrentUserAsync(userId);
            return OkApiResponse(userInfo, "Lấy thông tin tài khoản thành công.");
        }

        /// <summary>
        /// Đổi mật khẩu tài khoản người dùng
        /// </summary>
        /// <remarks>
        /// Theo ADR 0025: Mật khẩu mới tối thiểu 6 ký tự. Tài khoản có vai trò Admin được phép đổi mật khẩu mà không cần nhập mật khẩu cũ.
        /// </remarks>
        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var userRole = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

            if (string.IsNullOrEmpty(userId))
            {
                return ErrorApiResponse("Không xác định được danh tính người dùng trong token.", null, StatusCodes.Status401Unauthorized);
            }

            await _authService.ChangePasswordAsync(userId, userRole, request);
            return OkApiResponse(true, "Đổi mật khẩu thành công. Vui lòng sử dụng mật khẩu mới cho lần đăng nhập tiếp theo.");
        }
    }
}
