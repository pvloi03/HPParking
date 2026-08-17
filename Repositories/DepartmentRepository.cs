using HPParking.Data;
using HPParking.Interfaces;
using HPParking.Models.Entities;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace HPParking.Repositories
{
    public class DepartmentRepository(MongoContext contect) : IDepartmentRepository
    {
        private readonly IMongoCollection<Department> _collection = contect.GetCollection<Department>("Client");

        public async Task<Department> GetByDepartmentCode(string departmentCode)
        {
            return await _collection.Find(x => x.Code == departmentCode).FirstOrDefaultAsync();
        }
    }
}
