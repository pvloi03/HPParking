using Asp.Versioning;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Excel;
using HPParking.Api.DTOs.Vehicles;
using HPParking.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HPParking.Api.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/excel/vehicles")]
    public class VehiclesExcelController : BaseApiController
    {
        private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private readonly IVehicleExcelService _vehicleExcelService;

        public VehiclesExcelController(
            IVehicleExcelService vehicleExcelService,
            ILogger<VehiclesExcelController> logger)
            : base(logger)
        {
            _vehicleExcelService = vehicleExcelService;
        }

        /// <summary>
        /// Tải tệp Excel mẫu chuẩn (.xlsx) để nhập danh sách phương tiện (chỉ gồm các trường cơ bản)
        /// </summary>
        [HttpGet("template")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        public async Task<IActionResult> DownloadTemplate()
        {
            var content = await _vehicleExcelService.GenerateTemplateAsync();
            return File(content, ExcelContentType, "vehicle_import_template.xlsx");
        }

        /// <summary>
        /// Nhập hàng loạt phương tiện từ tệp Excel
        /// </summary>
        [HttpPost("import")]
        [Authorize(Roles = "Manager,Admin")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<ExcelImportResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        public async Task<IActionResult> ImportVehicles(
            IFormFile file,
            [FromQuery] bool dryRun = false,
            [FromQuery] DuplicateMode duplicateMode = DuplicateMode.Skip)
        {
            var result = await _vehicleExcelService.ImportVehiclesAsync(file, dryRun, duplicateMode);
            string msg = dryRun
                ? $"Kiểm tra thử nghiệm hoàn tất: {result.SuccessCount} hợp lệ, {result.FailedCount} lỗi."
                : $"Nhập dữ liệu hoàn tất: {result.SuccessCount} thành công, {result.FailedCount} thất bại, {result.SkippedCount} bỏ qua.";

            return OkApiResponse(result, msg);
        }

        /// <summary>
        /// Xuất danh sách phương tiện ra tệp Excel (POST kèm body bộ lọc)
        /// </summary>
        [HttpPost("export")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        public async Task<IActionResult> ExportVehiclesPost([FromBody] VehicleFilterQuery query)
        {
            var (content, fileName, isTruncated) = await _vehicleExcelService.ExportVehiclesAsync(query ?? new VehicleFilterQuery());
            if (isTruncated)
            {
                Response.Headers.Append("X-Export-Truncated", "true");
            }
            return File(content, ExcelContentType, fileName);
        }

        /// <summary>
        /// Xuất danh sách phương tiện ra tệp Excel (GET thuận tiện tải trực tiếp từ trình duyệt)
        /// </summary>
        [HttpGet("export")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        public async Task<IActionResult> ExportVehiclesGet([FromQuery] VehicleFilterQuery query)
        {
            var (content, fileName, isTruncated) = await _vehicleExcelService.ExportVehiclesAsync(query);
            if (isTruncated)
            {
                Response.Headers.Append("X-Export-Truncated", "true");
            }
            return File(content, ExcelContentType, fileName);
        }
    }
}
