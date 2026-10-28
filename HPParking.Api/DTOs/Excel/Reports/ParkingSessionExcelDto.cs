using HPParking.Api.Common.Excel;
using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.Excel.Reports
{
    /// <summary>
    /// DTO đại diện cho dữ liệu xuất Excel của Phiên đỗ xe / Lịch sử ra vào (Đầy đủ tất cả thông tin, khóa liên kết đổi thành Tên)
    /// </summary>
    public class ParkingSessionExcelDto
    {
        public string InOrderDisplay { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;
        public VehicleType? VehicleType { get; set; }
        public ParkingSessionStatus? Status { get; set; } = ParkingSessionStatus.Active;
        public string? CardNumber { get; set; }
        public string? ClientCode { get; set; }
        public string? ClientName { get; set; }
        public DateTime? InTime { get; set; }
        public string? InLaneName { get; set; }
        public string? InPlateImagePath { get; set; }
        public string? InOverviewImagePath { get; set; }
        public string? RawInPlateOcr { get; set; }
        public bool IsManualPlateIn { get; set; }
        public DateTime? OutTime { get; set; }
        public string? OutLaneName { get; set; }
        public string? OutPlateImagePath { get; set; }
        public string? OutOverviewImagePath { get; set; }
        public string? RawOutPlateOcr { get; set; }
        public bool IsManualPlateOut { get; set; }
        public string Duration { get; set; } = string.Empty;
        public decimal FeeAmount { get; set; }
        public bool IsFeeCollected { get; set; }
        public SessionCloseReason? CloseReason { get; set; }
        public string? ClosedBy { get; set; }
    }

    /// <summary>
    /// Cấu hình Fluent Profile cho xuất lịch sử phiên đỗ xe (Chỉ xuất - Read Only)
    /// </summary>
    public class ParkingSessionExcelProfile : ExcelProfile<ParkingSessionExcelDto>
    {
        public ParkingSessionExcelProfile()
        {
            Map(x => x.InOrderDisplay).ColumnName("Lượt vào").Order(1);
            Map(x => x.PlateNumber).ColumnName("Biển số xe").Order(2);
            Map(x => x.VehicleType).ColumnName("Loại xe").Order(3);
            Map(x => x.Status).ColumnName("Trạng thái").Order(4);
            Map(x => x.CardNumber).ColumnName("Mã thẻ").Order(5);
            Map(x => x.ClientCode).ColumnName("Mã chủ xe").Order(6);
            Map(x => x.ClientName).ColumnName("Họ và tên").Order(7);
            Map(x => x.InTime).ColumnName("Thời điểm vào").Order(8).Format("dd/MM/yyyy HH:mm:ss");
            Map(x => x.InLaneName).ColumnName("Làn vào").Order(9);
            Map(x => x.InPlateImagePath).ColumnName("Ảnh biển số vào").Order(10);
            Map(x => x.InOverviewImagePath).ColumnName("Ảnh toàn cảnh vào").Order(11);
            Map(x => x.RawInPlateOcr).ColumnName("Biển số OCR vào").Order(12);
            Map(x => x.OutTime).ColumnName("Thời điểm ra").Order(13).Format("dd/MM/yyyy HH:mm:ss");
            Map(x => x.OutLaneName).ColumnName("Làn ra").Order(14);
            Map(x => x.OutPlateImagePath).ColumnName("Ảnh biển số ra").Order(15);
            Map(x => x.OutOverviewImagePath).ColumnName("Ảnh toàn cảnh ra").Order(16);
            Map(x => x.RawOutPlateOcr).ColumnName("Biển số OCR ra").Order(17);
            Map(x => x.Duration).ColumnName("Thời lượng đỗ").Order(18);
            Map(x => x.FeeAmount).ColumnName("Phí đỗ xe").Order(19).Format("#,##0");
            Map(x => x.IsFeeCollected).ColumnName("Đã thu tiền").Order(20);
            Map(x => x.CloseReason).ColumnName("Lý do đóng").Order(21);
            Map(x => x.ClosedBy).ColumnName("Người đóng").Order(22);
        }
    }
}
