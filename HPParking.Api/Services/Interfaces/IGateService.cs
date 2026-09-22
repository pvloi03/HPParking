using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Gates;

namespace HPParking.Api.Services.Interfaces
{
    public interface IGateService
    {
        Task<PagedResult<GateDto>> GetGatesPagedAsync(GateFilterQuery query, CancellationToken cancellationToken = default);
        Task<GateDto> GetGateByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<GateDto> CreateGateAsync(CreateGateRequest request, CancellationToken cancellationToken = default);
        Task<GateDto> UpdateGateAsync(string id, UpdateGateRequest request, CancellationToken cancellationToken = default);
        Task<bool> DeleteGateAsync(string id, bool hardDelete = false, CancellationToken cancellationToken = default);
        Task<GateDto> RestoreGateAsync(string id, CancellationToken cancellationToken = default);
    }
}
