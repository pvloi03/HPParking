using HPParking.Api.Common.Excel;

namespace HPParking.Api.DTOs.Excel.MasterData
{
    /// <summary>
    /// DTO đại diện cho dữ liệu Excel của Phòng ban / Bộ phận
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
    /// Cấu hình Fluent Profile cho Phòng ban (ADR 0023)
    /// </summary>
    public class DepartmentExcelProfile : ExcelProfile<DepartmentExcelDto>
    {
        public DepartmentExcelProfile()
        {
            Map(x => x.Code).ColumnName("Mã phòng ban").Order(1).Required().WithComment("Mã định danh duy nhất (VD: PB-KT)");
            Map(x => x.Name).ColumnName("Tên phòng ban").Order(2).Required().WithComment("Tên đầy đủ của phòng ban");
            Map(x => x.CompanyCode).ColumnName("Mã công ty").Order(3).WithComment("Mã hoặc tên công ty trực thuộc");
            Map(x => x.ManagerName).ColumnName("Trưởng phòng").Order(4).WithComment("Họ và tên người quản lý");
            Map(x => x.PhoneNumber).ColumnName("Số điện thoại").Order(5).WithComment("Số điện thoại phòng ban");
            Map(x => x.Email).ColumnName("Email").Order(6).WithComment("Email liên hệ phòng ban");
            Map(x => x.IsActive).ColumnName("Trạng thái").Order(7).WithComment("TRUE: Hoạt động, FALSE: Vô hiệu");
        }
    }
}
