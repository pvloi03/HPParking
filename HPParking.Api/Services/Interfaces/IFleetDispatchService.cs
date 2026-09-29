using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.FleetDispatch;
using HPParking.Core.Models.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HPParking.Api.Services.Interfaces
{
    public interface IFleetDispatchService
    {
        Task<List<FleetTripDto>> GetActiveTripsAsync(CancellationToken cancellationToken = default);
        Task<PagedResult<FleetTripDto>> GetTripHistoryPagedAsync(PaginationQuery query, string? vehicleId = null, TripStatus? status = null, CancellationToken cancellationToken = default);
        Task<FleetTripDto> GetTripByIdAsync(string id, CancellationToken cancellationToken = default);
    }
}
