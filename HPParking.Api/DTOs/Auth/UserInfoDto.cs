using HPParking.Api.DTOs.Common;

namespace HPParking.Api.DTOs.Auth
{
    public class UserInfoDto : BaseDto
    {
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }
}
