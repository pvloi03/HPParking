using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.Clients
{
    public class ClientApprovalRequestDto
    {
        public string Id { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ApprovingDepartmentId { get; set; } = string.Empty;
        public string ApprovingDepartmentName { get; set; } = string.Empty;
        public string ApprovingDepartmentEmail { get; set; } = string.Empty;
        public ClientApprovalActionType ActionType { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsProcessed { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Note { get; set; }
        public bool EmailSent { get; set; } = true;
        public string? EmailError { get; set; }
    }
}
