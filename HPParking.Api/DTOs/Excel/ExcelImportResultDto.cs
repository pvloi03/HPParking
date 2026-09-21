namespace HPParking.Api.DTOs.Excel
{
    /// <summary>
    /// DTO chi tiết lỗi của một dòng trong tệp Excel trả về cho máy khách
    /// </summary>
    public class ExcelRowErrorDto
    {
        public int Row { get; set; }
        public string Column { get; set; } = string.Empty;
        public string? Value { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO phản hồi kết quả nhập liệu hàng loạt tệp Excel (Partial Success và Dry Run)
    /// </summary>
    public class ExcelImportResultDto
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public int SkippedCount { get; set; }
        public bool IsDryRun { get; set; }
        public List<ExcelRowErrorDto> Errors { get; set; } = new();
    }
}
