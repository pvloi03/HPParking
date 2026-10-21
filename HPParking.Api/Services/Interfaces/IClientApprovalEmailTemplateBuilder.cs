using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using System.Collections.Generic;

namespace HPParking.Api.Services.Interfaces
{
    /// <summary>
    /// Model dữ liệu truyền vào để tạo nội dung email phê duyệt hồ sơ nhân sự (ADR 0042)
    /// </summary>
    public class ClientApprovalEmailModel
    {
        public Client Client { get; set; } = null!;
        public Department TargetDepartment { get; set; } = null!;
        public ClientApprovalRequest ApprovalRequest { get; set; } = null!;
        public string ConfirmUrl { get; set; } = string.Empty;
        public string? WorkingDepartmentName { get; set; }
        public IReadOnlyList<string>? VehicleSummaries { get; set; }
    }

    /// <summary>
    /// Giao diện tạo tiêu đề và nội dung HTML template email phê duyệt hồ sơ nhân sự
    /// </summary>
    public interface IClientApprovalEmailTemplateBuilder
    {
        /// <summary>
        /// Tạo tiêu đề (Subject) email phê duyệt
        /// </summary>
        string BuildSubject(Client client, ClientApprovalActionType actionType);

        /// <summary>
        /// Tạo nội dung HTML body email phê duyệt kèm liên kết xác nhận
        /// </summary>
        string BuildHtmlBody(ClientApprovalEmailModel model);
    }
}
