using HPParking.Data;
using HPParking.Interfaces;
using HPParking.Models.Entities;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HPParking.Repositories
{
    public class ClientRepository(MongoContext context) : IClientRepository
    {
        private readonly IMongoCollection<Client> _collection = context.GetCollection<Client>("Client");

        public async Task<List<Client>> GetAll()
        {
            return await _collection.Find(x => !x.IsDelete).ToListAsync();
        }

        public async Task<Client?> GetByCardCode(string cardCode)
        {
            return await _collection.Find(x => x.Card_Code == cardCode && !x.IsDelete).FirstOrDefaultAsync();
        }

        public async Task<Client?> GetByIdCode(string idCode)
        {
            return await _collection.Find(x => x.ID_Code == idCode && !x.IsDelete).FirstOrDefaultAsync();
        }

        public async Task Insert(Client client)
        {
            client.CreatDay = DateTime.UtcNow;
            await _collection.InsertOneAsync(client);
        }
    }
}
