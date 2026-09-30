using Asp.Versioning;
using HPParking.Api.DTOs.Cards;
using HPParking.Api.DTOs.Common;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HPParking.Api.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class CardsController : BaseApiController
    {
        private readonly ICardService _cardService;

        public CardsController(
            ICardService cardService,
            ILogger<CardsController> logger)
            : base(logger)
        {
            _cardService = cardService;
        }

        /// <summary>
        /// Lấy danh sách thẻ có phân trang và lọc theo loại (Người / Phương tiện nội bộ), trạng thái
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<CardDto>>), 200)]
        public async Task<IActionResult> GetCards(
            [FromQuery] PaginationQuery query,
            [FromQuery] string? search,
            [FromQuery] CardTargetType? targetType,
            [FromQuery] CardStatus? status,
            CancellationToken cancellationToken)
        {
            var result = await _cardService.GetCardsPagedAsync(query, search, targetType, status, cancellationToken);
            return OkApiResponse(result, "Lấy danh sách thẻ thành công.");
        }

        /// <summary>
        /// Lấy chi tiết thẻ theo ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<CardDto>), 200)]
        public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
        {
            var result = await _cardService.GetByIdAsync(id, cancellationToken);
            return OkApiResponse(result, "Lấy thông tin thẻ thành công.");
        }

        /// <summary>
        /// Tạo mới thẻ định danh 10 số
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<CardDto>), 201)]
        public async Task<IActionResult> Create([FromBody] CreateCardRequest request, CancellationToken cancellationToken)
        {
            var result = await _cardService.CreateAsync(request, cancellationToken);
            return CreatedApiResponse(result, "Tạo mới thẻ thành công.");
        }

        /// <summary>
        /// Cập nhật thông tin thẻ
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<CardDto>), 200)]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateCardRequest request, CancellationToken cancellationToken)
        {
            var result = await _cardService.UpdateAsync(id, request, cancellationToken);
            return OkApiResponse(result, "Cập nhật thẻ thành công.");
        }

        /// <summary>
        /// Xóa thẻ khỏi hệ thống (Xóa mềm)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
        {
            await _cardService.DeleteAsync(id, cancellationToken);
            return OkApiResponse(new { id }, "Xóa thẻ thành công.");
        }
    }
}
