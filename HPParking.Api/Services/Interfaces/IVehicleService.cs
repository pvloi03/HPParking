using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Vehicles;

namespace HPParking.Api.Services.Interfaces
{
    public interface IVehicleService
    {
        Task<PagedResult<VehicleDto>> GetVehiclesPagedAsync(VehicleFilterQuery query, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<VehicleDto>> GetVehiclesByClientIdAsync(string clientId, CancellationToken cancellationToken = default);
        Task<VehicleDto> GetVehicleByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<VehicleDto> CreateVehicleAsync(string clientId, CreateVehicleRequest request, CancellationToken cancellationToken = default);
        Task<VehicleDto> UpdateVehicleAsync(string id, UpdateVehicleRequest request, CancellationToken cancellationToken = default);
        Task<bool> DeleteVehicleAsync(string id, bool hardDelete = false, CancellationToken cancellationToken = default);
    }
}
