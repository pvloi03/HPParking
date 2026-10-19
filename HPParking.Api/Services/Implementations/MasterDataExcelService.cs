using HPParking.Api.Common.Excel;
using HPParking.Api.Common.Exceptions;
using HPParking.Api.DTOs.Excel;
using HPParking.Api.DTOs.Vehicles;
using HPParking.Api.Services.Interfaces;

namespace HPParking.Api.Services.Implementations
{
    /// <summary>
    /// Hiện thực hóa dịch vụ Nhập/Xuất Excel cho danh mục Master Data (Nhóm II - ADR 0023)
    /// </summary>
    public class MasterDataExcelService : IMasterDataExcelService
    {
        private readonly IVehicleExcelService _vehicleExcelService;
        private readonly ILogger<MasterDataExcelService> _logger;

        public MasterDataExcelService(
            IVehicleExcelService vehicleExcelService,
            ILogger<MasterDataExcelService> logger)
        {
            _vehicleExcelService = vehicleExcelService;
            _logger = logger;
        }

        public async Task<(byte[] Content, string FileName)> GenerateTemplateAsync(string entity)
        {
            var normalized = NormalizeEntity(entity);
            return normalized switch
            {
                "vehicles" => (await _vehicleExcelService.GenerateTemplateAsync(), "Mau_Nhap_Phuong_Tien.xlsx"),
                _ => throw new BadRequestException($"Thực thể Master Data '{entity}' không hợp lệ. Các giá trị hỗ trợ: vehicles.")
            };
        }

        public async Task<ExcelImportResultDto> ImportAsync(
            string entity,
            Stream fileStream,
            DuplicateMode duplicateMode = DuplicateMode.Skip,
            bool dryRun = false,
            CancellationToken cancellationToken = default)
        {
            ExcelFileValidator.Validate(fileStream);

            var normalized = NormalizeEntity(entity);
            return normalized switch
            {
                "vehicles" => await _vehicleExcelService.ImportVehiclesAsync(fileStream, dryRun, duplicateMode, cancellationToken),
                _ => throw new BadRequestException($"Thực thể Master Data '{entity}' không hợp lệ.")
            };
        }

        public async Task<(byte[] Content, string FileName, bool IsTruncated)> ExportAsync(
            string entity,
            CancellationToken cancellationToken = default)
        {
            var normalized = NormalizeEntity(entity);

            return normalized switch
            {
                "vehicles" => await _vehicleExcelService.ExportVehiclesAsync(new VehicleFilterQuery(), cancellationToken),
                _ => throw new BadRequestException($"Thực thể Master Data '{entity}' không hợp lệ.")
            };
        }

        #region Helpers

        private static string NormalizeEntity(string entity) => (entity ?? string.Empty).Trim().ToLowerInvariant();

        #endregion
    }
}
