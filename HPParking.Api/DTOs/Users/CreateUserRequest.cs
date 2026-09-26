using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.Users
{
    /// <summary>
    /// DTO yêu cầu tạo mới tài khoản người dùng
    /// </summary>
    public class CreateUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public UserRole Role { get; set; } = UserRole.Viewer;
        public bool IsActive { get; set; } = true;
        public string? Note { get; set; }
    }
}
