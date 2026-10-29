using HPParking.Api.Common.Excel;
using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.Excel.Reports
{
    /// <summary>
    /// DTO đại diện cho một dòng dữ liệu trong Sheet 2 (Chi tiết giao dịch) của Báo cáo Doanh thu (ADR 0023, ADR 0043)
    /// </summary>
    public class RevenueTransactionExcelDto
    {
        public string InOrderDisplay { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;
        public VehicleType? VehicleType { get; set; }
        public string? CardNumber { get; set; }
        public string? ClientName { get; set; }
        public DateTime? InTime { get; set; }
        public DateTime? OutTime { get; set; }
        public string? InLaneName { get; set; }
        public string? OutLaneName { get; set; }
        public string Duration { get; set; } = string.Empty;
        public decimal FeeAmount { get; set; }
        public bool IsFeeCollected { get; set; }
    }

    /// <summary>
    /// Cấu hình Fluent Profile cho Sheet 2 Báo cáo Doanh thu chi tiết giao dịch (ADR 0023, ADR 0043)
    /// </summary>
    public class RevenueTransactionExcelProfile : ExcelProfile<RevenueTransactionExcelDto>
    {
        public RevenueTransactionExcelProfile()
        {
            Map(x => x.InOrderDisplay).ColumnName("Lượt vào").Order(1);
            Map(x => x.PlateNumber).ColumnName("Biển số xe").Order(2);
            Map(x => x.VehicleType).ColumnName("Loại xe").Order(3);
            Map(x => x.CardNumber).ColumnName("Mã thẻ").Order(4);
            Map(x => x.ClientName).ColumnName("Khách hàng").Order(5);
            Map(x => x.InTime).ColumnName("Thời điểm vào").Order(6).Format("dd/MM/yyyy HH:mm:ss");
            Map(x => x.OutTime).ColumnName("Thời điểm ra").Order(7).Format("dd/MM/yyyy HH:mm:ss");
            Map(x => x.InLaneName).ColumnName("Làn vào").Order(8);
            Map(x => x.OutLaneName).ColumnName("Làn ra").Order(9);
            Map(x => x.Duration).ColumnName("Thời lượng").Order(10);
            Map(x => x.FeeAmount).ColumnName("Tiền phí").Order(11).Format("#,##0");
            Map(x => x.IsFeeCollected).ColumnName("Trạng thái thu").Order(12).CustomFormat(v => v ? "Đã thu" : "Chưa thu");
        }
    }
}
