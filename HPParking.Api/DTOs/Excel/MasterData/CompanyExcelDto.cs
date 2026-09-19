using HPParking.Api.Common.Excel;

namespace HPParking.Api.DTOs.Excel.MasterData
{
    /// <summary>
    /// DTO đại diện cho dữ liệu Excel của Công ty / Đơn vị thành viên
    /// </summary>
    public class CompanyExcelDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public bool? IsActive { get; set; } = true;
    }

    /// <summary>
    /// Cấu hình Fluent Profile cho Công ty (ADR 0023)
    /// </summary>
    public class CompanyExcelProfile : ExcelProfile<CompanyExcelDto>
    {
        public CompanyExcelProfile()
        {
            Map(x => x.Code).ColumnName("Mã công ty").Order(1).Required().WithComment("Mã định danh duy nhất (VD: HP-01)");
            Map(x => x.Name).ColumnName("Tên công ty").Order(2).Required().WithComment("Tên đầy đủ của công ty");
            Map(x => x.PhoneNumber).ColumnName("Số điện thoại").Order(3).WithComment("Số điện thoại liên hệ");
            Map(x => x.Email).ColumnName("Email").Order(4).WithComment("Địa chỉ thư điện tử");
            Map(x => x.IsActive).ColumnName("Trạng thái").Order(5).WithComment("TRUE: Hoạt động, FALSE: Vô hiệu");
        }
    }
}
