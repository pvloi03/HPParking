using System.Threading.Tasks;
using Asp.Versioning;
using HPParking.Api.DTOs.Clients;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Excel;
using HPParking.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HPParking.Api.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/excel/clients")]
    public class ClientsExcelController : BaseApiController
    {
        private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private readonly IClientExcelService _clientExcelService;

        public ClientsExcelController(
            IClientExcelService clientExcelService,
            ILogger<ClientsExcelController> logger)
            : base(logger)
        {
            _clientExcelService = clientExcelService;
        }

        /// <summary>
        /// Tải tệp Excel mẫu chuẩn (.xlsx) để nhập danh sách khách hàng và phương tiện
        /// </summary>
        [HttpGet("template")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        public async Task<IActionResult> DownloadTemplate()
        {
            var content = await _clientExcelService.GenerateTemplateAsync();
            return File(content, ExcelContentType, "client_import_template.xlsx");
        }

        /// <summary>
        /// Nhập hàng loạt khách hàng và phương tiện từ tệp Excel
        /// </summary>
        [HttpPost("import")]
        [Authorize(Roles = "Manager,Admin")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<ExcelImportResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        public async Task<IActionResult> ImportClients(
            IFormFile file,
            [FromQuery] bool dryRun = false,
            [FromQuery] DuplicateMode duplicateMode = DuplicateMode.Skip)
        {
            var result = await _clientExcelService.ImportClientsAsync(file, dryRun, duplicateMode);
            string msg = dryRun
                ? $"Kiểm tra thử nghiệm hoàn tất: {result.SuccessCount} hợp lệ, {result.FailedCount} lỗi."
                : $"Nhập dữ liệu hoàn tất: {result.SuccessCount} thành công, {result.FailedCount} thất bại, {result.SkippedCount} bỏ qua.";

            return OkApiResponse(result, msg);
        }

        /// <summary>
        /// Xuất danh sách khách hàng và phương tiện ra tệp Excel (POST kèm body bộ lọc)
        /// </summary>
        [HttpPost("export")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        public async Task<IActionResult> ExportClientsPost([FromBody] ClientFilterQuery query)
        {
            var (content, fileName, isTruncated) = await _clientExcelService.ExportClientsAsync(query ?? new ClientFilterQuery());
            if (isTruncated)
            {
                Response.Headers.Append("X-Export-Truncated", "true");
            }
            return File(content, ExcelContentType, fileName);
        }

        /// <summary>
        /// Xuất danh sách khách hàng và phương tiện ra tệp Excel (GET thuận tiện tải trực tiếp từ trình duyệt)
        /// </summary>
        [HttpGet("export")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        public async Task<IActionResult> ExportClientsGet([FromQuery] ClientFilterQuery query)
        {
            var (content, fileName, isTruncated) = await _clientExcelService.ExportClientsAsync(query);
            if (isTruncated)
            {
                Response.Headers.Append("X-Export-Truncated", "true");
            }
            return File(content, ExcelContentType, fileName);
        }
    }
}
