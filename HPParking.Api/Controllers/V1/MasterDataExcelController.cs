using Asp.Versioning;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Excel;
using HPParking.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace HPParking.Api.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/excel/master-data")]
    [Route("api/v{version:apiVersion}/excel")]
    public class MasterDataExcelController : BaseApiController
    {
        private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private readonly IMasterDataExcelService _masterDataExcelService;

        public MasterDataExcelController(
            IMasterDataExcelService masterDataExcelService,
            ILogger<MasterDataExcelController> logger)
            : base(logger)
        {
            _masterDataExcelService = masterDataExcelService;
        }

        /// <summary>
        /// Tải tệp Excel mẫu chuẩn hóa (.xlsx) cho danh mục Master Data
        /// </summary>
        /// <param name="entity">companies, departments, contractors, gates, lanes, devices</param>
        [HttpGet("{entity}/template")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        public async Task<IActionResult> DownloadTemplate(string entity)
        {
            var (content, fileName) = await _masterDataExcelService.GenerateTemplateAsync(entity);
            return File(content, ExcelContentType, fileName);
        }

        /// <summary>
        /// Nhập dữ liệu danh mục Master Data từ tệp Excel hỗ trợ Dry-Run và xử lý trùng lặp
        /// </summary>
        /// <param name="entity">companies, departments, contractors, gates, lanes, devices</param>
        /// <param name="file">Tệp Excel .xlsx tải lên</param>
        /// <param name="dryRun">Chỉ kiểm tra tính hợp lệ dữ liệu mà không lưu CSDL</param>
        /// <param name="duplicateMode">Chiến lược trùng mã: 1=Skip (bỏ qua), 2=Update (ghi đè), 3=Error (báo lỗi)</param>
        [HttpPost("{entity}/import")]
        [Authorize(Roles = "Manager,Admin")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<ExcelImportResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        public async Task<IActionResult> ImportMasterData(
            string entity,
            IFormFile file,
            [FromQuery] bool dryRun = false,
            [FromQuery] DuplicateMode duplicateMode = DuplicateMode.Skip)
        {
            await using var stream = file?.OpenReadStream();
            if (stream == null)
            {
                return ErrorApiResponse("Vui lòng chọn tệp Excel hợp lệ để tải lên.");
            }

            var result = await _masterDataExcelService.ImportAsync(entity, stream, duplicateMode, dryRun);
            string msg = dryRun
                ? $"Kiểm tra thử nghiệm hoàn tất ({entity}): {result.SuccessCount} hợp lệ, {result.FailedCount} lỗi."
                : $"Nhập dữ liệu hoàn tất ({entity}): {result.SuccessCount} thành công, {result.FailedCount} thất bại, {result.SkippedCount} bỏ qua.";

            return OkApiResponse(result, msg);
        }

        /// <summary>
        /// Xuất dữ liệu danh mục Master Data ra tệp Excel có kẹp trần an toàn (tối đa 10,000 dòng)
        /// </summary>
        /// <param name="entity">companies, departments, contractors, gates, lanes, devices</param>
        [HttpGet("{entity}/export")]
        [HttpPost("{entity}/export")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        public async Task<IActionResult> ExportMasterData(string entity)
        {
            var (content, fileName, isTruncated) = await _masterDataExcelService.ExportAsync(entity);
            if (isTruncated)
            {
                Response.Headers.Append("X-Export-Truncated", "true");
            }
            return File(content, ExcelContentType, fileName);
        }
    }
}
