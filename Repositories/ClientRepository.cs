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
    public class ClientRepository(MongoContext contect) : IClientRepository
    {
        private readonly IMongoCollection<Client> _collection = contect.GetCollection<Client>("Client");

        public async Task<List<Client>> GetAll()
        {
            return await _collection.Find(x => !x.IsDelete).ToListAsync();
        }

        public async Task<Client> GetByCardCode(string cardCode)
        {
            try
            {
                return await _collection.Find(x => x.Card_Code == cardCode && !x.IsDelete).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lấy thông tin khách hàng: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }
        public async Task<Client> GetByIdCode(string idCode)
        {
            try
            {
                return await _collection.Find(x => x.ID_Code == idCode && !x.IsDelete).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lấy thông tin khách hàng: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        public async Task Insert(Client client)
        {
            try
            {
                client.CreatDay = DateTime.UtcNow;
                await _collection.InsertOneAsync(client);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi thêm khách hàng: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }
    }
}
