using HPParking.Api.DTOs.AuditLogs;
using HPParking.Api.DTOs.ParkingSessions;
using HPParking.Api.DTOs.Statistics;

namespace HPParking.Api.Services.Interfaces
{
    /// <summary>
    /// Giao diện dịch vụ Xuất Excel cho Sổ cái và Báo cáo (Nhóm III - ADR 0023, ADR 0043)
    /// Chỉ hỗ trợ Xuất (Read-only Safe Export), tuyệt đối không mở Import/Template cho Sổ cái
    /// </summary>
    public interface IReportExcelService
    {
        /// <summary>
        /// Xuất lịch sử phiên đỗ xe (ParkingSessions) kẹp trần tối đa 10,000 dòng
        /// </summary>
        Task<(byte[] Content, string FileName, bool IsTruncated)> ExportParkingSessionsAsync(
            ParkingSessionFilterQuery query,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Xuất nhật ký kiểm toán hệ thống (AuditLogs) - Dành riêng cho Quản trị viên (Admin)
        /// </summary>
        Task<(byte[] Content, string FileName, bool IsTruncated)> ExportAuditLogsAsync(
            AuditLogFilterQuery query,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Xuất báo cáo tổng hợp lưu lượng lượt ra vào theo từng người và phương tiện
        /// </summary>
        Task<(byte[] Content, string FileName, bool IsTruncated)> ExportTrafficSummaryAsync(
            TrafficSummaryFilterQuery query,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Xuất báo cáo doanh thu 2 sheet (Tổng hợp theo ngày &amp; Chi tiết giao dịch) kẹp trần an toàn 10,000 dòng
        /// </summary>
        Task<(byte[] Content, string FileName, bool IsTruncated)> ExportRevenueReportAsync(
            RevenueStatisticsQuery query,
            CancellationToken cancellationToken = default);
    }
}
