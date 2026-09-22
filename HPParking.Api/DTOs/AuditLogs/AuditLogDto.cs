using HPParking.Api.DTOs.Common;
using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.AuditLogs
{
    /// <summary>
    /// DTO tóm tắt nhật ký kiểm toán hệ thống phục vụ hiển thị danh sách
    /// </summary>
    public class AuditLogDto : BaseDto
    {
        public string? ActorId { get; set; }
        public string ActorUsername { get; set; } = string.Empty;
        public string ActorRole { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;

        public AuditActionType ActionType { get; set; } = AuditActionType.Login;
        public string TargetEntity { get; set; } = string.Empty;
        public string? TargetId { get; set; }
        public string? TargetDisplay { get; set; }

        public bool IsSuccess { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }
}
