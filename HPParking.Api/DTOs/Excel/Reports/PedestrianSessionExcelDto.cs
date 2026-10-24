using HPParking.Api.Common.Excel;
using System;

namespace HPParking.Api.DTOs.Excel.Reports
{
    /// <summary>
    /// DTO đại diện cho dữ liệu xuất Excel của Lịch sử người vào ra
    /// </summary>
    public class PedestrianSessionExcelDto
    {
        public string InOrderDisplay { get; set; } = string.Empty;
        public string? PersonCode { get; set; }
        public string? PersonName { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? InTime { get; set; }
        public string? InLaneName { get; set; }
        public string? InFaceImagePath { get; set; }
        public string? InOverviewImagePath { get; set; }
        public DateTime? OutTime { get; set; }
        public string? OutLaneName { get; set; }
        public string? OutFaceImagePath { get; set; }
        public string? OutOverviewImagePath { get; set; }
        public string Duration { get; set; } = string.Empty;
    }

    /// <summary>
    /// Cấu hình Fluent Profile cho xuất lịch sử người vào ra
    /// </summary>
    public class PedestrianSessionExcelProfile : ExcelProfile<PedestrianSessionExcelDto>
    {
        public PedestrianSessionExcelProfile()
        {
            Map(x => x.InOrderDisplay).ColumnName("Lượt vào").Order(1);
            Map(x => x.PersonCode).ColumnName("Mã định danh").Order(2);
            Map(x => x.PersonName).ColumnName("Họ và tên").Order(3);
            Map(x => x.Status).ColumnName("Trạng thái").Order(4);
            Map(x => x.InTime).ColumnName("Thời điểm vào").Order(5).Format("dd/MM/yyyy HH:mm:ss");
            Map(x => x.InLaneName).ColumnName("Cổng vào").Order(6);
            Map(x => x.InFaceImagePath).ColumnName("Ảnh khuôn mặt vào").Order(7);
            Map(x => x.InOverviewImagePath).ColumnName("Ảnh toàn cảnh vào").Order(8);
            Map(x => x.OutTime).ColumnName("Thời điểm ra").Order(9).Format("dd/MM/yyyy HH:mm:ss");
            Map(x => x.OutLaneName).ColumnName("Cổng ra").Order(10);
            Map(x => x.OutFaceImagePath).ColumnName("Ảnh khuôn mặt ra").Order(11);
            Map(x => x.OutOverviewImagePath).ColumnName("Ảnh toàn cảnh ra").Order(12);
            Map(x => x.Duration).ColumnName("Thời gian bên trong").Order(13);
        }
    }
}
