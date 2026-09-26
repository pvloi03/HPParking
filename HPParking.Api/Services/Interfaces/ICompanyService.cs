using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Companies;

namespace HPParking.Api.Services.Interfaces
{
    public interface ICompanyService
    {
        Task<PagedResult<CompanyDto>> GetCompaniesPagedAsync(CompanyFilterQuery query, CancellationToken cancellationToken = default);
        Task<CompanyDto> GetCompanyByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<CompanyDto> CreateCompanyAsync(CreateCompanyRequest request, CancellationToken cancellationToken = default);
        Task<CompanyDto> UpdateCompanyAsync(string id, UpdateCompanyRequest request, CancellationToken cancellationToken = default);
        Task<bool> DeleteCompanyAsync(string id, bool hardDelete = false, CancellationToken cancellationToken = default);
        Task<CompanyDto> RestoreCompanyAsync(string id, CancellationToken cancellationToken = default);
    }
}
