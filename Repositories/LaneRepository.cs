using HPParking.Data;
using HPParking.Interfaces;
using HPParking.Models.Entities;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HPParking.Repositories
{
    public class LaneRepository(MongoContext context) : ILaneRepository
    {
        private readonly IMongoCollection<Lane> _collection = context.GetCollection<Lane>("Lane");

        public async Task<List<Lane>> GetAllAsync() =>
            await _collection.Find(x => !x.IsDelete).ToListAsync();

        public async Task<bool> CreateLaneAsync(Lane lane)
        {
            await _collection.InsertOneAsync(lane);
            return true;
        }

        public async Task<bool> UpdateLaneAsync(string id, Lane lane)
        {
            // Xây dựng các phép cập nhật cho từng trường
            var update = Builders<Lane>.Update
                .Set(c => c.Name, lane.Name)
                .Set(c => c.Code, lane.Code)
                .Set(c => c.Type, lane.Type)
                .Set(c => c.CameraLicensePlate, lane.CameraLicensePlate)
                .Set(c => c.CameraClient, lane.CameraClient)
                .Set(c => c.Controller, lane.Controller)
                .Set(c => c.OutputRelay, lane.OutputRelay)
                .Set(c => c.InputReader, lane.InputReader)
                .Set(c => c.UpdateDay, DateTime.UtcNow);

            await _collection.UpdateOneAsync(x => x.Id == id, update);
            return true;
        }
    }
}