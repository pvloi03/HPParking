using HPParking.Api.DTOs.Cards;
using HPParking.Api.DTOs.Excel;

namespace HPParking.Api.Services.Interfaces
{
    /// <summary>
    /// Giao diện dịch vụ nghiệp vụ Nhập/Xuất Excel cho Thẻ định danh (CARDS)
    /// </summary>
    public interface ICardExcelService
    {
        /// <summary>
        /// Sinh tệp Excel mẫu chuẩn (.xlsx) có hướng dẫn, dropdown và dòng mẫu cho Thẻ định danh
        /// </summary>
        Task<byte[]> GenerateTemplateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Nhập hàng loạt danh sách thẻ định danh từ IFormFile với cơ chế Partial-Success và Dry-Run
        /// </summary>
        Task<ExcelImportResultDto> ImportCardsAsync(
            IFormFile file,
            bool dryRun = false,
            DuplicateMode duplicateMode = DuplicateMode.Skip,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Nhập hàng loạt danh sách thẻ định danh từ Stream với cơ chế Partial-Success và Dry-Run
        /// </summary>
        Task<ExcelImportResultDto> ImportCardsAsync(
            Stream stream,
            bool dryRun = false,
            DuplicateMode duplicateMode = DuplicateMode.Skip,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Xuất dữ liệu danh sách thẻ theo bộ lọc tìm kiếm (kẹp trần 10,000 dòng, phân giải tên đối tượng)
        /// </summary>
        Task<(byte[] Content, string FileName, bool IsTruncated)> ExportCardsAsync(
            CardFilterQuery query,
            CancellationToken cancellationToken = default);
    }
}
