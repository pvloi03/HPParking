using HPParking.Data;
using HPParking.Interfaces;
using HPParking.Models.Entities;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace HPParking.Repositories
{
    public class DepartmentRepository(MongoContext context) : IDepartmentRepository
    {
        private readonly IMongoCollection<Department> _collection = context.GetCollection<Department>("Department");

        public async Task<Department?> GetByDepartmentCode(string departmentCode)
        {
            return await _collection.Find(x => x.Code == departmentCode).FirstOrDefaultAsync();
        }
    }
}
