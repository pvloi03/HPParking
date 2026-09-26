namespace HPParking.Api.DTOs.AuditLogs
{
    /// <summary>
    /// DTO chi tiết bản ghi nhật ký kiểm toán kèm dữ liệu thay đổi và lý do
    /// </summary>
    public class AuditLogDetailDto : AuditLogDto
    {
        public string? Reason { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
