using HPParking.Api.DTOs.Excel;

namespace HPParking.Api.Common.Excel
{
    /// <summary>
    /// Các tùy chọn điều khiển hành vi khi nhập liệu tệp Excel
    /// </summary>
    public class ExcelImportOptions
    {
        /// <summary>
        /// Chế độ chạy thử nghiệm giả lập: Chỉ kiểm tra tính hợp lệ và không ghi dữ liệu vào CSDL
        /// </summary>
        public bool IsDryRun { get; set; }

        /// <summary>
        /// Hành vi xử lý khi gặp bản ghi trùng lặp (Mặc định: Skip)
        /// </summary>
        public DuplicateMode DuplicateAction { get; set; } = DuplicateMode.Skip;
    }
}
