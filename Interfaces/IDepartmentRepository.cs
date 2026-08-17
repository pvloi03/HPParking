#nullable enable
using HPParking.Models.Entities;
using System.Threading.Tasks;

namespace HPParking.Interfaces
{
    public interface IDepartmentRepository
    {
        Task<Department?> GetByDepartmentCode(string departmentCode);
    }
}
