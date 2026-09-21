namespace HPParking.Api.DTOs.AuditLogs
{
    /// <summary>
    /// DTO chi tiết bản ghi nhật ký kiểm toán kèm dữ liệu thay đổi và lý do
    /// </summary>
    public class AuditLogDetailDto : AuditLogDto
    {
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public List<string> ChangedProperties { get; set; } = new();
        public string? Reason { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
