using HPParking.Api.DTOs.Companies;
using HPParking.Api.DTOs.Contractors;
using HPParking.Api.DTOs.Departments;
using HPParking.Core.Models.Entities;
using Mapster;

namespace HPParking.Api.Configuration.Mappings
{
    /// <summary>
    /// Cấu hình quy tắc ánh xạ Mapster cho cụm nghiệp vụ Danh mục Tổ chức (Company, Department &amp; Contractor)
    /// </summary>
    public class OrganizationMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // 1. Ánh xạ Company -> CompanyDto
            config.NewConfig<Company, CompanyDto>();

            // 2. Ánh xạ Department -> DepartmentDto
            config.NewConfig<Department, DepartmentDto>();

            // 3. Ánh xạ Contractor -> ContractorDto
            config.NewConfig<Contractor, ContractorDto>();
        }
    }
}
