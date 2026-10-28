using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.Clients
{
    public class ConfirmClientStatusResponse
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string ClientCode { get; set; } = string.Empty;
        public ClientApprovalActionType ActionType { get; set; }
        public bool NewActiveStatus { get; set; }
        public DateTime ProcessedAt { get; set; }
        public string ApprovingDepartmentName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
