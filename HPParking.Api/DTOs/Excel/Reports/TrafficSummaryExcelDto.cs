using HPParking.Api.Common.Excel;
using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.Excel.Reports
{
    /// <summary>
    /// DTO đại diện cho một dòng dữ liệu trong Báo cáo Tổng hợp số lượt ra vào theo từng người và xe
    /// </summary>
    public class TrafficSummaryExcelDto
    {
        public int Index { get; set; }
        public string ClientCode { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string ClientTypeName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;
        public VehicleType VehicleType { get; set; } = VehicleType.Car;
        public int InCount { get; set; }
        public int OutCount { get; set; }
        public int CompletedCount { get; set; }
        public bool IsInParking { get; set; }
    }

    /// <summary>
    /// Cấu hình Fluent Profile cho Báo cáo Tổng hợp lượt ra vào theo đối tượng
    /// </summary>
    public class TrafficSummaryExcelProfile : ExcelProfile<TrafficSummaryExcelDto>
    {
        public TrafficSummaryExcelProfile()
        {
            Map(x => x.Index).ColumnName("STT").Order(1);
            Map(x => x.ClientCode).ColumnName("Mã khách hàng").Order(2);
            Map(x => x.ClientName).ColumnName("Họ và tên").Order(3);
            Map(x => x.ClientTypeName).ColumnName("Loại đối tượng").Order(4);
            Map(x => x.CompanyName).ColumnName("Đơn vị / Công ty").Order(5);
            Map(x => x.DepartmentName).ColumnName("Phòng ban").Order(6);
            Map(x => x.PlateNumber).ColumnName("Biển số xe").Order(7);
            Map(x => x.VehicleType).ColumnName("Loại xe").Order(8);
            Map(x => x.InCount).ColumnName("Tổng lượt vào").Order(9).Format("#,##0");
            Map(x => x.OutCount).ColumnName("Tổng lượt ra").Order(10).Format("#,##0");
            Map(x => x.CompletedCount).ColumnName("Lượt hoàn thành").Order(11).Format("#,##0");
            Map(x => x.IsInParking).ColumnName("Đang trong bãi").Order(12);
        }
    }
}
