using System.Threading;
using System.Threading.Tasks;
using HPParking.Api.DTOs.Common;
using HPParking.Api.DTOs.Departments;

namespace HPParking.Api.Services.Interfaces
{
    public interface IDepartmentService
    {
        Task<PagedResult<DepartmentDto>> GetDepartmentsPagedAsync(DepartmentFilterQuery query, CancellationToken cancellationToken = default);
        Task<DepartmentDto> GetDepartmentByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default);
        Task<DepartmentDto> UpdateDepartmentAsync(string id, UpdateDepartmentRequest request, CancellationToken cancellationToken = default);
        Task<bool> DeleteDepartmentAsync(string id, bool hardDelete = false, CancellationToken cancellationToken = default);
    }
}
