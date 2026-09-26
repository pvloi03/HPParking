using Asp.Versioning;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Contractors;
using HPParking.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HPParking.Api.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ContractorsController : BaseApiController
    {
        private readonly IContractorService _contractorService;

        public ContractorsController(
            IContractorService contractorService,
            ILogger<ContractorsController> logger)
            : base(logger)
        {
            _contractorService = contractorService;
        }

        /// <summary>
        /// Lấy danh sách nhà thầu có phân trang và tìm kiếm theo từ khóa
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<ContractorDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        public async Task<IActionResult> GetContractors([FromQuery] ContractorFilterQuery query)
        {
            var result = await _contractorService.GetContractorsPagedAsync(query);
            return OkApiResponse(result, "Lấy danh sách nhà thầu thành công.");
        }

        /// <summary>
        /// Xem chi tiết thông tin nhà thầu theo Id
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<ContractorDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetContractorById(string id)
        {
            var contractor = await _contractorService.GetContractorByIdAsync(id);
            return OkApiResponse(contractor, "Lấy thông tin nhà thầu thành công.");
        }

        /// <summary>
        /// Tạo mới nhà thầu, kiểm tra tính duy nhất của mã Code
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<ContractorDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> CreateContractor([FromBody] CreateContractorRequest request)
        {
            var created = await _contractorService.CreateContractorAsync(request);
            return CreatedApiResponse($"/api/v1/contractors/{created.Id}", created, "Tạo mới nhà thầu thành công.");
        }

        /// <summary>
        /// Cập nhật thông tin nhà thầu, kiểm tra trùng mã Code nếu thay đổi
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<ContractorDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> UpdateContractor(string id, [FromBody] UpdateContractorRequest request)
        {
            var updated = await _contractorService.UpdateContractorAsync(id, request);
            return OkApiResponse(updated, "Cập nhật thông tin nhà thầu thành công.");
        }

        /// <summary>
        /// Xóa nhà thầu (Mặc định xóa mềm, chặn xóa nếu còn khách hàng/nhân sự trực thuộc theo ADR 0030 &amp; ADR 0033)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> DeleteContractor(string id, [FromQuery] bool hardDelete = false)
        {
            await _contractorService.DeleteContractorAsync(id, hardDelete);
            var msg = hardDelete
                ? "Đã xóa vĩnh viễn nhà thầu khỏi cơ sở dữ liệu."
                : "Đã xóa mềm nhà thầu thành công.";

            return OkApiResponse(true, msg);
        }

        /// <summary>
        /// Khôi phục nhà thầu từ thùng rác (ADR 0031 Recycle Bin &amp; Restore)
        /// </summary>
        [HttpPost("{id}/restore")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<ContractorDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 409)]
        public async Task<IActionResult> RestoreContractor(string id)
        {
            var restored = await _contractorService.RestoreContractorAsync(id);
            return OkApiResponse(restored, "Khôi phục nhà thầu thành công.");
        }
    }
}
