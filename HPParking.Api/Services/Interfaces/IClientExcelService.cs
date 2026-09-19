using System.Threading;
using System.Threading.Tasks;
using HPParking.Api.DTOs.Clients;
using HPParking.Api.DTOs.Excel;
using Microsoft.AspNetCore.Http;

namespace HPParking.Api.Services.Interfaces
{
    /// <summary>
    /// Giao diện dịch vụ nghiệp vụ Nhập/Xuất Excel cho Khách hàng và Phương tiện
    /// </summary>
    public interface IClientExcelService
    {
        /// <summary>
        /// Sinh tệp Excel mẫu chuẩn (.xlsx) có hướng dẫn, dropdown Loại xe và dòng mẫu
        /// </summary>
        Task<byte[]> GenerateTemplateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Nhập hàng loạt danh sách khách hàng và phương tiện từ file Excel với cơ chế Partial-Success và Dry-Run
        /// </summary>
        Task<ExcelImportResultDto> ImportClientsAsync(
            IFormFile file,
            bool dryRun = false,
            DuplicateMode duplicateMode = DuplicateMode.Skip,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Xuất dữ liệu danh sách khách hàng và phương tiện theo bộ lọc tìm kiếm (kẹp trần 10,000 dòng)
        /// </summary>
        Task<(byte[] Content, string FileName, bool IsTruncated)> ExportClientsAsync(
            ClientFilterQuery query,
            CancellationToken cancellationToken = default);
    }
}
