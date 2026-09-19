using HPParking.Api.Common.Excel;
using HPParking.Core.Models.Enums;
using System;

namespace HPParking.Api.DTOs.Excel.Reports
{
    /// <summary>
    /// DTO đại diện cho dữ liệu xuất Excel của Phiên đỗ xe / Lịch sử ra vào (Sổ cái bất biến - ADR 0030)
    /// </summary>
    public class ParkingSessionExcelDto
    {
        public string PlateNumber { get; set; } = string.Empty;
        public VehicleType VehicleType { get; set; } = VehicleType.Car;
        public ParkingSessionStatus Status { get; set; } = ParkingSessionStatus.Active;
        public DateTime? InTime { get; set; }
        public string? InLaneName { get; set; }
        public DateTime? OutTime { get; set; }
        public string? OutLaneName { get; set; }
        public string Duration { get; set; } = string.Empty;
    }

    /// <summary>
    /// Cấu hình Fluent Profile cho xuất lịch sử phiên đỗ xe (Chỉ xuất - Read Only)
    /// </summary>
    public class ParkingSessionExcelProfile : ExcelProfile<ParkingSessionExcelDto>
    {
        public ParkingSessionExcelProfile()
        {
            Map(x => x.PlateNumber).ColumnName("Biển số xe").Order(1);
            Map(x => x.VehicleType).ColumnName("Loại xe").Order(2);
            Map(x => x.Status).ColumnName("Trạng thái").Order(3);
            Map(x => x.InTime).ColumnName("Thời điểm vào").Order(4).Format("dd/MM/yyyy HH:mm:ss");
            Map(x => x.InLaneName).ColumnName("Làn vào").Order(5);
            Map(x => x.OutTime).ColumnName("Thời điểm ra").Order(6).Format("dd/MM/yyyy HH:mm:ss");
            Map(x => x.OutLaneName).ColumnName("Làn ra").Order(7);
            Map(x => x.Duration).ColumnName("Thời lượng đỗ").Order(8);
        }
    }
}
