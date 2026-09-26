using HPParking.Api.Common.Excel;

namespace HPParking.Api.DTOs.Excel.MasterData
{
    /// <summary>
    /// DTO đại diện cho một dòng dữ liệu Cổng kiểm soát trong tệp Excel mẫu / nhập liệu (Dùng Mã công ty làm khóa tham chiếu)
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
    /// DTO đại diện cho dữ liệu xuất Excel của Cổng kiểm soát (Đầy đủ tất cả thông tin, khóa liên kết đổi thành Tên)
    /// </summary>
    public class GateExportExcelDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string MachineCode { get; set; } = string.Empty;
        public bool? IsActive { get; set; } = true;
    }

    /// <summary>
    /// Cấu hình Fluent Profile cho Mẫu / Nhập liệu Cổng kiểm soát
    /// </summary>
    public class GateExcelProfile : ExcelProfile<GateExcelDto>
    {
        public GateExcelProfile()
        {
            Map(x => x.Code).ColumnName("Mã cổng").Order(1).Required().WithComment("Mã định danh duy nhất (VD: GATE_01)");
            Map(x => x.Name).ColumnName("Tên cổng").Order(2).Required().WithComment("Tên hiển thị (VD: Cổng chính)");
            Map(x => x.CompanyCode).ColumnName("Mã công ty").Order(3).WithComment("Mã công ty quản lý cổng (VD: CTY-HP)");
            Map(x => x.MachineCode).ColumnName("Mã máy trạm").Order(4).Required().WithComment("Hardware fingerprint máy trạm bốt bảo vệ");
            Map(x => x.IsActive).ColumnName("Trạng thái").Order(5).WithComment("TRUE: Hoạt động, FALSE: Vô hiệu");
        }
    }

    /// <summary>
    /// Cấu hình Fluent Profile cho Xuất dữ liệu Cổng kiểm soát (Khóa liên kết đổi thành Tên)
    /// </summary>
    public class GateExportExcelProfile : ExcelProfile<GateExportExcelDto>
    {
        public GateExportExcelProfile()
        {
            Map(x => x.Code).ColumnName("Mã cổng").Order(1);
            Map(x => x.Name).ColumnName("Tên cổng").Order(2);
            Map(x => x.CompanyName).ColumnName("Công ty").Order(3);
            Map(x => x.MachineCode).ColumnName("Mã máy trạm").Order(4);
            Map(x => x.IsActive).ColumnName("Trạng thái").Order(5);
        }
    }
}
