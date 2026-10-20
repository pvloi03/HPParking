using HPParking.Core.Models.Enums;

namespace HPParking.Api.DTOs.Clients
{
    public class RequestClientStatusChangeRequest
    {
        /// <summary>
        /// ID Phòng ban phê duyệt nhận email (bắt buộc)
        /// </summary>
        public string ApprovingDepartmentId { get; set; } = string.Empty;

        /// <summary>
        /// Loại hành động: Activate (Kích hoạt lại) hoặc Deactivate (Khóa)
        /// </summary>
        public ClientApprovalActionType ActionType { get; set; } = ClientApprovalActionType.Activate;

        /// <summary>
        /// Ghi chú / lý do thay đổi trạng thái
        /// </summary>
        public string? Note { get; set; }
    }
}
