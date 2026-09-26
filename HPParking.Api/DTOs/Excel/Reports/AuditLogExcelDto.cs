using HPParking.Api.Common.Excel;
using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.Excel.Reports
{
    /// <summary>
    /// DTO đại diện cho dữ liệu xuất Excel của Nhật ký hệ thống / Audit Log (Chỉ Admin - ADR 0030)
    /// </summary>
    public class AuditLogExcelDto
    {
        public DateTime CreatedAt { get; set; }
        public string ActorUsername { get; set; } = string.Empty;
        public string ActorRole { get; set; } = string.Empty;
        public AuditActionType ActionType { get; set; } = AuditActionType.Login;
        public string TargetEntity { get; set; } = string.Empty;
        public string? TargetDisplay { get; set; }
        public bool IsSuccess { get; set; } = true;
        public string? Reason { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Cấu hình Fluent Profile cho xuất Audit Logs (Chỉ xuất - Read Only, bảo mật cao)
    /// </summary>
    public class AuditLogExcelProfile : ExcelProfile<AuditLogExcelDto>
    {
        public AuditLogExcelProfile()
        {
            Map(x => x.CreatedAt).ColumnName("Thời gian").Order(1).Format("dd/MM/yyyy HH:mm:ss");
            Map(x => x.ActorUsername).ColumnName("Người thực hiện").Order(2);
            Map(x => x.ActorRole).ColumnName("Vai trò").Order(3);
            Map(x => x.ActionType).ColumnName("Hành động").Order(4);
            Map(x => x.TargetEntity).ColumnName("Đối tượng").Order(5);
            Map(x => x.TargetDisplay).ColumnName("Chi tiết đối tượng").Order(6);
            Map(x => x.IsSuccess).ColumnName("Kết quả").Order(7);
            Map(x => x.Reason).ColumnName("Lý do").Order(8);
            Map(x => x.ErrorMessage).ColumnName("Thông báo lỗi").Order(9);
        }
    }
}
