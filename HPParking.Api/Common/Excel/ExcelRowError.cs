namespace HPParking.Api.Common.Excel
{
    /// <summary>
    /// Bản ghi lưu trữ thông tin lỗi chi tiết của một dòng dữ liệu trong tệp Excel
    /// </summary>
    public class ExcelRowError
    {
        public int Row { get; set; }
        public string Column { get; set; } = string.Empty;
        public string? Value { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
