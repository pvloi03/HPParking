using HPParking.Api.Common.Excel;
using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.Excel.MasterData
{
    /// <summary>
    /// DTO đại diện cho một dòng dữ liệu Làn kiểm soát trong tệp Excel mẫu / nhập liệu (Dùng Mã cổng làm khóa tham chiếu)
    /// </summary>
    public class LaneExcelDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? GateCode { get; set; }
        public LaneDirection? Direction { get; set; } = LaneDirection.In;
        public int OutputRelay { get; set; }
        public int InputReader { get; set; }
        public bool? IsActive { get; set; } = true;
    }

    /// <summary>
    /// DTO đại diện cho dữ liệu xuất Excel của Làn kiểm soát (Đầy đủ tất cả thông tin, khóa liên kết đổi thành Tên)
    /// </summary>
    public class LaneExportExcelDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? GateName { get; set; }
        public LaneDirection? Direction { get; set; } = LaneDirection.In;
        public int OutputRelay { get; set; }
        public int InputReader { get; set; }
        public bool? IsActive { get; set; } = true;
    }

    /// <summary>
    /// Cấu hình Fluent Profile cho Mẫu / Nhập liệu Làn kiểm soát
    /// </summary>
    public class LaneExcelProfile : ExcelProfile<LaneExcelDto>
    {
        public LaneExcelProfile()
        {
            Map(x => x.Code).ColumnName("Mã làn").Order(1).Required().WithComment("Mã định danh duy nhất (VD: LANE_01)");
            Map(x => x.Name).ColumnName("Tên làn").Order(2).Required().WithComment("Tên hiển thị (VD: Làn vào số 1)");
            Map(x => x.GateCode).ColumnName("Mã cổng").Order(3).WithComment("Mã cổng trực thuộc (VD: GATE_01)");
            Map(x => x.Direction).ColumnName("Hướng di chuyển").Order(4).EnumDropdown<LaneDirection>().WithComment("Chọn: In, Out, Bidirectional");
            Map(x => x.OutputRelay).ColumnName("Relay điều khiển").Order(5).WithComment("Số thứ tự Relay kích mở barie (VD: 1, 2)");
            Map(x => x.InputReader).ColumnName("Đầu đọc").Order(6).WithComment("Số thứ tự đầu đọc thẻ (VD: 1, 2)");
            Map(x => x.IsActive).ColumnName("Trạng thái").Order(7).WithComment("TRUE: Hoạt động, FALSE: Vô hiệu");
        }
    }

    /// <summary>
    /// Cấu hình Fluent Profile cho Xuất dữ liệu Làn kiểm soát (Khóa liên kết đổi thành Tên)
    /// </summary>
    public class LaneExportExcelProfile : ExcelProfile<LaneExportExcelDto>
    {
        public LaneExportExcelProfile()
        {
            Map(x => x.Code).ColumnName("Mã làn").Order(1);
            Map(x => x.Name).ColumnName("Tên làn").Order(2);
            Map(x => x.GateName).ColumnName("Cổng kiểm soát").Order(3);
            Map(x => x.Direction).ColumnName("Hướng di chuyển").Order(4);
            Map(x => x.OutputRelay).ColumnName("Relay điều khiển").Order(5);
            Map(x => x.InputReader).ColumnName("Đầu đọc").Order(6);
            Map(x => x.IsActive).ColumnName("Trạng thái").Order(7);
        }
    }
}
