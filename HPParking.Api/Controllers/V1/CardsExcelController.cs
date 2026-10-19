using Asp.Versioning;
using HPParking.Api.DTOs.Cards;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Excel;
using HPParking.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HPParking.Api.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/excel/cards")]
    public class CardsExcelController : BaseApiController
    {
        private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private readonly ICardExcelService _cardExcelService;

        public CardsExcelController(
            ICardExcelService cardExcelService,
            ILogger<CardsExcelController> logger)
            : base(logger)
        {
            _cardExcelService = cardExcelService;
        }

        /// <summary>
        /// Tải tệp Excel mẫu chuẩn (.xlsx) để nhập danh sách thẻ định danh
        /// </summary>
        [HttpGet("template")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        public async Task<IActionResult> DownloadTemplate()
        {
            var content = await _cardExcelService.GenerateTemplateAsync();
            return File(content, ExcelContentType, "card_import_template.xlsx");
        }

        /// <summary>
        /// Nhập hàng loạt thẻ định danh từ tệp Excel hỗ trợ Dry-Run và xử lý trùng lặp
        /// </summary>
        [HttpPost("import")]
        [Authorize(Roles = "Manager,Admin")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<ExcelImportResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        public async Task<IActionResult> ImportCards(
            IFormFile file,
            [FromQuery] bool dryRun = false,
            [FromQuery] DuplicateMode duplicateMode = DuplicateMode.Skip)
        {
            var result = await _cardExcelService.ImportCardsAsync(file, dryRun, duplicateMode);
            string msg = dryRun
                ? $"Kiểm tra thử nghiệm hoàn tất: {result.SuccessCount} hợp lệ, {result.FailedCount} lỗi."
                : $"Nhập dữ liệu hoàn tất: {result.SuccessCount} thành công, {result.FailedCount} thất bại, {result.SkippedCount} bỏ qua.";

            return OkApiResponse(result, msg);
        }

        /// <summary>
        /// Xuất danh sách thẻ định danh ra tệp Excel (POST kèm body bộ lọc)
        /// </summary>
        [HttpPost("export")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        public async Task<IActionResult> ExportCardsPost([FromBody] CardFilterQuery query)
        {
            var (content, fileName, isTruncated) = await _cardExcelService.ExportCardsAsync(query ?? new CardFilterQuery());
            if (isTruncated)
            {
                Response.Headers.Append("X-Export-Truncated", "true");
            }
            return File(content, ExcelContentType, fileName);
        }

        /// <summary>
        /// Xuất danh sách thẻ định danh ra tệp Excel (GET thuận tiện tải trực tiếp từ trình duyệt)
        /// </summary>
        [HttpGet("export")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        public async Task<IActionResult> ExportCardsGet([FromQuery] CardFilterQuery query)
        {
            var (content, fileName, isTruncated) = await _cardExcelService.ExportCardsAsync(query ?? new CardFilterQuery());
            if (isTruncated)
            {
                Response.Headers.Append("X-Export-Truncated", "true");
            }
            return File(content, ExcelContentType, fileName);
        }
    }
}
