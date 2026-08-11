using HPParking.Data;
using HPParking.Interfaces;
using HPParking.Models.Entities;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace HPParking.Repositories
{
    public class EventParkingRepository(MongoContext context) : IEventParkingRepository
    {
        private readonly IMongoCollection<EventParking> _collection = context.GetCollection<EventParking>("EventParking");

        public async Task<EventParking> GetParkingInProgress(string cardCode)
        {
            return await _collection.Find(x =>
                x.Card_Code == cardCode &&
                x.StatusInOut == false &&
                x.TimeOut == null &&
                !x.IsDelete)
                .FirstOrDefaultAsync();
        }

        public async Task Insert(EventParking parking)
        {
            await _collection.InsertOneAsync(parking);
        }

        public async Task Update(string id, EventParking parking)
        {
            await _collection.ReplaceOneAsync(x => x.Id == id, parking);
        }
    }
}
