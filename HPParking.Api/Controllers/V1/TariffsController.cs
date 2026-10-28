using Asp.Versioning;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Tariffs;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HPParking.Api.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class TariffsController : BaseApiController
    {
        private readonly ITariffService _tariffService;

        public TariffsController(
            ITariffService tariffService,
            ILogger<TariffsController> logger)
            : base(logger)
        {
            _tariffService = tariffService;
        }

        /// <summary>
        /// Lấy danh sách biểu phí có phân trang và lọc
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<TariffDto>>), 200)]
        public async Task<IActionResult> GetTariffs(
            [FromQuery] PaginationQuery query,
            [FromQuery] string? search,
            [FromQuery] VehicleType? vehicleType,
            [FromQuery] TariffPricingMode? pricingMode,
            [FromQuery] bool? isActive,
            CancellationToken cancellationToken)
        {
            var result = await _tariffService.GetPagedAsync(query, search, vehicleType, pricingMode, isActive, onlyDeleted: false, cancellationToken);
            return OkApiResponse(result, "Lấy danh sách biểu phí thành công.");
        }

        /// <summary>
        /// Lấy danh sách biểu phí trong thùng rác
        /// </summary>
        [HttpGet("deleted")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<TariffDto>>), 200)]
        public async Task<IActionResult> GetDeletedTariffs(
            [FromQuery] PaginationQuery query,
            [FromQuery] string? search,
            CancellationToken cancellationToken)
        {
            var result = await _tariffService.GetPagedAsync(query, search, onlyDeleted: true, cancellationToken: cancellationToken);
            return OkApiResponse(result, "Lấy danh sách biểu phí đã xóa thành công.");
        }

        /// <summary>
        /// Lấy chi tiết biểu phí theo ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<TariffDto>), 200)]
        public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
        {
            var result = await _tariffService.GetByIdAsync(id, cancellationToken);
            return OkApiResponse(result, "Lấy thông tin biểu phí thành công.");
        }

        /// <summary>
        /// Tạo mới biểu phí
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<TariffDto>), 201)]
        public async Task<IActionResult> Create([FromBody] CreateTariffRequest request, CancellationToken cancellationToken)
        {
            var result = await _tariffService.CreateAsync(request, cancellationToken);
            return CreatedApiResponse(result, "Tạo mới biểu phí thành công.");
        }

        /// <summary>
        /// Cập nhật biểu phí
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<TariffDto>), 200)]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateTariffRequest request, CancellationToken cancellationToken)
        {
            var result = await _tariffService.UpdateAsync(id, request, cancellationToken);
            return OkApiResponse(result, "Cập nhật biểu phí thành công.");
        }

        /// <summary>
        /// Xóa biểu phí (vào thùng rác)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
        {
            await _tariffService.DeleteAsync(id, cancellationToken);
            return OkApiResponse(new { id }, "Xóa biểu phí thành công.");
        }

        /// <summary>
        /// Khôi phục biểu phí từ thùng rác
        /// </summary>
        [HttpPost("{id}/restore")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<TariffDto>), 200)]
        public async Task<IActionResult> Restore(string id, CancellationToken cancellationToken)
        {
            var result = await _tariffService.RestoreTariffAsync(id, cancellationToken);
            return OkApiResponse(result, "Khôi phục biểu phí thành công.");
        }
    }
}
