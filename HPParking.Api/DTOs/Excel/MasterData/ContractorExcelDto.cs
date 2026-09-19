using HPParking.Api.Common.Excel;

namespace HPParking.Api.DTOs.Excel.MasterData
{
    /// <summary>
    /// DTO đại diện cho dữ liệu Excel của Nhà thầu / Đối tác
    /// </summary>
    public class ContractorExcelDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ContactPerson { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public bool? IsActive { get; set; } = true;
    }

    /// <summary>
    /// Cấu hình Fluent Profile cho Nhà thầu (ADR 0023)
    /// </summary>
    public class ContractorExcelProfile : ExcelProfile<ContractorExcelDto>
    {
        public ContractorExcelProfile()
        {
            Map(x => x.Code).ColumnName("Mã nhà thầu").Order(1).Required().WithComment("Mã định danh duy nhất (VD: NT-01)");
            Map(x => x.Name).ColumnName("Tên nhà thầu").Order(2).Required().WithComment("Tên đầy đủ nhà thầu");
            Map(x => x.ContactPerson).ColumnName("Người đại diện").Order(3).WithComment("Tên người đại diện liên hệ");
            Map(x => x.PhoneNumber).ColumnName("Số điện thoại").Order(4).WithComment("Số điện thoại liên hệ");
            Map(x => x.Email).ColumnName("Email").Order(5).WithComment("Email liên hệ");
            Map(x => x.IsActive).ColumnName("Trạng thái").Order(6).WithComment("TRUE: Hoạt động, FALSE: Vô hiệu");
        }
    }
}
