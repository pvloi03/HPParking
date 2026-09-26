using HPParking.Api.DTOs.Excel;

namespace HPParking.Api.Services.Interfaces
{
    /// <summary>
    /// Giao diện dịch vụ Nhập/Xuất Excel cho các danh mục Master Data (Nhóm II - ADR 0023)
    /// Hỗ trợ 6 thực thể: Companies, Departments, Contractors, Gates, Lanes, Devices
    /// </summary>
    public interface IMasterDataExcelService
    {
        /// <summary>
        /// Tạo file Excel mẫu chuẩn hóa kèm dropdown và tooltip cho thực thể
        /// </summary>
        /// <param name="entity">Tên thực thể (companies, departments, contractors, gates, lanes, devices)</param>
        Task<(byte[] Content, string FileName)> GenerateTemplateAsync(string entity);

        /// <summary>
        /// Nhập liệu từ file Excel hỗ trợ Dry-Run và xử lý trùng lặp (DuplicateMode)
        /// </summary>
        Task<ExcelImportResultDto> ImportAsync(
            string entity,
            Stream fileStream,
            DuplicateMode duplicateMode = DuplicateMode.Skip,
            bool dryRun = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Xuất dữ liệu danh mục ra file Excel có kẹp trần an toàn (tối đa 10,000 dòng)
        /// </summary>
        Task<(byte[] Content, string FileName, bool IsTruncated)> ExportAsync(
            string entity,
            CancellationToken cancellationToken = default);
    }
}
