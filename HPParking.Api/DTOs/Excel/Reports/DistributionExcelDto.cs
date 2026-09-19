using HPParking.Api.Common.Excel;

namespace HPParking.Api.DTOs.Excel.Reports
{
    /// <summary>
    /// DTO đại diện cho dữ liệu xuất Excel của Báo cáo Ma trận phân bổ (Distribution Matrix)
    /// </summary>
    public class DistributionExcelDto
    {
        public string CompanyName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public long ClientCount { get; set; }
        public long VehicleCount { get; set; }
        public long GateCount { get; set; }
        public long LaneCount { get; set; }
    }

    /// <summary>
    /// Cấu hình Fluent Profile cho Ma trận phân bổ
    /// </summary>
    public class DistributionExcelProfile : ExcelProfile<DistributionExcelDto>
    {
        public DistributionExcelProfile()
        {
            Map(x => x.CompanyName).ColumnName("Công ty").Order(1);
            Map(x => x.DepartmentName).ColumnName("Phòng ban").Order(2);
            Map(x => x.ClientCount).ColumnName("Số lượng khách hàng").Order(3);
            Map(x => x.VehicleCount).ColumnName("Số lượng phương tiện").Order(4);
            Map(x => x.GateCount).ColumnName("Số lượng cổng").Order(5);
            Map(x => x.LaneCount).ColumnName("Số lượng làn").Order(6);
        }
    }
}
