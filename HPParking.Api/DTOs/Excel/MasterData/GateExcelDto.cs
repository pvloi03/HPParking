using HPParking.Api.Common.Excel;

namespace HPParking.Api.DTOs.Excel.MasterData
{
    /// <summary>
    /// DTO đại diện cho dữ liệu Excel của Cổng kiểm soát (Gate)
    /// </summary>
    public class GateExcelDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? CompanyCode { get; set; }
        public string MachineCode { get; set; } = string.Empty;
        public bool? IsActive { get; set; } = true;
    }

    /// <summary>
    /// Cấu hình Fluent Profile cho Cổng kiểm soát (ADR 0023)
    /// </summary>
    public class GateExcelProfile : ExcelProfile<GateExcelDto>
    {
        public GateExcelProfile()
        {
            Map(x => x.Code).ColumnName("Mã cổng").Order(1).Required().WithComment("Mã định danh duy nhất (VD: GATE_01)");
            Map(x => x.Name).ColumnName("Tên cổng").Order(2).Required().WithComment("Tên hiển thị (VD: Cổng chính)");
            Map(x => x.CompanyCode).ColumnName("Mã công ty").Order(3).WithComment("Mã hoặc tên công ty sở hữu");
            Map(x => x.MachineCode).ColumnName("Mã máy trạm").Order(4).Required().WithComment("Hardware fingerprint máy trạm bốt bảo vệ");
            Map(x => x.IsActive).ColumnName("Trạng thái").Order(5).WithComment("TRUE: Hoạt động, FALSE: Vô hiệu");
        }
    }
}
