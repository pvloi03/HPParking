using HPParking.Api.DTOs.Common;
using HPParking.Core.Models.Enums;
using System;

namespace HPParking.Api.DTOs.Users
{
    /// <summary>
    /// DTO đại diện cho tài khoản người dùng hiển thị trên danh sách và chi tiết
    /// </summary>
    public class UserDto : AuditableDto
    {
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public UserRole Role { get; set; } = UserRole.Viewer;
        public bool IsActive { get; set; } = true;
        public DateTime? LastLoginAt { get; set; }
        public DateTime? LastLogoutAt { get; set; }
    }
}
