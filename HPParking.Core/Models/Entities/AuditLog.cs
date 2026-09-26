using HPParking.Core.Models.Common;
using HPParking.Core.Models.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace HPParking.Core.Models.Entities
{
    /// <summary>
    /// Entity đại diện cho nhật ký kiểm toán hệ thống (Audit Log)
    /// </summary>
    [BsonIgnoreExtraElements]
    public class AuditLog : BaseEntity
    {
        // =========================================================================
        // --- THÔNG TIN NGƯỜI THỰC HIỆN (ACTOR) ---
        // =========================================================================
        public string? ActorId { get; set; }
        public string ActorUsername { get; set; } = string.Empty;
        public string ActorRole { get; set; } = string.Empty;

        // =========================================================================
        // --- NGUỒN & MÔI TRƯỜNG THỰC HIỆN (SOURCE / ENVIRONMENT) ---
        // =========================================================================
        public string Source { get; set; } = "WebAdmin";

        // =========================================================================
        // --- HÀNH ĐỘNG & THỰC THỂ TÁC ĐỘNG (ACTION & TARGET) ---
        // =========================================================================
        [BsonRepresentation(BsonType.String)]
        public AuditActionType ActionType { get; set; } = AuditActionType.Login;
        public string TargetEntity { get; set; } = string.Empty;
        public string? TargetId { get; set; }
        public string? TargetDisplay { get; set; }

        // =========================================================================
        // --- CHI TIẾT & LÝ DO THỰC HIỆN (REASON) ---
        // =========================================================================
        public string? Reason { get; set; }

        // =========================================================================
        // --- KẾT QUẢ & TRẠNG THÁI (STATUS & ERROR) ---
        // =========================================================================
        public bool IsSuccess { get; set; } = true;
        public string? ErrorMessage { get; set; }

        public AuditLog()
        {
        }

        public static AuditLog CreateAuthLog(
            string username,
            AuditActionType actionType,
            bool isSuccess,
            string? actorId = null,
            string? actorRole = null,
            string? errorMessage = null)
        {
            return new AuditLog
            {
                ActorId = actorId,
                ActorUsername = username,
                ActorRole = actorRole ?? string.Empty,
                ActionType = actionType,
                TargetEntity = "User",
                TargetDisplay = username,
                TargetId = actorId,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                Source = "WebAdmin"
            };
        }
    }
}
