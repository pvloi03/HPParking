namespace HPParking.Api.DTOs.Excel
{
    /// <summary>
    /// Chế độ giải quyết xung đột khi phát hiện bản ghi trùng lặp lúc nhập liệu Excel (ADR 0023)
    /// </summary>
    public enum DuplicateMode
    {
        /// <summary>
        /// Bỏ qua bản ghi trùng lặp và tiếp tục xử lý các dòng khác
        /// </summary>
        Skip = 1,

        /// <summary>
        /// Cập nhật thông tin mới vào bản ghi hiện có
        /// </summary>
        Update = 2,

        /// <summary>
        /// Ghi nhận bản ghi trùng lặp như một lỗi dòng trong danh sách lỗi
        /// </summary>
        Error = 3
    }
}
