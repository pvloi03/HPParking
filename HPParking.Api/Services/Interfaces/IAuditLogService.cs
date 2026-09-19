using System.Threading;
using System.Threading.Tasks;
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
    }
}
