using Asp.Versioning;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.FleetDispatch;
using HPParking.Api.Services.Interfaces;
using HPParking.Core.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HPParking.Api.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/fleet-dispatch")]
    public class FleetDispatchController : BaseApiController
    {
        private readonly IFleetDispatchService _fleetService;

        public FleetDispatchController(
            IFleetDispatchService fleetService,
            ILogger<FleetDispatchController> logger)
            : base(logger)
        {
            _fleetService = fleetService;
        }

        /// <summary>
        /// Lấy danh sách các phương tiện nội bộ đang trong chuyến đi (InTransit, WorkingAtGate, Overdue)
        /// </summary>
        [HttpGet("active")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<List<FleetTripDto>>), 200)]
        public async Task<IActionResult> GetActiveTrips(CancellationToken cancellationToken)
        {
            var result = await _fleetService.GetActiveTripsAsync(cancellationToken);
            return OkApiResponse(result, "Lấy danh sách phương tiện đang điều vận thành công.");
        }

        /// <summary>
        /// Lấy lịch sử các chuyến đi có phân trang và lọc theo trạng thái, phương tiện
        /// </summary>
        [HttpGet("history")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<FleetTripDto>>), 200)]
        public async Task<IActionResult> GetTripHistory(
            [FromQuery] PaginationQuery query,
            [FromQuery] string? vehicleId,
            [FromQuery] TripStatus? status,
            CancellationToken cancellationToken)
        {
            var result = await _fleetService.GetTripHistoryPagedAsync(query, vehicleId, status, cancellationToken);
            return OkApiResponse(result, "Lấy lịch sử điều vận thành công.");
        }

        /// <summary>
        /// Lấy chi tiết một chuyến đi
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Viewer,Manager,Admin")]
        [ProducesResponseType(typeof(ApiResponse<FleetTripDto>), 200)]
        public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
        {
            var result = await _fleetService.GetTripByIdAsync(id, cancellationToken);
            return OkApiResponse(result, "Lấy chi tiết chuyến đi thành công.");
        }
    }
}
