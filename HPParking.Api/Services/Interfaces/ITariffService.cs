using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Tariffs;
using HPParking.Core.Models.Enums;

namespace HPParking.Api.Services.Interfaces
{
    public interface ITariffService
    {
        Task<PagedResult<TariffDto>> GetPagedAsync(
            PaginationQuery query,
            string? search = null,
            VehicleType? vehicleType = null,
            TariffPricingMode? pricingMode = null,
            bool? isActive = null,
            bool onlyDeleted = false,
            CancellationToken cancellationToken = default);

        Task<TariffDto> GetByIdAsync(string id, CancellationToken cancellationToken = default);

        Task<TariffDto> CreateAsync(CreateTariffRequest request, CancellationToken cancellationToken = default);

        Task<TariffDto> UpdateAsync(string id, UpdateTariffRequest request, CancellationToken cancellationToken = default);

        Task DeleteAsync(string id, CancellationToken cancellationToken = default);

        Task<TariffDto> RestoreTariffAsync(string id, CancellationToken cancellationToken = default);
    }
}
