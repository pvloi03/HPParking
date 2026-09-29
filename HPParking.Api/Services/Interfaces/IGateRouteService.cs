using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.GateRoutes;
using System.Threading;
using System.Threading.Tasks;

namespace HPParking.Api.Services.Interfaces
{
    public interface IGateRouteService
    {
        Task<PagedResult<GateRouteDto>> GetRoutesPagedAsync(PaginationQuery query, string? search = null, bool? isActive = null, CancellationToken cancellationToken = default);
        Task<GateRouteDto> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<GateRouteDto> CreateAsync(CreateGateRouteRequest request, CancellationToken cancellationToken = default);
        Task<GateRouteDto> UpdateAsync(string id, UpdateGateRouteRequest request, CancellationToken cancellationToken = default);
        Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    }
}
