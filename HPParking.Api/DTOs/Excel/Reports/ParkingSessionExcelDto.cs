using HPParking.Api.Common.Excel;
using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.Excel.Reports
{
    /// <summary>
    /// DTO đại diện cho dữ liệu xuất Excel của Phiên đỗ xe / Lịch sử ra vào (Đầy đủ tất cả thông tin, khóa liên kết đổi thành Tên)
    /// </summary>
    public class ParkingSessionExcelDto
    {
        public string PlateNumber { get; set; } = string.Empty;
        public VehicleType VehicleType { get; set; } = VehicleType.Car;
        public ParkingSessionStatus Status { get; set; } = ParkingSessionStatus.Active;
        public string? ClientCode { get; set; }
        public string? ClientName { get; set; }
        public DateTime? InTime { get; set; }
        public string? InLaneName { get; set; }
        public string? InPlateImagePath { get; set; }
        public string? InOverviewImagePath { get; set; }
        public DateTime? OutTime { get; set; }
        public string? OutLaneName { get; set; }
        public string? OutPlateImagePath { get; set; }
        public string? OutOverviewImagePath { get; set; }
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
            Map(x => x.ClientCode).ColumnName("Mã chủ xe").Order(4);
            Map(x => x.ClientName).ColumnName("Họ và tên").Order(5);
            Map(x => x.InTime).ColumnName("Thời điểm vào").Order(6).Format("dd/MM/yyyy HH:mm:ss");
            Map(x => x.InLaneName).ColumnName("Làn vào").Order(7);
            Map(x => x.InPlateImagePath).ColumnName("Ảnh biển số vào").Order(8);
            Map(x => x.InOverviewImagePath).ColumnName("Ảnh toàn cảnh vào").Order(9);
            Map(x => x.OutTime).ColumnName("Thời điểm ra").Order(10).Format("dd/MM/yyyy HH:mm:ss");
            Map(x => x.OutLaneName).ColumnName("Làn ra").Order(11);
            Map(x => x.OutPlateImagePath).ColumnName("Ảnh biển số ra").Order(12);
            Map(x => x.OutOverviewImagePath).ColumnName("Ảnh toàn cảnh ra").Order(13);
            Map(x => x.Duration).ColumnName("Thời lượng đỗ").Order(14);
        }
    }
}
