using System.Threading.Tasks;
using Asp.Versioning;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Companies;
using HPParking.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HPParking.Api.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class CompaniesController : BaseApiController
    {
        private readonly ICompanyService _companyService;

        public CompaniesController(
            ICompanyService companyService,
            ILogger<CompaniesController> logger)
            : base(logger)
        {
            _companyService = companyService;
        }

        /// <summary>
        /// Lấy danh sách toàn bộ công ty / đơn vị thành viên có phân trang và tìm kiếm theo từ khóa
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<CompanyDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        public async Task<IActionResult> GetCompanies([FromQuery] CompanyFilterQuery query)
        {
            var result = await _companyService.GetCompaniesPagedAsync(query);
            return OkApiResponse(result, "Lấy danh sách công ty thành công.");
        }

        /// <summary>
        /// Xem chi tiết thông tin một công ty theo Id
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<CompanyDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetCompanyById(string id)
        {
            var company = await _companyService.GetCompanyByIdAsync(id);
            return OkApiResponse(company, "Lấy thông tin công ty thành công.");
        }

        /// <summary>
        /// Tạo mới công ty, kiểm tra tính duy nhất của mã Code
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<CompanyDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> CreateCompany([FromBody] CreateCompanyRequest request)
        {
            var created = await _companyService.CreateCompanyAsync(request);
            return CreatedApiResponse($"/api/v1/companies/{created.Id}", created, "Tạo mới công ty thành công.");
        }

        /// <summary>
        /// Cập nhật thông tin công ty, kiểm tra trùng mã Code nếu thay đổi
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<CompanyDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> UpdateCompany(string id, [FromBody] UpdateCompanyRequest request)
        {
            var updated = await _companyService.UpdateCompanyAsync(id, request);
            return OkApiResponse(updated, "Cập nhật thông tin công ty thành công.");
        }

        /// <summary>
        /// Xóa công ty (Mặc định xóa mềm, chặn xóa nếu còn phòng ban hoặc cổng trực thuộc theo ADR 0030)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> DeleteCompany(string id, [FromQuery] bool hardDelete = false)
        {
            await _companyService.DeleteCompanyAsync(id, hardDelete);
            var msg = hardDelete
                ? "Đã xóa vĩnh viễn công ty khỏi cơ sở dữ liệu."
                : "Đã xóa mềm công ty thành công.";

            return OkApiResponse(true, msg);
        }

        /// <summary>
        /// Khôi phục công ty từ thùng rác (ADR 0031 Recycle Bin &amp; Restore)
        /// </summary>
        [HttpPost("{id}/restore")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<CompanyDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> RestoreCompany(string id)
        {
            var restored = await _companyService.RestoreCompanyAsync(id);
            return OkApiResponse(restored, "Khôi phục công ty thành công.");
        }
    }
}
