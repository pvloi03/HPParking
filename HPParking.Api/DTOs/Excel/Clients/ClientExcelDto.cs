using System;
using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.Excel.Clients
{
    /// <summary>
    /// DTO đại diện cho một dòng dữ liệu Khách hàng và Phương tiện trong tệp Excel
    /// </summary>
    public class ClientExcelDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime? BirthDay { get; set; }
        public string? Address { get; set; }
        public string? PlateNumber { get; set; }
        public VehicleType? VehicleType { get; set; }
        public string? CompanyName { get; set; }
        public string? DepartmentName { get; set; }
    }
}
