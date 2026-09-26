using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.Excel.Clients
{
    /// <summary>
    /// DTO đại diện cho một dòng dữ liệu Khách hàng trong tệp Excel mẫu / nhập liệu (Dùng Mã công ty, phòng ban, nhà thầu làm khóa tham chiếu)
    /// </summary>
    public class ClientExcelDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime? BirthDay { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
        public ClientType? Type { get; set; } = ClientType.Employee;
        public string? CompanyCode { get; set; }
        public string? DepartmentCode { get; set; }
        public string? ContractorCode { get; set; }
        public bool? IsActive { get; set; } = true;
    }

    /// <summary>
    /// DTO đại diện cho dữ liệu xuất Excel của Khách hàng (Đầy đủ tất cả thông tin, khóa liên kết đổi thành Tên)
    /// </summary>
    public class ClientExportExcelDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime? BirthDay { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
        public ClientType? Type { get; set; } = ClientType.Employee;
        public string? CompanyName { get; set; }
        public string? DepartmentName { get; set; }
        public string? ContractorName { get; set; }
        public string HasFaceId { get; set; } = "Chưa có";
        public bool? IsActive { get; set; } = true;
    }
}
