using HPParking.Api.Common.Excel;

namespace HPParking.Api.DTOs.Excel.MasterData
{
    /// <summary>
    /// DTO đại diện cho một dòng dữ liệu Phòng ban trong tệp Excel mẫu / nhập liệu (Dùng Mã công ty làm khóa tham chiếu)
    /// </summary>
    public class DepartmentExcelDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? CompanyCode { get; set; }
        public string? ManagerName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public bool? IsActive { get; set; } = true;
    }

    /// <summary>
    /// DTO đại diện cho dữ liệu xuất Excel của Phòng ban (Đầy đủ tất cả thông tin, khóa liên kết đổi thành Tên)
    /// </summary>
    public class DepartmentExportExcelDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string? ManagerName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public bool? IsActive { get; set; } = true;
    }

    /// <summary>
    /// Cấu hình Fluent Profile cho Mẫu / Nhập liệu Phòng ban
    /// </summary>
    public class DepartmentExcelProfile : ExcelProfile<DepartmentExcelDto>
    {
        public DepartmentExcelProfile()
        {
            Map(x => x.Code).ColumnName("Mã phòng ban").Order(1).Required().WithComment("Mã định danh duy nhất (VD: PB-KT)");
            Map(x => x.Name).ColumnName("Tên phòng ban").Order(2).Required().WithComment("Tên đầy đủ của phòng ban");
            Map(x => x.CompanyCode).ColumnName("Mã công ty").Order(3).WithComment("Mã công ty trực thuộc (VD: CTY-HP)");
            Map(x => x.ManagerName).ColumnName("Trưởng phòng").Order(4).WithComment("Họ và tên người quản lý");
            Map(x => x.PhoneNumber).ColumnName("Số điện thoại").Order(5).WithComment("Số điện thoại phòng ban");
            Map(x => x.Email).ColumnName("Email").Order(6).WithComment("Email liên hệ phòng ban");
            Map(x => x.IsActive).ColumnName("Trạng thái").Order(7).WithComment("TRUE: Hoạt động, FALSE: Vô hiệu");
        }
    }

    /// <summary>
    /// Cấu hình Fluent Profile cho Xuất dữ liệu Phòng ban (Khóa liên kết đổi thành Tên)
    /// </summary>
    public class DepartmentExportExcelProfile : ExcelProfile<DepartmentExportExcelDto>
    {
        public DepartmentExportExcelProfile()
        {
            Map(x => x.Code).ColumnName("Mã phòng ban").Order(1);
            Map(x => x.Name).ColumnName("Tên phòng ban").Order(2);
            Map(x => x.CompanyName).ColumnName("Công ty").Order(3);
            Map(x => x.ManagerName).ColumnName("Trưởng phòng").Order(4);
            Map(x => x.PhoneNumber).ColumnName("Số điện thoại").Order(5);
            Map(x => x.Email).ColumnName("Email").Order(6);
            Map(x => x.IsActive).ColumnName("Trạng thái").Order(7);
        }
    }
}
