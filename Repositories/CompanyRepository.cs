using HPParking.Data;
using HPParking.Interfaces;
using HPParking.Models.Entities;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

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
            try
            {
                return await _collection.Find(_ => true).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lấy thông tin công ty từ MongoDB: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        public async Task<bool> CreateCompanyAsync(Company company)
        {
            try
            {
                company.CreatDay = DateTime.UtcNow;

                await _collection.InsertOneAsync(company);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi thêm công ty vào MongoDB: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        public async Task<bool> UpdateCompanyAsync(Company company)
        {
            try
            {
                company.CreatDay = DateTime.UtcNow;
                var update = Builders<Company>.Update
                    .Set(x => x.Name, company.Name.Trim())
                    .Set(x => x.Lisen, company.Lisen.Trim())
                    .Set(x => x.TimeWait, company.TimeWait)
                    .Set(x => x.TimeFree, company.TimeFree)
                    .Set(x => x.PathImage, company.PathImage.Trim());

                await _collection.UpdateOneAsync(
                    x => x.Id == company.Id,
                    update
                );
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi cập nhật công ty trong MongoDB: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteCompanyAsync(string id)
        {
            try
            {
                var result = await _collection.DeleteOneAsync(c => c.Id == id);
                return result.IsAcknowledged && result.DeletedCount > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi xóa công ty trong MongoDB: {ex.Message}");
                throw;
            }
        }
    }
}
