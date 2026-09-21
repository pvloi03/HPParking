using HPParking.Api.DTOs.Excel;
using HPParking.Api.DTOs.Vehicles;

namespace HPParking.Api.Services.Interfaces
{
    /// <summary>
    /// Giao diện dịch vụ nghiệp vụ Nhập/Xuất Excel cho Phương tiện (VEHICLES)
    /// </summary>
    public interface IVehicleExcelService
    {
        /// <summary>
        /// Sinh tệp Excel mẫu chuẩn (.xlsx) có hướng dẫn, dropdown Loại xe và dòng mẫu cho Phương tiện
        /// </summary>
        Task<byte[]> GenerateTemplateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Nhập hàng loạt danh sách phương tiện từ IFormFile với cơ chế Partial-Success và Dry-Run
        /// </summary>
        Task<ExcelImportResultDto> ImportVehiclesAsync(
            IFormFile file,
            bool dryRun = false,
            DuplicateMode duplicateMode = DuplicateMode.Skip,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Nhập hàng loạt danh sách phương tiện từ Stream với cơ chế Partial-Success và Dry-Run
        /// </summary>
        Task<ExcelImportResultDto> ImportVehiclesAsync(
            Stream stream,
            bool dryRun = false,
            DuplicateMode duplicateMode = DuplicateMode.Skip,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Xuất dữ liệu danh sách phương tiện theo bộ lọc tìm kiếm (kẹp trần 10,000 dòng, khóa ngoại đổi thành Tên chủ xe)
        /// </summary>
        Task<(byte[] Content, string FileName, bool IsTruncated)> ExportVehiclesAsync(
            VehicleFilterQuery query,
            CancellationToken cancellationToken = default);
    }
}
