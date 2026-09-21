using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Contractors;

namespace HPParking.Api.Services.Interfaces
{
    public interface IContractorService
    {
        Task<PagedResult<ContractorDto>> GetContractorsPagedAsync(ContractorFilterQuery query, CancellationToken cancellationToken = default);
        Task<ContractorDto> GetContractorByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<ContractorDto> CreateContractorAsync(CreateContractorRequest request, CancellationToken cancellationToken = default);
        Task<ContractorDto> UpdateContractorAsync(string id, UpdateContractorRequest request, CancellationToken cancellationToken = default);
        Task<bool> DeleteContractorAsync(string id, bool hardDelete = false, CancellationToken cancellationToken = default);
        Task<ContractorDto> RestoreContractorAsync(string id, CancellationToken cancellationToken = default);
    }
}
