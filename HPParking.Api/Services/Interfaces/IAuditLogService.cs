using HPParking.Api.DTOs.AuditLogs;
using HPParking.Api.DTOs.Common;

namespace HPParking.Api.Services.Interfaces
{
    /// <summary>
    /// Giao diện dịch vụ tra cứu chỉ đọc nhật ký kiểm toán hệ thống (Audit Log - Read-Only và Admin Only)
    /// </summary>
    public interface IAuditLogService
    {
        /// <summary>
        /// Lấy danh sách nhật ký kiểm toán có phân trang và bộ lọc đa tiêu chí
        /// </summary>
        Task<PagedResult<AuditLogDto>> GetAuditLogsPagedAsync(AuditLogFilterQuery query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy chi tiết một bản ghi nhật ký kiểm toán kèm dữ liệu thay đổi và lý do
        /// </summary>
        Task<AuditLogDetailDto> GetAuditLogByIdAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Ghi nhận sự kiện kiểm toán hệ thống bất biến vào CSDL
        /// </summary>
        Task LogActivityAsync(
            HPParking.Core.Models.Enums.AuditActionType actionType,
            string targetEntity,
            string? targetId = null,
            string? targetDisplay = null,
            string? reason = null,
            bool isSuccess = true,
            string? errorMessage = null,
            CancellationToken cancellationToken = default);
    }
}
