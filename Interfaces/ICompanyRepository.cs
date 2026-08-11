using HPParking.Models.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HPParking.Interfaces
{
    public interface ICompanyRepository
    {
        Task<List<Company>> GetAllCompany();
        Task<Company> GetByIdAsync(string id);
        Task<Company> GetFirstCompanyAsync();
        Task<bool> CreateCompanyAsync(Company company);
        Task<bool> UpdateCompanyAsync(Company company);
        Task<bool> DeleteCompanyAsync(string id);
    }
}
