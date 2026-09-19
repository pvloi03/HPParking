using System.Threading;
using System.Threading.Tasks;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Lanes;

namespace HPParking.Api.Services.Interfaces
{
    public interface ILaneService
    {
        Task<PagedResult<LaneDto>> GetLanesPagedAsync(LaneFilterQuery query, CancellationToken cancellationToken = default);
        Task<LaneDto> GetLaneByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<LaneDetailDto> GetLaneDetailByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<LaneDto> CreateLaneAsync(CreateLaneRequest request, CancellationToken cancellationToken = default);
        Task<LaneDto> UpdateLaneAsync(string id, UpdateLaneRequest request, CancellationToken cancellationToken = default);
        Task<bool> DeleteLaneAsync(string id, bool hardDelete = false, CancellationToken cancellationToken = default);
        Task<LaneDto> RestoreLaneAsync(string id, CancellationToken cancellationToken = default);
    }
}
