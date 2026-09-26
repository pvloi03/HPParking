using HPParking.Api.DTOs.Common;
using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.Users
{
    /// <summary>
    /// Tham số lọc và phân trang tra cứu danh sách tài khoản người dùng
    /// </summary>
    public class UserFilterQuery : PaginationQuery
    {
        public string? Keyword { get; set; }
        public UserRole? Role { get; set; }
        public bool? IsActive { get; set; }
    }
}
