using HPParking.Core.Models.Common;
using HPParking.Core.Models.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace HPParking.Core.Models.Entities
{
    /// <summary>
    /// Thực thể lưu trữ yêu cầu phê duyệt trạng thái kích hoạt hồ sơ nhân sự qua email (ADR 0042)
    /// </summary>
    [BsonIgnoreExtraElements]
    public class ClientApprovalRequest : BaseEntity
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string ClientId { get; set; } = string.Empty;

        [BsonRepresentation(BsonType.ObjectId)]
        public string ApprovingDepartmentId { get; set; } = string.Empty;

        public string ApprovingDepartmentEmail { get; set; } = string.Empty;

        [BsonRepresentation(BsonType.String)]
        public ClientApprovalActionType ActionType { get; set; } = ClientApprovalActionType.Activate;

        /// <summary>
        /// Mã token bảo mật ngẫu nhiên phục vụ liên kết 1-click trong email
        /// </summary>
        public string Token { get; set; } = string.Empty;

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime ExpiresAt { get; set; }

        public bool IsProcessed { get; set; } = false;

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? ProcessedAt { get; set; }

        public string? RequestedByUserId { get; set; }

        public bool EmailSent { get; set; } = true;

        public string? EmailError { get; set; }

        /// <summary>
        /// Kiểm tra token còn hiệu lực và chưa được xử lý
        /// </summary>
        public bool IsValidForExecution() => !IsProcessed && !IsDeleted && DateTime.UtcNow <= ExpiresAt;
    }
}
