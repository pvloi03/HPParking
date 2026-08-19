using HPParking.Data;
using HPParking.Interfaces;
using HPParking.Models.Entities;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HPParking.Repositories
{
    public class CompanyRepository(MongoContext context) : ICompanyRepository
    {
        private readonly IMongoCollection<Company> _collection = context.GetCollection<Company>("Company");

        public async Task<List<Company>> GetAllCompany()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }

        public async Task<Company> GetByIdAsync(string id)
        {
            return await _collection.Find(c => c.Id == id).FirstOrDefaultAsync();
        }

        public async Task<Company> GetFirstCompanyAsync()
        {
            return await _collection.Find(_ => true).FirstOrDefaultAsync();
        }

        public async Task<bool> CreateCompanyAsync(Company company)
        {
            company.CreatDay = DateTime.UtcNow;
            await _collection.InsertOneAsync(company);
            return true;
        }

        public async Task<bool> UpdateCompanyAsync(Company company)
        {
            var update = Builders<Company>.Update
                .Set(x => x.Name, company.Name.Trim())
                .Set(x => x.Lisen, company.Lisen.Trim())
                .Set(x => x.TimeWait, company.TimeWait)
                .Set(x => x.TimeFree, company.TimeFree)
                .Set(x => x.PathImage, company.PathImage.Trim());

            var result = await _collection.UpdateOneAsync(
                x => x.Id == company.Id,
                update
            );
            return result.IsAcknowledged && result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteCompanyAsync(string id)
        {
            var result = await _collection.DeleteOneAsync(c => c.Id == id);
            return result.IsAcknowledged && result.DeletedCount > 0;
        }
    }
}
